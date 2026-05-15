using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.FAServices;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.Core.Applications;
using MECF.Framework.RT.Core.IoProviders;
using MECF.Framework.RT.ModuleLibrary.Commons;
using MECF.Framework.RT.ModuleLibrary.SystemModules.Routines;

namespace MECF.Framework.RT.ModuleLibrary.SystemModules
{
    public enum RtState
    {
        Init,

        Initializing,
        Idle,

        Transfer,
        AutoRunning,

        AutoIdle,

        ReturnAllWafer,

        Error,

    }

    public class EquipmentManager : FsmDevice
    {
        public enum MSG
        {
            MoveWafer,
            ReturnAllWafer,

            Stop,

            HOME,
            RESET,
            ABORT,
            ERROR,

            ToInit,

            FAJobCommand,
            SetAutoMode,
            SetManualMode,
            
            CreateJob,
            PauseJob,
            ResumeJob,
            PauseAllJob,
            ResumeAllJob,
            StartJob,
            StopJob,
            AbortJob,

            JobDone,

            ModuleError,

            Map,

            ToAutoRunning,
        }

        public static Dictionary<ModuleName, ModuleFsmDevice> Modules { get; set; }

        public bool IsAutoMode
        {
            get
            {
                return FsmState == (int)RtState.AutoRunning || FsmState == (int)RtState.AutoIdle;
            }
        }

        public bool IsInit
        {
            get { return FsmState == (int)RtState.Init; }
        }

        public bool IsIdle
        {
            get { return FsmState == (int)RtState.Idle; }
        }
        public bool IsAlarm
        {
            get { return FsmState == (int)RtState.Error; }
        }
        public bool IsOnline { get; set; }
        public bool IsRunning
        {
            get
            {
                return !IsAlarm && !IsIdle && !IsInit && (FsmState != (int)RtState.AutoIdle);
            }
        }

        private bool _isInited;

        protected IRoutine _manualTransfer;
        protected IAutoTransfer _auto;
        protected IRoutine _homeAll;
        protected IRoutine _returnAll;

        private List<string> _modules;

        private PeriodicJob _thread;

        private bool _isJumpAutoRunning = false;

        private bool _isPreviousVCEACassPresent = false;
        private bool _isPreviousVCEBCassPresent = false;

        public EquipmentManager()
        {
            Module = "System";
            Name = "System";
            Modules = new Dictionary<ModuleName, ModuleFsmDevice>();
            _modules = new List<string>() { "System" };

            //_thread = new PeriodicJob(200, OnTimer, "Monitor Job Status", true);
        }

        public override bool Initialize()
        {
            InitDevices();

            InitModules();

            EnumLoop<RtState>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });
            EnableFsm(100, RtState.Init);

            BuildTransitionTable();

            SubscribeFAEvents();

            SubscribeDataVariable();

            SubscribeOperation();

            InitRoutine();

            Singleton<EventManager>.Instance.OnAlarmEvent += Instance_OnAlarmEvent;

