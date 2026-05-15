using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.MKSs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.STeam
{
    public class STeamRFMatch : RfMatchBase, IConnection
    {
        public override EnumRfMatchTuneMode TuneMode1
        {
            get
            {
                return _statusData.Status2.Net1AutoMode ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
            }
        }

        public override EnumRfMatchTuneMode TuneMode2
        {
            get
            {
                return _statusData.Status2.Net2AutoMode ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
            }
        }
        public override float LoadPosition1
        {
            get { return _statusData.LoadPosi1; }
        }

        public override float LoadPosition2
        {
            get { return _statusData.LoadPosi2; }
        }

        public override float TunePosition1
        {
            get { return _statusData.TunePosi1; }
        }

        public override float TunePosition2
        {
            get { return _statusData.TunePosi2; }
        }
        public override float BiasPeak
        {
            get { return _statusData.BiasPeak; }
        }

        public override float DCBias
        {
            get { return _statusData.DCBias; }
        }
        public override AITRfMatchData DeviceData
        {
            get
            {
                AITRfMatchData data = new AITRfMatchData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    LoadPosition1 = LoadPosition1,
                    LoadPosition2 = LoadPosition2,

                    TunePosition1 = TunePosition1,
                    TunePosition2 = TunePosition2,

                    TuneMode1 = TuneMode1,
                    TuneMode2 = TuneMode2,

                    BiasPeak = BiasPeak,
                    DCBias = DCBias,
                };

                return data;
            }
        }
        private MksStatusData _statusData;


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

        private STeamRFMatchConnection _connection;
        public STeamRFMatchConnection Connection
        {
            get { return _connection; }
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

        public STeamRFMatch(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }

        public override bool Initialize()
        {
            base.Initialize();

            _address = _scRoot;//SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new STeamRFMatchConnection(this, _address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            //_lstMonitorHandler.AddLast(new STeamRFMatchOnOrOffFrequencyCounterHandler(this,1000,true));
            //_lstMonitorHandler.AddLast(new STeamRFMatchStartMesurementHandler(this));
            //_lstMonitorHandler.AddLast(new STeamRFMatchSetAutotuningParameterHandler(this,25,10,true,false,150));

            //_lstMonitorHandler.AddLast(new STeamRFMatchSetHysteresisParameterHandler(this,7));

            //_lstMonitorHandler.AddLast(new STeamRFMatchSetContinuousAutotuningHandler(this, AutotuningEnum.ON));

            //_lstMonitorHandler.AddLast(new STeamRFMatchSingleAutotuningStepHandler(this));


            //_lstMonitorHandler.AddLast(new STeamRFMatchSendTunePosOnorOffHandler(this,true));

            _lstMonitorHandler.AddLast(new STeamRFMatchSetAlarmParameterHandler(this, 60, 70, 3, false));

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _QueryTimer.Start(_QueryInterval);
            //_lstMonitorHandler.AddLast(new KaimeiRFMatchGetStatusHandler(this));

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            //for recipe

            return true;
        }

        internal void NoteStatus(MksStatusData data)
        {
            _statusData = data;
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
