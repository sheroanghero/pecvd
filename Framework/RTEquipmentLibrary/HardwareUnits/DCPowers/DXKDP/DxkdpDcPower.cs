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
    public class DxkdpDcPower : RfPowerBase
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

                    CurrentSetPoint = _setCurrent,
                    VoltageSetPoint = _setVoltage,

                    ScaleCurrent = _scaleCurrent,

                    UnitCurrent = "A",
                    UnitVoltage = "V",

                    IsRfOn = IsPowerOn,
                    IsRfAlarm = IsError,

                    Frequency = Frequency,
                    PulsingFrequency = PulsingFrequency,
                    PulsingDutyCycle = PulsingDutyCycle,
                };

                return data;
            }
        }

        public DxkdpDcPowerConnection Connection
        {
            get { return _connection; }
        }

        private DxkdpDcPowerConnection _connection;

        private byte _deviceAddress;

        private float _powerSetPoint;
        private float _setVoltage;
        private float _setCurrent;
        private float _reflectPower;
        private float _scaleCurrent;
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



        private int _vmss = 2;
        private int _cmss = 3;

        private float _sysVoltage;
        private float _sysCurrent;

        private int _polarity;
        private bool _remoteMode;

        private EnumRfPowerRegulationMode _regulationMode;
        private EnumRfPowerCommunicationMode _commMode;

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

        public DxkdpDcPower(string module, string name) : base(module, name)
        {

        }

        string portName;
        public override bool Initialize()
        {
            base.Initialize();

            portName = SC.GetStringValue($"{Module}.{Name}.Address");
            //int bautRate = SC.GetValue<int>($"{ScBasePath}.{Name}.BaudRate");
            //int dataBits = SC.GetValue<int>($"{ScBasePath}.{Name}.DataBits");
            //Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.Parity"), out Parity parity);
            //Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.StopBits"), out StopBits stopBits);

            _unit = SC.GetStringValue($"{Module}.{Name}.Unit");
            _deviceAddress = (byte)SC.GetValue<int>($"{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _scaleCurrent = (float)SC.GetValue<double>($"{Module}.{Name}.ScaleCurrent");

            _queryInterval = SC.GetValue<int>($"{Module}.{Name}.QueryInterval");
            _scRetryCount = SC.GetConfigItem($"{Module}.{Name}.RetryCount");
            _scRetryAlarmRange = SC.GetConfigItem($"{Module}.{Name}.RetryAlarmRange");

            _connection = new DxkdpDcPowerConnection(portName);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);
     
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(_queryInterval, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            DATA.Subscribe($"{Module}.{Name}.Current", () => Current);
            DATA.Subscribe($"{Module}.{Name}.Voltage", () => Voltage);


            OP.Subscribe($"{Module}.{Name}.SetVoltage", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!SetVoltage(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set Voltage, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set Voltage to {args[0]}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetCurrent", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                SetCurrent(value);

                EV.PostInfoLog(Module, $"{Module}.{Name} set Current to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetCurrentForRecipe", (out string reason, int time, object[] args) =>
            {
                float value = Convert.ToSingle(args[0]);
                bool isOn = value > 0;
                reason = string.Empty;

                SetCurrent(value);

                if (isOn ^ IsPowerOn)
                {
                    SetPowerOnOff(isOn, out _);
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set Current to {value}");
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
                    _lstHandler.AddLast(new DxkdpRfPowerSetModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
                        SetCurrent(0);
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
                            if (!_remoteMode)
                            {
                                _lstHandler.AddLast(new DxkdpRfPowerSetModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                            }

                            if (_setOn || _isOn)
                            {
                                //_lstHandler.AddLast(new DxkdpRfPowerQuerySysInformationHandler(this, _deviceAddress));
                                _lstHandler.AddLast(new DxkdpRfPowerQueryStateHandler(this, _deviceAddress));
                                _lstHandler.AddLast(new DxkdpRfPowerQueryPracticalHandler(this, _deviceAddress));                   
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

                    if (_scRetryCount.IntValue > 0 && _RetryCount <= _scRetryCount.IntValue && !_RetryOK)
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

                    if (_current >= _setCurrent * (1 - _scRetryAlarmRange.DoubleValue / 100f) && _current <= _setCurrent * (1 + _scRetryAlarmRange.DoubleValue / 100f))
                    {
                        _RetryCount = 0;
                        _RetryOK = true;

                    }
                    else
                    {
                        if (_RetryCount > _scRetryCount.IntValue)
                        {
                            _RetryCount = 0;
                            EV.PostAlarmLog(Module, $"{Module}.{Name} Retry: {_scRetryCount.IntValue} Failed, SetCurrent: {_setCurrent}A, Current: {_current}A");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }
                        else
                        {
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry: {_RetryCount} , SetCurrent: {_setCurrent}A, Current: {_current}A");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }


                    }


                    _RetryDelayChecktimer.Stop();
                }

                _trigRetryFailedDelay.CLK = _RetryFailedDelaytimer.IsTimeout();
                if (_trigRetryFailedDelay.Q)
                {
                    SetCurrent(_setCurrent);
                    SetPowerOnOff(true, out _);
                    EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power On");


                }

                #endregion

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

                if (_scRetryCount.IntValue > 0)
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

        public void SetCurrent(float current)
        {
            _setCurrent = current;

            lock (_locker)
            {
                UInt16 setCurrent = Convert.ToUInt16(current * Math.Pow(10, _cmss));

                if (current > 0)
                    _lstHandler.AddLast(new DxkdpRfPowerSetElectricityHandler(this, _deviceAddress, setCurrent));
                else
                    _lstHandler.AddFirst(new DxkdpRfPowerSetElectricityHandler(this, _deviceAddress, setCurrent));
            }
        }

        public bool SetVoltage(out string reason, int time, float voltage)
        {
            reason = string.Empty;
            lock (_locker)
            {
                UInt16 setVoltage = Convert.ToUInt16(voltage * Math.Pow(10, _vmss));
                _lstHandler.AddLast(new DxkdpRfPowerSetVoltageHandler(this, _deviceAddress, setVoltage));
            }

            return true;
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
                    _lstHandler.AddLast(new DxkdpRfPowerSwitchOnOffHandler(this, _deviceAddress, isOn));
                else
                    _lstHandler.AddFirst(new DxkdpRfPowerSwitchOnOffHandler(this, _deviceAddress, isOn));
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

        internal void NoteStatus(byte data)
        {
            _isOn = data == 0x01;
        }

        internal void NoteMinVStepSize(int vmss)
        {
            _vmss = vmss;
        }
        internal void NoteMinCStepSize(int cmss)
        {
            _cmss = cmss;
        }
        internal void NoteSysCurrent(int sysCurrent)
        {
            _sysCurrent = (float)sysCurrent /(float)Math.Pow(10, _cmss);
        }
        internal void NoteSysVoltage(int sysVoltage)
        {
            _sysVoltage = (float)sysVoltage / (float)Math.Pow(10, _vmss);
        }

        internal void NoteVoltage(int voltage)
        {
            _voltage = (float)voltage / (float)Math.Pow(10, _vmss);
        }

        internal void NoteCurrent(int current)
        {
            _current = (float)current / (float)Math.Pow(10, _cmss);
        }

        internal void NoteMode(bool mode)
        {
            _remoteMode = mode;
        }
    }
}
