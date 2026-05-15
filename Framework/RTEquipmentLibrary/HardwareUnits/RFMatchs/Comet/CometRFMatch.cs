using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{
    public class CometRFMatch : RfMatchBase, IConnection
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

        private CometRFMatchConnection _connection;
        public CometRFMatchConnection Connection
        {
            get { return _connection; }
        }

        public override bool IsStable
        {
            get
            {
                if (_scStableCriteria == null || _scStableCriteria.DoubleValue == 0)
                    return false;

                if (CapacitanceSetpoint == 0)
                    return true;

                if (100 * Capacitance / CapacitanceSetpoint < _scStableCriteria.DoubleValue)
                    _stableTimer.Stop();

                if (_stableTimer.IsIdle() && 100 * Capacitance / CapacitanceSetpoint >= _scStableCriteria.DoubleValue)
                    _stableTimer.Start(_stableTime * 1000);

                return _stableTimer.IsTimeout();
            }
        }

        private int CapacitanceMaxValue => _scCapacitanceMaxValue != null ? _scCapacitanceMaxValue.IntValue : 0;
        private int CapacitanceMinValue => _scCapacitanceMinValue != null ? _scCapacitanceMinValue.IntValue : 0;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        //private DeviceTimer _QueryTimer = new DeviceTimer();
        //private readonly int _QueryInterval = 500;

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        public Dictionary<int, string> ErrorCodeReference;

        private object _locker = new object();

        private SCConfigItem _scCapacitanceMaxValue;
        private SCConfigItem _scCapacitanceMinValue;
        private bool _isGotoCapCompleted = true;
        private const int _capacitanceSetpointMin = 5;//unit %
        private const int _capacitanceSetpointMax = 95;//unit %
        private double _RampInterval_Seconds = 0.5;

        private bool _enableLog;
        private string _address;
        private string _scRoot;
        public CometRFMatch(string module, string name, string scRoot) : base(module, name)
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
            base.Initialize();

            ResetPropertiesAndResponses();

            _scCapacitanceMaxValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.CapacitanceMaxValue");
            _scCapacitanceMinValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.CapacitanceMinValue");

            _address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");

            var item = SC.GetConfigItem($"{Module}.{Name}.CapRampIntervalSeconds");
            if (item != null)
            {
                double.TryParse(item.Default, out _RampInterval_Seconds);
            }

            _connection = new CometRFMatchConnection(_address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _lstMonitorHandler.AddLast(new CommetRFMatchGetActualCapHandler(this));
            _lstMonitorHandler.AddLast(new CommetRFMatchGetStatusHandler(this));
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            //_QueryTimer.Start(_QueryInterval);

            ErrorCodeReference = new Dictionary<int, string>()
            {
                {0x00, "No error" },
                {0x01, "Overcurrent bridge A low side" },//ok: 0, error: 1
                {0x02, "Overcurrent bridge B low side" },//ok: 0, error: 1
                {0x04, "Overcurrent high side" },//ok: 0, error: 1
                {0x08, "Driver under voltage" },//ok: 0, error: 1
                {0x10, "Over temperature" },//ok: 0, overtemp: 1
                {0x20, "Reset indicator" },//ok: 0, reset actuated: 1
            };

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

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
                        _connection.SetPortAddress(SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new CommetRFMatchQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new CommetRFMatchSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
                            if (!_isGotoCapCompleted)
                                _lstHandler.AddLast(new CommetRFMatchCheckGotoCapCompleteHandler(this));

                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

                            //if (_QueryTimer.IsTimeout())
                            //{
                            //    _lstHandler.AddLast(new CommetRFMatchGetActualCapHandler(this));
                            //    _lstHandler.AddLast(new CommetRFMatchGetStatusHandler(this));
                            //    _QueryTimer.Start(_QueryInterval);
                            //}
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

        internal void NoteNAK()
        {
            TriggerNAK = true;
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

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        #region Command Functions

        public void PerformRawCommand(string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchRawCommandHandler(this, command, completeEvent, comandArgument));
            }
        }

        internal void NoteActualCap(float value)
        {
            Capacitance = 100 * (value - CapacitanceMinValue) / (CapacitanceMaxValue - CapacitanceMinValue);
        }

        public void TriggerFullRefRun()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerFullRefRunHandler(this));
            }
        }

        protected float _rampTargetCap;
        protected bool _isLastSetCap = false;

        public void SetLastSetCap() { _isLastSetCap = true; }
        public bool IsLastSetCap => _isLastSetCap;

        public override bool SetCapacitance(float cap, out string reason)
        {
            return SetCapacitance(cap, 1, out reason);
        }
        public override bool SetCapacitance(float cap, float rampTimes, out string reason)
        {
            LOG.Info($"{Module}.{Name} Start SetCapacitance Target Cap={cap}, ramp times={rampTimes}");
            _isLastSetCap = false;
            reason = string.Empty;
            if (!_isGotoCapCompleted)
            {
                reason = $"last command set capacitance is not finished";
                return false;
            }

            if (cap < _capacitanceSetpointMin || cap > _capacitanceSetpointMax)
            {
                reason = $"set capacitance {cap} is not in the scope [{_capacitanceSetpointMin}, {_capacitanceSetpointMax}]";
                return false;
            }

            //_isGotoCapCompleted = false;

            float lastCap = CapacitanceSetpoint;
            CapacitanceSetpoint = cap;
            int times = (int)rampTimes;
            _rampTargetCap = (CapacitanceMaxValue - CapacitanceMinValue) * cap / 100 + CapacitanceMinValue;
            if (rampTimes > 1 && _RampInterval_Seconds > 0)
            {
                float rampInitCap = (CapacitanceMaxValue - CapacitanceMinValue) * lastCap / 100 + CapacitanceMinValue;
                //float rampInitCap = _rampTargetCap / rampTimes;
                Task.Run(() =>
                {
                    for (int ii = 1; ii <= times; ii++)
                    {

                        if (ii == times)
                        {
                            lock (_locker)
                            {
                                LOG.Info($"{Module}.{Name} SetCap {_rampTargetCap} in {ii} times");
                                _lstHandler.AddLast(new CommetRFMatchTriggerGotoCapHandler(this, _rampTargetCap, true));
                            }
                        }
                        else
                        {
                            float toSendCap = (_rampTargetCap - rampInitCap) / rampTimes * ii + rampInitCap;
                            lock (_locker)
                            {
                                LOG.Info($"{Module}.{Name} SetCap {toSendCap} in {ii} times");
                                _lstHandler.AddLast(new CommetRFMatchTriggerGotoCapHandler(this, toSendCap, false));
                            }
                        }
                        Thread.Sleep((int)(_RampInterval_Seconds * 1000));
                    }
                });
            }
            else
            {
                _lstHandler.AddLast(new CommetRFMatchTriggerGotoCapHandler(this, _rampTargetCap, true));
            }
            return true;
        }

        public void SetAccelerationSpeedIndex(byte acceleration, byte speed)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetAccelerationSpeedIndexHandler(this, acceleration.ToString("X2") + "," + speed.ToString("X2")));
            }
        }
        public void SetAccelerationSpeedIndex(string acceleration, string speed)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new CommetRFMatchSetAccelerationSpeedIndexHandler(this, acceleration + "," + speed));
            }
        }
        public void GetActualCap(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchGetActualCapHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchGetActualCapHandler(this));
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

        internal void NoteFullRefRunCompleted()
        {
            FullRefRunCompleted = true;
        }

        public void MonitorRawCommand(bool isSelected, string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(CommetRFMatchRawCommandHandler) && ((CometRFMatchHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new CommetRFMatchRawCommandHandler(this, command, completeEvent, comandArgument));
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

        internal void NoteGotoCapCompleted()
        {
            _isGotoCapCompleted = true;
        }

        public bool TriggerNAK { get; private set; }
        public bool FullRefRunCompleted { get; private set; }
        public byte Acceleration { get; private set; }
        public byte SpeedIndex { get; private set; }

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
                    EV.PostAlarmLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
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

        internal void NoteAccelerationSpeedIndex(string parameter)
        {
            var valueArray = parameter.Split(',');
            Acceleration = Convert.ToByte(valueArray[0], 16);
            SpeedIndex = Convert.ToByte(valueArray[1], 16);
        }

        #endregion
    }
}
