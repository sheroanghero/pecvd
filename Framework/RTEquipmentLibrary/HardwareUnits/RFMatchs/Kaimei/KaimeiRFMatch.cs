using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Kaimei
{
    public class KaimeiRFMatch : RfMatchBase, IConnection
    {
        public string Address => Connection.Address;
        public new bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
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

        private KaimeiRFMatchConnection _connection;
        public KaimeiRFMatchConnection Connection
        {
            get { return _connection; }
        }
        public override AITRfMatchData DeviceData
        {
            get
            {
                return new AITRfMatchData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DisplayName = Name,
                    TuneMode1 = TuneMode1,
                    TunePosition1 = TunePosition1,
                    LoadPosition1 = LoadPosition1,
                    LoadPosition1SetPoint = LoadPosition1Setpoint,
                    TunePosition1SetPoint = TunePosition1Setpoint,
                    TuneRange = (float)SC.GetValue<double>($"{Module}.{Name}.TuneRange"),
                    LoadRange = (float)SC.GetValue<double>($"{Module}.{Name}.LoadRange"),
                    IsInterlockOk = true,
                };
            }
        }

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
        public KaimeiRFMatch(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

        }

        public override bool Initialize()
        {
            base.Initialize();

            _address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new KaimeiRFMatchConnection(_address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            //_lstMonitorHandler.AddLast(new CommetRFMatchGetActualCapHandler(this));
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _QueryTimer.Start(_QueryInterval);
            _lstMonitorHandler.AddLast(new KaimeiRFMatchGetStatusHandler(this));

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetLoad1", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                if (TuneMode1Setpoint != EnumRfMatchTuneMode.Auto)
                    SetLoad1(Convert.ToSingle(param[0]));
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetTune1", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                if (TuneMode1Setpoint != EnumRfMatchTuneMode.Auto)
                    SetTune1(Convert.ToSingle(param[0]));
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetTuneMode1", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetTuneMode1(param[0].ToString().ToUpper() == "AUTO" ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SavePreset", (function, args) =>
            {
                SavePreset();
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
                            if (_QueryTimer.IsTimeout())
                            {
                                foreach (var monitorHandler in _lstMonitorHandler)
                                {
                                    _lstHandler.AddLast(monitorHandler);
                                }
                                _QueryTimer.Start(_QueryInterval);
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
        internal void NoteActualCap(float value)
        {
        }

        public override void SetTuneMode1(EnumRfMatchTuneMode enumRfMatchTuneMode)
        {
            TuneMode1Setpoint = enumRfMatchTuneMode;

            //if (SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
            //    TuneMode1 = TuneMode1Setpoint;

            lock (_locker)
            {
                _lstHandler.AddLast(new KaimeiRFMatchPresetValveHandler(this, TuneMode1Setpoint, TunePosition1Setpoint, LoadPosition1Setpoint));
            }
        }

        public override void SetLoad1(float load)
        {
            LoadPosition1Setpoint = load;

            //if (SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
            //    LoadPosition1 = LoadPosition1Setpoint;
            lock (_locker)
            {
                _lstHandler.AddLast(new KaimeiRFMatchPresetValveHandler(this, TuneMode1Setpoint, TunePosition1Setpoint, LoadPosition1Setpoint));
            }
        }

        public override void SetTune1(float tune)
        {
            TunePosition1Setpoint = tune;

            //if (SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
            //    TunePosition1 = TunePosition1Setpoint;
            lock (_locker)
            {
                _lstHandler.AddLast(new KaimeiRFMatchPresetValveHandler(this, TuneMode1Setpoint, TunePosition1Setpoint, LoadPosition1Setpoint));
            }
        }

        public void  SavePreset()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new KaimeiRFMatchSetCurrentValveHandler(this));
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

        #endregion
    }
}
