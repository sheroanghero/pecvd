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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.BaecChiller
{
    public class BaecChiller : ChillerBase, IConnection
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

        public byte SlaveAddress { get; private set; } = 0x01;

        public string PortStatus { get; set; } = "Closed";

        private BaecChillerConnection _connection;
        public BaecChillerConnection Connection
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

        public override float TemperatureHighLimit => (float)_scTemperatureMaxValue.DoubleValue;
        public override float TemperatureLowLimit => (float)_scTemperatureMinValue.DoubleValue;
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
        public BaecChiller(string module, string name, string scRoot) : base()
        {
            _scRoot = scRoot;
            base.Module = module;
            base.Name = name;
        }

        public override bool Initialize()
        {
            base.Initialize();

            _scTemperatureMaxValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMaxValue");
            _scTemperatureMinValue = SC.GetConfigItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.TemperatureMinValue");
            _address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new BaecChillerConnection(_address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            //_lstMonitorHandler.AddLast(new CommetRFMatchGetActualCapHandler(this));
            _thread = new PeriodicJob(2000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _QueryTimer.Start(_QueryInterval);
            _lstMonitorHandler.AddLast(new BaecChillerGetStatusHandler(this));

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
                            //_lstHandler.AddLast(new RisshiChillerQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RisshiChillerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
                //_connection.EnableLog(_enableLog);


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

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }


        #region Command Functions
        public void SetTemperature(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempHighWarning(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempLowWarning(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetRemoteMode(string angle)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWS", angle));
            }
        }

        public void SetRunMode(string waferSize)
        {
            lock (_locker)
            {
                // _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWS", waferSize));
            }
        }

        public override void SetChillerCH1OnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BaecChillerSetOnOffHandler(this, true, isOn));
            }
        }
        public override void SetChillerCH2OnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BaecChillerSetOnOffHandler(this, false, isOn));
            }
        }

        public override void SetChillerCH1Temperature(float temp)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BaecChillerSetTemperatureHandler(this, true, temp));
            }
        }

        public override void SetChillerCH2Temperature(float temp)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BaecChillerSetTemperatureHandler(this, false, temp));
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

        #endregion
    }
}
