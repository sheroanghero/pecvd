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
using System.Threading;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using Aitex.Core.RT.OperationCenter;
using System.Diagnostics;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners
{
    public class FuqiAligner : AlignerBaseDevice, IConnection
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


        private AlignerType _tazmotype = AlignerType.Vaccum;
        public AlignerType TazmoType { get => _tazmotype; }

        public bool IsAlignSuccess { get; set; }

        protected Stopwatch _timerActionMonitor = new Stopwatch();

        protected int _retryTime;

        protected int TotalRetryTimes
        {
            get
            {
                if(SC.ContainsItem("{ _scRoot}.{ Name}.RetryTime"))
                {
                    return SC.GetValue<int>("{ _scRoot}.{ Name}.RetryTime");
                }
                return 3;
            }
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
            get; set;

        }

        public bool Busy { get { return _connection.IsBusy || _lstHandler.Count != 0; } }


      

        public bool TaExecuteSuccss
        {
            get; set;
        }



        public void OnWaferPresent(bool iswaferon)
        {
            if(iswaferon)
            {
                if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);
            }
            else
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    var wafer = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                    WaferManager.Instance.UpdateWaferE90State(wafer.WaferID, EnumE90Status.Aborted);
                }
            }    
        }

        //private int _deviceAddress;
        public override bool IsNeedRelease
        {
            get
            {
                return true;
            }
        }

        public FuqiAlignerConnection Connection
        {
            get => _connection;
        }
        private FuqiAlignerConnection _connection;

        //private int _presetNumber;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        private static Object _locker = new Object();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private bool _enableLog = true;

        private bool _commErr = false;

        private int _defaultChuckPosition;
        //private bool _exceuteErr = false;
        //private string _addr;


        private DeviceTimer _timerQuery = new DeviceTimer();
        private string _scRoot;


        private string AlarmMechanicalAlignmentError = "MechanicalAlignmentError";


        private IoSensor _diOcrOn6Inch;
        private IoSensor _diOcrOn8Inch;
        private IoSensor _diOcrOn12Inch;
        private IoSensor _diOcrOn4Inch;
        private IoSensor _diWaferPresent;

        private IoTrigger _doOcrTo4Inch;
        private IoTrigger _doOcrTo6Inch;
        private IoTrigger _doOcrTo8Inch;
        private IoTrigger _doOcrTo12Inch;



        private bool _isEnableTwiceAdjustment
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{Name}.EnableTwiceAdjustment"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.EnableTwiceAdjustment");
                return true;
            }
        }

        public FuqiAligner(string module, string name,string scRoot,int alignertype=0)
            : base(module,name)
        {
            Name = name;
            Module = module;
            _scRoot = scRoot;

            InitializeFuqi();
            //WaferManager.Instance.SubscribeLocation(name, 1);
        }
        public FuqiAligner(string module, string name, string scRoot, IoSensor[] dis,IoTrigger[] dos)
            : base(module, name)
        {
            Name = name;
            Module = module;
            _scRoot = scRoot;
            _diOcrOn6Inch = dis[0];
            _diOcrOn8Inch = dis[1];
            _diWaferPresent = dis[2];

            _doOcrTo6Inch = dos[0];
            _doOcrTo8Inch = dos[1];

            InitializeFuqi();
            //WaferManager.Instance.SubscribeLocation(name, 1);
        }
        public FuqiAligner(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos,WaferSize[] wsizes=null)
            : base(module, name)
        {
            Name = name;
            Module = module;
            _scRoot = scRoot;

            if(wsizes == null || wsizes.Length ==1)
            {
                _diWaferPresent = dis[0]; 
                InitializeFuqi();
                return;
            }
            switch(wsizes[0])
            {
                case WaferSize.WS4:
                    _diOcrOn4Inch = dis[0];
                    _doOcrTo4Inch = dos[0];
                    break;
                case WaferSize.WS6:
                    _diOcrOn6Inch = dis[0];
                    _doOcrTo6Inch = dos[0];
                    break;
                case WaferSize.WS8:
                    _diOcrOn8Inch = dis[0];
                    _doOcrTo8Inch = dos[0];
                    break;
                case WaferSize.WS12:
                    _diOcrOn12Inch = dis[0];
                    _doOcrTo12Inch = dos[0];
                    break;
                default:
                    break;
            }

            switch(wsizes[1])
            {
                case WaferSize.WS4:
                    _diOcrOn4Inch = dis[1];
                    _doOcrTo4Inch = dos[1];
                    break;
                case WaferSize.WS6:
                    _diOcrOn6Inch = dis[1];
                    _doOcrTo6Inch = dos[1];
                    break;
                case WaferSize.WS8:
                    _diOcrOn8Inch = dis[1];
                    _doOcrTo8Inch = dos[1];
                    break;
                case WaferSize.WS12:
                    _diOcrOn12Inch = dis[1];
                    _doOcrTo12Inch = dos[1];
                    break;
                default:
                    break;
            }
            if(dis.Length >2)
                _diWaferPresent = dis[2];
            InitializeFuqi();
            //WaferManager.Instance.SubscribeLocation(name, 1);
        }

        public bool InitializeFuqi()
        {
            string portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            int bautRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.StopBits"), out StopBits stopBits);            
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");

            _connection = new FuqiAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;

            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            //            string str = string.Empty;
            SubscribeSpecialData();
            //            _timerQuery.Start(_queryPeriod);

            return true;
        }

        private void SubscribeSpecialData()
        {
            EV.Subscribe(new EventItem(0, "Event", AlarmMechanicalAlignmentError, "Aligner error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));



            OP.Subscribe(String.Format("{0}.{1}", Name, "SetAlignmentLine"), (out string reason, int time, object[] param) =>
            {
                bool ret = ExecuteSetLine(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "LiftDown");
                    return true;
                }
                reason = "";
                return false;
            });


            OP.Subscribe(String.Format("{0}.{1}", Name, "SetAlignmentNotch"), (out string reason, int time, object[] param) =>
            {
                bool ret = ExecuteSetNotch(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "LiftDown");
                    return true;
                }
                reason = "";
                return false;
            });

            



        }


        public bool ExecuteSetNotch(out string reason)
        {
            reason = "";
            if(!IsReady())
            {
                reason = "NotReady";
                return false;
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetNotchProduct));

            }
            if (SC.ContainsItem($"Aligner.{RobotModuleName}.WS6ProductType"))
                SC.SetItemValue($"Aligner.{RobotModuleName}.WS6ProductType", "Notch");

            CurrentWaferAlignmentType = WaferAlignerTypeEnum.Notch;
            
            return true;
        }

        public bool ExecuteSetLine(out string reason)
        {
            reason = "";
            if (!IsReady())
            {
                reason = "NotReady";
                return false;
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetLineProduct));

            }
            if (SC.ContainsItem($"Aligner.{RobotModuleName}.WS6ProductType"))
                SC.SetItemValue($"Aligner.{RobotModuleName}.WS6ProductType", "Line");

            CurrentWaferAlignmentType = WaferAlignerTypeEnum.Line;

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.EnableLog(_enableLog);
                _connection.MonitorTimeout();

                _trigCommunicationError.CLK = _connection.IsCommunicationError ||
                    (_connection.ActiveHandler != null && _connection.ActiveHandler.IsCompleteTimeout);
                if (_trigCommunicationError.Q)
                {
                    OnError("CommunicationError");
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
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null)); 
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null)); 
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            _lstHandler.RemoveFirst();
                            if (handler != null) _connection.Execute(handler);
                            
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


        #region Command

        



       
        public virtual bool MoveToReady(out string reason)
        {
            reason = "";
            return true;
        }


        
        public bool RequestPlace(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));

            }

            reason = "";
            return true;
        }


        public override bool IsReady()
        {
            return AlignerState == AlignerStateEnum.Idle && !IsBusy;
        }

        public override bool IsWaferPresent(int slotindex)
        {
            if(_diWaferPresent == null)
                return WaferManager.Instance.CheckHasWafer(RobotModuleName, 0);

            if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                return !_diWaferPresent.Value;
            return true;


        }

        protected override bool fStartLiftup(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }           
            return true;
        }

        protected override bool fMonitorLiftup(object[] param)
        {
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }
        protected override bool fStartPrepareAccept(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }
            return true;
        }
        protected override bool fMonitorPrepareAccept(object[] param)
        {
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStartLiftdown(object[] param)
        {
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStartAlign(object[] param)
        {
            double aligneangle = (double)param[0];
            int nAngle = Convert.ToInt32(aligneangle);
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, $"B{nAngle}"));
                //_lstHandler.AddLast(new FuqiRequestHandler(this, $"B{aligneangle.ToString("f1")}"));
                //_lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOffAfterAlign));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestFinishPlace));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOff));
            }
            _timerActionMonitor.Restart();
            _retryTime = 0;
            return true;
        }

        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimeLimitAlignWafer))
            {
                _timerActionMonitor.Stop();
                OnError("AlignmentTimeout");
            }

            if(_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                if(!IsAlignSuccess)
                {
                    if (_retryTime >= TotalRetryTimes)
                    {
                        OnError("AlignmentFailed");
                    }
                    else
                    {
                       
                        lock (_locker)
                        {                            
                            _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestFinishPlace));
                            _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOff));
                            _retryTime++;
                        }                        
                    }
                    return false;
                }
                return true;
            }
            return false;
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

        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOff));
            }            
            return true;
        }

        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestVacuumOn));

            }
            return true;
        }

        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            if(!_connection.IsConnected)
            {
                string portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
                int bautRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"{_scRoot}.{Name}.StopBits"), out StopBits stopBits);
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");

                _connection = new FuqiAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
                _connection.Connect();
            }

            

            _trigRetryConnect.RST = true;


            lock (_locker)
            {
                _lstHandler.Clear();                
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetUseNewCommand));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOffAfterAlign));
                if(SC.ContainsItem($"{_scRoot}.{Name}.CenterAndNotch") && SC.GetValue<bool>($"{_scRoot}.{Name}.CenterAndNotch"))
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetCenterAndNotch));
                else
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetOnlyNotch));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetWIDReaderOff));
            }            
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
            {
                Size = WaferSize.WS0;
            }
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }
        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetUseNewCommand));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetWIDReaderOff));
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetVacuumOnAfterAlign));
                if (SC.ContainsItem($"{_scRoot}.{Name}.CenterAndNotch") && SC.GetValue<bool>($"{_scRoot}.{Name}.CenterAndNotch"))
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetCenterAndNotch));
                else
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetOnlyNotch));

                if(_isEnableTwiceAdjustment)
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetAdjustTwice));
                else
                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetAdjustFirstTime));

                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.RequestPlace));
            }
            return true;
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if(WaferManager.Instance.CheckNoWafer(RobotModuleName,0))
            {
                Size = WaferSize.WS0;
            }
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }
        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                
            }
            return true;
        }
        protected override bool fMonitorHome(object[] param)
        {

            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }
        private DateTime _dtActionStart;
        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _currentSetParameter = param[0].ToString();
            switch (_currentSetParameter)
            {
                case "WaferSize":
                    //if (GetWaferState() != RobotArmWaferStateEnum.Absent)
                    //{
                    //    EV.PostAlarmLog("System", "Can't set wafersize when wafer is not absent");
                    //    return false;
                    //}
                    _currentSetWaferSize = (WaferSize)param[1];

                    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    }
                    
                    switch (_currentSetWaferSize)
                    {
                        case WaferSize.WS12:
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(true, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetNotchProduct));
                                CurrentWaferAlignmentType = WaferAlignerTypeEnum.Notch;
                            }
                            break;

                        case WaferSize.WS8:                            
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(true, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);

                            lock (_locker)
                            {
                                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetNotchProduct));
                                CurrentWaferAlignmentType = WaferAlignerTypeEnum.Notch;
                            }
                            break;
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(true, out _);
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetNotchProduct));
                                CurrentWaferAlignmentType = WaferAlignerTypeEnum.Notch;
                            }
                            break;

                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS7:
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(true, out _);
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);                            
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);                            
                            lock (_locker)
                            {
                                if(SC.ContainsItem($"Aligner.{RobotModuleName}.WS6ProductType") &&
                                    SC.GetStringValue($"Aligner.{RobotModuleName}.WS6ProductType")=="Notch")
                                {
                                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetNotchProduct));
                                    CurrentWaferAlignmentType = WaferAlignerTypeEnum.Notch;
                                }
                                else
                                {
                                    _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetLineProduct));

                                    CurrentWaferAlignmentType = WaferAlignerTypeEnum.Line;
                                }
                                   


                            }
                            break;
                        default:
                            break;
                    }
                    lock (_locker)
                    {
                        if (Size != _currentSetWaferSize)
                        {
                            string strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                            if (strpara == "7")
                                strpara = "6";
                            _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.SetWaferSize + strpara));
                            _lstHandler.AddLast(new FuqiRequestHandler(this, FuqiAlignerCommand.Reset));
                        }
                        Size = _currentSetWaferSize;
                    }
                    break;
            }
            return true;
        }
        private string _currentSetParameter;
        private WaferSize _currentSetWaferSize = WaferSize.WS0;
        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignWafer))
                OnError("Set parameter timeout");
            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;

            switch (_currentSetParameter)
            {
                case "WaferSize":                    
                    switch (_currentSetWaferSize)
                    {
                        case WaferSize.WS6:
                           if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && !_diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS8:                           
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && !_diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS4:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && !_diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS12:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && !_diOcrOn12Inch.Value)
                                return false;
                            break;
                        default:
                            
                            return true;
                    }
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    return true;
                default:
                    return true;
            }            
        }

        public override RobotArmWaferStateEnum GetWaferState(int slotindex = 0)
        {            
            return IsWaferPresent(slotindex) ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
        }
        public override bool IsNeedChangeWaferSize(WaferSize wz)
        {
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }
        
        public void OnActionDone()
        {
            IsBusy = false;
            //if (_lstHandler.Count == 0)
            //    OnActionDone(null);
        }
        #endregion

        public override bool IsNeedPrepareBeforePlaceWafer()
        {
            return true;
        }

        public override double CompensationAngleValue
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.CompensationAngle"))
                    return SC.GetValue<double>($"Aligner.{Name}.CompensationAngle");
                return 0;
            }
        }

    }
}


