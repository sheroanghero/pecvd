using System;
using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Serens
{
    public class SerenRfPower : RfPowerBase
    {
        public  override  bool IsError { get; set; }

        public override bool IsPowerOn
        {
            get { return _isOn; }
        }

        public override EnumRfPowerRegulationMode RegulationMode
        {
            get { return _regulationMode; }
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
                    ScalePower = PowerRange,

                    IsRfOn = IsPowerOn,
                    IsRfAlarm = IsError,
                    IsInterlockOk = !_isInterlockNotOK,

                    Frequency = Frequency,
                    PulsingFrequency = PulsingFrequency,
                    PulsingDutyCycle = PulsingDutyCycle,
                };

                return data;
            }
        }

        public SerenRfPowerConnection Connection
        {
            get { return _connection; }
        }

        private SerenRfPowerConnection _connection;

        private byte _deviceAddress ;
 
        private float _powerSetPoint;
        private float _reflectPower;
        private bool _isOn;
        private float _forwardPower;
        private EnumRfPowerRegulationMode _regulationMode;
        private EnumRfPowerCommunicationMode _commMode;
        private bool _isInterlockNotOK;

        private RD_TRIG _trigRfOnOff = new RD_TRIG();


        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigWarningMessage = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
        private bool _isHaloInstalled;

        private string _scRoot;

        public SerenRfPower(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

        }

        
        public bool Initialize(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            base.Initialize();
            _enableLog = true;
            _connection = new SerenRfPowerConnection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        public override bool Initialize()
        {
            base.Initialize();
 
            string portName = SC.GetStringValue($"{_scRoot}.Address");
            int bautRate = SC.GetValue<int>($"{_scRoot}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.StopBits"), out StopBits stopBits);

            _deviceAddress = (byte)SC.GetValue<int>($"{_scRoot}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");
            _scPowerScale = SC.GetConfigItem($"{ScBasePath}.{Name}.PowerScale");

            _connection = new SerenRfPowerConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);



            OP.Subscribe($"{Module}.{Name}.WaveModulation", (out string reason, int time, object[] args) =>
            {
                reason = "";

                //if (!Enum.TryParse((string)args[0], out EnumRpsControlMode mode))
                //{
                //    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not mode, {args[0]} is not a valid mode value");
                //    return false;
                //}

                if (!PerformWaveModulation(out reason, time, ""))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set mode to {args[0]}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.PowerMode", (out string reason, int time, object[] args) =>
            {
                reason = "";

                string mode = (string)args[0];

                if (!PerformPowerMode(out reason, time, mode))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set mode to {mode}");
                return true;
            });


            OP.Subscribe($"{Module}.{Name}.SetHighPower", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetHighPower(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set high power to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetLowPower", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetLowPower(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set low power to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.PulseFreq", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformPulseFreq(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set pulse frequency to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.PulseDutyCycle", (out string reason, int time, object[] args) =>
            {
                reason = "";

                float value = Convert.ToSingle((string)args[0]);

                if (!PerformPulseDutyCycle(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set pulse duty cycle to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RampMode", (out string reason, int time, object[] args) =>
            {
                reason = "";

                if (!PerformRampMode(out reason, time, ""))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set ramp mode to ");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RampUp", (out string reason, int time, object[] args) =>
            {
                reason = "";


                if (!PerformRampUp(out reason, time, ""))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set ramp up");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.RampDown", (out string reason, int time, object[] args) =>
            {
                reason = "";

                if (!PerformRampDown(out reason, time, ""))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set ramp down");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.WarningPower", (out string reason, int time, object[] args) =>
            {
                reason = "";

                float value = Convert.ToSingle((string)args[0]);

                if (!PerformWarningPower(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set warning power to {value}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.AlarmPower", (out string reason, int time, object[] args) =>
            {
                reason = "";

                float value = Convert.ToSingle((string)args[0]);

                if (!PerformAlarmPower(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set alarm power to {value}");
                return true;
            });
            return true;
        }

        private bool PerformWaveModulation(out string reason, int time, string mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformAlarmPower(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformWarningPower(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformPowerMode(out string reason, int time, string mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformSetHighPower(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformSetLowPower(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformPulseFreq(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformPulseDutyCycle(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformRampMode(out string reason, int time, string v)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformRampUp(out string reason, int time, string mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformRampDown(out string reason, int time, string mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
 
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
 
                            _lstHandler.AddLast(new SerenRfPowerQueryStatusHandler(this));
                             
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
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
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

            _enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public override void SetPower(float power)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerSetPowerHandler(this,  (int)power));
            }
        }

        public bool SetEchoOnOff(bool isOn, out string reason)
        {
            reason = "";
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerSetEchoHandler(this, isOn));
            }

            return true;
        }

        public override bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = "";
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerSwitchOnOffHandler(this,   isOn));
            }

            return true;
        }

        public override void SetRegulationMode(EnumRfPowerRegulationMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerSetRegulationModeHandler(this, _deviceAddress, mode));
            }
        }

        public void QueryStatus()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerQueryStatusHandler(this));
            }
        }


        public void SetSerialControl()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfPowerSetSerialModeHandler(this));
            }
        }

        internal void NoteError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
        }
 
        internal void NoteReflectPower(float reflect)
        {
            _reflectPower = reflect;
        }

        internal void NoteCommMode(EnumRfPowerCommunicationMode mode)
        {
            _commMode = mode;
        }

        internal void NoteStatus(byte[] data)
        {
            _isOn = (data[0] & 0x40) == 0x40;
        }
        internal void NoteStatus(bool isOn, bool isAlarm, bool isInterlockNotOK)
        {
            _isOn = isOn;
            _isInterlockNotOK = isInterlockNotOK;
            IsError = isAlarm;
        }

        internal void NoteRegulationModeSetPoint(EnumRfPowerRegulationMode regMode)
        {
            _regulationMode = regMode;
        }

        internal void NotePowerSetPoint(int power)
        {
            _powerSetPoint = power;
        }

        internal void NoteForwardPower(int power)
        {
            _forwardPower = power;
        }

        internal void NoteHaloInstalled(bool isInstalled)
        {
            _isHaloInstalled = isInstalled;
        }
    }

    public enum EnumRfPowerCommunicationMode
    {
        Undefined,
        Host = 2,
        UserPort = 4, //Analog
        Diagnostic = 8,
        DeviceNet = 16,
        EtherCat32 = 32,
    }


}
