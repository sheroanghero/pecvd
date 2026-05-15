using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using Brooks.WinSECS;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Fortrend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.Fortrend
{
    public class FortrendSmifPort : LoadPortBaseDevice, IConnection
    {
        //private IoSensor _sensorpodopen;
        //private IoTrigger _triggerSafetytolp;

        public WinSECS wshost { get; set; }
        public FortrendSMIFHostAgent ha { get; set; }
        public bool IsQueryComplete { get; set; }
        private bool _isLoaded;
        public override bool IsLoaded => _isLoaded;

        public bool IsAutoMode { get; set; }
        public override bool IsHomed { get; set; }
        public bool IsPodPresent { get; set; }
        public bool IsPodLocked { get; set; }
        public bool IsArmRetract { get; set; }
        public bool IsAlarm { get; set; }

        public FunctionEnum FunctionBeingDoneThisTime { get; set; }
        public CollisionStatusEnum CurrentCollisonStatus { get; set; }
        public PortReadyStatusEnum CurrentPortReadyStatus { get; set; }
        public bool IsLiftAtOverTravelLimit { get; set; }
        public bool IsLiftAtUpLimit { get; set; }
        public bool IsLiftAtStagePosition { get; set; }
        public WaferSeaterStateEnum CurrentWaferSeaterStatus { get; set; }

        public bool IsConnected { get; set; }

        // public override bool IsLoad { get; set; }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private DeviceTimer _reConnectdTimer = new DeviceTimer();

        private PeriodicJob _thread;

        private object _locker = new object();

        private bool _enableLog;
        private string _errorCode = "";
        private DeviceTimer _setStatusTimer = new DeviceTimer();
        private int SetStatusTime = 800;

        private DateTime _dtStartAction;
        private string _scRoot;

        public override WaferSize GetCurrentWaferSize()
        {
            return WaferSize.WS8;

        }

        public string Address { get; private set; }
        public FortrendSmifPort(string module, string name, string scRoot, RobotBaseDevice robot) : base(module, name, robot)
        {
            Module = module;
            Name = name;
            _scRoot = scRoot;
            string commPort = SC.GetStringValue($"LoadPort.{Name}.PortName");
            if (!Enum.TryParse(name, out ModuleName m))
                Enum.TryParse(module, out m);


            //Instantiate the WinSecs objects for the Host
            wshost = new WinSECS();

            //Set the PortType for RS232 communication
            wshost.PortType = SECS_PORT_TYPE.SECS1_SERIAL;
            //Properties that apply to all port types
            wshost.AutoDevice = true;
            wshost.DefaultDeviceID = 0;
            wshost.Secs1.IgnoreSytemBytes = false;
            wshost.MultipleOpen = true;
            wshost.MonitorEnabled = true;
            //Properties that apply to all SECS1 port types, which includes RS232
            wshost.Secs1.AcceptDupBlock = false;
            wshost.Secs1.Interleave = true;
            wshost.Secs1.RetryCount = 10;
            wshost.Secs1.SecsHost = true;
            wshost.Secs1.T1 = 1;
            wshost.Secs1.T2 = 10;
            wshost.Secs1.T3 = 30;
            wshost.Secs1.T4 = 45;

            wshost.Secs1.AutoBaud = false;
            wshost.Secs1.PortName = commPort;
            wshost.Secs1.BaudRate = 9600;
            //Set up the agents that deals with the SECS messages
            ha = new FortrendSMIFHostAgent(wshost, this);


            wshost.OpenPort(ha);
            if (wshost.PortIsOpen)
            {
                IsConnected = true;
            }
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            IsMapWaferByLoadPort = true;
        }

        public void OnErrorArrived(string alarmcode, string alarmdecri, string alarmtext)
        {
            EV.PostAlarmLog(Name, $"AlarmID:({alarmdecri}){_ErrorDict[alarmdecri]},AlarmText{$"({alarmtext})"}");
            switch (alarmcode)
            {
                case "1":
                    {
                        var code=  alarmtext.Replace("Error Code ", "|").Split('|')[1];
                        switch (code)
                        {
                            case "52":
                                IsError = true;
                                EV.PostAlarmLog(Name, $"{Name} Has Protrude Error.");
                                IsBusy = false;
                                CheckToPostMessage((int)LoadPortMsg.Error, null);
                                break;
                        }  
                    }
                    break;
                case "2":
                case "5":
                case "6":
                case "43":
                    if (_ErrorDict.ContainsKey(alarmdecri))
                        EV.PostAlarmLog(Name, $"AlarmID:({alarmdecri}){_ErrorDict[alarmdecri]},AlarmText{$"({alarmtext})"}");
                    OnError();
                    break;
                case "38":
                case "39":
                case "40":
                case "41":
                case "44":
                case "45":
                case "49":
                    if (_ErrorDict.ContainsKey(alarmdecri))
                        EV.PostWarningLog(Name, $"AlarmID:({alarmdecri}){_ErrorDict[alarmdecri]},AlarmText{$"({alarmtext})"}");
                    break;
            }
        }

        public void OnStausArrived(bool isplace, bool home, bool alarm, bool isbusy)
        {
            IsBusy = isbusy;
            IsPodPresent = isplace;
            IsIdle = home;
            if (isplace)
            {
                OnCarrierPresent();
                OnCarrierPlaced();
            }
        }

        public void OnEventArrived(string eventID)
        {
            EV.PostInfoLog(Name, $"EventId：{eventID}");
            if (int.TryParse(eventID, out int index))
            {
                FortrendSMIFEventEnum eventname = (FortrendSMIFEventEnum)index;

                switch (eventname)
                {
                    case FortrendSMIFEventEnum.PodRemoved:
                        OnCarrierNotPresent();
                        OnCarrierNotPlaced();
                        break;
                    case FortrendSMIFEventEnum.PodArrived:
                        OnCarrierPresent();
                        OnCarrierPlaced();
                        break;
                    case FortrendSMIFEventEnum.AutoMode:
                        IsAutoMode = true;
                        break;
                    case FortrendSMIFEventEnum.ManualMode:
                        IsAutoMode = false;
                        break;
                    case FortrendSMIFEventEnum.PowerUp:
                        break;
                    case FortrendSMIFEventEnum.LoadStart:
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} start to load");
                        break;
                    case FortrendSMIFEventEnum.LoadComplete:
                        _isLoaded = true;
                        DockState = FoupDockState.Docked;
                        break;
                    case FortrendSMIFEventEnum.LoadAborted:
                        OnError("LoadAborted");
                        break;
                    case FortrendSMIFEventEnum.UnloadStart:
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} start to unload");
                        break;
                    case FortrendSMIFEventEnum.UnloadComplete:
                        DockState = FoupDockState.Undocked;
                        _isLoaded = false;
                        IsIdle = true;
                        break;
                    case FortrendSMIFEventEnum.UnloadAborted:
                        OnError("UnloadAborted");
                        break;
                    case FortrendSMIFEventEnum.HomeStart:
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} start to home");
                        break;
                    case FortrendSMIFEventEnum.HomeComplete:
                        DockState = FoupDockState.Undocked;
                        IsHomed = true;
                        IsError = false;
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} Home Complete");
                        //if (!_sensorpodopen.Value) _triggerSafetytolp.SetTrigger(false, out _);
                        break;
                    case FortrendSMIFEventEnum.HomeAborted:
                        OnError("HomeAborted");
                        break;
                    case FortrendSMIFEventEnum.PortLockComplete:
                         IsIdle = true;
                         ClampState = FoupClampState.Close;
                         LOG.Write($"{LPModuleName} Lock Complete");
                        break;
                    case FortrendSMIFEventEnum.PortUnlockComplete:
                        IsIdle = true;
                        ClampState = FoupClampState.Open;
                        LOG.Write($"{LPModuleName} UnLock Complete");
                        break;
                }


            }
        }

        internal void ParseStatus3(byte[] statusValue)
        {

        }

        internal void ParseStatus2(byte[] statusValue)
        {
            CurrentPortReadyStatus = (PortReadyStatusEnum)statusValue[0];
        }

        public void ParseStatus1(byte[] statusValue)
        {
            IsAutoMode = statusValue[0] == 1;
            IsPodPresent = statusValue[1] == 1;

            if (IsPodPresent)
            {
                OnCarrierPresent();
                OnCarrierPlaced();
            }
            else
            {
                OnCarrierNotPresent();
                OnCarrierNotPlaced();
            }


            //IsHomed = statusValue[2] == 1;
            FunctionBeingDoneThisTime = (FunctionEnum)statusValue[3];
            CurrentCollisonStatus = (CollisionStatusEnum)statusValue[4];
            IsPodLocked = statusValue[5] == 1;



        }

        private static Dictionary<string, string> _ErrorDict = new Dictionary<string, string>()
        {       
                {"1","PLM Error" },
                {"2","Pod not present" },
                {"38","Latch error" },
                {"39","Pod Remove" },
                {"40","Mask Position Incorrect" },
                {"41","Inhibit Detected while Elevator going Down" },
                {"43","Protrusion Detected" },

                {"44","END EFFECTOR PROTRUSION" },
                {"45","PELLICLE ON TOP" },
                {"49","PLM not Ready" },
         };
        private static Dictionary<string, string> _RealyMessageDict = new Dictionary<string, string>()
            {
                {"OK","Comment accepted" },
                {"BUSY","Command rejected, LPI is busy" },
                {"ALARM","Command rejected, LPI in alarm state" },
                {"NO_POD","Command rejected, Pod not on LPI" },
                {"NOT_READY","Command rejected, Host interlock not enabled" },
                {"INVALID_ARG","Command rejected, at least one invalid parameter" },
                {"CANNOT_PERFORM","LPI not in the proper state to perform the Host command" },
                {"DENIED","Command rejected for other reason" },
            };

        private static Dictionary<string, string> _AlarmTimeCodeDict = new Dictionary<string, string>()
            {
                {"00","No alarm" },
                {"41","Alarm occurred during a fetch or open operation" },
                {"42","Alarm occurred during a cassette placement on the process tool" },
                {"43","Alarm occurred during a returning of the minienvironment to Home" },
                {"44","Alarm occurred during a retrieval of the cassette from the Tool" },
                {"45","Alarm occurred during a placement of the cassette on the Pod door" },
                {"46","Non-recoverable fatal error" }
            };

        private static Dictionary<string, string> _ErrorCodeDict = new Dictionary<string, string>()
            {
                {"00","No error" },
                {"01","Position following error" },
                {"02","LPI not Home" },
                {"03","LPI busy" },
                {"04","Pod removed/missing elevator door" },
                {"05","Aborted by user" },
                {"07","Protrusion sensor failure" },
                {"08","Slot sensor failure" },
                {"09","Wafer presence sensor failure" },
                {"10","Flash sensor failure" },
                {"11","Elevator over-travel limit trip" },
                {"13","Excessive wafer protrusion" },
                {"14","System internal time-out" },
                {"15","Servo command error" },
                {"16","Pod door open time-out" },
                {"17","Pod door close time-out" },
                {"18","Pod hold-down open time-out" },
                {"19","Pod hold-down close time-out" },
                {"20","Wafer seater failed to move toward wafer" },
                {"21","Wafer seater failed to return to Home" },
                {"22","Elevator failed to reach target position" },
                {"24","System error" },
                {"27","Loss of configuration data" },
                {"28","Cassette not present" },
                {"29","Loss of air flow" },
                {"31","Gripper open time out" },
                {"32","Gripper close time out" },
                {"33","Interprocessor communication error" },
                {"34","Gripper overtravel" },
                {"35","Cassette found during unload" },
            };



        internal void OnAlarmArrived(string alarmData)
        {
            EV.PostAlarmLog(Name, $"alarmdata：{alarmData}");
            if (alarmData.Length != 4)
                return;

            string alarmTimeCode = alarmData.Substring(0, 2);
            string alarmInfoCode = alarmData.Substring(2, 2);

            AlarmOccurTime = alarmTimeCode + "=" + _AlarmTimeCodeDict[alarmTimeCode];
            AlarmInfo = alarmInfoCode + "=" + _ErrorCodeDict[alarmInfoCode];

            EV.PostAlarmLog(Module, $"{Module} {AlarmOccurTime}, for {AlarmInfo}");
            OnError();
            IsAlarm = true;
        }



        public override void Reset()
        {
            _trigError.RST = true;

            _trigCommunicationError.RST = true;
            _trigRetryConnect.RST = true;
            CheckToPostMessage((int)LoadPortMsg.Reset, null);



        }

        protected override bool fStartReset(object[] param)
        {
            lock (_locker)
            {
                SECSTransactionBuilder.BuildS1F5(0).Send(wshost);
            }
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsError = false;
            IsBusy = false;
            return true;
        }


        #region Command Functions


        protected override bool fStartInit(object[] param)
        {
            LOG.Write($"{LPModuleName} start to home,isHome:{IsHomed}");
            IsHomed = false;
            lock (_locker)
            {
                SECSTransactionBuilder.BuildS1F5(0).Send(wshost);
                SECSTransactionBuilder.BuildS2F21((int)FortrendSMIFRCMDEnum.GoHome).Send(wshost);

            }
            _dtStartAction = DateTime.Now;
            return true;
        }

        protected override bool fMonitorInit(object[] param)
        {
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitHome))
            {
                OnError("InitTimeout");
                return true;
            }
            IsBusy = false;
            return IsHomed;
        }






        protected override bool fStartLoad(object[] objs)
        {
            ResetRoutine();
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            LoadCassette((int)LpStepEnum.Step1, TimelimitAction, OnError);
            if (ExecuteResult.Item1)
            {
                return false;
            }

            QueryMap((int)LpStepEnum.Step2, TimelimitAction, OnError);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            
            return true;
        }


        protected override bool fStartExecute(object[] param)
        {
            try
            {
                switch (param[0].ToString())
                {

                    case "Unclamp":
                        IsIdle = false;
                        lock (_locker)
                        {
                            SECSTransactionBuilder.BuildS2F21((int)FortrendSMIFRCMDEnum.HostToUnlockPort).Send(wshost);
                        }
                        break;
                    case "Clamp":
                        IsIdle = false;
                        lock (_locker)
                        {
                            SECSTransactionBuilder.BuildS2F21((int)FortrendSMIFRCMDEnum.HostToLockPort).Send(wshost);
                        }
                        break;

                    case "MapWafer":
                        lock (_locker)
                        {
                            if (!IsMapWaferByLoadPort)
                            {
                                if (MapRobot != null)
                                    return MapRobot.WaferMapping(LPModuleName, out _);
                                return false;
                            }

                        }
                        break;
                    case "QueryState":
                        IsQueryComplete = false;
                        lock (_locker)
                        {
                            SECSTransactionBuilder.BuildS1F5(0).Send(wshost);
                        }



                        break;

                }
                _dtStartAction = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }

        protected override bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitHome))
            {
                OnError("InitTimeout");
                return true;
            }
            return IsIdle;


        }


        public override bool Stop(out string reason)
        {
            lock (_locker)
            {

            }

            return base.Stop(out reason);
        }


        public bool QueryMapStatus(out string reason)
        {
            //IsBusy = true;
            reason = string.Empty;
            lock (_locker)
            {
                SECSTransactionBuilder.BuildS1F5(2).Send(wshost);
            }

            IsIdle = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            return true;
        }
        public bool QueryPortStatus(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                SECSTransactionBuilder.BuildS1F5(0).Send(wshost);
            }
            //IsIdle = false;
            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            return true;
        }

        protected override bool fStartUnload(object[] param)
        {
            lock (_locker)
            {
                SECSTransactionBuilder.BuildS2F21((int)FortrendSMIFRCMDEnum.StartUnload).Send(wshost);
            }
            IsIdle = false;
            _dtStartAction = DateTime.Now;
            return true;
        }
        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("UnloadTimeout");
                return true;
            }



            return IsIdle;
        }
        public void OnCarrierNotPlaced()
        {
            _isPlaced = false;
            ConfirmRemoveCarrier();
        }


        public void OnCarrierNotPresent()
        {
            _isPresent = false;
            //ConfirmRemoveCarrier();
        }

        public void OnCarrierPlaced()
        {
            _isPlaced = true;
            ConfirmAddCarrier();

        }


        public void OnCarrierPresent()
        {
            _isPresent = true;
            //ConfirmAddCarrier();
        }


        public override void OnSlotMapRead(string slotMap)
        {
            CurrentSlotMapResult = slotMap.Replace("0", "?").Replace("2", "0");
            int slotErrorcout = 0;

            for (int i = 0; i < slotMap.Length; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                
                WaferInfo wafer = null;
                switch (slotMap[i])
                {
                    case '2':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal, WaferSize.WS8);
                        WaferManager.Instance.CheckWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '4':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed, WaferSize.WS8);
                        WaferManager.Instance.CheckWaferSize(LPModuleName, i, GetCurrentWaferSize());

                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostAlarmLog(Name,$"{Name} Slot:{i+1} occur crossed. ");
                        //NotifyWaferError(Name, i, WaferStatus.Crossed);
                        slotErrorcout=slotErrorcout + 1;
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double, WaferSize.WS8);
                        WaferManager.Instance.CheckWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Double);
                        break;
                    case '0':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown, WaferSize.WS8);
                        WaferManager.Instance.CheckWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Unknown);
                        break;
                }
            }
            if (slotErrorcout == 0) 
                MapError = false;
            else MapError = true;

            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = CurrentSlotMapResult;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierIndex"] = InfoPadCarrierIndex;
            dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            if (CurrentSlotMapResult.Contains("4"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMapCrossedWafer);
                EV.Notify(AlarmLoadPortMappingError);
                OnError("MappingError");
            }
            if (CurrentSlotMapResult.Contains("3"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMapDoubleWafer);
                EV.Notify(AlarmLoadPortMappingError);
                OnError("MappingError");
            }
            if (CurrentSlotMapResult.Contains("W"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMapDoubleWafer);
                EV.Notify(AlarmLoadPortMappingError);
                OnError("MappingError");
            }
            if (CurrentSlotMapResult.Contains("?"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMapUnknownWafer);
                EV.Notify(AlarmLoadPortMappingError);
                OnError("MappingError");
            }

            if (LPCallBack != null)
                LPCallBack.MappingComplete(_carrierId, CurrentSlotMapResult);


            _isMapped = true;




        }


        #endregion

        #region Properties


        public string AlarmOccurTime { get; private set; }
        public string AlarmInfo { get; private set; }


        #endregion



        public override void Terminate()
        {
        }


        public override void Monitor()
        {
            if (!IsConnected && ha != null && (_reConnectdTimer.IsIdle() || _reConnectdTimer.GetElapseTime() > 10000))
            {
                wshost.ClosePort();
                wshost.OpenPort(ha);
                _reConnectdTimer.Start(0);
            }
            if (IsConnected)
            {
                _reConnectdTimer.Stop();
            }
        }
        public override bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            if (_isPresent && _isPlaced && _isLoaded && IsReady() && _isMapped)
                return true;

            return false;
        }

        public override bool Connect()
        {
            return true;
        }
       
        public bool Disconnect()
        {
            return true;
        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }

        public override bool ReadCarrierID(int offset = 0, int length = 16)
        {
            return ReadCarrierIDByIndex();
        }
        public override bool ReadCarrierIDByIndex(int offset = 0, int length = 16, int index = 0)
        {
            IsBusy = true;
            SECSTransaction secsTransaction = new SECSTransaction(100, 121);

            secsTransaction.Primary.Root.Name = "S100F121";
            secsTransaction.Primary.Root.AddNew("Param1", "");
            secsTransaction.Primary.Root.Item("Param1").Format = SECS_FORMAT.U4;
            secsTransaction.Primary.Root.Item("Param1").Value = 320724;

            secsTransaction.ReplyExpected = true;

            secsTransaction.Send(wshost);

            return true;
        }

        //public override bool WriteCarrierID(string cid, int offset = 0, int length = 16, int index = 0)
        //{
        //    return ReadCarrierIDByIndex();
        //}
        public override bool WriteCarrierID(string cid, int offset = 0, int length = 16)
        {
            IsBusy = true;
            //string cids = $"CST:C082259 TYPE:CST25_AU LOT:{cid} QTY:24          FLOW: 033000     STATUS: HOLD PRI:3 - 1         PD: 0418A CSRLOT:         CLEAN: 2021 / 11 / 14 19:57:50       EQPGRP: DUMMY STAGE:CP - TEST1  RECIPE: DUMMY EMP:N. ";
            SECSTransaction secsTransaction = new SECSTransaction(100, 119);
            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("L4", "");
            secsTransaction.Primary.Root.Item("L4").Format = SECS_FORMAT.LIST;

            secsTransaction.Primary.Root.Item("L4").AddNew("NAME", "ECID NAME");
            secsTransaction.Primary.Root.Item("L4").Item("NAME").Format = SECS_FORMAT.U4;
            secsTransaction.Primary.Root.Item("L4").Item("NAME").Value = 50801;

            secsTransaction.Primary.Root.Item("L4").AddNew("VALUE", "ECID VALUE");
            secsTransaction.Primary.Root.Item("L4").Item("VALUE").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L4").Item("VALUE").Value = 1;

            secsTransaction.Primary.Root.Item("L4").AddNew("DateTime", "ECID DateTime");
            secsTransaction.Primary.Root.Item("L4").Item("DateTime").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L4").Item("DateTime").Value = "2002031112121200";

            secsTransaction.Primary.Root.Item("L4").AddNew("DataItem", "ECID DataItem");
            secsTransaction.Primary.Root.Item("L4").Item("DataItem").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L4").Item("DataItem").Value = cid;
            secsTransaction.ReplyExpected = true;
            secsTransaction.Send(wshost);          

            return true;
        }
        public override bool WriteCarrierIDByIndex(string cid, int offset = 0, int length = 16, int index = 0)
        {   
            IsBusy = true;
            string cids = $"CST:C080164     TYPE:CST25_AU   LOT:{cid}      QTY:12          FLOW:033000     STATUS:WAITING  PRI:3-1         PD:0418A        CSRLOT:         CLEAN:2021/11/10 19:22:04       EQPGRP:DUMMY    STAGE:CP-TEST1  RECIPE:DUMMY    EMP:N           ;";// $"CST:C082259 TYPE:CST25_AU LOT:{cid} QTY:24          FLOW: 033000     STATUS: HOLD PRI:3 - 1         PD: 0418A CSRLOT:         CLEAN: 2021 / 11 / 14 19:57:50       EQPGRP: DUMMY STAGE:CP - TEST1  RECIPE: DUMMY EMP:N. ";
            SECSTransaction secsTransaction = new SECSTransaction(100, 119);
            secsTransaction.Primary.Root.Name = "HCS";
            secsTransaction.Primary.Root.AddNew("L4", "");
            secsTransaction.Primary.Root.Item("L4").Format = SECS_FORMAT.LIST;

            secsTransaction.Primary.Root.Item("L4").AddNew("NAME", "ECID NAME");
            secsTransaction.Primary.Root.Item("L4").Item("NAME").Format = SECS_FORMAT.U4;
            secsTransaction.Primary.Root.Item("L4").Item("NAME").Value = 50801;

            secsTransaction.Primary.Root.Item("L4").AddNew("VALUE", "ECID VALUE");
            secsTransaction.Primary.Root.Item("L4").Item("VALUE").Format = SECS_FORMAT.U1;
            secsTransaction.Primary.Root.Item("L4").Item("VALUE").Value = 1;

            secsTransaction.Primary.Root.Item("L4").AddNew("DateTime", "ECID DateTime");
            secsTransaction.Primary.Root.Item("L4").Item("DateTime").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L4").Item("DateTime").Value = "2002031112121200";

            secsTransaction.Primary.Root.Item("L4").AddNew("DataItem", "ECID DataItem");
            secsTransaction.Primary.Root.Item("L4").Item("DataItem").Format = SECS_FORMAT.ASCII;
            secsTransaction.Primary.Root.Item("L4").Item("DataItem").Value = cids;
            secsTransaction.ReplyExpected = true;
            secsTransaction.Send(wshost);

            return true;
        }

        public void LoadCassette(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog("System", $"{LPModuleName} start loading");
                var reason = string.Empty;
                _isLoaded = false;
                lock (_locker)
                {
                    SECSTransactionBuilder.BuildS2F21((int)FortrendSMIFRCMDEnum.StartLoad).Send(wshost);
                }

                return true;

            }, () =>
            {
                if (_isLoaded)
                    return Result.DONE;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error("load failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error("load timeout");

                }
            }
        }


        protected void QueryMap(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog("System", $"{LPModuleName} start to query map");
                var reason = string.Empty;
                _isMapped = false;
                lock (_locker)
                {
                    SECSTransactionBuilder.BuildS1F3(new int[] { 1, 2 }).Send(wshost);
                }
                return true;

            }, () =>
            {
                if (_isMapped)
                    return Result.DONE;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error("map failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error("map timeout");

                }
            }
        }

        public enum LpStepEnum
        {
            Step1,
            Step2,
            Step3,
            Step4,
            Step5,
            Step6,
            Step7,
            Step8,
            Step9,
            Step10,
            Step11,
            Step12,
        }



        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        private enum STATE
        {
            IDLE,
            WAIT,
        }

        public int TokenId
        {
            get { return _id; }
        }
        private int _id;         //step index

        private int _currentTokenId = -1;
        /// <summary>
        /// already done steps
        /// </summary>
        private Stack<int> _steps = new Stack<int>();

        private STATE state;    //step state //idel,wait,

        //loop control
        private int loop = 0;
        private int loopCount = 0;
        private int loopID = 0;

        private DeviceTimer timer = new DeviceTimer();

        public int LoopCounter { get { return loop; } }
        public int LoopTotalTime { get { return loopCount; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        protected Tuple<bool, Result> ExecuteResult;

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
            _currentTokenId = -1;
            ExecuteResult = Tuple.Create(false, Result.DONE);
        }

        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!Acitve(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            loop = loopCount;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                loopID = idx;
                loopCount = count;

                next();
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (++loop >= loopCount)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    loop = 0;
                    loopCount = 0;  // Loop 结束时，当前loop和loop总数都清零

                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                next(loopID);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    else if (startRet == Result.DONE)
                    {
                        next();
                        return Tuple.Create(true, Result.DONE);
                    }
                    state = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    next();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }


        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    state = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    next();

                    if (bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<Result?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            Result? bExecute = Result.RUN;
            if (bActive)
            {
                //if (idx != _currentTokenId && ExecuteResult.Item1) return ExecuteResult;

                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        ExecuteResult = Tuple.Create(bActive, Result.FAIL);
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                    _currentTokenId = idx;
                }
                bExecute = check();

                if (bExecute == null)
                {
                    ExecuteResult = Tuple.Create(bActive, Result.FAIL);
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute == Result.DONE)       //检查Success, next
                    {
                        next();
                        ExecuteResult = Tuple.Create(true, Result.RUN);
                        return Tuple.Create(true, Result.RUN);
                    }
                    if (bExecute == Result.Succeed)       //检查Success, next
                    {
                        next();
                        ExecuteResult = Tuple.Create(true, Result.RUN);
                        return Tuple.Create(true, Result.RUN);
                    }
                    if (bExecute == Result.FAIL)       //检查 Fail 直接返回Fail
                    {
                        ExecuteResult = Tuple.Create(true, Result.FAIL);
                        return Tuple.Create(true, Result.FAIL);
                    }
                }

                if (timer.IsTimeout())
                {
                    ExecuteResult = Tuple.Create(true, Result.TIMEOUT);
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    rt.Start();
                    state = STATE.WAIT;
                }

                Result ret = rt.Monitor();

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                //if (_currentTokenId != idx && !ExecuteResult.Item1) return ExecuteResult;
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        ExecuteResult = Tuple.Create(true, Result.FAIL);
                        return Tuple.Create(true, Result.FAIL);
                    }
                    _currentTokenId = idx;
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
        #endregion


        private Tuple<bool, bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                //if (ExecuteResult.Item1) return Tuple.Create(true, true);
                bExecute = func();
                if (bExecute)
                {
                    next();
                }
            }

            return Tuple.Create(bActive, bExecute);
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = Acitve(id);

            bool bExecute = false;
            if (bActive)
            {
                if (ExecuteResult.Item1) return Tuple.Create(true, true);
                bExecute = func();
                next();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>

        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = Acitve(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool, Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    ExecuteResult = Tuple.Create(true, Result.FAIL);
                    return Tuple.Create(true, Result.FAIL);
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool Acitve(int id) //
        {
            if (_steps.Contains(id))
                return false;

            this._id = id;
            return true;
        }

        private void next()
        {
            _steps.Push(this._id);
            state = STATE.IDLE;
        }

        private void next(int step)   //loop
        {
            while (_steps.Pop() != step) ;

            state = STATE.IDLE;
        }


        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                return true;
            }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {

                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }


    }


    public enum FunctionEnum
    {
        Idle,
        Load,
        Unload,
    }

    public enum CollisionStatusEnum
    {
        NoCollision,
        Collision,
        Impropersquatduringcassettepickupduringload,
        Impropersquatduringcassetteplacementduringload,
        Impropersquatduringcassettepickupduringunload,
        Impropersquatduringcassetteplacementduringunload,
        Improperwaferseating,
        Cassettedetectedduringunload,
    }
    public enum PortReadyStatusEnum
    {
        NotReady,
        ReadyToLoad,
        ReadyToUnload,
        ReadyToLoadUnload,
    }

    public enum PortStateEnum
    {
        Unlock,
        Lock,
        Other,
    }
    public enum WaferSeaterStateEnum
    {
        SeaterAtHome,
        SeaterOut,
        None,
    }



}
