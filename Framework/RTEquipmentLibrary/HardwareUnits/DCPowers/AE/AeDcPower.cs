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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.DCPowers.AE
{
    public class AeDcPower : RfPowerBase
    {
        public override bool IsConnected => _connection != null ? _connection.IsConnected : false;
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
            get { return _forwardPower; }
        }

        public override float ReflectPower
        {
            get { return _reflectPower; }
        }

        public override  float PowerSetPoint
        {
            get { return _powerSetPoint; }
        }

        public float Voltage
        {
            get { return _voltage; }
        }

        public float Current
        {
            get { return _current; }
        }

        public override float Frequency { get; set; }
        public override float PulsingFrequency { get; set; }
        public override float PulsingDutyCycle { get; set; }
        public override float PulsingReverseTime { get; set; }

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
                    Voltage = Voltage,
                    Current = Current,

                    IsRfOn = IsPowerOn,
                    IsRfAlarm = IsError,

                    Frequency = Frequency,
                    PulsingFrequency = PulsingFrequency,
                    PulsingDutyCycle = PulsingDutyCycle,
                    PulsingReverseTime = PulsingReverseTime,

                    ScalePower = _scalePower,
                    IsPulseType = _scPowerType.StringValue == "DCPulse",
                };

                return data;
            }
        }

        public AeDcPowerConnection Connection
        {
            get { return _connection; }
        }

        private AeDcPowerConnection _connection;

        private byte _deviceAddress;

        private float _powerSetPoint;
        private float _reflectPower;
        private float _scalePower;
        private float _factor;
        private bool _isOn;
        private bool _isError;
        private string _errorCode;
        private float _forwardPower;
        private string _unit;
        private float _voltage;
        private float _current;
        private int _queryInterval;

        private bool _setOn;

        public bool _RetryOK;
        private int _RetryCount;
        public SCConfigItem _scRetryCount;
        public SCConfigItem _scRetryAlarmRange;
        private DeviceTimer _RetryDelayChecktimer = new DeviceTimer();
        private DeviceTimer _RetryFailedDelaytimer = new DeviceTimer();
        private R_TRIG _trigRetryDelayCheck = new R_TRIG();
        private R_TRIG _trigRetryFailedDelay = new R_TRIG();

        private SCConfigItem _scPowerType;
        private SCConfigItem _scPulseType;
        private SCConfigItem _scPulseReverseTime;

        private EnumRfPowerRegulationMode _regulationMode;
        private EnumRfPowerCommunicationMode _commMode;
        private EnumDcPowerPulseType _pulseType;

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

        public AeDcPower(string module, string name) : base(module, name)
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
            _deviceAddress = (byte)SC.GetValue<int>($"{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");

            _factor = SC.GetValue<int>($"{Module}.{Name}.Factor");

            _queryInterval = SC.GetValue<int>($"{Module}.{Name}.QueryInterval");

            _scRetryCount = SC.GetConfigItem($"{Module}.{Name}.RetryCount");
            _scRetryAlarmRange = SC.GetConfigItem($"{Module}.{Name}.RetryAlarmRange");

            _scPowerType = SC.GetConfigItem($"System.SetUp.{Module}.PowerType");
            _scPulseType = SC.GetConfigItem($"System.SetUp.{Module}.PulseType");
            _scPulseReverseTime = SC.GetConfigItem($"System.SetUp.{Module}.PulseReverseTime");

            _connection = new AeDcPowerConnection(portName);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(_queryInterval, OnTimer, $"{Module}.{Name} MonitorHandler", true);


            DATA.Subscribe($"{Module}.{Name}.Current", () => Current);
            DATA.Subscribe($"{Module}.{Name}.Voltage", () => Voltage);


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

            OP.Subscribe($"{Module}.{Name}.SetPulseType", (function, args) =>
            {
                if (!Enum.TryParse((string)args[0], out EnumDcPowerPulseType type))
                {
                    EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
                    return false;
                }
                SetPulseType(type);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPulseFrequency", (function, args) =>
            {
                int value = Convert.ToInt32(args[0]);
                SetPulseFrequency(value);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPulseReverseTime", (function, args) =>
            {
                float value = Convert.ToSingle(args[0]);
                SetPulseReverseTime(value);
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
                    _lstHandler.AddLast(new AeRfPowerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                    _lstHandler.AddLast(new AeRfPowerSetRegulationModeHandler(this, _deviceAddress, EnumRfPowerRegulationMode.Power));
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
                            if (_commMode != EnumRfPowerCommunicationMode.Host)
                            {
                                _lstHandler.AddLast(new AeRfPowerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                                _lstHandler.AddLast(new AeRfPowerQueryCommModeHandler(this, _deviceAddress));
                            }

                            if (_regulationMode != EnumRfPowerRegulationMode.Power)
                            {
                                _lstHandler.AddLast(new AeRfPowerSetRegulationModeHandler(this, _deviceAddress, EnumRfPowerRegulationMode.Power));
                                _lstHandler.AddLast(new AeRfPowerQueryRegulationModeHandler(this, _deviceAddress));
                            }

                            if (_setOn || _isOn)
                            {
                                _lstHandler.AddLast(new AeRfPowerQueryStatusHandler(this, _deviceAddress));
                                _lstHandler.AddLast(new AeRfPowerQueryPowerVoltageCurrentHandler(this, _deviceAddress));
                                
                                //_lstHandler.AddLast(new AeRfPowerQuerySetPointHandler(this, _deviceAddress));
                                //_lstHandler.AddLast(new AeRfPowerQueryFaultorWarningHandler(this, _deviceAddress));

                               
                            }

                            if (SC.GetStringValue($"System.SetUp.{Module}.PowerType") == "DCPulse")
                            {
                                _lstHandler.AddLast(new AeRfPowerQueryPulseFrequencyHandler(this, _deviceAddress));
                                _lstHandler.AddLast(new AeRfPowerQueryPulseReverseTimeHandler(this, _deviceAddress));
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

                    if (_scRetryCount.IntValue > 0 && _RetryCount<= _scRetryCount.IntValue && !_RetryOK)
                    {
                        _RetryDelayChecktimer.Start(2000);
                        
                    }
                    else
                    {
                        _RetryCount = 0;
                    }
                }

                if (_trigRfOnOff.T)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is off");
                    _RetryOK = false;
                    if (_RetryCount > 0)
                    {
                        _RetryFailedDelaytimer.Start(500);
                    }
                   
                }

                #region Retry

                _trigRetryDelayCheck.CLK = _RetryDelayChecktimer.IsTimeout();
                if (_trigRetryDelayCheck.Q)
                {
                    _RetryCount++;

                    if (ForwardPower >= PowerSetPoint * (1- _scRetryAlarmRange.DoubleValue/100f) && ForwardPower <= PowerSetPoint * (1 + _scRetryAlarmRange.DoubleValue / 100f))
                    {
                        _RetryCount = 0;
                        _RetryOK = true;
                       
                    }
                    else
                    {
                        if (_RetryCount > _scRetryCount.IntValue)
                        {
                            _RetryCount = 0;
                            EV.PostAlarmLog(Module, $"{Module}.{Name} Retry: {_scRetryCount.IntValue} Failed, PowerSetPoint: {PowerSetPoint}W, ForwardPower: {ForwardPower}W");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }
                        else
                        {
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry: {_RetryCount} , PowerSetPoint: {PowerSetPoint}W, ForwardPower: {ForwardPower}W");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }

                        
                    }


                    _RetryDelayChecktimer.Stop();
                }

                _trigRetryFailedDelay.CLK = _RetryFailedDelaytimer.IsTimeout();
                if (_trigRetryFailedDelay.Q)
                {
                    SetPower(_powerSetPoint);
                    SetPowerOnOff(true, out _);
                    EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power On");


                }

                #endregion







                _trigError.CLK = IsError;
                if (_trigError.Q)
                {
                    _RetryCount = 0;
                    EV.PostAlarmLog(Module, $"{Module}.{Name} is error, error code {_errorCode:D3}");
                }

                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                //MonitorRampingPower();

                if (_scRetryCount.IntValue>0)
                {
                    if (_RetryOK)
                    {
                        base.Monitor();
                    }
                }
                else
                {
                    base.Monitor();
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

            _trigForceAction.RST = true;

            base.Reset();
        }

        public override void SetPower(float power)
        {
            _powerSetPoint = power;

            lock (_locker)
            {
                if (power > 0)
                    _lstHandler.AddLast(new AeRfPowerSetPowerHandler(this, _deviceAddress, _isHaloInstalled ? (int)(power * _factor) : (int)(power / _factor)));
                else
                    _lstHandler.AddFirst(new AeRfPowerSetPowerHandler(this, _deviceAddress, _isHaloInstalled ? (int)(power * _factor) : (int)(power / _factor)));
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
                    _lstHandler.AddLast(new AeRfPowerSwitchOnOffHandler(this, _deviceAddress, isOn));
                else
                    _lstHandler.AddFirst(new AeRfPowerSwitchOnOffHandler(this, _deviceAddress, isOn));
            }

            return true;
        }

        public override void SetRegulationMode(EnumRfPowerRegulationMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfPowerSetRegulationModeHandler(this, _deviceAddress, mode));
            }
        }

        public void SetPulseType(EnumDcPowerPulseType type)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfPowerSetPulseTypeHandler(this, _deviceAddress, type));
            }
        }

        public void SetPulseFrequency(float frequency)
        {
            //PulsingFrequency = frequency;

            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfPowerSetPulseFrequencyHandler(this, _deviceAddress, (byte)(frequency / 5)));
            }
        }

        public void SetPulseReverseTime(float time)
        {
            //PulsingReverseTime = time;

            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfPowerSetPulseReverseTimeHandler(this, _deviceAddress, (byte)(time * 10)));
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
            _reflectPower = reflect * 10;
        }

        internal void NoteCommMode(EnumRfPowerCommunicationMode mode)
        {
            _commMode = mode;
        }

        internal void NoteStatus(byte[] data)
        {
            _isOn = (data[0] & 0x08) == 0x08;
        }

        internal void NoteRegulationModeSetPoint(EnumRfPowerRegulationMode mode)
        {
            _regulationMode = mode;
        }

        internal void NotePowerSetPoint(int power)
        {
            _powerSetPoint = power * _factor;
        }

        internal void NoteForwardPower(int power)
        {
            _forwardPower = power * _factor;
        }

        internal void NoteVoltage(int voltage)
        {
            _voltage = voltage;
        }

        internal void NoteCurrent(int current)
        {
            _current = current / 100.0f;
        }

        internal void NoteHaloInstalled(bool isInstalled)
        {
            _isHaloInstalled = isInstalled;
        }

        internal void NoteErrorStatus(bool isError, string errorCode)
        {
            _isError = isError;
            _errorCode = errorCode;
        }

        internal void NotePulseFrequency(int frequency)
        {
            PulsingFrequency = frequency * 5;
        }

        internal void NotePulseReverseTime(int time)
        {
            PulsingReverseTime = time / 10.0f;
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

    public enum EnumDcPowerPulseType
    {
        Current = 0,
        Always = 1,
        Voltage = 2,
    }
}
