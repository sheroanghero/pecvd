using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{
    public class TruPlasmaRF1000  : RfPowerBase, IConnection
    {
        public string Address => Connection.Address;
        public override bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public string PortStatus { get; set; } = "Closed";

        private TruPlasmaRF1000Connection _connection;
        public TruPlasmaRF1000Connection Connection
        {
            get { return _connection; }
        }

        public override AITRfPowerData DeviceData
        {
            get
            {
                var data = new AITRfPowerData()
                {
                    DeviceName = Name,
                    Module = Module,
                    DisplayName = Name,
                    ScalePower = _scPowerScale.IntValue,
                    ForwardPower = _originalPowerSetPoint > 0 ? CalibrationData(ForwardPower, false) : 0,
                    ReflectPower = _originalPowerSetPoint > 0 ? ReflectPower : 0,
                    PowerSetPoint = _originalPowerSetPoint,
                    IsInterlockOk = true,
                    IsRfOn = IsPowerOn,
                    FrequencySetPoint = _frequencySetPoint,
                    Frequency = Frequency,
                    ClockMode = ClockMode,
                    ClockModeSetpoint = _clockModeSetpoint,
                    IsRfAlarm = IsError,
                };
                return data;
            }
        }

        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        private bool _isAlarm;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        private float _frequencySetPoint;
        private EnumRfPowerClockMode _clockModeSetpoint;

        private object _locker = new object();
        private int _workFrequency = 60000;//60MHz

        private bool _enableLog;
        private string _scRoot;
        private string _ipAddress;

        public TruPlasmaRF1000(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }
        private void ResetPropertiesAndResponses()
        {

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        public override bool Initialize()
        {
            ResetPropertiesAndResponses();
            ScBasePath = $"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}";

            _ipAddress = SC.GetStringValue($"{ScBasePath}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");
            _scPowerScale = SC.GetConfigItem($"{ScBasePath}.{Name}.PowerScale");
            _connection = new TruPlasmaRF1000Connection(_ipAddress);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                InitHandler();
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _lstMonitorHandler.AddLast(new TruPlasmaRF1000ReadPiValueHandler(this));
            _lstMonitorHandler.AddLast(new TruPlasmaRF1000ReadPrValueHandler(this));
            _lstMonitorHandler.AddLast(new TruPlasmaRF1000ReadPowerOnOffHandler(this));
            _lstMonitorHandler.AddLast(new TruPlasmaRF1000ReadClockModeHandler(this));
            _lstMonitorHandler.AddLast(new TruPlasmaRF1000ReadFreqHandler(this));

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            base.Initialize();
            return true;
        }

        public override void InitSc()
        {
            _scEnableAlarm = SC.GetConfigItem($"{ScBasePath}.{Name}.EnableAlarm");
            _scAlarmRange = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmRange");
            _scAlarmTime = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmTime");
            _scWarningRange = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningRange");
            _scWarningTime = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningTime");
            _scRecipeIgnoreTime = SC.GetConfigItem($"{ScBasePath}.{Name}.RecipeIgnoreTime");
            _scReflectedPowerMonitorTime = SC.GetConfigItem($"{ScBasePath}.{Name}.ReflectedPowerMonitorTime");
            _scFineTuningValue = SC.GetConfigItem($"{ScBasePath}.FineTuning.{Name}");
            _scFineTuningEnable = SC.GetConfigItem($"{ScBasePath}.FineTuning.IsEnable");
            _scEnableCalibration = SC.GetConfigItem($"{ScBasePath}.{Name}.EnableCalibration");
            _scCalibrationTable = SC.GetConfigItem($"{ScBasePath}.{Name}.CalibrationTable");
        }

        private void InitHandler()
        {
            _lstHandler.AddLast(new TruPlasmaRF1000GetControlHandler(this));
            _lstHandler.AddLast(new TruPlasmaRF1000ResetHandler(this));
            _lstHandler.AddLast(new TruPlasmaRF1000SetGainHandler(this, SC.GetValue<int>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Gain")));
            _lstHandler.AddLast(new TruPlasmaRF1000SetRelativeGainHandler(this, SC.GetValue<int>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.RelativeGain")));
            _lstHandler.AddLast(new TruPlasmaRF1000SetModulationDeviationHandler(this, SC.GetValue<int>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.ModulationDeviation")));
            _lstHandler.AddLast(new TruPlasmaRF1000SetRelativeModulationDeviationHandler(this, SC.GetValue<int>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.RelativeModulationDeviation")));
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
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new TruPlasmaRF1000QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new TruPlasmaRF1000SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                            InitHandler();
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
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
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

        internal void NoteInterfaceActived(bool actived)
        {
            InterfaceActived = actived;
        }

        public override void Monitor()
        {
            try
            {
                //_connection.EnableLog(_enableLog);
 
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                MonitorRamping();

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
 
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");
            if(_isAlarm)
            {
                _isAlarm = false;
                lock (_locker)
                {
                    _lstHandler.AddLast(new TruPlasmaRF1000ResetHandler(this));
                }
            }

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public override void CheckTolerance()
        {
            if (!EnableAlarm || PowerSetPoint.IsZero()| (RecipeIgnoreTime.GreaterThan(0) && _recipeIgnoreTimer.GetElapseTime() < RecipeIgnoreTime * 1000 + _rampTime))
                return;

            _toleranceAlarmChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - AlarmRange / 100)), (PowerSetPoint * (1 + AlarmRange / 100)), AlarmTime);

            _toleranceWarningChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - WarningRange / 100)), (PowerSetPoint * (1 + WarningRange / 100)), WarningTime);

            if (_PrThreshold > 0)
                _prThresholdChecker.Monitor(ReflectPower, 0, _PrThreshold, PrThresholdMonitorTime);
        }

        #region Command Functions
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000RawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000RawCommandHandler(this, command));
            }
        }

        public void GetControl()
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new TruPlasmaRF1000GetControlHandler(this));
            }
        }
        public void ReleaseControl()
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new TruPlasmaRF1000ReleaseControlHandler(this));
            }
        }
        public void PreSetPiValue(int piValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000PreSetPiValueHandler(this, piValue));
            }
        }
        public void ReadPiValue()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000ReadPiValueHandler(this));
            }
        }

        public override void SetPower(float power)
        { 
            PowerSetPoint = power;
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000PreSetPiValueHandler(this, (int)PowerSetPoint.HalfAdjust()));
            }
        }

        public override void SetRegulationMode(EnumRfPowerRegulationMode enumRfPowerControlMode)
        {

        }

        public override void SetWorkMode(EnumRfPowerWorkMode enumRfPowerWorkMode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000SetPulseModeHandler(this, enumRfPowerWorkMode));
            }
        }

        public override void SetClockMode(EnumRfPowerClockMode clockMode)
        {
            _clockModeSetpoint = clockMode;
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000SetClockModeHandler(this, clockMode));
            }
        }

        public override void SetFreq(float freq)
        {
            _frequencySetPoint = freq;
            var offset = (freq - _workFrequency) * 1000.0f;//unit kHz => Hz
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000SetFreqHandler(this, (int)(offset.HalfAdjust())));
            }
        }

        public override bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000SetPowerOnOffHandler(this, isOn));
            }
            return true;
        }
        protected override void SetRampPower(float power)
        {
            _PowerSetPoint = power;
            lock (_locker)
            {
                _lstHandler.AddLast(new TruPlasmaRF1000PreSetPiValueHandler(this, (int)PowerSetPoint.HalfAdjust()));
            }
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(TruPlasmaRF1000RawCommandHandler) && ((TruPlasmaRF1000Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new TruPlasmaRF1000RawCommandHandler(this, command, comandArgument));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }
        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(TruPlasmaRF1000RawCommandHandler) && ((TruPlasmaRF1000Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new TruPlasmaRF1000RawCommandHandler(this, command));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }



        #endregion

        #region Properties
        public string Error { get; private set; }
        public bool InterfaceActived { get; private set; }


        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();



        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
                _isAlarm = true;
            }
            else
            {
                Error = null;
            }
        }


        internal void NoteRawCommandInfo(string command, string data)
        {
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
    }
}
