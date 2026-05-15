using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using TSC = Aitex.Sorter.Common;
using Aitex.Core.Common;
using MECF.Framework.Common.SubstrateTrackings;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    /// <summary>  
    /// 代码说明：
    ///  

    /// </summary>
    /// 
    public enum CarrierOnLPState
    {
        Unknow=0,
        On,
        Off,
    }
    public class TDKLoadPort : LoadPort,IConnection
    {
        public string Address { get; set; }

        public bool IsConnected
        {
            get { return _port.IsOpen(); }
        }

        public bool UnlockKey
        {
            set
            {
            }
        }
 
        public bool Enable
        {
            set
            {
                if (_enableTrigger != null)
                {
                    _enableTrigger.SetTrigger(value, out _);
                }
            }
        }

        public bool FFUIsOK { get; set; }


        private LoadportCassetteState _cassetteState = LoadportCassetteState.None;
        public override LoadportCassetteState CassetteState
        {
            get { return _cassetteState; }
            set
            {
                _cassetteState = value;
            }
        }
 

        public bool Processed { get; set; }



        public bool Communication
        {
            get
            {
                return _port == null ? !_commErr : !_commErr && _port.IsOpen();
            }
        }

        //
        public override bool IsBusy
        {
            get { return _foregroundHandler != null || _handlers.Count > 0 ; }
        }

        
        public override  bool IsMoving
        {
            get { return _foregroundHandler != null && _handlers.Count > 0; }
        }
 
        public const string delimiter = "\r";
        public const int min_len = 3;       //S00

        private static Object _locker = new Object();

        private AsyncSerial _port;

        private bool _commErr = false;

        private IHandler _eventHandler = null;

        private IHandler _foregroundHandler = null;  //moving
        private Queue<IHandler> _handlers = new Queue<IHandler>();

 
        private IoSensor _sensorEnable = null;
        private IoTrigger _enableTrigger = null;
 

        public TDKLoadPort(string module, string name,  string port, IoSensor sensorEnable, IoTrigger triggerEnable,
            EnumLoadPortType portType =EnumLoadPortType.LoadPort)
            : base(module, name )
        {
            _port = new AsyncSerial(port, 9600, 8);
            _port.OnDataChanged +=  (OnDataChanged);
            _port.OnErrorHappened +=  (OnErrorHandler);

            _eventHandler = new handler<OnEventHandler>(this, Name);

            _sensorEnable = sensorEnable;
            _enableTrigger = triggerEnable;

            Initalized = false;

            IsMapWaferByLoadPort = portType== EnumLoadPortType.LoadPort;
            PortType = portType;

            IndicatiorLoad = IndicatorState.UNKNOW;
            IndicatiorUnload = IndicatorState.UNKNOW;
            IndicatiorPresence = IndicatorState.UNKNOW;
            IndicatorAlarm = IndicatorState.UNKNOW;
            IndicatiorPlacement = IndicatorState.UNKNOW;
            IndicatiorOpAccess = IndicatorState.UNKNOW;
            IndicatiorStatus1 = IndicatorState.UNKNOW;
            IndicatiorStatus2 = IndicatorState.UNKNOW;

            IndicatiorAccessAuto = IndicatorState.UNKNOW;
            IndicatiorAccessManual = IndicatorState.UNKNOW;

            DoorState = FoupDoorState.Unknown;
            ClampState = FoupClampState.Unknown;
            CassetteState = LoadportCassetteState.Unknown;
            //if (!this.QueryState(out string reason))
            //{
            //    EV.PostAlarmLog(module,$"Query state error.{reason}");
            //}

            Enable = true;

            Address = port; 
        }

        public void SetCassetteState(LoadportCassetteState state)
        {
            _cassetteState = state;

            if (state == LoadportCassetteState.Normal)
            {
                _isPlaced = true;
                _isPresent = true;
                ConfirmAddCarrier();
            }
            else if(state == LoadportCassetteState.Absent)
            {
                _isPlaced = false;
                _isPresent = false;
                ConfirmRemoveCarrier(); 
            }
            else
            {
                _isPlaced = false;                
                ConfirmRemoveCarrier();
            }
        }
        public override bool ReadRfId(out string reason)
        {
            OP.DoOperation("ReadCarrierId", Name);
            reason = "";
            return true;

        }
        public override bool Initialize()
        {
            base.Initialize();
            
            _commErr = true;

            ConnectionManager.Instance.Subscribe($"{Name}", this);

            Connect();

            //if (!RetryInstance.Instance().Execute<bool>(
            //    ()=> _port.Open(),
            //    SC.GetValue<int>("System.ComPortRetryDelayTime"),
            //    SC.GetValue<int>("System.ComPortRetryCount"),
            //    true
            //))
            //{
            //    return false;
            //}
            
            return true;
        }

        public override bool Connect()
        {
            _commErr = false;
            Task.Factory.StartNew(() =>
            {
                int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
                int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
                if (sleep <= 0 || sleep > 10)
                    sleep = 2;

                int retry = 0;
                do
                {
                    _port.Close();
                    Thread.Sleep(sleep * 1000);
                    if (_port.Open())
                    {
                        //LOG.Write($"Connected with {Module}.{Name} .");
                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
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

            });

            return true;
        }

        public bool Disconnect()
        {
            return true;
        }
        public override void Terminate()
        {
            _port.Close();

        }
        public override bool IsEnableMapWafer()
        {
            if (IsIdle 
                && _isPresent 
                && _isPlaced 
                && DoorState == FoupDoorState.Open 
                && CassetteState == LoadportCassetteState.Normal
                )
                return true;

            return false;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            reason = "";

            if (_isPlaced 
                && _isPresent 
                && _isMapped 
                && IsIdle 
                && DoorState==FoupDoorState.Open 
                && CassetteState==LoadportCassetteState.Normal
                && IsWaferEnableTransfer()
                )  return true;

            return false;
        }


        public override void Monitor()
        { 
            base.Monitor();
        }

        /// <summary>
        ///The Indicator is for EAP/Operator
        /// LOAD READY: LoadPort Ready to receive FOUP
        /// UNLOAD READY: FOUP ready to remove
        /// 
        /// </summary>
        public void OnStateUpdated()
        {
            //if (!SC.ContainsItem("Process.OptionLoadportMonitor") || !SC.GetValue<bool>("Process.OptionLoadportMonitor"))
            //    return;

            //if (_isPresent && _isPlaced && this.ClampState == FoupClampState.Open &&this.DockPOS == TDKY_AxisPos.Undock) 
            //{
            //    if (IndicatiorUnload != IndicatorState.ON)
            //        SetIndicator(Indicator.UNLOAD, IndicatorState.ON);
            //    if (IndicatiorLoad != IndicatorState.OFF)
            //        SetIndicator(Indicator.LOAD, IndicatorState.OFF);
            //    return;
            //}

            //if (!_isPresent || !_isPlaced)
            //{
            //    if (IndicatiorUnload != IndicatorState.OFF)
            //        SetIndicator(Indicator.UNLOAD, IndicatorState.OFF);
            //    if (IndicatiorLoad != IndicatorState.ON)
            //        SetIndicator(Indicator.LOAD, IndicatorState.ON);
            //    return;
            //}

            //if (IndicatiorUnload != IndicatorState.OFF)
            //    SetIndicator(Indicator.UNLOAD, IndicatorState.OFF);
            //if (IndicatiorLoad != IndicatorState.OFF)
            //    SetIndicator(Indicator.LOAD, IndicatorState.OFF);
        }

        public void OnAccessButtonPushed()
        {
            if (_isPresent && _isPlaced)
            {

            }
        }

        /// <summary>
        ///reset load port error
        /// 
        /// </summary>
        public override void Reset()
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }
            FFUIsOK = true;
            _commErr = false;
            Enable = true;
            Error = false;

            base.Reset();
        }

        public override bool Home(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<HomeHandler>(this, Name), out reason);
        }
        
        public override bool ForceHome(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<ForceHomeHandler>(this, Name), out reason);
        }

        public override bool FOSBMode(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<FOSBModeHandler>(this, Name), out reason);
        }
        
        public override bool FOUPMode(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<FOUPModeHandler>(this, Name), out reason);
        }

        public override bool FOSBDock(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<DockPosHandler>(this, Name), out reason);
        }

        public override bool FOSBUnDock(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<UnDockPosHandler>(this, Name), out reason);
        }

        public override bool FOSBDoorOpen(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<DoorOpenHandler>(this, Name), out reason);
        }
        
        public override bool FOSBDoorClose(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<DoorCloseHandler>(this, Name), out reason);
        }

        public override bool FOSBDoorUp(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<DoorUpHandler>(this, Name), out reason);
        }
        
        public override bool FOSBDoorDown(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<DoorDownHandler>(this, Name), out reason);
        }

        /// <summary>
        /// Clear LP error
        /// </summary>
        public override bool ClearError(out string reason)
        {

            reason = string.Empty;
            Error = false;
            ExecuteError = false;
            reason = string.Empty;
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            return execute(new handler<ResetHandler>(this, Name), out reason);
        }

        public override bool Stop(out string reason)
        {
            reason = string.Empty;

            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }
            return execute(new handler<MovHandler>(this, Name, MovType.STOP_), out reason);
        }
        public override bool FALoad(out string reason)  //map and loads
        {
            reason = "";
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            OP.DoOperation("LoadFoup", Name);
            
            return true;
        }

        public override bool Load(out string reason)  //map and loads
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CLDMP, out reason);
        }

        public override bool LoadWithoutMap(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CLOAD, out reason);
        }

        public override bool Unload(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CULOD, out reason);
        }

        public override bool Clamp(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.PODCL, out reason);
        }

        public override bool Unclamp(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.PODOP, out reason);
        }

        public override bool Dock(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CLDDK, out reason);
        }

        public override bool Undock(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CULFC, out reason);  //？？？？
        }

        public override bool OpenDoor(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CLMPO, out reason);
        }

        public override bool OpenDoorNoMap(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CLDOP, out reason);
        }

        public override bool CloseDoor(out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            return Move(MovType.CULDK, out reason);
        }

        public override bool SetIndicator(Indicator light, IndicatorState op, out string reason)
        {
            reason = string.Empty;
            return execute(new handler<IndicatorHandler>(this,Name, light, op), out reason);
        }

        public bool SetOnlineMode(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<ModHandler>(this, Name, Mode.ONMGV), out reason);
        }

        public override bool QueryState(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<QueryStateHandler>(this, Name), out reason);
        }

        public override bool QueryIndicator(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<QueryIndicatorHandler>(this, Name), out reason);
        }

        public override bool QueryWaferMap(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<QueryWaferMappingHandler>(this, DeviceID), out reason);
        }

        public override bool QueryFOSBMode(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<QueryFOSBModeHandler>(this, DeviceID), out reason);
        }
        
        public bool Move(MovType fun, out string reason)
        {
            if (State != DeviceState.Idle)
            {
                reason = $"LoadPort is in {State}";
                return false;
            }
            reason = string.Empty;
            return execute(new handler<MovHandler>(this, Name, fun), out reason);
        }

        //public bool Query(QueryType type, out string reason)
        //{
        //    reason = string.Empty;
        //    return execute(new handler<QueryWaferMappingHandler>(Name), out reason);
        //}


        public bool SetEvent(EvtType fun, out string reason)
        {
            reason = string.Empty;
            return execute(new handler<SetEventHandler>(this, Name, fun), out reason);
        }



        public bool OnEvent(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                if (!execute(new handler<QueryStateHandler>(this, Name), out reason))
                    return false;

                return execute(new handler<QueryIndicatorHandler>(this, Name), out reason);
            }



        }

        private bool execute(IHandler handler1, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                if (_foregroundHandler == null)
                {
                    if (!handler1.Execute(ref _port))
                    {
                        reason = "Communication failed, please recovery it.";
                        return false;
                    }

                    _foregroundHandler = handler1;
                }
                else
                {
                    var type = handler1.GetType();
                    string name = type.Name;

                    if (type.GenericTypeArguments.Length > 0)
                        name = type.GenericTypeArguments[0].Name;

                    LOG.Info(string.Format("Add command {0}", name));
                    
                    _handlers.Enqueue(handler1);
                }
      
            }
            return true;
        }

        private bool execute(out string reason)
        {
            reason = string.Empty;
            lock(_locker)
            {
                if (_handlers.Count > 0)
                {
                    IHandler handler = _handlers.Dequeue();

                    if (!handler.Execute(ref _port))
                    {
                        reason = " communication failed, please recovery it.";
                        LOG.Error(reason);
                        EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】" + reason);

                        return false;
                    }

                    _foregroundHandler = handler;
                }
            }
            return true;
        }

        private void OnDataChanged(string package)
        {
            try
            {
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > min_len)
                    {
                        bool completed = false;
                        string resp = msg.Substring(3, msg.Length - min_len);

                        lock (_locker)
                        {
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _port, resp, out completed))
                            {
                                string reason = string.Empty;
                                if (!_foregroundHandler.IsBackground)
                                {
                                    _foregroundHandler = null;
                                    execute(out reason);
                                    OnActionDone(true);
 
                                }
                                else
                                {
                                    if (completed)
                                    {                                       
                                        _foregroundHandler = null;
                                        QueryState(out reason);
                                        if (_foregroundHandler is handler<HomeHandler>)
                                        {
                                            QueryIndicator(out reason);
                                        }
                                    }
                                }
                               
                            }
                            else
                            {
                                _eventHandler.OnMessage(ref _port, resp, out completed);  //process event
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException ex)
            {
                EV.PostWarningLog(Module, $"{Name} action failed, "+ex.Message);
                _foregroundHandler = null;
                OnError( $"{ Name} action failed");
            }
            catch (InvalidPackageException ex)
            {
                EV.PostWarningLog(Module, $"{Name} received unprocessed message, {ex.Message}" );
                OnError($"{Name} received unprocessed message");
            }
            catch (Exception ex)
            {
                LOG.Write($" {Name} has exception：" + ex.Message);
            }

        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
            _commErr = true;
            Initalized = false;
 
            EV.PostAlarmLog(Module, $"{Display} Communication error, {args.Reason}");

            OnError($"{Display} Communication error");
         }

        public  void OnCarrierNotPlaced()
        {
            _isPlaced = false;             
        }


        public  void OnCarrierNotPresent()
        {
            _isPresent = false;
            
        }
 
        public  void OnCarrierPlaced()
        {
            //if (SC.ContainsItem("CarrierInfo.InfoPadType") && (SC.GetValue<int>("CarrierInfo.InfoPadType") == 0))    //0 is TDK sensor
            //{
            //    int cindex = (IsInfoPadAOn ? 8 : 0) + (IsInfoPadBOn ? 4 : 0) + (IsInfoPadCOn ? 2 : 0) + (IsInfoPadDOn ? 1 : 0);
            //    InfoPadCarrierIndex = cindex;
            //    if (SC.ContainsItem($"CarrierInfo.CarrierName{cindex}"))
            //        InfoPadCarrierType = SC.GetStringValue($"CarrierInfo.CarrierName{cindex}");
            //    if (SC.ContainsItem($"CarrierInfo.{Name}CarrierIndex"))
            //        SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", cindex);
            //    LOG.Write($"{Name} detect carrier Type:{InfoPadCarrierType}");
            //}
            _isPlaced = true;
            _isPresent = true;
        }

        

        public  void OnCarrierPresent()
        {
            _isPresent = true;
           
        }

        public void OnSwitchKey1()
        {
            _isAccessSwPressed = true;
        }

        public void OnSwitchKey2()
        {
 
        }

        public void OffSwitchKey1()
        {
            _isAccessSwPressed = false;
        }

        public void OffSwitchKey2()
        {

        }


        public void SetIndicator(Indicator led, IndicatorState state)
        {
            SetIndicator(led, state, out string reason);
        }


        //impment E84 interface

        public bool ReadyForLoad()
        {
            return CassetteState == LoadportCassetteState.Absent;
        }
        public bool ReadyForUnload()
        {
            return CassetteState == LoadportCassetteState.Normal;
        }

        public bool FoupArrirved()
        {
            return CassetteState == LoadportCassetteState.Normal;
        }
        public bool FoupRemoved()
        {
            return CassetteState == LoadportCassetteState.Absent; 
        }
        public LPTransferState GetTransferState()
        {
            return  LPTransferState.IN_SERVICE;
        }
        public LPAccessMode GetAccessMode()
        {
            return  LPAccessMode.AUTO;
        }
        public LPReservedState GetReservedState()
        {
            return  LPReservedState.NOT_RESERVED;
        }

        public void LPTSTrans(LPTransferState lpts)
        {
            return;
        }

        public bool IsSystemAutoMode()
        {
            return false;
        }

        public void SetAccessMode(bool isauto)
        {
            return;
        }

        public void E84Retry()
        {
            throw new NotImplementedException();
        }

        public void SetCompleted()
        {
            return;
        }
    }
}
