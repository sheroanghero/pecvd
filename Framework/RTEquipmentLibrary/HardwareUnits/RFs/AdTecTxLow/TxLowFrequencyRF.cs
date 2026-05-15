using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxLow
{
    public class LowFrequencyRF : RfPowerBase
    {
        public enum EnumLowRfPowerCommunicationMode
        {
            Manual = 1,
            RS232C = 2,
        }

        public override bool IsConnected => _connection!=null? _connection.IsConnected : false;

        public override bool IsPowerOn
        {
            get { return _isOn; }
        }

        public override bool IsSetPowerOn
        {
            get { return _setOn; }
        }

        public override bool IsError
        {
            get { return _isError; }
        }

        public override EnumRfPowerRegulationMode RegulationMode
        {
            get { return _regulationMode; }
        }

        public override float ForwardPower
        {
            get { return _forwardPower/_factor; }
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

        private float _factor;

        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();

        public override AITRfPowerData DeviceData
        {
            get
            {
                AITRfPowerData data = new AITRfPowerData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    UnitPower = _unit,

                    ForwardPower = ForwardPower,
                    ReflectPower = ReflectPower,
                    PowerSetPoint = PowerSetPoint,
                    RegulationMode = RegulationMode,

                    IsRfOn = IsPowerOn,
                    IsRfAlarm = IsError,

                    Frequency = Frequency,
                    PulsingFrequency = PulsingFrequency,
                    PulsingDutyCycle = PulsingDutyCycle,

                    ScalePower = _scalePower
                };

                return data;
            }
        }

        public LowFrequencyRFConnection Connection
        {
            get { return _connection; }
        }

        private LowFrequencyRFConnection _connection;

        //private byte _deviceAddress;

        private float _powerSetPoint;
        private float _reflectPower;
        private float _scalePower;
        private bool _isOn;
        private bool _isError;
        private string _errorCode;
        private float _forwardPower;
        private string _unit;
        private bool _setOn;

        private EnumRfPowerRegulationMode _regulationMode;
        private EnumLowRfPowerCommunicationMode _commMode;

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

        public LowFrequencyRF(string module, string name) : base(module, name)
        {

        }

        public override bool Initialize()
        {
            base.Initialize();

            string portName = SC.GetStringValue($"{Module}.{Name}.Address");
            //int bautRate = SC.GetValue<int>($"{ScBasePath}.{Name}.BaudRate");
            //int dataBits = SC.GetValue<int>($"{ScBasePath}.{Name}.DataBits");
            //Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.Parity"), out Parity parity);
            //Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.StopBits"), out StopBits stopBits);

            _unit = SC.GetStringValue($"{Module}.{Name}.Unit");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _factor = (float)SC.GetValue<double>($"{Module}.{Name}.Factor");

            _connection = new LowFrequencyRFConnection(portName);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(150, OnTimer, $"{Module}.{Name} MonitorHandler", true);


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

        protected override void SetRampPower(float power)
        {
            //_PowerSetPoint = power;
            //lock (_locker)
            //{
            //    _lstHandler.AddLast(new TruPlasmaRF1001PreSetPiValueHandler(this, (int)PowerSetPoint.HalfAdjust()));
            //}
        }
       
        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                _trigConnect.CLK = _connection.IsConnected;
                if (_trigConnect.Q)
                {
                    _lstHandler.AddLast(new LowFrequencyRFSetControlModeHandler(this, (int)EnumLowRfPowerCommunicationMode.RS232C));
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
                            if (_commMode != EnumLowRfPowerCommunicationMode.RS232C)
                            {
                                _lstHandler.AddLast(new LowFrequencyRFSetControlModeHandler(this, (int)EnumLowRfPowerCommunicationMode.RS232C));
                            }

                            //if (_setOn || _isOn)
                            {
                                _lstHandler.AddLast(new LowFrequencyRFGetControlModeHandler(this));
                                //_lstHandler.AddLast(new LowFrequencyRFGetSetPointHandler(this));
                                _lstHandler.AddLast(new LowFrequencyRFGetSwitchOnOffHandler(this));
                                _lstHandler.AddLast(new LowFrequencyGetForwardAndRefectedfHandler(this));

                                //_lstHandler.AddLast(new LowFrequencyRFWaringStatusHandler(this));
                                _lstHandler.AddLast(new LowFrequencyRFGetAlarmStatusHandler(this));
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

                _trigError.CLK = IsError;
                if (_trigError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} is error, error code {_errorCode:D3}");
                }

                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                //MonitorRampingPower();

                base.Monitor();
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

            base.Reset();
        }

        public override void SetPower(float power)
        {
            _powerSetPoint = power;

            lock (_locker)
            {
                //if (power > 0)
                _lstHandler.AddLast(new LowFrequencyRFSetPointHandler(this, _isHaloInstalled ? (int)(power * 10 * _factor) : (int)(power * _factor)));
                //else
                //    _lstHandler.AddFirst(new LowFrequencyRFSetPointHandler(this, _isHaloInstalled ? (int)(power * 10) : (int)power));
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
                //if (isOn)
                _lstHandler.AddLast(new LowFrequencyRFSwitchOnOffHandler(this, isOn));
                //else
                //    _lstHandler.AddFirst(new LowFrequencyRFSwitchOnOffHandler(this, isOn));
            }

            return true;
        }

        internal void NoteCommMode(EnumLowRfPowerCommunicationMode mode)
        { 
            _commMode = mode;
        }

        internal void NoteStatus(bool isOn)
        {
            _isOn = isOn;
        }

        internal void NoteErrorStatus(bool isError, string errorCode)
        {
            _isError = isError;
            _errorCode = errorCode;
        }

        internal void NotePowerSetPoint(int power)
        {
            _powerSetPoint = power/_factor;
        }

        internal void NoteForwardPowerAndReflectPower(byte[] data)
        {
            _forwardPower = GetData(data[6], data[7], data[8], data[9]);
            _reflectPower = GetData(data[10], data[11], data[12], data[13]);
        }

        private static int GetData(byte b1, byte b2, byte b3, byte b4)
        {
            byte[] byteData = new byte[4] { b1, b2, b3, b4 };
            string str = Encoding.ASCII.GetString(byteData);

            int value = Convert.ToInt32(str, 16);
            return value;
        }

        internal void NoteError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
        }
    }
}