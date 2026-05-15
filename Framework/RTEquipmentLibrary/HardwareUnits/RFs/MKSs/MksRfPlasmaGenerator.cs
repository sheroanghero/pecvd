using System;
using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Mkss
{
    public class MksRfPlasmaGenerator : RfPowerBase
    {

        public override bool IsConnected => _connection != null ? _connection.IsConnected : false;
        public override bool IsError { get; set; }

        public override bool IsPowerOn
        {
            get { return _isOn; }
        }

        public override bool IsSetPowerOn
        {
            get { return _setOn; }
        }

        public override float ForwardPower
        {
            get { return _forwardPower; }
        }

        public override float ReflectPower
        {
            get { return _reflectPower; }
        }

        public override float PowerSetPoint
        {
            get { return _powerSetPoint; }
        }

        public override float Frequency { get; set; }
        public override float PulsingFrequency { get; set; }
        public override float PulsingDutyCycle { get; set; }

        public string ControlSource { get; set; }
        public string RegulationSource { get; set; }
        public string SetpointSource { get; set; }

        public override AITRfPowerData DeviceData
        {
            get
            {
                AITRfPowerData data = new AITRfPowerData()
                {
                    DeviceName = Name,
                    Module = Module,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    ForwardPower = ForwardPower,
                    ReflectPower = ReflectPower,
                    PowerSetPoint = PowerSetPoint,
                    RegulationMode = RegulationMode,
                    ScalePower = _scalePower,

                    IsRfOn = IsPowerOn,
                    IsRfAlarm = IsError,

                    Frequency = Frequency,
                    PulsingFrequency = PulsingFrequency,
                    PulsingDutyCycle = PulsingDutyCycle,

                    UnitPower = "W",
                };

                return data;
            }
        }

        public MksRfPlasmaGeneratorConnection Connection
        {
            get { return _connection; }
        }

        private MksRfPlasmaGeneratorConnection _connection;

        private byte _deviceAddress;

        private float _powerSetPoint;
        private float _reflectPower;
        private bool _isOn;
        private float _forwardPower;
        private float _voltage;
        private float _frequency;
        private float _scalePower;

        private bool _setOn;

        private int _queryInterval;

        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();

        private RD_TRIG _trigRfOnOff = new RD_TRIG();


        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigConnect = new R_TRIG();

        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
        private bool _isHaloInstalled;

        public MksRfPlasmaGenerator(string module, string name, string scRoot) : base(module, name)
        {
 
        }

        internal void NotePowerOnOff(int onOff)
        {
            _isOn = onOff == 1 ? true : false;
        }

        internal void NotInterface(int v)
        {
            //throw new NotImplementedException();
        }

        internal void NotePulseLowTime(string v)
        {
            //throw new NotImplementedException();
        }

        internal void NotePulseHighTime(string v)
        {
            //throw new NotImplementedException();
        }

        internal void NoteControllerOutput(int v)
        {
            //throw new NotImplementedException();
        }

        internal void NoteDeliveredPower(int v)
        {
            //throw new NotImplementedException();
        }

        internal void NoteReversePower(int v)
        {
            _reflectPower = v;
            //throw new NotImplementedException();
        }

        internal void NoteStatus(string v)
        {
            //EnableAlarm
            //throw new NotImplementedException();
        }

        internal void NoteFault(string v)
        {
            //IsError = false;
            for (int i=0;i<v.Length;i++)
            {
                if (v.Substring(i, 1) == "1")
                {
                    IsError = true;
                    break;
                }
            }
            
        }

        public override bool Initialize()
        {
            base.Initialize();

            string portName = SC.GetStringValue($"{Module}.{Name}.Address");
            //int bautRate = SC.GetValue<int>($"{_scRoot}.BaudRate");
            //int dataBits = SC.GetValue<int>($"{_scRoot}.DataBits");
            //Enum.TryParse(SC.GetStringValue($"{_scRoot}.Parity"), out Parity parity);
            //Enum.TryParse(SC.GetStringValue($"{_scRoot}.StopBits"), out StopBits stopBits);

            //_deviceAddress = (byte)SC.GetValue<int>($"{_scRoot}.Address");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");

            _queryInterval = SC.GetValue<int>($"{Module}.{Name}.QueryInterval");

            _connection = new MksRfPlasmaGeneratorConnection(portName);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(_queryInterval, OnTimer, $"{Module}.{Name} MonitorHandler", true);


            OP.Subscribe($"{Module}.{Name}.SetPowerForRecipe", (function, args) =>
            {
                float value = Convert.ToSingle(args[0]);
                SetPower(value);
                bool isOn = value > 0;

                if (isOn ^ IsPowerOn)
                {
                    SetPowerOnOff(isOn, out _);
                }
                return true;
            });

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                _trigConnect.CLK = _connection.IsConnected;
                if (_trigConnect.Q)
                {
                    _lstHandler.AddLast(new MksRfPlasmaGeneratorEnableHandler(this, true));
                }

                if (!_connection.IsConnected)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{Module}.{Name}.Address"));
                        if (_connection.Connect() && _connection.IsConnected)
                        {
                            _connection.ForceClear();

                            EV.PostInfoLog(Module, $"Reconnect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }
                    return true;
                }


                // 达到一定条件，强制关闭电源
                if (FuncForceAction != null)
                {
                    _trigForceAction.CLK = FuncForceAction(IsPowerOn);
                    if (_trigForceAction.Q)
                    {
                        SetPower(0);
                        SetPowerOnOff(false, out string reason);
                        //EV.PostAlarmLog(Module, $"Force set {Name} off for interlock");
                    }
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            if (_setOn || _isOn)
                            {
                                _lstHandler.AddLast(new MksRfPlasmaGeneratorQueryStatusHandler(this));
                            }
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public override void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

                _trigRfOnOff.CLK = _isOn;
                if (_trigRfOnOff.R)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is on");
                }

                if (_trigRfOnOff.T)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is off");
                }

                //_trigError.CLK = IsError;
                //if (_trigError.Q)
                //{
                //    EV.PostAlarmLog(Module, $"{Module}.{Name} is error, error code {_errorCode:D3}");
                //}

                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public override void Reset()
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            _lstHandler.AddLast(new MksRfPlasmaGeneratorEnableHandler(this, true));

            base.Reset();
        }

        public override void SetPower(float power)
        {
            lock (_locker)
            {
                if (power > 0)
                {
                    _lstHandler.AddLast(new MksRfPlasmaGeneratorReadySetPowerHandler(this));
                    _lstHandler.AddLast(new MksRfPlasmaGeneratorSetPowerHandler(this, Convert.ToInt32(power)));
                }
                else
                {
                    _lstHandler.AddFirst(new MksRfPlasmaGeneratorSetPowerHandler(this, Convert.ToInt32(power)));
                    _lstHandler.AddFirst(new MksRfPlasmaGeneratorReadySetPowerHandler(this));
                }
            }
        }

        public override bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = "";

            if (FuncCheckInterLock != null)
            {
                if (!FuncCheckInterLock(isOn))
                {
                    return false;
                }
            }

            _setOn = isOn;

            lock (_locker)
            {
                if (isOn)
                    _lstHandler.AddLast(new MksRfPlasmaGeneratorSwitchOnOffHandler(this, isOn));
                else
                    _lstHandler.AddFirst(new MksRfPlasmaGeneratorSwitchOnOffHandler(this, isOn));
            }

            return true;
        }

        internal void NoteError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
        }

        internal void NotePowerSetPoint(int power)
        {
            _powerSetPoint = power;
        }

        internal void NoteForwardPower(int power)
        {
            _forwardPower = power;
        }
    }
}
