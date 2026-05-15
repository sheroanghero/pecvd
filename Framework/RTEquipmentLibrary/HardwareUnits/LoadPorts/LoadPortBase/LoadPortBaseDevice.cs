using Aitex.Core.RT.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using Aitex.Core.RT.Device.Unit;
using System.Threading;
using static Aitex.Core.RT.Device.Unit.IOE84Passive;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using Aitex.Core.RT.Fsm;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase
{
    public abstract class LoadPortBaseDevice : Entity, IEntity, ILoadPort
    {
        public LoadPortBaseDevice(string module, string name, RobotBaseDevice robot = null, IE84CallBack e84 = null) : base()
        {
            Module = module;
            Name = name;
            LPModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
            _robot = robot;

            if (_robot != null)
                MapRobot.OnSlotMapRead += MapRobot_OnSlotMapRead;
            _lpE84Callback = e84;


            InitializeLP();
        }
        public virtual void InformProcessComplete()
        {

        }
        public virtual void InformProcessStart()
        {

        }
        public virtual int TimelimitAction
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.TimeLimitAction"))
                    return SC.GetValue<int>($"LoadPort.{Name}.TimeLimitAction");
                return 90;
            }
        }
        public virtual int TimelimitHome
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.TimeLimitLoadportHome"))
                    return SC.GetValue<int>($"LoadPort.{Name}.TimeLimitLoadportHome");
                return 90;
            }
        }

        public virtual int TimeLimitNoOperation
        {
            get
            {
                if(SC.ContainsItem($"LoadPort.{Name}.TimeLimitNoOperation"))
                    return SC.GetValue<int>($"LoadPort.{Name}.TimeLimitNoOperation");
                return 0;
            }
        }

        
        public virtual bool IsAutoReadCarrierID
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.EnableAutoCarrierIdRead") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.EnableAutoCarrierIdRead") : true;
            }
        }
        public virtual bool IsAutoUnloadAfterProcess
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.EnableAutoUnload") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.EnableAutoUnload") : false;
            }
        }
        public virtual bool IsCloseDoorWhenIdle
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.CloseDoorWhenIdle") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.CloseDoorWhenIdle") : false;
            }
        }
        public virtual bool IsBypassCarrierIDReader
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.BypassCarrierIDReader") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.BypassCarrierIDReader") : false;

            }
        }
        public virtual bool IsAutoDetectCarrierType
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{LPModuleName}.AutoDetectCarrierType") ?
                        SC.GetValue<bool>($"LoadPort.{LPModuleName}.AutoDetectCarrierType") : true;
            }
        }
        public virtual LightCurtainHandleEnum CurrentLightCurtainHandle
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.LightCurtainHandle"))
                    return (LightCurtainHandleEnum)SC.GetValue<int>($"LoadPort.{LPModuleName}.LightCurtainHandle");
                return LightCurtainHandleEnum.Bypass;
            }
        }

        public virtual bool IsNeedMapOnUnload
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.IsNeedMapOnUnload"))
                    return SC.GetValue<bool>($"LoadPort.{LPModuleName}.IsNeedMapOnUnload");
                return false;
            }
        }


        public virtual bool IsKeepClampAfterUnload
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}"))
                    return SC.GetValue<bool>($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}");

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.KeepClampedAfterUnloadCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.KeepClampedAfterUnloadCarrier");

                return false;
            }
        }
        public virtual bool IsCarrierEnabled
        {
            get
            {
                

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.EnableCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.EnableCarrier");

                return true;
            }
        }
        public virtual bool IsRemoteOperationPermit { get; set; } = true;
        public virtual int[] WaferMapThickness { get; set; }
        public virtual bool IsHomed { get; set; }
        public virtual Tuple<int, int> WaferThicknessTolerance  //um
        {
            get
            {
                int lowlimit = 0;
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ThicknessLowLimit"))
                    lowlimit = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ThicknessLowLimit");
                int highlimit = 5000;
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ThicknessHighLimit"))
                    highlimit = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ThicknessHighLimit");
                return new Tuple<int, int>(lowlimit, highlimit);
            }
        }//um
        public virtual int SlotPositionBaseLine   //um
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.SlotPositionBaseLine"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.SlotPositionBaseLine");
                return 0;
            }
        }
        public virtual int SlotPitch   //um
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.SlotPitch"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.SlotPitch");
                return 10000;
            }
        }

        public virtual int WaferCenterDeviationLimit   //um
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.WaferCenterDeviationLimit"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.WaferCenterDeviationLimit");
                return 1000;
            }
        }
        public virtual E84SigState LPE84SigState 
        { 
            get
            {
                if (_lpE84Callback != null)
                {
                    return new E84SigState()
                    {
                        LightCurtain = _lpE84Callback.GetE84SignalState(E84SignalID.LightCurtain),
                        CS_0 = _lpE84Callback.GetE84SignalState(E84SignalID.CS_0),
                        CS_1 = _lpE84Callback.GetE84SignalState(E84SignalID.CS_1),
                        AM_AVBL = _lpE84Callback.GetE84SignalState(E84SignalID.AM_AVBL),
                        VALID = _lpE84Callback.GetE84SignalState(E84SignalID.VALID),
                        TR_REQ = _lpE84Callback.GetE84SignalState(E84SignalID.TR_REQ),
                        BUSY = _lpE84Callback.GetE84SignalState(E84SignalID.BUSY),
                        COMPT = _lpE84Callback.GetE84SignalState(E84SignalID.COMPT),
                        CONT = _lpE84Callback.GetE84SignalState(E84SignalID.CONT),
                        L_REQ = _lpE84Callback.GetE84SignalState(E84SignalID.L_REQ),
                        U_REQ = _lpE84Callback.GetE84SignalState(E84SignalID.U_REQ),
                        VA = _lpE84Callback.GetE84SignalState(E84SignalID.VA),
                        READY = _lpE84Callback.GetE84SignalState(E84SignalID.READY),
                        VS_0 = _lpE84Callback.GetE84SignalState(E84SignalID.VS_0),
                        VS_1 = _lpE84Callback.GetE84SignalState(E84SignalID.VS_1),
                        HO_AVBL = _lpE84Callback.GetE84SignalState(E84SignalID.HO_AVBL),
                        ES = _lpE84Callback.GetE84SignalState(E84SignalID.ES),
                    };
                }
                return _lpE84SigState;
    
            }
            set { _lpE84SigState = value; }
        }

        private E84SigState _lpE84SigState;
        public bool SetCarrierIndex(int indexValue, out string reason)
        {
            reason = "";
            if (IsAutoDetectCarrierType)
            {
                reason = "Auto Detect is On";
                EV.PostWarningLog(Module, $"{Name} can not set carrier type index when auto detection is enable.");
                return false;
            }
            if (indexValue >= ValidCarrierTypeCount)
            {
                reason = "Invalid Carrier Index Value";
                EV.PostWarningLog(Module, $"{Name} can not set carrier type index with {indexValue} larger than {ValidCarrierTypeCount-1}.");
                return false;
            }

            if (SC.ContainsItem($"LoadPort.{LPModuleName}.CarrierIndex"))
            {
                SC.SetItemValue($"LoadPort.{LPModuleName}.CarrierIndex", indexValue);
            }
            InfoPadCarrierIndex = indexValue;
            EV.PostInfoLog(Module, $"{Name} set carrier type index to {InfoPadCarrierIndex}");
            return true;
        }


        public bool SetCarrierName(string carrierName, out string reason)
        {
            reason = "";
            for (int i = 0; i < ValidCarrierTypeCount; i++)
            {
                
                if (SC.ContainsItem($"CarrierInfo.EnableCarrier{i}") && !SC.GetValue<bool>($"CarrierInfo.EnableCarrier{i}"))
                    continue;
                string carriertype = "";
                if (SC.ContainsItem($"CarrierInfo.CarrierName{i}"))
                {
                    carriertype = SC.GetStringValue($"CarrierInfo.CarrierName{i}");
                }

                if (SC.ContainsItem($"CarrierInfo.Carrier{i}.CarrierName"))
                {
                    carriertype = SC.GetStringValue($"CarrierInfo.Carrier{i}.CarrierName");
                }


                if (IsAutoDetectCarrierType)
                {
                    reason = "AutoDetectOn";
                    return false;
                }
                if (carrierName == carriertype)
                {
                    EV.PostInfoLog(Module, $"{Name} set carrier Name to {carrierName}.");
                    return SetCarrierIndex(i, out reason);
                }
            }
            reason = $"{carrierName} not existed.";
            return false;

        }

        public int ValidCarrierTypeCount { get; set; } = 16;
        public virtual Tuple<int, string>[] ValidCarrierTypeList
        {
            get
            {
                List<Tuple<int, string>> ret = new List<Tuple<int, string>>();

                for (int i = 0; i < ValidCarrierTypeCount; i++)
                {
                    if (!SC.ContainsItem($"CarrierInfo.CarrierName{i}"))
                        continue;
                    if (SC.ContainsItem($"CarrierInfo.EnableCarrier{i}") && !SC.GetValue<bool>($"CarrierInfo.EnableCarrier{i}"))
                        continue;
                    if (!SC.ContainsItem($"CarrierInfo.CarrierName{i}"))
                        continue;
                    if (!SC.ContainsItem($"CarrierInfo.CarrierWaferSize{i}"))
                        continue;
                    string carriertype = "Carrier Type: " + SC.GetStringValue($"CarrierInfo.CarrierName{i}")
                        + (SC.ContainsItem($"CarrierInfo.CarrierWaferSize{i}") ? ($";WaferSize:WS{SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{i}")}") : "");
                    ret.Add(new Tuple<int, string>(i, carriertype));
                }

                for (int i = 0; i < ValidCarrierTypeCount; i++)
                {
                    if (!SC.ContainsItem($"CarrierInfo.Carrier{i}.CarrierName"))
                        continue;
                    if (SC.ContainsItem($"CarrierInfo.Carrier{i}.EnableCarrier") && !SC.GetValue<bool>($"CarrierInfo.Carrier{i}.EnableCarrier"))
                        continue;
                    if (!SC.ContainsItem($"CarrierInfo.Carrier{i}.CarrierName"))
                        continue;
                    if (!SC.ContainsItem($"CarrierInfo.Carrier{i}.CarrierWaferSize"))
                        continue;
                    string carriertype = "Carrier Type: " + SC.GetStringValue($"CarrierInfo.Carrier{i}.CarrierName")
                        + (SC.ContainsItem($"CarrierInfo.Carrier{i}.CarrierWaferSize") ? ($";WaferSize:WS{SC.GetValue<int>($"CarrierInfo.Carrier{i}.CarrierWaferSize")}") : "");
                    ret.Add(new Tuple<int, string>(i, carriertype));
                }



                return ret.ToArray();
            }
        }
        public virtual DispatchLPType DspLpType
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.DispatchLpType"))
                {
                    int inttype = SC.GetValue<int>($"LoadPort.{Name}.DispatchLpType");
                    return (DispatchLPType)inttype;
                }
                return DispatchLPType.NA;
            }
        }


        private void MapRobot_OnSlotMapRead(ModuleName arg1, string arg2)
        {
            if (arg1 == LPModuleName)
            {
                OnSlotMapRead(arg2);
                NoteTransferStop();
            }

        }

        private void InitializeLP()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();
            RegisterOperation();
        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<LoadPortBaseDevice>(Name + ".LPStateMachine", (int)LoadPortStateEnum.Init, 50);
            AnyStateTransition(LoadPortMsg.Error, fError, LoadPortStateEnum.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)LoadPortStateEnum.Error);
            AnyStateTransition(LoadPortMsg.Abort, fStartAbort, LoadPortStateEnum.Init);


            Transition(LoadPortStateEnum.Init, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);
            Transition(LoadPortStateEnum.TransferBlock, LoadPortMsg.Reset, fStartReset, LoadPortStateEnum.Resetting);

            Transition(LoadPortStateEnum.Resetting, LoadPortMsg.ResetComplete, fCompleteReset, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Resetting, LoadPortMsg.ActionDone, fCompleteReset, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Init, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.Init, fStartInit, LoadPortStateEnum.Initializing);

            Transition(LoadPortStateEnum.Error, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Init, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Initializing, LoadPortMsg.ActionDone, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Initializing, LoadPortMsg.InitComplete, fCompleteInit, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Init, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);
            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.StartHome, fStartHomet, LoadPortStateEnum.Homing);

            Transition(LoadPortStateEnum.Homing, LoadPortMsg.ActionDone, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Homing, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Homing, FSM_MSG.TIMER, fMonitorHome, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Init, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Error, LoadPortMsg.HomeComplete, fCompleteHome, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Load, fStartLoad, LoadPortStateEnum.Loading);
            Transition(LoadPortStateEnum.Loading, LoadPortMsg.ActionDone, fCompleteLoad, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Loading, LoadPortMsg.LoadComplete, fCompleteLoad, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Loading, FSM_MSG.TIMER, fMonitorLoad, LoadPortStateEnum.Idle);


            Transition(LoadPortStateEnum.Idle, LoadPortMsg.Unload, fStartUnload, LoadPortStateEnum.Unloading);
            Transition(LoadPortStateEnum.Unloading, LoadPortMsg.ActionDone, fCompleteUnload, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Unloading, LoadPortMsg.UnloadComplete, fCompleteUnload, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Unloading, FSM_MSG.TIMER, fMonitorUnload, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartExecute, fStartExecute, LoadPortStateEnum.Executing);
            Transition(LoadPortStateEnum.Executing, LoadPortMsg.ActionDone, fCompleteExecute, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Executing, LoadPortMsg.MoveComplete, fCompleteExecute, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.Executing, FSM_MSG.TIMER, fMonitorExecuting, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartReadData, fStartRead, LoadPortStateEnum.ReadingData);
            Transition(LoadPortStateEnum.ReadingData, LoadPortMsg.ActionDone, fCompleteRead, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.ReadingData, LoadPortMsg.ReadComplete, fCompleteRead, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.ReadingData, FSM_MSG.TIMER, fMonitorReadingData, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartWriteData, fStartWrite, LoadPortStateEnum.WrittingData);
            Transition(LoadPortStateEnum.WrittingData, LoadPortMsg.ActionDone, fCompleteWrite, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.WrittingData, LoadPortMsg.MoveComplete, fCompleteWrite, LoadPortStateEnum.Idle);

            Transition(LoadPortStateEnum.Idle, LoadPortMsg.StartAccess, fStartAccess, LoadPortStateEnum.TransferBlock);
            Transition(LoadPortStateEnum.TransferBlock, LoadPortMsg.CompleteAccess, fCompleteAccess, LoadPortStateEnum.Idle);
            //Transition(LoadPortStateEnum.TransferBlock, LoadPortMsg.ActionDone, fCompleteAccess, LoadPortStateEnum.Idle);
            Transition(LoadPortStateEnum.TransferBlock, FSM_MSG.TIMER, fMonitorTransferBlock, LoadPortStateEnum.TransferBlock);
        }

        protected virtual bool fMonitorReadingData(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorTransferBlock(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorExecuting(object[] param)
        {
            return false;
        }
        
        protected virtual bool fMonitorInit(object[] param)
        {
            return false;
        }
        protected virtual bool fMonitorHome(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorUnload(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorLoad(object[] param)
        {
            return false;
        }

        protected virtual bool fCompleteAccess(object[] param)
        {
            return true;
        }

        protected virtual bool fStartAccess(object[] param)
        {
            return true;
        }

        protected virtual bool fCompleteHome(object[] param)
        {
            return true;
        }

        protected virtual bool fStartHomet(object[] param)
        {
            return true;
        }

        protected virtual bool fCompleteWrite(object[] param)
        {
            return true;
        }

        protected abstract bool fStartWrite(object[] param);

        protected virtual bool fCompleteRead(object[] param)
        {
            return true;
        }

        protected abstract bool fStartRead(object[] param);

        protected virtual bool fCompleteMove(object[] param)
        {
            return true;
        }

        protected abstract bool fStartExecute(object[] param);

        protected virtual bool fCompleteExecute(object[] param)
        {
            return true;
        }

        protected abstract bool fStartUnload(object[] param);
        protected virtual bool fCompleteUnload(object[] param)
        {
            OnUnloaded();
            return true;
        }
        protected virtual bool fCompleteLoad(object[] param)
        {
            OnLoaded();
            return true;
        }

        protected abstract bool fStartLoad(object[] param);

        protected virtual bool fCompleteInit(object[] param)
        {
            return true;
        }

        protected abstract bool fStartInit(object[] param);

        protected virtual bool fCompleteReset(object[] param)
        {
            return true;
        }

        protected abstract bool fStartReset(object[] param);

        protected virtual bool fStartAbort(object[] param)
        {
            return true;
        }

        protected virtual bool fError(object[] param)
        {
            return true;
        }

        private void SubscribeDeviceOperation()
        {

        }

        private void SubscribeOperation()
        {

        }

        protected virtual void SubscribeDataVariable()
        {
            
            WaferManager.Instance.SubscribeLocation(LPModuleName, 25);

            CarrierManager.Instance.SubscribeLocation(LPModuleName.ToString());

            DATA.Subscribe(Name, "IsPresent", () => _isPresent);
            DATA.Subscribe(Name, "IsPlaced", () => _isPlaced);
            DATA.Subscribe(Name, "IsClamped", () => ClampState == FoupClampState.Close);
            DATA.Subscribe(Name, "IsDocked", () => DockState == FoupDockState.Docked);
            DATA.Subscribe(Name, "IsDoorOpen", () => DoorState == FoupDoorState.Open);
            DATA.Subscribe(Name, "IsAccessSwPressed", () => IsAccessSwPressed);
            DATA.Subscribe(Name, "CarrierId", () => _carrierId);
            DATA.Subscribe(Name, "LPLotID", () => _lplotID);
            DATA.Subscribe(Name, "IsMapped", () => _isMapped);
            DATA.Subscribe(Name, "IsAutoDetectCarrierType", () => IsAutoDetectCarrierType);
            DATA.Subscribe(Name, "ValidCarrierTypeList", () => ValidCarrierTypeList);



            DATA.Subscribe($"{Name}.LoadportState", () => CurrentState.ToString());
            DATA.Subscribe($"{Name}.Status", () => CurrentState.ToString());

            //DATA.Subscribe($"{Name}.LoadportBusy", () =>  );
            DATA.Subscribe($"{Name}.LoadportError", () => ErrorCode);
            DATA.Subscribe($"{Name}.CassetteState", () => CassetteState);
            DATA.Subscribe($"{Name}.FoupClampState", () => ClampState);
            DATA.Subscribe($"{Name}.FoupDockState", () => DockState);
            DATA.Subscribe($"{Name}.FoupDoorState", () => DoorState);
            DATA.Subscribe($"{Name}.FoupDoorPosition", () => DoorPosition);

            DATA.Subscribe($"{Name}.SlotMap", () => SlotMap);
            DATA.Subscribe($"{Name}.WaferMapThickness", () => WaferMapThickness);
            DATA.Subscribe($"{Name}.IndicatiorLoad", () => IndicatiorLoad);
            DATA.Subscribe($"{Name}.IndicatiorUnload", () => IndicatiorUnload);
            DATA.Subscribe($"{Name}.IndicatiorPresence", () => IndicatiorPresence);
            DATA.Subscribe($"{Name}.IndicatiorPlacement", () => IndicatiorPlacement);
            DATA.Subscribe($"{Name}.IndicatiorAlarm", () => IndicatorAlarm);
            DATA.Subscribe($"{Name}.IndicatiorAccessAuto", () => IndicatiorAccessAuto);
            DATA.Subscribe($"{Name}.IndicatiorAccessManual", () => IndicatiorAccessManual);
            DATA.Subscribe($"{Name}.IndicatiorOpAccess", () => IndicatiorOpAccess);
            DATA.Subscribe($"{Name}.IndicatiorStatus1", () => IndicatiorStatus1);
            DATA.Subscribe($"{Name}.IndicatiorStatus2", () => IndicatiorStatus2);

            DATA.Subscribe($"{Name}.CasstleType", () => (int)CasstleType);
            DATA.Subscribe($"{Name}.InfoPadCarrierType", () => SpecCarrierType);
            DATA.Subscribe($"{Name}.InfoPadCarrierTypeInformation", () => SpecCarrierInformation);


            DATA.Subscribe($"{Name}.InfoPadCarrierIndex", () => InfoPadCarrierIndex);
            DATA.Subscribe($"{Name}.CarrierWaferSize", () => GetCurrentWaferSize().ToString());
            DATA.Subscribe($"{Name}.IsError", () => CurrentState == LoadPortStateEnum.Error);
            DATA.Subscribe($"{Name}.PreDefineWaferCount", () => PreDefineWaferCount);
            DATA.Subscribe($"{Name}.IsVerifyPreDefineWaferCount", () => IsVerifyPreDefineWaferCount);
            DATA.Subscribe($"{Name}.ValidSlotsNumber", () => ValidSlotsNumber);

            EV.Subscribe(new EventItem("Event", EventCarrierArrived, "Carrier arrived on port"));
            EV.Subscribe(new EventItem("Event", EventCarrierRemoved, "Carrier removed from port"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdRead, "Carrier ID read"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdReadFailed, "Carrier ID read failed"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWrite, "Carrier ID write"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWriteFailed, "Carrier ID write failed"));
            EV.Subscribe(new EventItem("Event", EventSlotMapAvailable, "Slot map available"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnloaded, "Carrier unloaded"));
            EV.Subscribe(new EventItem("Event", EventCarrierloaded, "Carrier loaded"));

            EV.Subscribe(new EventItem("Event", EventCarrierOpened, "Carrier door opened successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierClosed, "Carrier door closed successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierLocationChange, "Carrier location changed successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierClamped, "Carrier is clamped on port successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnclamped, "Carrier is unclamped on port successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierDocked, "Carrier is docked successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierUndocked, "Carrier is undocked v"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdReadFailed1, "Carrier ID read failed on port"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnknowCid, "Carrier ID is unknow"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdRead1, "Carrier ID is read successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierArrived1, "Carrier arrived on port successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierRemoved1, "Carrier removed from port successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWrite1, "Carrier ID is written successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWriteFailed1, "Carrier ID is written failed"));
            EV.Subscribe(new EventItem("Event", EventMapComplete, "Carrier is mapped successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnloaded1, "Carrier is unloaded successfully"));
            EV.Subscribe(new EventItem("Event", EventCarrierLoaded1, "Carrier is loaded successfully"));


            //SubscribeAlarm();
            IndicatorStateFeedback = new IndicatorState[20];


            if (_lpE84Callback != null)
            {
                _lpE84Callback.OnE84ActiveSignalChange += _lpE84Callback_OnE84ActiveSignalChange;
                _lpE84Callback.OnE84PassiveSignalChange += _lpE84Callback_OnE84PassiveSignalChange;

                _lpE84Callback.OnE84HandOffComplete += _lpE84Callback_OnE84HandOffComplete;
                _lpE84Callback.OnE84HandOffStart += _lpE84Callback_OnE84HandOffStart;
                _lpE84Callback.OnE84HandOffTimeout += _lpE84Callback_OnE84HandOffTimeout;
            }
        }


        private void SubscribeAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortError, $"Load Port {Name} occurred error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortCommunicationError, $"Load Port {Name}error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortWaferProtrusion, $"Load Port {Name} wafer protrusion error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortLoadTimeOut, $"Load Port {Name} load timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortLoadFailed, $"Load Port {Name} load failed.", EventLevel.Alarm, EventType.EventUI_Notify));


            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortUnloadTimeOut, $"Load Port {Name} unload timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortUnloadFailed, $"Load Port {Name} unload failed.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMappingError, $"Load Port {Name} mapping error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMappingTimeout, $"Load Port {Name} mapping Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmCarrierIDReadError, $"Load Port {Name} occurred error when read carrier ID.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmCarrierIDWriteError, $"Load Port {Name} occurred error when write carrierID.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapCrossedWafer, $"Load Port {Name} mapped crossed wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapDoubleWafer, $"Load Port {Name} mapped double wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortMapUnknownWafer, $"Load Port {Name} mapped unknown wafer.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortCarrierIDIndexConfigError, $"Load Port {Name} carrierID reader configuration is invalid.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortHomeFailed, $"Load Port {Name} occurred error when home.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortHomeTimeout, $"Load Port {Name} home timeout.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP1Timeout, $"Load Port {Name} occurred TP1 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP2Timeout, $"Load Port {Name} occurred TP2 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP3Timeout, $"Load Port {Name} occurred TP3 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP4Timeout, $"Load Port {Name} occurred TP4 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP5Timeout, $"Load Port {Name} occurred TP5 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmLoadE84TP6Timeout, $"Load Port {Name} occurred TP6 timeout during loading foup.", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP1Timeout, $"Load Port {Name} occurred TP1 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP2Timeout, $"Load Port {Name} occurred TP2 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP3Timeout, $"Load Port {Name} occurred TP3 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP4Timeout, $"Load Port {Name} occurred TP4 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP5Timeout, $"Load Port {Name} occurred TP5 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmUnloadE84TP6Timeout, $"Load Port {Name} occurred TP6 timeout during unloading foup.", EventLevel.Alarm, EventType.EventUI_Notify));
        }

        public LoadPortStateEnum CurrentState => (LoadPortStateEnum)fsm.State;
        public LoadPortStateEnum PreviousState => (LoadPortStateEnum)fsm.PrevState;
        public ModuleName LPModuleName { get; protected set; }

        public virtual int InfoPadCarrierIndex { get; set; }
        public virtual int InfoPadSensorIndex { get; set; }
        public string SpecPortName { get; set; } = string.Empty;
        public string Module { get; set; }
        public string Name { get; set; }

        protected event Action<bool> ActionDone;
        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public virtual bool IsAccessAuto { get; set; }
        public bool IsBusy { get; set; }
        public virtual bool IsIdle
        {
            get; set;
        }
        public virtual bool IsReady()
        {
            return fsm.State == (int)LoadPortStateEnum.Idle && !IsBusy;
        }
        public IndicatorState IndicatiorLoad { get; set; }
        public IndicatorState IndicatiorUnload { get; set; }
        public IndicatorState IndicatiorPresence { get; set; }
        public IndicatorState IndicatorAlarm { get; set; }
        public IndicatorState IndicatiorPlacement { get; set; }
        public IndicatorState IndicatiorOpAccess { get; set; }
        public IndicatorState IndicatiorAccessAuto { get; set; }
        public IndicatorState IndicatiorAccessManual { get; set; }
        public IndicatorState IndicatiorStatus1 { get; set; }
        public IndicatorState IndicatiorStatus2 { get; set; }
        public IndicatorState IndicatiorManualMode { get; set; }
        public IndicatorState IndicatiorClampUnclamp { get; set; }
        public IndicatorState IndicatiorDockUndock { get; set; }

        public static Dictionary<IndicatorType, Indicator> LoadPortIndicatorLightMap =
            new Dictionary<IndicatorType, Indicator>()
            {
                {IndicatorType.Load, Indicator.LOAD},
                {IndicatorType.Unload, Indicator.UNLOAD},
                {IndicatorType.AccessAuto, Indicator.ACCESSAUTO },
                {IndicatorType.AccessManual, Indicator.ACCESSMANUL},
                {IndicatorType.Alarm, Indicator.ALARM},
                {IndicatorType.Presence, Indicator.PRESENCE},
                {IndicatorType.Placement, Indicator.PLACEMENT},
                {IndicatorType.Status1, Indicator.RESERVE1},
                {IndicatorType.Status2, Indicator.RESERVE2},
            };

        public IndicatorState[] IndicatorStateFeedback { get; set; }
        public virtual FoupClampState ClampState { get; set; }
        public virtual FoupDockState DockState { get; set; }
        public virtual FoupDoorState DoorState { get; set; }
        public virtual FoupDoorPostionEnum DoorPosition { get; set; }
        public virtual bool IsVacuumON { get; set; }
        public virtual CasstleType CasstleType { get; set; }
        public virtual string SlotMap
        {
            get
            {
                WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
                string slot = "";
                for (int i = 0; i < 25; i++)
                {
                    slot += wafers[i].IsEmpty ? "0" : "1";
                }

                return slot;
            }
        }
        public virtual string CurrentSlotMapResult
        {
            get;set;
        }

        //Hirata
        public int WaferCount { get; set; }
        public string LoadPortType { get; protected set; }

        public virtual bool IsInfoPadAOn { get; set; }
        public virtual bool IsInfoPadBOn { get; set; }
        public virtual bool IsInfoPadCOn { get; set; }
        public virtual bool IsInfoPadDOn { get; set; }
        public virtual bool IsWaferProtrude { get; set; }


        public virtual string SpecCarrierType
        {
            get; set;

        }

        public virtual string SpecCarrierInformation
        {
            get
            {
                if (_isPresent)
                    return "Index:" + InfoPadCarrierIndex.ToString()
                        + $"\r\nInfoPad:{InfoPadSensorIndex}"
                        + $"\r\nType:{SpecCarrierType}"
                        + $"\r\nSize:{GetCurrentWaferSize()}"
                        + (IsVerifyPreDefineWaferCount ? ("\r\n" + "Pre-Count:" + PreDefineWaferCount.ToString()) : "")
                        + $"\r\n{(IsMapped? "Mapped":"Not Mapped")}";

                return "";
            }
        }

        public virtual LoadportCassetteState CassetteState
        {
            get;
            set;
        }
        public virtual int PreDefineWaferCount
        {
            get; set;
        }

        public virtual bool IsVerifyPreDefineWaferCount
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount"))
                    return SC.GetValue<bool>($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount");
                return false;
            }
        }

        public virtual WaferSize GetCurrentWaferSize()
        {
            int intwz = 0;
            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize"))
                intwz = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize");
            else
                return WaferSize.WS12;

            switch (intwz)
            {
                case 0:
                    return WaferSize.WS0;
                case 1:
                    return WaferSize.WS0;
                case 2:
                    return WaferSize.WS2;
                case 3:
                    return WaferSize.WS3;
                case 4:
                    return WaferSize.WS4;
                case 5:
                    return WaferSize.WS5;
                case 6:
                    return WaferSize.WS6;
                case 7:
                case 8:
                    return WaferSize.WS8;
                case 12:
                    return WaferSize.WS12;
                default:
                    return WaferSize.WS0;
            }

        }
        public virtual bool IsMapWaferByLoadPort { get; set; }

        public bool IsPresent
        {
            get { return _isPresent; }
        }
        public bool IsPlacement
        {
            get { return _isPlaced; }
        }
        public bool IsMapped
        {
            get { return _isMapped; }
        }

        public virtual bool IsLoaded
        {
            get
            {
                return DockState == FoupDockState.Docked && DoorState == FoupDoorState.Open && DoorPosition == FoupDoorPostionEnum.Down;

            }

        }
        public bool IsComplete { get; set; }

        public bool IsAccessSwPressed
        {
            get { return _isAccessSwPressed; }
        }
        public virtual bool IsError { get; set; }
        public virtual string ErrorCode { get; set; } = "";
        public bool MapError { get; protected set; }
        public bool ReadCarrierIDError { get; set; }
        /// <summary>
        /// 是否处于FOSB模式
        /// </summary>
        /// 
        public virtual bool IsRequestFOSBMode
        {
            get;set;
        }

        public bool IsFosbModeActual { get; set; }
        public string CarrierId
        {
            get { return _carrierId; }
        }
        public string LPLotID
        {
            get { return _lplotID; }
        }

        public RD_TRIG PresentTrig
        {
            get { return _presentTrig; }
        }

        public RD_TRIG PlacedtTrig
        {
            get { return _placetTrig; }
        }

        public RD_TRIG AccessSwPressedTrig
        {
            get { return _accessSwPressedTrig; }
        }

        public RD_TRIG ClampedTrig
        {
            get { return _clampTrig; }
        }

        public RD_TRIG DockTrig
        {
            get { return _dockTrig; }
        }

        public RD_TRIG DoorTrig
        {
            get { return _doorTrig; }
        }

        protected virtual bool _isPresent { get; set; }
        protected virtual bool _isPlaced { get; set; }
        protected bool _isDocked { get => DockState == FoupDockState.Docked; }
        protected bool _isAccessSwPressed;
        protected string _carrierId = string.Empty;
        protected string _carrierIdToBeWrite = string.Empty;
        protected string _rfid;
        protected string _lplotID;

        protected bool _isMapped;
        private readonly RD_TRIG _presentTrig = new RD_TRIG();
        private readonly RD_TRIG _placetTrig = new RD_TRIG();
        private readonly RD_TRIG _accessSwPressedTrig = new RD_TRIG();
        private readonly RD_TRIG _clampTrig = new RD_TRIG();
        private readonly RD_TRIG _dockTrig = new RD_TRIG();
        private readonly RD_TRIG _doorTrig = new RD_TRIG();

        public R_TRIG TrigWaferProtrude = new R_TRIG();
        public int PortID
        {
            get => (int)LPModuleName;
        }

        protected string EventCarrierArrived = "CARRIER_ARRIVED";
        protected string EventCarrierIdRead = "CARRIER_ID_READ";   
        protected string EventCarrierIdReadFailed = "CARRIER_ID_READ_FAILED";
        protected string EventCarrierIdWrite = "CARRIER_ID_WRITE";
        protected string EventCarrierIdWriteFailed = "CARRIER_ID_WRITE_FAILED";
        protected string EventSlotMapAvailable = "SLOT_MAP_AVAILABLE";
        protected string EventCarrierRemoved = "CARRIER_REMOVED";
        protected string EventCarrierUnloaded = "CARRIER_UNLOADED";
        protected string EventCarrierloaded = "CARRIR_DOCK_COMPLETE";
        protected string EventLPHomed = "LP_HOMED";

        
        

        protected string EventCarrierOpened = "CR_OPN";
        protected string EventCarrierClosed = "CR_CLS";
        protected string EventCarrierLocationChange = "CLCHG";
        protected string EventCarrierClamped = "CCLMP";
        protected string EventCarrierUnclamped = "CUCLMP";
        protected string EventCarrierDocked = "CDOCK";
        protected string EventCarrierUndocked = "CUDOCK";
        protected string EventCarrierIdReadFailed1 = "CIDRFAIL";
        protected string EventCarrierUnknowCid = "UNKNOWN_CID";
        protected string EventCarrierIdRead1 = "CIDREAD";

        protected string EventCarrierArrived1 = "CR_ARRIVE";
        protected string EventCarrierRemoved1 = "CR_REMOVE";
        protected string EventCarrierIdWrite1 = "CID_WRITE";
        protected string EventCarrierIdWriteFailed1 = "CID_WRITE_FAIL";
        protected string EventMapComplete = "CR_MAPPING_COMPLETE";
        protected string EventCarrierUnloaded1 = "CR_UNLOADED";
        protected string EventCarrierLoaded1 = "CR_LOADED";


        //protected string PORT_ID = "PORT_ID";
        //protected string CAR_ID = "CAR_ID";
        //protected string SLOT_MAP = "SLOT_MAP";
        //protected string PORT_CTGRY = "PORT_CTGRY";
        //protected string RF_ID = "RF_ID";
        //protected string PORT_CARRIER_TYPE = "PORT_CARRIER_TYPE";



        public string AlarmLoadPortError { get => LPModuleName.ToString() + "Error"; }
        public string AlarmLoadPortCommunicationError { get => LPModuleName.ToString() + "CommunicationError"; }
        public string AlarmLoadPortWaferProtrusion { get => LPModuleName.ToString() + "WaferProtrusion"; }
        public string AlarmLoadPortLoadTimeOut { get => LPModuleName.ToString() + "LoadTimeOut"; }
        public string AlarmLoadPortLoadFailed { get => LPModuleName.ToString() + "LoadFailed"; }
        public string AlarmLoadPortUnloadTimeOut { get => LPModuleName.ToString() + "UnloadTimeOut"; }
        public string AlarmLoadPortUnloadFailed { get => LPModuleName.ToString() + "UnloadFailed"; }
        public string AlarmLoadPortMappingError { get => LPModuleName.ToString() + "MappingError"; }
        public string AlarmLoadPortUnloadMappingError { get => LPModuleName.ToString() + "UnloadMappingError"; }
        public string AlarmLoadPortMappingTimeout { get => LPModuleName.ToString() + "MappingTimeout"; }
        public string AlarmCarrierIDReadError { get => LPModuleName.ToString() + "CarrierIDReadFail"; }
        public string AlarmCarrierIDWriteError { get => LPModuleName.ToString() + "CarrierIDWriteFail"; }

        public string AlarmLoadPortHomeFailed { get => LPModuleName.ToString() + "HomeFailed"; }
        public string AlarmLoadPortHomeTimeout { get => LPModuleName.ToString() + "HomeTimeout"; }

        public string AlarmLoadE84TP1Timeout { get => LPModuleName.ToString() + "E84LoadTP1TimeOut"; }
        public string AlarmLoadE84TP2Timeout { get => LPModuleName.ToString() + "E84LoadTP2TimeOut"; }
        public string AlarmLoadE84TP3Timeout { get => LPModuleName.ToString() + "E84LoadTP3TimeOut"; }
        public string AlarmLoadE84TP4Timeout { get => LPModuleName.ToString() + "E84LoadTP4TimeOut"; }
        public string AlarmLoadE84TP5Timeout { get => LPModuleName.ToString() + "E84LoadTP5TimeOut"; }
        public string AlarmLoadE84TP6Timeout { get => LPModuleName.ToString() + "E84LoadTP6TimeOut"; }

        public string AlarmUnloadE84TP1Timeout { get => LPModuleName.ToString() + "E84UnloadTP1TimeOut"; }
        public string AlarmUnloadE84TP2Timeout { get => LPModuleName.ToString() + "E84UnloadTP2TimeOut"; }
        public string AlarmUnloadE84TP3Timeout { get => LPModuleName.ToString() + "E84UnloadTP3TimeOut"; }
        public string AlarmUnloadE84TP4Timeout { get => LPModuleName.ToString() + "E84UnloadTP4TimeOut"; }
        public string AlarmUnloadE84TP5Timeout { get => LPModuleName.ToString() + "E84UnloadTP5TimeOut"; }
        public string AlarmUnloadE84TP6Timeout { get => LPModuleName.ToString() + "E84UnloadTP6TimeOut"; }

        public string AlarmLoadPortMapCrossedWafer => LPModuleName.ToString() + "MapCrossedWafer";
        public string AlarmLoadPortMapDoubleWafer => LPModuleName.ToString() + "MapDoubleWafer";
        public string AlarmLoadPortMapUnknownWafer => LPModuleName.ToString() + "MapUnknownWafer";
        public string AlarmLoadPortCarrierIDIndexConfigError => LPModuleName.ToString() + "LoadPortCarrierIDIndexConfigError";

        public string AlarmLoadPortUnloadMapCrossedWafer => LPModuleName.ToString() + "UnloadMapCrossedWafer";
        public string AlarmLoadPortUnloadMapDoubleWafer => LPModuleName.ToString() + "UnloadMapDoubleWafer";
        public string AlarmLoadPortUnloadMapUnknownWafer => LPModuleName.ToString() + "UnloadMapUnknownWafer";
     





        private IE87CallBack _lpcallback = null;
        public IE87CallBack LPCallBack
        {
            get { return _lpcallback; }
            set { _lpcallback = value; }
        }
        private IE84CallBack _lpE84Callback = null;
        public IE84CallBack LPE84Callback
        {
            get { return _lpE84Callback; }
            set
            {
                _lpE84Callback = value;
                _lpE84Callback.OnE84ActiveSignalChange += _lpE84Callback_OnE84ActiveSignalChange;
                _lpE84Callback.OnE84PassiveSignalChange += _lpE84Callback_OnE84PassiveSignalChange;

                _lpE84Callback.OnE84HandOffComplete += _lpE84Callback_OnE84HandOffComplete;
                _lpE84Callback.OnE84HandOffStart += _lpE84Callback_OnE84HandOffStart;
                _lpE84Callback.OnE84HandOffTimeout += _lpE84Callback_OnE84HandOffTimeout;
            }
        }    

        public virtual CIDReaderBaseDevice[] CIDReaders
        {
            get;
            set;
        }

        public CIDReaderBaseDevice CurrentReader
        {
            get
            {
                if (CIDReaders == null) return null;
                if (CIDReaders.Length > CurrentCidIndex)
                    return CIDReaders[CurrentCidIndex];
                return null;
            }
        }

        private RobotBaseDevice _robot = null;
        public RobotBaseDevice MapRobot
        {
            get { return _robot; }
            set { _robot = value; }
        }



        public LoadPortBaseDevice()
        {

        }



        private void _lpE84Callback_OnE84PassiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        private void _lpE84Callback_OnE84HandOffTimeout(E84Timeout _timeout, string LoadorUnload)
        {
            NoteTransferStop();
            Thread.Sleep(100);
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
            if (LoadorUnload == "Load")
            {
                switch (_timeout)
                {
                    case E84Timeout.TP1:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP1 timeout";

                        EV.Notify(AlarmLoadE84TP1Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP1 timeout");

                        break;
                    case E84Timeout.TP2:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP2 timeout";

                        EV.Notify(AlarmLoadE84TP2Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP2 timeout");

                        break;
                    case E84Timeout.TP3:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP3 timeout";

                        EV.Notify(AlarmLoadE84TP3Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP3 timeout");

                        break;
                    case E84Timeout.TP4:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP4 timeout";

                        EV.Notify(AlarmLoadE84TP4Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP4 timeout");

                        break;
                    case E84Timeout.TP5:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP5 timeout";

                        EV.Notify(AlarmLoadE84TP5Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Load TP5 timeout");

                        break;
                    default:
                        break;
                }
            }
            if (LoadorUnload == "Unload")
            {
                switch (_timeout)
                {
                    case E84Timeout.TP1:

                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP1 timeout";


                        EV.Notify(AlarmUnloadE84TP1Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP1 timeout");

                        break;
                    case E84Timeout.TP2:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP2 timeout";

                        EV.Notify(AlarmUnloadE84TP2Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP2 timeout");

                        break;
                    case E84Timeout.TP3:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP3 timeout";

                        EV.Notify(AlarmUnloadE84TP3Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP3 timeout");

                        break;
                    case E84Timeout.TP4:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP4 timeout";

                        EV.Notify(AlarmUnloadE84TP4Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP4 timeout");

                        break;
                    case E84Timeout.TP5:
                        dvid = new SerializableDictionary<string, object>();
                        dvid["AlarmDescription"] = $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP5 timeout";

                        EV.Notify(AlarmUnloadE84TP5Timeout,dvid);
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} with carrier:{CarrierId} occurred E84 Unload TP5 timeout");
                        break;
                    default:
                        break;
                }
            }
        }

        private void _lpE84Callback_OnE84HandOffStart(string arg2)
        {
            NoteTransferStart();
            OnE84HandOffStart(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84HandOffComplete(string arg2)
        {
            NoteTransferStop();
            Thread.Sleep(100);
            OnE84HandOffComplete(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84ActiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        public virtual bool Connect()
        {
            return true;
        }

        private void RegisterOperation()
        {
            OP.Subscribe($"{Name}.SetPreDefineWaferCount", (string cmd, object[] param) =>
            {

                PreDefineWaferCount = Convert.ToInt32(param[0]);
                EV.PostInfoLog(Module, $"{Name} set pre define wafer count to {PreDefineWaferCount}");
                return true;
            });

            OP.Subscribe($"{Name}.SetInfoPadIndex", (string cmd, object[] param) =>
            {
                int indexValue = Convert.ToInt32(param[0]);

                return SetCarrierIndex(indexValue, out _);
            });


            OP.Subscribe($"{Name}.ReadCarrierIDByIndex", (string cmd, object[] param) =>
            {
                if (!ReadCarrierIDByIndex(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.WriteCarrierIDByIndex", (string cmd, object[] param) =>
            {
                if (!WriteCarrierIDByIndex(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not write carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start write carrierID");
                return true;
            });



            OP.Subscribe($"{Name}.ReadCarrierID", (string cmd, object[] param) =>
            {
                if (!ReadCarrierID(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.ReadCarrierIDByLP", (string cmd, object[] param) =>
            {
                string reason;
                if (!ReadCarrierIDByLP(param, out reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not read carrier ID:{reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read carrierID");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportHome", (string cmd, object[] param) =>
            {
                if (!Home(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start home");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportHomeModule", (string cmd, object[] param) =>
            {
                if (!HomeModule(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start home");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportReset", (string cmd, object[] param) =>
            {
                if (!LoadportReset(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not reset, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start reset");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportStop", (string cmd, object[] param) =>
            {
                if (!Stop(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not stop, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} stop");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoad", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithoutMap", (string cmd, object[] param) =>
            {
                if (!LoadWithoutMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load without map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load without map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithMap", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load with map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load with map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnload", (string cmd, object[] param) =>
            {
                if (!Unload(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unload, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unload");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportUnloadWithMap", (string cmd, object[] param) =>
            {
                if (!UnloadWithMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unload, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unload");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportClamp", (string cmd, object[] param) =>
            {
                if (!Clamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not clamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start clamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnclamp", (string cmd, object[] param) =>
            {
                if (!Unclamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unclamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unclamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoor", (string cmd, object[] param) =>
            {
                if (!OpenDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoorNoMap", (string cmd, object[] param) =>
            {
                if (!OpenDoorNoMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportCloseDoor", (string cmd, object[] param) =>
            {
                if (!CloseDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not close door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start close door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportDock", (string cmd, object[] param) =>
            {
                if (!Dock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not dock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start dock");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUndock", (string cmd, object[] param) =>
            {
                if (!Undock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not undock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start undock");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportQueryState", (string cmd, object[] param) =>
            {
                if (!QueryState(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportQueryLED", (string cmd, object[] param) =>
            {
                if (!QueryIndicator(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query led state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportSetLED", (string cmd, object[] param) =>
            {
                int light = (int)param[0];
                int state = (int)param[1];
                if (!SetIndicator((Indicator)light, (IndicatorState)state, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set led state, {reason}");
                    return true;
                }

                EV.PostInfoLog(Module, $"{Name} start set led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportMap", (string cmd, object[] param) =>
            {
                if (!LoadPortMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start loadport map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start loadport map");
                return true;

            });
            OP.Subscribe($"{Name}.MapWafer", (string cmd, object[] param) =>
            {
                if (!MapWafer(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start map");
                return true;
            });

            OP.Subscribe($"{Name}.SetCassetteType", (string cmd, object[] param) =>
            {
                if (!SetCassetteType(param, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set type, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} cassette type have set to {CasstleType}");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportForceHome", (string cmd, object[] param) =>
            {
                if (!ForceHome(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start force home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start force home");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOSBMode", (string cmd, object[] param) =>
            {
                if (!FOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOUPMode", (string cmd, object[] param) =>
            {
                if (!FOUPMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }


                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportQueryFOSBMode", (string cmd, object[] param) =>
            {
                if (!QueryFOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOUP mode, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.SetLoadportLotID", (string cmd, object[] param) =>
            {
                if (!SetLotID(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} set lotID for loadport.");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportExecuteCommand", (string cmd, object[] param) =>
            {
                if (!LoadPortExecute(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.E84Retry", (string cmd, object[] param) =>
            {
                if (!LoadPortE84Retry(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.E84Complete", (string cmd, object[] param) =>
            {
                if (!LoadPortE84Complete(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });
            OP.Subscribe($"{Name}.ManualSetCarrierID", (string cmd, object[] param) =>
            {
                OnCarrierIdRead(param[0].ToString());

                EV.PostInfoLog(Module, $"{Name} execute command {cmd[0].ToString()}.");
                return true;
            });

        }



        public virtual bool LoadPortE84Complete(object[] param, out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.Complete();
            }
            reason = "";
            return true;
        }

        public virtual bool LoadPortE84Retry(object[] param, out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.ResetE84();
            }
            reason = "";
            return true;
        }

        private bool LoadPortMap(out string reason)
        {
            reason = "";
            if (!IsMapWaferByLoadPort)
            {
                if (_robot == null)
                {
                    reason = "No mapping tools";
                    return false;
                }
                if (!_robot.IsReady())
                {
                    reason = "Robot not ready";
                    return false;
                }
                if (!IsEnableMapWafer(out reason))
                {
                    EV.PostAlarmLog("System", $"{LPModuleName} with carrier:{CarrierId} is not ready:{reason}.");
                    return false;
                }
                return _robot.WaferMapping(LPModuleName, out reason);
            }
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "MapWafer" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public bool LoadPortExecute(object[] para, out string reason)
        {
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, para);
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            reason = "";
            return true;
        }

        public bool LoadportReset(out string reason)
        {
            reason = "";
            return CheckToPostMessage((int)LoadPortMsg.Reset, null);
        }

        public bool SetLotID(object[] param, out string reason)
        {
            reason = "";
            if (param == null || param.Length == 0) return false;
            _lplotID = param[0].ToString();
            return true;
        }

        private bool SetCassetteType(object[] param, out string reason)
        {
            reason = "";
            if (param.Length != 1)
            {
                reason = "Invalid setting parameter.";
                return false;
            }
            CasstleType = (CasstleType)int.Parse(param[0].ToString());
            return true;
        }
        public virtual int ValidSlotsNumber
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierSlotsNumber"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierSlotsNumber");

                return ValidEndSlotIndex - ValidStartSlotIndex + 1;

            }
        }
        public virtual int ValidStartSlotIndex
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.ValidStartSlot"))
                    return SC.GetValue<int>($"LoadPort.{LPModuleName}.ValidStartSlot") - 1;
                return 0;
            }
        }
        public virtual int ValidEndSlotIndex
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.ValidEndSlot"))
                    return SC.GetValue<int>($"LoadPort.{LPModuleName}.ValidEndSlot") - 1;

                if (SC.ContainsItem($"LoadPort.{LPModuleName}.ValidSlotNumber"))
                    return SC.GetValue<int>($"LoadPort.{LPModuleName}.ValidSlotNumber") + ValidStartSlotIndex - 1;
                return 24;
            }
        }
        public virtual bool Load(out string reason)
        {
            if (!IsEnableLoad(out reason))
            {
                EV.PostAlarmLog("LoadPort", $"Can't execute load command due to {reason}");
                if (reason == "CarrierNotEnabled")
                    EV.Notify($"{LPModuleName}CarrierNotEnabled");
                return false;
            }
            reason = "";
            bool ret;
            if (IsMapWaferByLoadPort)
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithMap" });
            else
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool LoadWithCloseDoor(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret;
            if (IsMapWaferByLoadPort)
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithCloseDoor" });
            else
                ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMapWithCloseDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool LoadWithMap(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool LoadWithoutMap(out string reason)
        {
            if (!IsEnableLoad(out reason))
                return false;
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Load, new object[] { "LoadWithoutMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }
        public virtual bool MapWafer(out string reason)
        {
            reason = "";
            if (!IsMapWaferByLoadPort)
            {
                if (_robot == null)
                {
                    reason = "No mapping tools";
                    EV.PostAlarmLog("System", $"{LPModuleName} with carrier:{CarrierId} is not ready:{reason}.");
                    return false;
                }
                if (!_robot.IsReady())
                {
                    reason = "Robot not ready";
                    EV.PostAlarmLog("System", $"{LPModuleName} with carrier:{CarrierId} is not ready:{reason}.");
                    return false;
                }
                if (!IsEnableMapWafer(out reason))
                {
                    EV.PostAlarmLog("System", $"{LPModuleName} with carrier:{CarrierId} is not ready:{reason}.");
                    return false;
                }
                return _robot.WaferMapping(LPModuleName, out reason);
            }
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "MapWafer" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool QueryWaferMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartReadData, new object[] { "MapData" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool QueryFOSBMode(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartReadData, new object[] { "FOSBMode" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool QueryCarrierID(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartReadData, new object[] { "CarrierID" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的Dock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Dock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的FOSBUnDock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBUnDock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "UnDock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的开门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorOpen(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "FOSBDoorOpen" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的关门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorClose(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "FOSBDoorClose" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的门下移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool DoorDown(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorDown" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// FOSB模式下的门上移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool DoorUp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorUp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
                return false;
            }
            return true;
        }

        public virtual bool SetIndicator(IndicatorType light, IndicatorState state)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                SetIndicator(LoadPortIndicatorLightMap[light], state, out string _);
                return true;
            }
            //EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return false;
        }

        public IndicatorState GetIndicator(IndicatorType light)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                return IndicatorStateFeedback[(int)LoadPortIndicatorLightMap[light]];
            }
            //EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return IndicatorState.OFF;
        }

        public virtual bool SetE84Available(out string reason)
        {
            IsAccessAuto = true;
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(true);
            }
            reason = "";
            return true;
        }
        public virtual bool SetE84Unavailable(out string reason)
        {
            IsAccessAuto = false;
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(false);
            }
            reason = "";
            return true;
        }

        public virtual bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "SetIndicator", light, state });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool QueryIndicator(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "QueryIndicator" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool QueryState(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "QueryState" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Undock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Undock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Dock(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Dock" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool CloseDoor(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "CloseDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool OpenDoor(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoor" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool OpenDoorAndDown(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorAndDown" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool DoorUpAndClose(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "DoorUpAndClose" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }



        public virtual bool OpenDoorNoMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorNoMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool OpenDoorAndMap(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "OpenDoorAndMap" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Unclamp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Unclamp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Clamp(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "Clamp" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Unload(out string reason)
        {
            reason = "";
            if (!IsEnableUnload(out reason))
                return false;
            bool ret = CheckToPostMessage((int)LoadPortMsg.Unload);
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool UnloadWithMap(out string reason)
        {
            reason = "";
            if (!IsEnableUnload(out reason))
                return false;
            bool ret = CheckToPostMessage((int)LoadPortMsg.Unload, "UnloadWithMap");
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }



        public virtual bool Stop(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Abort, null);
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool ClearError(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool Init(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "Init" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool Home(out string reason)
        {
            IsHomed = false;
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "Home" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }
        public virtual bool HomeModule(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartHome, new object[] { "Home" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool ForceHome(out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.Init, new object[] { "ForceHome" });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }

        public virtual bool FOSBMode(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool FOUPMode(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool ReadCarrierIDByLP(object[] para, out string reason)
        {
            reason = "";
            if (para != null && para.Length == 2)
                return ReadCarrierIDByLP(Convert.ToInt32(para[0]), Convert.ToInt32(para[1]), out reason);
            return
                ReadCarrierIDByLP(0, 8, out reason);
        }

        public virtual bool ReadCarrierIDByLP(int offset, int length, out string reason)
        {
            reason = "";
            bool ret = CheckToPostMessage((int)LoadPortMsg.StartExecute, new object[] { "ReadCarrierID", offset, length });
            if (!ret)
            {
                reason = $"Can't execute on {CurrentState}";
            }
            return ret;
        }



        public virtual bool ChangeAccessMode(bool auto, out string reason)
        {
            if(auto)
            {
                IsAccessAuto = true;
                if (_lpE84Callback != null)
                {
                    _lpE84Callback.SetHoAutoControl(false);
                    _lpE84Callback.SetHoAvailable(true);
                }
            }
            else
            {
                IsAccessAuto = false;
                if (_lpE84Callback != null)
                {
                    _lpE84Callback.SetHoAutoControl(false);
                    _lpE84Callback.SetHoAvailable(false);
                }
            }


            reason = "";
            return true;
        }


        public virtual bool SetServiceCommand(bool inService, out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool GetE84HandoffActiveSignalState(E84PioPosition piopostion, E84PioSignalAtoP AtoPsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (AtoPsignal)
            {
                case E84PioSignalAtoP.AM_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.AM_AVBL);
                case E84PioSignalAtoP.BUSY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.BUSY);
                case E84PioSignalAtoP.COMPT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.COMPT);
                case E84PioSignalAtoP.CONT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CONT);
                case E84PioSignalAtoP.CS_0:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_0);
                case E84PioSignalAtoP.CS_1:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_1);
                case E84PioSignalAtoP.TR_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.TR_REQ);
                case E84PioSignalAtoP.VALID:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.VALID);
                default:
                    return false;
            }
        }
        public virtual bool GetE84HandoffPassiveSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.ES);
                case E84PioSignalPtoA.HO_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.HO_AVBL);
                case E84PioSignalPtoA.L_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.L_REQ);
                case E84PioSignalPtoA.READY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.READY);
                case E84PioSignalPtoA.U_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.U_REQ);
                default:
                    return false;
            }



        }
        public virtual void SetE84HandoffSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal, bool state)
        {
            if (_lpE84Callback == null) return;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.ES, state);
                    break;
                case E84PioSignalPtoA.HO_AVBL:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.HOAvbl, state);
                    break;
                case E84PioSignalPtoA.L_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.LoadReq, state);
                    break;
                case E84PioSignalPtoA.READY:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.Ready, state);
                    break;
                case E84PioSignalPtoA.U_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.UnloadReq, state);
                    break;
                default:
                    break;

            }
        }

        public virtual void Monitor()
        {
            _presentTrig.CLK = _isPresent;
            _placetTrig.CLK = _isPlaced;
            _dockTrig.CLK = _isDocked;
            _clampTrig.CLK = ClampState == FoupClampState.Close;
            _doorTrig.CLK = DoorState == FoupDoorState.Close;
            _accessSwPressedTrig.CLK = _isAccessSwPressed;

            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetFoupStatus(_isPlaced);

                _lpE84Callback.SetReadyTransferStatus(IsReadyForE84Transfer);

                _lpE84Callback.SetLightCurtainHandle(CurrentLightCurtainHandle);
            }
        }


        public virtual bool IsReadyForE84Transfer
        {
            get
            {
                if ((CurrentState == LoadPortStateEnum.Idle || CurrentState == LoadPortStateEnum.TransferBlock)
                    && ClampState == FoupClampState.Open && DoorState == FoupDoorState.Close &&
                    DockState == FoupDockState.Undocked)
                    return true;
                return false;
            }
        }

        public virtual void Reset()
        {
            MapError = false;
            ReadCarrierIDError = false;
            if (CurrentReader != null)
                CurrentReader.Reset();

        }
        public virtual bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.EnableDualTransfer") &&
                SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.EnableDualTransfer"))
            {

                return true;
            }


            return false;
        }
        public virtual bool IsEnableMapWafer(out string reason)
        {
            reason = "";
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isDocked)
            {
                reason = "carrier is not docked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            if (DoorState != FoupDoorState.Open)
            {
                reason = "Door is not opened";
                return false;
            }
            if (DoorPosition != FoupDoorPostionEnum.Down)
            {
                reason = "Door is not down";
                return false;
            }
            return true;

        }

        public virtual bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "LoadPort In Error";
                return false;
            }
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isMapped)
            {
                reason = "Not Mapped";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            if (!IsCarrierEnabled)
            {
                reason = "Carrier Not Enabled";
                return false;
            }
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
            foreach (WaferInfo wafer in wafers)
            {
                if (wafer.IsEmpty) continue;
                if (wafer.Status == WaferStatus.Crossed || wafer.Status == WaferStatus.Double || wafer.Status == WaferStatus.Unknown)
                {
                    //EV.PostWarningLog(Name, $"At least one wafer is {wafer.Status.ToString()}.");
                    reason = $"At least one wafer is {wafer.Status.ToString()}.";
                    return false;
                }
            }
            return true;
        }

        public virtual bool IsEnableLoad(out string reason)
        {
            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (_isDocked)
            {
                reason = "Carrier is docked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            if(!IsCarrierEnabled)
            {
                reason = "CarrierNotEnabled";
                return false;
            }

            reason = "";
            return true;
        }
        public virtual bool IsEnableUnload(out string reason)
        {
            if(!IsRemoteOperationPermit)
            {
                reason = "RemoteOperationNotAllowed";
                return false;
            }


            if (!_isPlaced)
            {
                reason = "No carrier placed";
                return false;
            }
            if (!_isDocked)
            {
                reason = "Carrier is undocked";
                return false;
            }
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            reason = "";
            return true;
        }

        public virtual bool IsForbidAccessSlotAboveWafer()
        {
            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ForbidAccessAboveWaferCarrier"))
                return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ForbidAccessAboveWaferCarrier");

            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ForbidAccessAboveWafer"))
                return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.ForbidAccessAboveWafer");
            

            return false;
        }

        public CarrierOnLPState m_CarrierOnState { get; set; } = CarrierOnLPState.Unknow;

        public bool HasAlarm => false;

        protected virtual void ConfirmAddCarrier()
        {
            _isPlaced = true;
            IsComplete = false;
            if (m_CarrierOnState != CarrierOnLPState.On)
            {
                if (m_CarrierOnState == CarrierOnLPState.Off)
                {
                    if (_lpE84Callback != null && IsAccessAuto)
                    {
                        if (_lpE84Callback.GetCurrentE84State() != E84State.Busy ||
                            !_lpE84Callback.GetE84SignalState(E84SignalID.VALID))
                        {
                            OnError("Unexpected carrier placed on access auto");
                        }
                    }
                }

                if(CarrierManager.Instance.GetCarrier(Name).IsEmpty)
                {
                    CarrierManager.Instance.CreateCarrier(Name);
                        
                }
                _carrierId = CarrierManager.Instance.GetCarrier(Name).CarrierId;
                m_CarrierOnState = CarrierOnLPState.On;
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                EV.Notify(EventCarrierArrived, dvid);
                EV.Notify(EventCarrierArrived1, dvid);
                if (_lpcallback != null)
                    _lpcallback.CarrierArrive();
                _isMapped = false;
                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrier arrived.");
            }               
           
        }

        protected virtual void ConfirmRemoveCarrier()
        {
            _isPlaced = false;
            IsComplete = false;
            _isMapped = false;
            

            if (m_CarrierOnState != CarrierOnLPState.Off)
            {
                if (m_CarrierOnState == CarrierOnLPState.On)
                {
                    if (_lpE84Callback != null && IsAccessAuto)
                    {
                        if (_lpE84Callback.GetCurrentE84State() != E84State.Busy ||
                            !_lpE84Callback.GetE84SignalState(E84SignalID.VALID))
                        {
                            OnError("Unexpected carrier removed on access auto");
                        }
                    }
                }





                WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
                //for (int i = 0; i < 25; i++)
                //{
                //    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
                //}
                CarrierManager.Instance.DeleteCarrier(Name);
                m_CarrierOnState = CarrierOnLPState.Off;
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CarrierID"] = _carrierId ?? "";
                dvid["CAR_ID"] = _carrierId ?? "";
                EV.Notify(EventCarrierRemoved, dvid);
                EV.Notify(EventCarrierRemoved1, dvid);

                if (_lpcallback != null) _lpcallback.CarrerRemove(_carrierId);

                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrier:{_carrierId} was removed.");

                _carrierId = "";

            }

                
            
        }

        public virtual void OnSlotMapRead(string _slotMap)
        {
            CurrentSlotMapResult = _slotMap;
            
            if (!IsMapWaferByLoadPort)
                OnActionDone(new object[] { });

            //if (_slotMap.Length < ValidSlotsNumber)
            //{
            //    EV.PostAlarmLog("LoadPort", $"{LPModuleName} Mapping Data Error," +
            //        $"slotmap length is {_slotMap.Length} not match the valid slots number:{ValidSlotsNumber}.");
            //}
            string tempSlotMap = _slotMap;//.Substring(ValidStartSlotIndex, ValidSlotsNumber);



            for (int i = 0; i < tempSlotMap.Length; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                WaferInfo wafer = null;
                switch (tempSlotMap[i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapCrossedWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map crossed wafer on carrier:{_carrierId},slot:{i+1}.");
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapDoubleWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map double wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map unknown wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    default:
                        tempSlotMap = tempSlotMap.Substring(0,i) + "?" + tempSlotMap.Substring(i+1);
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map unknown wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;



                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = tempSlotMap;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierIndex"] = InfoPadCarrierIndex;
            dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            EV.Notify(EventMapComplete, dvid);
            if (tempSlotMap.Contains("2"))
            {
                MapError = true;

                string slotSpec = "";
                for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                {
                    if (CurrentSlotMapResult[i] == '2')
                    {
                        if (slotSpec == "")
                            slotSpec += $"{i + 1}";
                        else
                            slotSpec += $",{i + 1}";
                    }
                }

                SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();
                dvid["AlarmDescription"] = $"{LPModuleName} load map crossed wafer on slot:{slotSpec} of carrier:{_carrierId}";


                EV.Notify(AlarmLoadPortMapCrossedWafer, dvid1);
                SerializableDictionary<string, object> dvid2 = new SerializableDictionary<string, object>();

                dvid1["AlarmDescription"] = $"{LPModuleName} map error:crossed wafer when load carrier:{_carrierId}";
                EV.Notify(AlarmLoadPortMappingError, dvid2);
                OnError("MappingError");
            }
            if (tempSlotMap.Contains("3"))
            {
                MapError = true;
                string slotSpec = "";
                for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                {
                    if (CurrentSlotMapResult[i] == '3')
                    {
                        if (slotSpec == "")
                            slotSpec += $"{i + 1}";
                        else
                            slotSpec += $",{i + 1}";
                    }
                }

                SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();
                dvid["AlarmDescription"] = $"{LPModuleName} load map double wafer on slot:{slotSpec} of carrier:{_carrierId}";


                EV.Notify(AlarmLoadPortMapDoubleWafer, dvid1);
                SerializableDictionary<string, object> dvid2 = new SerializableDictionary<string, object>();
                dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when load carrier:{_carrierId}";
                EV.Notify(AlarmLoadPortMappingError, dvid2);
                OnError("MappingError");

            }
            if (tempSlotMap.Contains("W"))
            {
                MapError = true;
                string slotSpec = "";
                for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                {
                    if (CurrentSlotMapResult[i] == 'W')
                    {
                        if (slotSpec == "")
                            slotSpec += $"{i + 1}";
                        else
                            slotSpec += $",{i + 1}";
                    }

                }

                SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();
                dvid["AlarmDescription"] = $"{LPModuleName} load map double wafer on slot:{slotSpec} of carrier:{_carrierId}";

                EV.Notify(AlarmLoadPortMapDoubleWafer, dvid1);
                SerializableDictionary<string, object> dvid2 = new SerializableDictionary<string, object>();

                dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when load carrier:{_carrierId}";
                EV.Notify(AlarmLoadPortMappingError, dvid2);
                OnError("MappingError");
            }
            if (tempSlotMap.Contains("?"))
            {
                MapError = true;
                string slotSpec = "";
                for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                {
                    if (CurrentSlotMapResult[i] == '?')
                    {
                        if (slotSpec == "")
                            slotSpec += $"{i + 1}";
                        else
                            slotSpec += $",{i + 1}";
                    }

                }

                SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();
                dvid["AlarmDescription"] = $"{LPModuleName} load unknow wafer on slot:{slotSpec} of carrier:{_carrierId}";

                EV.Notify(AlarmLoadPortMapUnknownWafer, dvid1);
                SerializableDictionary<string, object> dvid2 = new SerializableDictionary<string, object>();

                dvid1["AlarmDescription"] = $"{LPModuleName} map error:unknow wafer when load carrier:{_carrierId}";
                EV.Notify(AlarmLoadPortMappingError, dvid2);
                OnError("MappingError");
            }

            if (LPCallBack != null && IsPlacement)
                LPCallBack.MappingComplete(_carrierId, CurrentSlotMapResult);

            _isMapped = true;
        }



        /// <summary>
        /// 获取LP中空缺Slot
        /// </summary>
        /// <returns>返回一个list, 顺序为从下到上.(0-25)</returns>       
        public virtual bool ReadCarrierIDByIndex(int offset = 0, int length = 16, int index = 0)
        {
            if (CIDReaders == null || CIDReaders.Length <= index) return false;
            return CIDReaders[index].ReadCarrierID(offset, length);
        }
        public virtual bool WriteCarrierIDByIndex(string cid, int offset = 0, int length = 16, int index = 0)
        {
            if (CIDReaders == null || CIDReaders.Length <= index) return false;
            _carrierIdToBeWrite = cid;

            if (!CIDReaders[index].WriteCarrierID(offset, length, cid))
                return false;
            int waitCount = 0;
            while(true)
            {
                if (CIDReaders[index].IsReady) 
                    return true;
                
                Thread.Sleep(100);
                waitCount++;
                if (waitCount > 150)
                    break;
            }
            return false;
        }
        public bool ReadCarrierIDByIndex(object[] para)
        {
            int offset = SC.ContainsItem($"LoadPort.{LPModuleName}.StartPage") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.StartPage") : 0;
            int length = SC.ContainsItem($"LoadPort.{LPModuleName}.DataReadSize") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.DataReadSize") : 16;
            int index = 0;
            if (para != null)
                index = Convert.ToInt32(para[0]);
            return ReadCarrierIDByIndex(offset, length, index);
        }
        public bool WriteCarrierIDByIndex(object[] para)
        {
            int offset = SC.ContainsItem($"LoadPort.{LPModuleName}.StartPage") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.StartPage") : 0;
            int length = SC.ContainsItem($"LoadPort.{LPModuleName}.DataReadSize") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.DataReadSize") : 16;

            if (para == null) return false;
            int index = Convert.ToInt32(para[0]);
            string cid = para[1].ToString();
            return WriteCarrierIDByIndex(cid, offset, length, index);
        }


        protected int CurrentCidIndex
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}"))
                {
                    return SC.GetValue<int>($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}");
                }

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CIDReaderIndex"))
                {
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CIDReaderIndex");
                }
                return 0;

            }
        }


        public virtual bool ReadCarrierID(int offset, int length)
        {

            
            if (CIDReaders == null || CIDReaders.Length < CurrentCidIndex)
            {
                return false;
            }
            return CIDReaders[CurrentCidIndex].ReadCarrierID(offset, length);

        }


        public virtual bool GetCarrierID(int offset, int length, out string carrierId)
        {
            carrierId = _carrierId;

            if (CIDReaders == null || CIDReaders.Length < CurrentCidIndex)
            {
                return false;
            }
            if(!CIDReaders[CurrentCidIndex].ReadCarrierID(offset, length))
            {
                return false;
            }
            int waitCount = 0;  
            while(!CIDReaders[CurrentCidIndex].IsReady)
            {
                Thread.Sleep(100);
                waitCount++;

                if(waitCount>200)
                {
                    return false;
                }
            }
            carrierId = CIDReaders[CurrentCidIndex].CarrierIDBeRead;
            return true;

        }





        public virtual bool ReadCarrierID()
        {
            int offset = SC.ContainsItem($"LoadPort.{LPModuleName}.StartPage") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.StartPage") : 0;
            int length = SC.ContainsItem($"LoadPort.{LPModuleName}.DataReadSize") ?
                SC.GetValue<int>($"LoadPort.{LPModuleName}.DataReadSize") : 16;

            if (CIDReaders == null || CIDReaders.Length < CurrentCidIndex)
            {
                return false;
            }
            return CIDReaders[CurrentCidIndex].ReadCarrierID(offset, length);

        }
        public virtual bool ReadCarrierID(object[] para)
        {
            if (para != null && para.Length == 2)
            {
                return ReadCarrierID(Convert.ToInt32(para[0]), Convert.ToInt32(para[1]));
            }
            return ReadCarrierID();
        }
        public virtual bool WriteCarrierID(string carrierID, int offset = 0, int length = 16)
        {

            if (CIDReaders == null || CIDReaders.Length < CurrentCidIndex)
            {
                return false;
            }
            
            if (!CIDReaders[CurrentCidIndex].WriteCarrierID(offset, length, carrierID))
                return false;
            int waitCount = 0;
            while (true)
            {
                if (CIDReaders[CurrentCidIndex].IsReady)
                    return true;

                Thread.Sleep(100);
                waitCount++;
                if (waitCount > 150)
                    break;
            }

            return false;
        }

        public virtual bool WriteCarrierID(string cid, int offset, int length, out string reason)
        {
            reason = "";
           

            if (CIDReaders == null || CIDReaders.Length < CurrentCidIndex)
            {
                reason = "No valid reader";
                return false;
            }

            if (!CIDReaders[CurrentCidIndex].WriteCarrierID(offset, length, cid))
                return false;
            int waitCount = 0;
            while (true)
            {
                if (CIDReaders[CurrentCidIndex].IsReady)
                    return true;

                Thread.Sleep(100);
                waitCount++;
                if (waitCount > 150)
                    break;
            }

            return false;
        }
        public virtual void OnCarrierIdRead(string carrierId, int readerIndex = 1, int startPage = 1, int length = 16,bool updateCarrierID = true)
        {
            if (_isPlaced && _isPresent)
            {

                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CarrierID"] = carrierId;
                dvid["CIDReaderIndex"] = readerIndex;
                dvid["StartPage"] = startPage;
                dvid["PageLength"] = length;
                EV.Notify(EventCarrierIdRead, dvid);
                EV.Notify(EventCarrierIdRead1, dvid);
                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrierID read successfully:{carrierId}.");
                
                
                ReadCarrierIDError = false;

                if (updateCarrierID)
                {
                    _carrierId = carrierId;
                    CarrierManager.Instance.UpdateCarrierId(Name, carrierId);
                    if (_lpcallback != null)
                        _lpcallback.CarrierIDReadSuccess(carrierId);
                }

            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }
        public bool NoteTransferStart()
        {
            return CheckToPostMessage((int)LoadPortMsg.StartAccess, null);
        }
        public bool NoteTransferStop()
        {
            return CheckToPostMessage((int)LoadPortMsg.CompleteAccess, null);
        }

        public void ProceedSetCarrierID(string cid)
        {
            _carrierId = cid;
            CarrierManager.Instance.UpdateCarrierId(Name, cid);
        }
        public void OnCarrierIdWrite(string carrierId, int readerIndex = 1, int startPage = 1, int length = 16,bool isUpdateCid = true)
        {
            
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CarrierID"] = carrierId;
                dvid["CAR_ID"] = carrierId;
                dvid["CIDReaderIndex"] = readerIndex;
                dvid["StartPage"] = startPage;
                dvid["PageLength"] = length;

                EV.Notify(EventCarrierIdWrite, dvid);
                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrierID write successfully:{carrierId}.");

                if (isUpdateCid)
                    _carrierId = carrierId;
            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }
        public void OnCarrierIdReadFailed(int readerIndex = 1)
        {
            if (_isPlaced && _isPresent)
            {
                //_carrierId = "";

                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CIDReaderIndex"] = readerIndex;

                EV.Notify(EventCarrierIdReadFailed, dvid);
                EV.Notify(EventCarrierIdReadFailed1, dvid);
                if (_lpcallback != null) _lpcallback.CarrierIDReadFail();

                EV.Notify(AlarmCarrierIDReadError, new SerializableDictionary<string, object> {
                    {"AlarmText","CarrierID read fail." }
                });
                ReadCarrierIDError = true;

                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrierID read fail.");

            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id read is not valid");
            }
        }
        public void OnCarrierIdWriteFailed(int readerIndex = 1)
        {
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                dvid["PORT_ID"] = PortID;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CIDReaderIndex"] = readerIndex;
                EV.Notify(EventCarrierIdWriteFailed, dvid);
                EV.Notify(EventCarrierIdWriteFailed1, dvid);
                EV.PostInfoLog("LoadPort", $"{LPModuleName} carrierID write fail carrierID:{_carrierIdToBeWrite ?? ""}.");
            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id not valid");
            }
        }


        public void OnCarrierIdRead(ModuleName module, string carrierId, int readerIndex = 1, int startPage = 1, int length = 16,bool isUpdateLPCarrier = true)
        {
            OnCarrierIdRead(carrierId, readerIndex, startPage, length, isUpdateLPCarrier);
        }
        public void OnCarrierIdReadFailed(ModuleName module, int readerIndex = 1)
        {
            OnCarrierIdReadFailed(readerIndex);
        }
        public void OnCarrierIdWrite(ModuleName module, string carrierId, int readerIndex = 1, int startPage = 1, int length = 16,bool isUpdateCid =true)
        {
            OnCarrierIdWrite(carrierId, readerIndex, startPage, length,isUpdateCid);
        }

        public void OnCarrierIdWriteFailed(ModuleName module, int readerIndex = 1)
        {
            OnCarrierIdWriteFailed(readerIndex);
        }

        public virtual void OnLoaded()
        {
            var dvid = new SerializableDictionary<string, object>
            {
                ["CarrierID"] = _carrierId ?? "",
                ["CAR_ID"] = _carrierId ?? "",
                ["PORT_ID"] = PortID,
                ["PortID"] = PortID,
                ["PORT_CTGRY"] = SpecPortName,
                ["CarrierType"] = SpecCarrierType,
                ["CarrierIndex"] = InfoPadCarrierIndex,
                ["InfoPadSensorIndex"] = InfoPadSensorIndex,

            };
            EV.Notify(EventCarrierloaded, dvid);
            EV.PostInfoLog("LoadPort", $"{LPModuleName} load complete with carrier type:{SpecCarrierType ?? ""}.");
            EV.Notify(EventCarrierLoaded1, dvid);
            //if (_lpcallback != null) _lpcallback.LoadComplete();

            //}
        }
        public virtual void OnUnloaded()
        {

            var dvid = new SerializableDictionary<string, object>();
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = _carrierId;
            dvid["CAR_ID"] = _carrierId ?? "";
            dvid["PORT_ID"] = PortID;
            dvid["SlotMap"] = CurrentSlotMapResult; 
            EV.Notify(EventCarrierUnloaded, dvid);
            EV.Notify(EventCarrierUnloaded1, dvid);
            EV.PostInfoLog("LoadPort", $"{LPModuleName} unload complete with carrier:{CarrierId ?? ""}.");
            //}
            DockState = FoupDockState.Undocked;

            if (_lpcallback != null) _lpcallback.UnloadComplete();

            //_isMapped = false;
        }

        public virtual void OnE84HandOffStart(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffStart(isload);
        }
        public virtual void OnE84HandOffComplete(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffComplete(isload);
        }


        public void OnHomed()
        {
            //for (int i = 0; i < 25; i++)
            //{
            if (!_isPlaced)
                WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, 25);
            //}
            _isMapped = false;
            var dvid = new SerializableDictionary<string, object>();
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierIndex"] = InfoPadCarrierIndex;
            dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
            dvid["CarrierID"] = _carrierId;


            EV.Notify(EventLPHomed);
            EV.PostInfoLog("LoadPort", $"{LPModuleName} home complete with carrier:{CarrierId ?? ""}.");

            if (_lpcallback != null) _lpcallback.OnLPHomed();


        }

        public virtual void OnError(string error = "")
        {
            EV.Notify(AlarmLoadPortError);

            EV.PostAlarmLog("LoadPort", $"{LPModuleName} occurried error:{error} with carrier:{CarrierId??""}.");
            IsBusy = false;
            CheckToPostMessage((int)LoadPortMsg.Error, null);

            if (ActionDone != null)
                ActionDone(false);
        }

        protected void SetPresent(bool isPresent)
        {
            _isPresent = isPresent;
            if (_isPresent)
            {
                //ConfirmAddCarrier();
            }
            else
            {
                //ConfirmRemoveCarrier();
            }
        }

        protected virtual void SetPlaced(bool isPlaced)
        {
            _isPlaced = isPlaced;
            if (_isPlaced)
            {
                ConfirmAddCarrier();
            }
            else
            {
                ConfirmRemoveCarrier();
            }
        }
        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            CheckToPostMessage((int)LoadPortMsg.ActionDone, new object[] { "ActionDone" });
            if (ActionDone != null)
                ActionDone(true);
            return true;
        }

        public void OnActionDone(bool result)
        {
            if (ActionDone != null)
                ActionDone(result);
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                if ((LoadPortMsg)msg != LoadPortMsg.ActionDone && (LoadPortMsg)msg != LoadPortMsg.StartExecute)
                    EV.PostInfoLog(Name, $"{Name} is in { (LoadPortStateEnum)fsm.State} state，can not do {(LoadPortMsg)msg}");
                return false;
            }
            CurrentParamter = args;
            if (msg < 10)
                IsBusy = true;
            else
                IsBusy = false;
            fsm.PostMsg(msg, args);

            return true;
        }
        public bool CheckToPostMessage(LoadPortMsg msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
                if ((LoadPortMsg)msg != LoadPortMsg.ActionDone && (LoadPortMsg)msg != LoadPortMsg.StartExecute)
                    EV.PostWarningLog(Name, $"{Name} is in { (LoadPortStateEnum)fsm.State} state，can not do {(LoadPortMsg)msg}");
                return false;
            }
            CurrentParamter = args;
            if ((int)msg < 10)
                IsBusy = true;
            else
                IsBusy = false;
            fsm.PostMsg((int)msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (LoadPortStateEnum)fsm.State, (LoadPortMsg)msg);
                return false;
            }
            reason = "";

            return true;
        }

        public virtual bool FALoad(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool FAUnload(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool WriteRfid(string cid, int startpage, int length, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ReadRfId(out string reason)
        {
            reason = "";
            return true;
        }

        public object[] CurrentParamter { get; private set; }

        public virtual DeviceState State
        {
            get
            {
                if (CurrentState == LoadPortStateEnum.Idle)
                {
                    return DeviceState.Idle;
                }
                if (MapError || ReadCarrierIDError || CurrentState == LoadPortStateEnum.Error)
                {
                    return DeviceState.Error;
                }

                if (IsBusy)
                    return DeviceState.Busy;

                return DeviceState.Unknown;
            }
        }

        public string InfoPadCarrierType { get; set; }
    }
    public enum LoadPortStateEnum
    {
        Undefined = 0,
        Init,
        Initializing,
        Homing,
        Resetting,
        Idle,
        Mapping,
        Loading,
        Unloading,
        Executing,
        Error,
        ReadingData,
        WrittingData,
        TransferBlock,
    };
    public enum LoadPortMsg
    {
        Init,
        StartReadData,
        StartWriteData,
        Load,
        Unload,
        Reset,
        StartExecute,
        StartHome,
        InitComplete = 10,
        ReadComplete,
        WriteComplete,
        LoadComplete,
        UnloadComplete,
        ResetComplete,
        HomeComplete,
        Error,
        Abort,
        Stop,
        MoveComplete,
        ActionDone,
        StartAccess,
        CompleteAccess,
    }

    public enum DispatchLPType
    {
        NA,
        Loader,
        Unloader,

    }

    public enum E84DeviceTypeEnum
    {
        None,
        Internal,
        External,
    }

    public class E84SigState
    {
        public bool LightCurtain{ get; set; }
        public bool CS_0{ get; set; }
        public bool CS_1 { get; set; }
        public bool AM_AVBL{ get; set; }
        public bool VALID { get; set; }
        public bool TR_REQ { get; set; }
        public bool BUSY { get; set; }
        public bool COMPT { get; set; }
        public bool CONT { get; set; }
        public bool L_REQ { get; set; }
        public bool U_REQ { get; set; }
        public bool VA { get; set; }
        public bool READY { get; set; }
        public bool VS_0 { get; set; }
        public bool VS_1 { get; set; }
        public bool HO_AVBL { get; set; }
        public bool ES { get; set; }
    }

}