            return true;
        }

        protected void BuildModules(params ModuleFsmDevice[] modules)
        {
            if (modules != null && modules.Length > 0)
            {
                foreach (var moduleFsmDevice in modules)
                {
                    Modules[ModuleHelper.Converter(moduleFsmDevice.Module)] = moduleFsmDevice;
                }

                foreach (var modulesKey in Modules.Keys)
                {
                    _modules.Add(modulesKey.ToString());
                }

                foreach (var modulesValue in Modules.Values)
                {
                    modulesValue.Initialize();
                }
            }
        }

        protected virtual void InitRoutine()
        {

        }

        protected virtual void InitModules()
        {
        }

        protected virtual void InitDevices()
        {
        }

        protected virtual void TMRobotSetLoad(int slot ,bool isOn)
        {
        }



        //private PeriodicJob _job1;

        //Stopwatch _sw = new Stopwatch();

        //private bool OnJob1()
        //{
        //    _sw.Restart();

        //    BufferModule buffer = Modules[ModuleName.Buffer] as BufferModule;

        //    if (!buffer.CheckToPostMessage(BufferModule.MSG.InTransfer))
        //    {
        //        System.Diagnostics.Trace.Assert(false)
        //        System.Diagnostics.Trace.WriteLine("fail to in transfer");
        //    }

        //    if (buffer.IsIdle)
        //    {
        //        System.Diagnostics.Trace.Assert(false);
        //        System.Diagnostics.Trace.WriteLine("!!!==========>IsIdle");
        //    }
        //    //System.Diagnostics.Trace.Assert(buffer.IsIdle, $"in {buffer.StringFsmStatus}");

        //    while (buffer.IsIdle)
        //    {
        //        Thread.Sleep(5);
        //    }

        //    //buffer.NoteTransferStop(ModuleName.EfemRobot, Hand.Blade1, 0, EnumTransferType.Place);
        //    //Thread.Sleep(15);
        //    if (!buffer.CheckToPostMessage(BufferModule.MSG.TransferComplete))
        //    {
        //        System.Diagnostics.Trace.Assert(false);

        //        System.Diagnostics.Trace.WriteLine("fail to complete");
        //    }

        //    //if (!buffer.IsIdle)
        //    //{
        //    //    System.Diagnostics.Trace.WriteLine("not idle");
        //    //}
        //    System.Diagnostics.Trace.WriteLine($" === {_sw.ElapsedMilliseconds}");

        //    while (!buffer.IsIdle)
        //    {
        //        Thread.Sleep(5);
        //    }

        //    return true;
        //}


        
        private void BuildTransitionTable()
        {
            //Init sequence
            Transition(RtState.Init, MSG.HOME, FsmStartHome, RtState.Initializing);
            Transition(RtState.Idle, MSG.HOME, FsmStartHome, RtState.Initializing);
            Transition(RtState.Error, MSG.HOME, FsmStartHome, RtState.Initializing);
            Transition(RtState.Error, MSG.ToInit, null, RtState.Init);

            Transition(RtState.Initializing, FSM_MSG.TIMER, FsmMonitorHome, RtState.Idle);
            Transition(RtState.Initializing, MSG.ERROR, fError, RtState.Error);
            Transition(RtState.Initializing, MSG.ABORT, FsmAbort, RtState.Init);

            //Reset
            AnyStateTransition(MSG.RESET, fStartReset, RtState.Idle);

            AnyStateTransition(MSG.ERROR, fError, RtState.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)RtState.Error);

            //Auto/manual sequence
            Transition(RtState.Idle, MSG.SetAutoMode, fStartAutoTransfer, RtState.AutoIdle);
            Transition(RtState.AutoRunning, MSG.FAJobCommand, FsmFAJobCommand, RtState.AutoRunning);
            Transition(RtState.AutoRunning, FSM_MSG.TIMER, fAutoTransfer, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.ABORT, FsmAbort, RtState.AutoIdle);
            Transition(RtState.AutoRunning, MSG.JobDone, null, RtState.AutoIdle);
            Transition(RtState.AutoRunning, MSG.CreateJob, FsmCreateJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.StartJob, FsmStartJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.PauseJob, FsmPauseJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.ResumeJob, FsmResumeJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.PauseAllJob, FsmPauseAllJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.ResumeAllJob, FsmResumeAllJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.StopJob, FsmStopJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.AbortJob, FsmAbortJob, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.ModuleError, FsmModuleError, RtState.AutoRunning);
            Transition(RtState.AutoRunning, MSG.Map, FsmMap, RtState.AutoRunning);
            EnterExitTransition<RtState, FSM_MSG>(RtState.AutoRunning, null, FSM_MSG.NONE, fExitAutoTransfer);

            Transition(RtState.AutoIdle, MSG.FAJobCommand, FsmFAJobCommand, RtState.AutoIdle);
            Transition(RtState.AutoIdle, FSM_MSG.TIMER, FsmMonitorAutoIdle, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.SetManualMode, FsmStartSetManualMode, RtState.Idle);
            Transition(RtState.AutoIdle, MSG.CreateJob, FsmCreateJob, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.StartJob, FsmStartJob, RtState.AutoRunning);
            Transition(RtState.AutoIdle, MSG.ABORT, FsmAbort, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.AbortJob, FsmAbortJob, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.Map, FsmMap, RtState.AutoIdle);
            Transition(RtState.AutoIdle, MSG.ToAutoRunning, FsmToAutoRunning, RtState.AutoRunning);

            //return all wafer
            Transition(RtState.Idle, MSG.ReturnAllWafer, FsmStartReturnAllWafer, RtState.ReturnAllWafer);
            Transition(RtState.ReturnAllWafer, FSM_MSG.TIMER, FsmMonitorReturnAllWafer, RtState.Idle);
            Transition(RtState.ReturnAllWafer, MSG.ABORT, FsmAbort, RtState.Idle);

            //Transfer sequence
            Transition(RtState.Idle, MSG.MoveWafer, fStartTransfer, RtState.Transfer);
            Transition(RtState.Transfer, FSM_MSG.TIMER, fTransfer, RtState.Idle);
            Transition(RtState.Transfer, MSG.ABORT, FsmAbort, RtState.Idle);
            EnterExitTransition<RtState, FSM_MSG>(RtState.Transfer, null, FSM_MSG.NONE, fExitTransfer);

        }


        protected virtual void SubscribeFAEvents()
        {
        }

        void SubscribeDataVariable()
        {
            DATA.Subscribe("Rt.Status", () => StringFsmStatus);

            DATA.Subscribe("System.IsInitialized", () => _isInited);

            DATA.Subscribe("System.IsIdle", () => IsIdle || IsInit || (FsmState == (int)RtState.AutoIdle));
            DATA.Subscribe("System.IsAlarm", () => IsAlarm);
            DATA.Subscribe("System.IsBusy", () => IsRunning);
            DATA.Subscribe("System.IsAutoRunning", () => IsRunning);

            DATA.Subscribe("System.Modules", () => _modules);
            DATA.Subscribe("System.IsOnline", () => IsOnline);

            EV.Subscribe(new EventItem("Event", UniversalEvents.EquipmentChangeToAuto, "Equipment Mode Change To Auto")); //Linkable Vid : null
            EV.Subscribe(new EventItem("Event", UniversalEvents.EquipmentChangeToManual, "Equipment Mode Change To Manual")); //Linkable Vid : null
            EV.Subscribe(new EventItem("Event", UniversalEvents.CarrierProcessStart, "Carrier Process Start")); //Linkable Vid :  DataVariables.CarrierID , DataVariables.PortID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CarrierProcessComplete, "Carrier Process Complete"));//Linkable Vid : DataVariables.CarrierID , DataVariables.PortID
            EV.Subscribe(new EventItem("Event", UniversalEvents.RecipeStart, "Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.RecipeComplete, "Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.RecipeStepStart, "Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.RecipeStepEnd, "Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.RecipeFailed, "Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID

            EV.Subscribe(new EventItem("Event", UniversalEvents.CH1RecipeStart, "CH1 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH1RecipeComplete, "CH1 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH1RecipeStepStart, "CH1 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH1RecipeStepEnd, "CH1 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH1RecipeFailed, "CH1 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID



            EV.Subscribe(new EventItem("Event", UniversalEvents.CH2RecipeStart, "CH2 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH2RecipeComplete, "CH2 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH2RecipeStepStart, "CH2 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH2RecipeStepEnd, "CH2 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH2RecipeFailed, "CH2 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID



            EV.Subscribe(new EventItem("Event", UniversalEvents.CH3RecipeStart, "CH3 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH3RecipeComplete, "CH3 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH3RecipeStepStart, "CH3 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH3RecipeStepEnd, "CH3 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH3RecipeFailed, "CH3 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID



            EV.Subscribe(new EventItem("Event", UniversalEvents.CH4RecipeStart, "CH4 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH4RecipeComplete, "CH4 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH4RecipeStepStart, "CH4 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH4RecipeStepEnd, "CH4 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH4RecipeFailed, "CH4 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID



            EV.Subscribe(new EventItem("Event", UniversalEvents.CH5RecipeStart, "CH5 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH5RecipeComplete, "CH5 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH5RecipeStepStart, "CH5 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH5RecipeStepEnd, "CH5 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH5RecipeFailed, "CH5 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID



            EV.Subscribe(new EventItem("Event", UniversalEvents.CH6RecipeStart, "CH6 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH6RecipeComplete, "CH6 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH6RecipeStepStart, "CH6 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH6RecipeStepEnd, "CH6 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            EV.Subscribe(new EventItem("Event", UniversalEvents.CH6RecipeFailed, "CH6 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID

            //EV.Subscribe(new EventItem("Event", UniversalEvents.CH7RecipeStart, "CH7 Recipe Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            //EV.Subscribe(new EventItem("Event", UniversalEvents.CH7RecipeComplete, "CH7 Recipe Complete"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID
            //EV.Subscribe(new EventItem("Event", UniversalEvents.CH7RecipeStepStart, "CH7 Recipe Step Start"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            //EV.Subscribe(new EventItem("Event", UniversalEvents.CH7RecipeStepEnd, "CH7 Recipe Step End"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.RecipeStepNumber , DataVariables.SlotID
            //EV.Subscribe(new EventItem("Event", UniversalEvents.CH7RecipeFailed, "CH7 Recipe Failed"));//Linkable Vid : DataVariables.RecipeID , DataVariables.PortID , DataVariables.StationName , DataVariables.SlotID

            //EV.Subscribe(new EventItem("Event", UniversalEvents.VCEA_PlatformInFinished, "VCEA_PlatformInFinished"));
            //EV.Subscribe(new EventItem("Event", UniversalEvents.VCEB_PlatformInFinished, "VCEB_PlatformInFinished"));
            //EV.Subscribe(new EventItem("Event", UniversalEvents.VCEA_PlatformOutFinished, "VCEA_PlatformOutFinished"));
            //EV.Subscribe(new EventItem("Event", UniversalEvents.VCEB_PlatformOutFinished, "VCEB_PlatformOutFinished"));

        }

        void SubscribeOperation()
        {
            OP.Subscribe("CreateWafer", InvokeCreateWafer);

            OP.Subscribe("DeleteWafer", InvokeDeleteWafer);

            OP.Subscribe("ReturnWafer", InvokeReturnWafer);

            OP.Subscribe("MoveWafer", InvokeMoveWafer);

            OP.Subscribe("System.ReturnAllWafer", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ReturnAllWafer, (bool)args[0], (int)args[1]);
            });

            OP.Subscribe("System.MoveWafer", (string cmd, object[] args) =>
            {
                if (!Enum.TryParse((string)args[0], out ModuleName source))
                {
                    EV.PostWarningLog(Name, $"Parameter source {(string)args[0]} not valid");
                    return false;
                }

                if (!Enum.TryParse((string)args[2], out ModuleName destination))
                {
                    EV.PostWarningLog(Name, $"Parameter destination {(string)args[1]} not valid");
                    return false;
                }
                if (args.Length > 8)
                {
                    return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3],
                        (bool)args[4], (int)args[5], (bool)args[6], (int)args[7]);
                }
                else if (args.Length > 5)
                {
                    return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3], (bool)args[4]);
                }

                return CheckToPostMessage((int)MSG.MoveWafer, source, (int)args[1], destination, (int)args[3]);
            });

            OP.Subscribe("System.HomeAll", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.HOME);
            });
            OP.Subscribe("System.Abort", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ABORT);
            });

            OP.Subscribe("System.Reset", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.RESET);
            });

            OP.Subscribe("System.SetAutoMode", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetAutoMode);
            });
            OP.Subscribe("System.SetManualMode", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.SetManualMode);
            });

            OP.Subscribe("System.CreateJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.CreateJob, args[0]);
            });

            OP.Subscribe("System.StartJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.StartJob, args[0]);
            });

            OP.Subscribe("System.PauseJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.PauseJob, args[0]);
            });

            OP.Subscribe("System.ResumeJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ResumeJob, args[0]);
            });

            OP.Subscribe("System.PauseAllJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.PauseAllJob);
            });

            OP.Subscribe("System.ResumeAllJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.ResumeAllJob);
            });

            OP.Subscribe("System.StopJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.StopJob, args[0]);
            });

            OP.Subscribe("System.AbortJob", (string cmd, object[] args) =>
            {
                return CheckToPostMessage((int)MSG.AbortJob, args[0]);
            });

            OP.Subscribe("System.ShutDown", InvokeShutDown);

            OP.Subscribe("System.MapWafer", InvokeEfemMap);
            OP.Subscribe($"System.SetOnline", (string cmd, object[] args) =>
            {
                IsOnline = true;
                return FsmStartSetOnline(args);
            });

            OP.Subscribe($"System.SetOffline", (string cmd, object[] args) =>
            {
                IsOnline = false;
                return FsmStartSetOffline(args);
            });
        }

        private bool FsmStartSetOnline(object[] param)
        {
            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.SetOnline();
            }
            IsOnline = true;
            EV.PostInfoLog(Name, "Change to online");
            return true;
        }
        private bool FsmStartSetOffline(object[] param)
        {
            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.IsOnline = false;
            }
            IsOnline = false;
            EV.PostInfoLog(Name, "Change to offline");
            return true;
        }

        private void Instance_OnAlarmEvent(EventItem obj)
        {


        }

        #region Init
        private bool FsmStartHome(object[] objs)
        {
            _isInited = false;
            return _homeAll.Start() == Result.RUN;
        }

        private bool FsmMonitorHome(object[] objs)
        {
            Result ret = _homeAll.Monitor();
            if (ret == Result.DONE)
            {
                _isInited = true;
                return true;
            }
            if (ret == Result.FAIL)
            {
                PostMsg(MSG.ERROR);
            }
            return false;
        }

        private bool fError(object[] objs)
        {
            if (FsmState == (int)RtState.Transfer)
            {
            }

            return true;
        }


        #endregion

        #region AutoTransfer

        private bool FsmMonitorAutoIdle(object[] param)
        {
            return true;
            //Result ret = _auto.Monitor();

            //if (!_auto.CheckAllJobDone())
            //{
            //    return false;
            //}

            //return ret == Result.DONE;
        }

        private bool FsmStartSetManualMode(object[] objs)
        {
            //if (_auto.HasJobRunning)
            //{
            //    EV.PostWarningLog("System", "Can not change to manual mode, abort running job first");
            //    return false;
            //}

            EV.Notify(UniversalEvents.EquipmentChangeToManual, new SerializableDictionary<string, object>()
            {
            });


            return true;
        }

        private bool fStartAutoTransfer(object[] objs)
        {
            Result ret = _auto.Start(objs);
            if (ret == Result.RUN)
            {
                EV.Notify(UniversalEvents.EquipmentChangeToAuto, new SerializableDictionary<string, object>()
                {
                });
                _isJumpAutoRunning = true;
            }

            return ret == Result.RUN;
        }

        private bool fAutoTransfer(object[] objs)
        {
            Result ret = _auto.Monitor();

            if (_auto.CheckAllJobDone())
            {
                if (!CheckToPostMessage((int)MSG.JobDone))
                    return false;
            }

            return ret == Result.DONE;
        }

        private bool fExitAutoTransfer(object[] objs)
        {
            return true;
        }

        private bool fAbortAutoTransfer(object[] objs)
        {
            return true;
        }

        #endregion


        #region  return all wafer

        //private bool FsmAbortReturnAllWafer(object[] param)
        //{
        //    return true;
        //}

        private bool FsmMonitorReturnAllWafer(object[] param)
        {
            return _returnAll.Monitor() == Result.DONE;
        }

        private bool FsmStartReturnAllWafer(object[] param)
        {
            return _returnAll.Start(param) == Result.RUN;
        }
        #endregion

        #region Transfer
        private bool fStartTransfer(object[] objs)
        {
            Result ret = _manualTransfer.Start(objs);
            if (ret == Result.FAIL || ret == Result.DONE)
                return false;
            return ret == Result.RUN;
        }


        private bool fTransfer(object[] objs)
        {
            Result ret = _manualTransfer.Monitor();

            return ret == Result.DONE;
        }

        private bool fExitTransfer(object[] objs)
        {

            return true;
        }

        private bool fAbortTransfer(object[] objs)
        {
            return true;
        }
        #endregion

        #region reset

        private bool fStartReset(object[] objs)
        {
            EV.ClearAlarmEvent();

            //Singleton<DeviceEntity>.Instance.PostMsg(DeviceEntity.MSG.RESET);

            IoProviderManager.Instance.Reset();

            ResetEquipment();

            foreach (var modulesValue in Modules.Values)
            {
                modulesValue.Reset();
            }

            if (FsmState == (int)RtState.Error)
            {
                if (!_isInited)
                {
                    PostMsg(MSG.ToInit);
                    return false;
                }
                return true;
            }


            return false;
        }

        protected virtual void ResetEquipment()
        {

        }

        #endregion

        private bool FsmFAJobCommand(object[] param)
        {
            switch ((string)param[0])
            {
                case "CreateProcessJob":
                    _auto.CreateProcessJob((string)param[1], (string)param[2], (List<int>)param[3], (bool)param[4], (string)param[5]);
                    break;
                case "CreateControlJob":
                    _auto.CreateControlJob((string)param[1], (string)param[2], (List<string>)param[3], (bool)param[4]);
                    //_auto.StartJob((string)param[1]);
                    CheckToPostMessage((int)MSG.StartJob, (string)param[1]);
                    break;
            }

            return true;
        }
        private bool FsmCreateJob(object[] param)
        {
            _auto.CreateJob((Dictionary<string, object>)param[0]);

            return true;
        }

        private bool FsmAbortJob(object[] param)
        {
            _auto.AbortJob((string)param[0]);
            return true;
        }

        private bool FsmStopJob(object[] param)
        {
            _auto.StopJob((string)param[0]);
            //CheckToPostMessage((int)MSG.StopJob);//
            return true;
        }

        private bool FsmPauseJob(object[] param)
        {
            _auto.PauseJob((string)param[0]);
            return true;
        }

        private bool FsmResumeJob(object[] param)
        {
            _auto.ResumeJob((string)param[0]);
            return true;
        }

        private bool FsmPauseAllJob(object[] param)
        {
            _auto.PauseAllJob();
            return true;
        }

        private bool FsmResumeAllJob(object[] param)
        {
            _auto.ResumeAllJob();
            return true;
        }

        private bool FsmStartJob(object[] param)
        {
            return _auto.StartJob((string)param[0]);
        }

        private bool FsmAbort(object[] param)
        {
            if (FsmState == (int)RtState.Transfer)
            {
                _manualTransfer.Abort();
            }

            if (FsmState == (int)RtState.AutoRunning)
            {
                _auto.Clear();
                _auto.PauseAllJob();
                _isJumpAutoRunning = false;
                WaferManager.Instance.BackupLocationWafers();
            }

            if (FsmState == (int)RtState.Initializing)
            {
                _homeAll.Abort();
            }

            if (FsmState == (int)RtState.ReturnAllWafer)
            {
                _returnAll.Abort();
            }

            return true;
        }


        private bool FsmModuleError(object[] param)
        {
            _auto.ModuleError((string)param[0]);

            return true;
        }

        private bool FsmMap(object[] param)
        {
            _auto.Map((string)param[0]);

            return true;
        }

        private bool FsmToAutoRunning(object[] param)
        {
            return true;
        }

        private bool InvokeReturnWafer(string arg1, object[] args)
        {
            ModuleName target = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];
            bool isPassCooler = (bool)args[2];
            int coolTime = (int)args[3];
            if (ModuleHelper.IsLoadPort(target))
            {
                EV.PostInfoLog("System", string.Format("Wafer already at LoadPort {0} {1}, return operation is not valid", target.ToString(), slot + 1));
                return false;
            }

            if (!WaferManager.Instance.IsWaferSlotLocationValid(target, slot))
            {
                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", target.ToString(), slot.ToString()));
                return false;
            }

            WaferInfo wafer = WaferManager.Instance.GetWafer(target, slot);
            if (wafer.IsEmpty)
            {
                EV.PostInfoLog("System", string.Format("No wafer at {0} {1}, return operation is not valid", target.ToString(), slot + 1));
                return false;
            }

            return CheckToPostMessage((int)MSG.MoveWafer,
                target, slot,
                (ModuleName)wafer.OriginStation, wafer.OriginSlot,
                false, 5, isPassCooler, coolTime);
        }
        private bool InvokeDeleteWafer(string arg1, object[] args)
        {
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];

            if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
            {
                if (WaferManager.Instance.CheckHasWafer(chamber, slot))
                {
                    WaferManager.Instance.DeleteWafer(chamber, slot);
                    if (chamber==ModuleName.TMRobot)
                    {
                        TMRobotSetLoad(slot,false);
                    }
                    EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferDelete, chamber.ToString(), slot + 1);

                }
                else
                {
                    EV.PostInfoLog("System", string.Format("No wafer at {0} {1}, delete not valid", chamber.ToString(), slot + 1));
                }
            }
            else
            {

                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", chamber.ToString(), slot.ToString()));
                return false;
            }

            return true;
        }

        private bool InvokeCreateWafer(string arg1, object[] args)
        {
            ModuleName chamber = ModuleHelper.Converter(args[0].ToString());
            int slot = (int)args[1];
            WaferStatus state = WaferStatus.Normal;


            //if (ModuleHelper.IsLoadPort(chamber))
            //{
            //    var lp = Modules[chamber] as LoadPortModule;
            //    if (lp.LPDevice.CassetteState != LoadportCassetteState.Normal)
            //    {
            //        EV.PostWarningLog("System", $"Can not create wafer at {chamber}.{slot + 1}, Cassette not placed.");
            //        return false;
            //    }
            //}

            if (WaferManager.Instance.IsWaferSlotLocationValid(chamber, slot))
            {
                if (WaferManager.Instance.CheckHasWafer(chamber, slot))
                {
                    EV.PostInfoLog("System", string.Format("{0} slot {1} already has wafer.create wafer is not valid", chamber, slot));
                }
                else if (WaferManager.Instance.CreateWafer(chamber, slot, state) != null)
                {
                    if (chamber == ModuleName.TMRobot)
                    {
                        TMRobotSetLoad(slot, true);
                    }
                    EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferCreate, chamber.ToString(), slot + 1, state.ToString());
                }
            }
            else
            {
                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", chamber.ToString(), slot.ToString()));
                return false;
            }


            return true;
        }

        private bool InvokeMoveWafer(string arg1, object[] args)
        {



            ModuleName moduleFrom = ModuleHelper.Converter(args[0].ToString());
            int slotFrom = (int)args[1];

            ModuleName moduleTo = ModuleHelper.Converter(args[2].ToString());
            int slotTo = (int)args[3];


            if (WaferManager.Instance.IsWaferSlotLocationValid(moduleTo, slotTo))
            {
                if (WaferManager.Instance.CheckHasWafer(moduleFrom, slotFrom))
                {
                    if (WaferManager.Instance.CheckHasWafer(moduleTo, slotTo))
                    {
                        EV.PostWarningLog("System", string.Format("Has wafer at {0} {1}, move not valid", moduleTo.ToString(), slotTo + 1));
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(moduleFrom, slotFrom, moduleTo, slotTo);
                        EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferMoved, moduleFrom.ToString(), slotFrom + 1);
                    }


                }
                else
                {
                    EV.PostInfoLog("System", string.Format("No wafer at {0} {1}, move not valid", moduleFrom.ToString(), slotFrom + 1));
                }
            }
            else
            {

                EV.PostWarningLog("System", string.Format("Invalid position，{0}，{1}", moduleTo.ToString(), slotTo + 1));
                return false;
            }

            return true;
        }

        private bool InvokeShutDown(string arg1, object[] arg2)
        {
            if (IsAlarm || IsIdle || IsInit)
            {
                EV.PostWarningLog(Module, $"System start shut down");

                EV.PostKickoutMessage("ShutDown");

                RtApplication.Instance.Terminate();

                return true;
            }

            EV.PostWarningLog(Module, $"System in {StringFsmStatus} mode, can not shut down");
            return false;
        }


        private bool InvokeEfemMap(string arg1, object[] arg2)
        {
            ModuleName target = ModuleHelper.Converter((string)arg2[0]);
            if (!ModuleHelper.IsLoadPort(target))
            {
                EV.PostWarningLog("System", $"Invalid map target {target}");
                return false;
            }

            for (int i = 0; i < 25; i++)
            {
                WaferInfo wafer = WaferManager.Instance.GetWafer(target, i);
                if (wafer.IsEmpty)
                    continue;
                if (wafer.ProcessState == EnumWaferProcessStatus.Completed ||
                    wafer.ProcessState == EnumWaferProcessStatus.Failed)
                {
                    EV.PostWarningLog("System", $"{target} wafer is processed, can not map again");
                    return false;
                }
            }


            if (IsAutoMode)
            {
                _auto.Map((string)arg2[0]);

                return true;
            }

            //EfemModule efem = Modules[ModuleName.EfemRobot] as EfemModule;

            //if (!efem.Map(ModuleHelper.Converter((string) arg2[0]), out string reason))
            //{
            //    EV.PostWarningLog(Module, reason);
            //}
            return true;
        }

        private void OnModuleError(string module)
        {
            if (FsmState == (int)RtState.AutoRunning)
            {
                ModuleName mod = ModuleHelper.Converter(module);

                PostMsg(MSG.ModuleError, module);
            }
        }


        public override void Monitor()
        {
            base.Monitor();
        }

        private bool OnTimer()
        {
            try
            {
                if (_auto != null && _auto.HasJobRunning && _isJumpAutoRunning && FsmState == (int)RtState.AutoIdle)
                {
                    PostMsg(MSG.ToAutoRunning);
                }

                if (SC.ContainsItem($"VCE.EnableClearJobWhenCassetteRemoved") && SC.GetValue<bool>($"VCE.EnableClearJobWhenCassetteRemoved"))
                {
                    List<string> VCEs = new List<string>() { "VCEA", "VCEB" };

                    foreach (var VCE in VCEs)
                    {
                        var isPresent = DATA.Poll($"{VCE}.IsPresent");

                        if (isPresent == null)
                            continue;

                        bool isPreviousPresent = VCE == "VCEA" ? _isPreviousVCEACassPresent : _isPreviousVCEBCassPresent;

                        if (isPreviousPresent && !(bool)isPresent)
                        {
                            string jobName = (string)DATA.Poll($"{VCE}.LocalJobName");
                            if (!string.IsNullOrEmpty(jobName))
                            {
                                _auto.AbortJob(jobName);
                                EV.PostWarningLog("System", $"{VCE} cassette is not present, clear {VCE} job {jobName}");
                            }
                            else
                            {
                                EV.PostWarningLog("System", $"{VCE} cassette is not present, no job in {VCE}");
                            }
                        }

                        if (VCE == "VCEA")
                            _isPreviousVCEACassPresent = (bool)isPresent;
                        else
                            _isPreviousVCEBCassPresent = (bool)isPresent;
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }
    }
}
