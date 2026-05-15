using System;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

using Aitex.Sorter.Common;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using System.Collections.Generic;
using Aitex.Core.RT.SCCore;
using System.IO.Ports;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAlignerIIs
{
    public class TazmoAlignerII : AlignerBaseDevice, IConnection
    {
        public enum AlignerType
        {
            Mechnical = 0,
            Vaccum,
        }
        public string Address
        {
            get
            {
                return "";

            }
        }

        public virtual bool IsConnected
        {
            get { return true; }
        }

        public virtual bool Disconnect()
        {
            return true;
        }
        public virtual bool Connect()
        {
            return true;
        }
        public const string delimiter = "\r";

        public int LastErrorCode { get; set; }
        public int Status { get; set; }


        public int ElapseTime { get; set; }

        public int Notch { get; set; }

        public bool Initalized { get; set; }


        protected AlignerType _tazmotype => (AlignerType)SC.GetValue<int>($"{_scRoot}.{Name}.AlignerType");
        public AlignerType TazmoType { get => _tazmotype; }


        //public override bool IsNeedRelease
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}


        public int AligneTimesNoHome
        {
            get;set;
        }

        public bool Communication
        {
            get
            {
                return !_commErr;
            }
        }

        public virtual bool Error
        {
            get
            {
                return (int)TaAlignerStatus1 >= 0x111 || _commErr;
            }
        }

        public bool Busy { get { return _connection.IsBusy || _lstHandler.Count != 0; } }



        public TazmoState1 TaAlignerStatus1
        {
            get; set;
        }

        public TazmoStatus TaAlignerStatus2Status
        {
            get; set;
        }

        public LiftStatus TaAlignerStatus2Lift
        {
            get; set;
        }
        public bool IsWaferPresentByCheckResult { get; set; }
        //public override bool IsWaferPresent(int slotindex)
        //{
        //    return _isWaferPresentByCheckResult;
        //}

        public NotchDetectionStatus TaAlignerStatus2Notch
        {
            get; set;
        }
        public int TaAlignerStatus2DeviceStatus
        { get; set; }

        public int TaAlignerStatus2ErrorCode
        {
            get; set;
        }

        public int TaAlignerStatus2LastErrorCode
        {
            get; set;
        }

        public bool TaExecuteSuccss
        {
            get; set;
        }


        public TazmoAlignerIIConnection Connection
        {
            get => _connection;
        }
        protected TazmoAlignerIIConnection _connection;

        //private int _presetNumber;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        protected static Object _locker = new Object();
        private DateTime _dtActionStart;
        protected LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private bool _enableLog => SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");

        private bool _commErr = false;

        protected int _defaultChuckPosition => SC.ContainsItem($"{_scRoot}.{Name}.DefaultChuckPosition") ?
                SC.GetValue<int>($"{_scRoot}.{Name}.DefaultChuckPosition") : 1;

        private string _scRoot;


        private string AlarmMechanicalAlignmentError = "MechanicalAlignmentError";
        public TazmoAlignerII(string module, string name, string scRoot) : base(module, name)
        {
            Name = name;
            _scRoot = scRoot;

            InitializeAligner();
        }

        public bool InitializeAligner()
        {
            string portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            int bautRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.StopBits"), out StopBits stopBits);
            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress"); 
            _connection = new TazmoAlignerIIConnection(portName);//, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);
            int _retryTime = 0;
            while(!_connection.Connect())
            {
                _retryTime++;
                Thread.Sleep(1000);
                _connection.Disconnect();
                if(_retryTime >10)
                    EV.PostInfoLog(Module, $"Can't connect to {portName} for {Module}.{Name}.");                
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            EV.Subscribe(new EventItem(0, "Event", AlarmMechanicalAlignmentError, "Aligner error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.EnableLog(_enableLog);
                _connection.MonitorTimeout();
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.PortName"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
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
                            
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            if (handler != null) _connection.Execute(handler);
                            _lstHandler.RemoveFirst();
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
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


        #region Command


        public virtual void Pause()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.PauseMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
        }

        public virtual void CancelPause()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CancelthepauseMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
            }
        }



        public virtual void MoveChuck(int position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, position.ToString("0")));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
        }

        private bool _resetCpu
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{RobotModuleName}.RestartCpuOnReset"))
                    return SC.GetValue<bool>($"Aligner.{RobotModuleName}.RestartCpuOnReset");
                return false;
            }
        }
        protected override bool fReset(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            _lstHandler.Clear();
            _connection.ForceClear();
            _trigCommunicationError.RST = true;
            _trigRetryConnect.RST = true;

            if(!_connection.IsConnected)
            {
                _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.PortName"));
                if (!_connection.Connect())
                {
                    EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                }
                else
                {
                    IsBusy = false;
                    OnError("CommunicationError");
                    return false;
                }
            }

            lock (_locker)
            {               
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CancelerrorSet, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
                if (_resetCpu)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CPUresetMotion, null));
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to reset.");
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Reset timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} reset complete.");
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.InitializeMotion, null));
                if (_tazmotype == AlignerType.Vaccum)
                    _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, _defaultChuckPosition.ToString("0")));

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to initialize.");
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Init timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            IsBusy = false;
            IsNeedRelease = true;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} initialize complete.");
            AligneTimesNoHome = 0;
            return true;

        }
        protected override bool fStartHome(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovealignertohomepositionMotion, null));
                if (_tazmotype == AlignerType.Mechnical)
                {
                    string para1 = $"A," + "0000";
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                    _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovetopickpositionMotion, "A"));
                }

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to home.");
            return true;
        }
        protected override bool fMonitorHome(object[] param)
        { 
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Home timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            IsBusy = false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} home complete.");
            AligneTimesNoHome = 0;
            return true;
        }
        protected override bool fStartLiftup(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                string para1 = $"A," + "0000";
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovetopickpositionMotion, "A"));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start lift up.");
            return true;
        }
        protected override bool fMonitorLiftup(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Liftup timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} liftup complete.");
            return true;
        }
        protected override bool fStartLiftdown(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovealignertohomepositionMotion, ""));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to lift down.");
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Lifdown timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} liftdown complete.");
            return true;
        }
        protected override bool fStartAlign(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                double aligneangle = (double)param[0];
                int anglevalue = (int)aligneangle * 10;
                string para1 = $"1," + anglevalue.ToString("0000") + ",000";
                if (_tazmotype == AlignerType.Mechnical)
                {
                    anglevalue = (int)aligneangle * 100;
                    para1 = $"1," + anglevalue.ToString("00000");
                }
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.SeriesofalignmentMotion, "1,5"));

                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.OpenalignerchuckMotion, null));


                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Aligning timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            IsNeedRelease = false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} aligning complete.");
            AligneTimesNoHome++;
            return true;
        }
        protected override bool fStartSetParameters(object[] param)
        {
            return true;
        }
        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {

                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.OpenalignerchuckMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to ungrip wafer.");
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("UnGrip timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            IsNeedRelease = false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} ungrip complete.");
            return true;
        }
        protected override bool fStartGrip(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.ClosealignerchuckMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to grip wafer.");
            IsNeedRelease = true;
            _dtActionStart = DateTime.Now; 
            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Grip timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} grip complete.");
            return true;
        }

        protected override bool fStartPrepareAccept(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                //_lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.InitializeMotion, null));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, _defaultChuckPosition.ToString()));
                if (_tazmotype == AlignerType.Mechnical)
                {                   
                    _lstHandler.AddLast(new CheckWaferPresentHandler(this, TazmoCommand.CheckwaferpresenceMotionNoEValve));
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
                }
                else
                    _lstHandler.AddLast(new CheckWaferPresentHandler(this, TazmoCommand.CheckwaferpresenceMotion));
                
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to prepare accept wafer.");
            return true;
        }
        protected override bool fMonitorPrepareAccept(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Prepare accept timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;

            if(IsWaferPresentByCheckResult)
            {
                OnError("Prepare Failed due to detect wafer ON");
                return false;
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} prepare accept complete.");
            AligneTimesNoHome = 0;
            return true;
        }


        protected override bool fStop(object[] param)
        {
            return true;
        }

        protected override bool FsmAbort(object[] param)
        {
            return true;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        protected override bool fStartReadData(object[] param)
        {
            return true;
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }
        protected override bool fError(object[] param)
        {
            return true;
        }

        #endregion



    }
}


