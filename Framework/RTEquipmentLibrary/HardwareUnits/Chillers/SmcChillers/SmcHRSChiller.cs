using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData.DeviceData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.SmcHRSChillers
{
    [Serializable]
    public class SmcHRSChiller : ChillerBase, IConnection
    {
        public string Address => Connection?.Address;
        public override bool IsConnected => Connection != null && Connection.IsConnected && !_connection.IsCommunicationError;
        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public byte SlaveAddress { get; private set; } = 0x01;

        public string PortStatus { get; set; } = "Closed";

        private SmcHRSChillerConnection _connection;
        public SmcHRSChillerConnection Connection
        {
            get { return _connection; }
        }

        public override AITChillerData1 DeviceData
        {
            get
            {
                return new AITChillerData1()
                {
                    Module = Module,
                    DeviceName = Name,
                    DisplayName = Name,
                    IsCH1On = IsCH1On,
                    IsCH2On = IsCH2On,
                    IsCH1Alarm = IsCH1Alarm,
                    IsCH2Alarm = IsCH2Alarm,
                    CH1Temperature = CH1TemperatureFeedback,
                    CH2Temperature = CH2TemperatureFeedback,
                    CH1TemperatureSetPoint = CH1TemperatureSetpoint,
                    CH2TemperatureSetPoint = CH2TemperatureSetpoint,
                    CH1WaterFlow = CH1WaterFlow,
                    CH2WaterFlow = CH2WaterFlow,
                    FormatString = "f1",
                    TemperatureHighLimit = TemperatureHighLimit,
                    TemperatureLowLimit = TemperatureLowLimit,
                };
            }
        }

        public override float TemperatureHighLimit => _scTemperatureMaxValue == null ? 0 : (float)_scTemperatureMaxValue.DoubleValue;
        public override float TemperatureLowLimit => _scTemperatureMinValue == null ? 0 : (float)_scTemperatureMinValue.DoubleValue;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        private DeviceTimer _QueryTimer = new DeviceTimer();
        private readonly int _QueryInterval = 2000;

        private object _locker = new object();
        private bool _enableLog;
        private string _address;
        private string _scRoot;
        private SCConfigItem _scTemperatureMaxValue;
        private SCConfigItem _scTemperatureMinValue;

        private R_TRIG[] _trigAlarm = new R_TRIG[32];

        public SmcHRSChiller(string module, string name, string scRoot) : base()
        {
            _scRoot = scRoot;
            base.Module = module;
            base.Name = name;

            for (int i = 0; i < _trigAlarm.Length; i++)
            {
                _trigAlarm[i] = new R_TRIG();
            }
        }

        public override bool Initialize()
        {
            base.Initialize();

            _scTemperatureMaxValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMaxValue");
            _scTemperatureMinValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMinValue");
            _address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new SmcHRSChillerConnection(_address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            //_lstMonitorHandler.AddLast(new CommetRFMatchGetActualCapHandler(this));
            _thread = new PeriodicJob(2000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _QueryTimer.Start(_QueryInterval);
            _lstMonitorHandler.AddLast(new SmcHRSChillerGetStatusHandler(this));

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


        public bool Initialize(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            base.Initialize();

            //_scTemperatureMaxValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMaxValue");
            //_scTemperatureMinValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMinValue");
            //_address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = true;// SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");

            _connection = new SmcHRSChillerConnection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            _QueryTimer.Start(_QueryInterval);

            //_lstMonitorHandler.AddLast(new SmcHRZChillerGetStatusHandler(this));


            return true;
        }
        private bool OnTimer()
        {
            try
            {
                //_connection.MonitorTimeout();

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
                            //_lstHandler.AddLast(new SmcHRZChillerQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new SmcHRZChillerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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



        public override void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);


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

        internal void NoteIsWarning(bool isWarning)
        {
            IsCH1Warning = isWarning;
        }

        internal void NotePressUnit(bool pressUnit)
        {

        }

        internal void NoteIsRemote(bool isRemote)
        {
            IsCH1Remote = isRemote;
        }

        internal void NoteIsTempReady(bool isTempReady)
        {

        }

        internal void NoteIsTimeout(bool isTimeout)
        {

        }

        internal void NoteFlowUnit(bool flowUnit)
        {

        }

        internal void NoteIsFault(bool isFault)
        {
            if (IsCH1Alarm != isFault)
            {
                IsCH1Alarm = isFault;
            }
        }

        internal void NoteIsRun(bool isRun)
        {
            if (IsCH1On != isRun)
            {
                IsCH1On = isRun;
            }
        }

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            for (int i = 0; i < _trigAlarm.Length; i++)
            {
                _trigAlarm[i].RST = true;
            }

            base.Reset();
        }


        #region Command Functions
        public void SetTemperature(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new SmcHRZChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempHighWarning(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new SmcHRZChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempLowWarning(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new SmcHRZChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetRemoteMode(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new SmcHRZChillerSimpleSetHandler(this, "SWS", angle));
            }
        }

        public void SetRunMode(string waferSize)
        {
            lock (_locker)
            {
                // _lstHandler.AddLast(new SmcHRZChillerSimpleSetHandler(this, "SWS", waferSize));
            }
        }

        public void QueryAllTemp()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SmcHRSChillerGetStatusHandler(this));
            }
        }

        public override void SetChillerCH1OnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SmcHRSChillerSetOnOffHandler(this, isOn));
            }
        }
        public override void SetChillerCH2OnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SmcHRSChillerSetOnOffHandler(this, isOn));
            }
        }

        public override void SetChillerCH1Temperature(float temp)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SmcHRSChillerSetTemperatureHandler(this, temp));
            }
        }

        public override void SetChillerCH2Temperature(float temp)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SmcHRSChillerSetTemperatureHandler(this, temp));
            }
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
            }
            else
            {
                Error = null;
            }
        }

        public void NoteTemp(float value)
        {
            CH1TemperatureFeedback = value;
        }
        public void NoteTempSetpoint(float value)
        {
            CH1TemperatureSetpoint = value;
        }

        public void NotePressure(float value)
        {
        }
        public void NoteErrorFlag(int index, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                _trigAlarm[index].CLK = true;
                if (_trigAlarm[index].Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {error}");
                }
                Error = error;
            }
            else
            {
                Error = null;
            }
        }

        #endregion
    }
}
