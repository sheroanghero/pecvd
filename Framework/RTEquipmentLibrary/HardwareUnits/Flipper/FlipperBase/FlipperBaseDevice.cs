using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Flipper.FlipperBase
{
    public abstract class FlipperBaseDevice : Entity, IEntity, IFlipper
    {
        public const string EventWaferTurnOverStart = "WAFER_TURN_OVER_START";
        public const string EventWaferTurnOverEnd = "WAFER_TURN_OVER_COMPLETE";
        public ModuleName FlipperModuleName { get; protected set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public bool IsBusy { get; set; }

        public abstract FlipperPosEnum CurrentFlipperPosition { get; }
        public abstract GripPosEnum CurrentGripperPositon { get; }



        public FlipperStateEnum CurrentState => (FlipperStateEnum)fsm.State;

        public virtual bool IsReady => CurrentState == FlipperStateEnum.Idle && !IsBusy;

        public virtual bool IsPlacement { get; set; }


        public virtual int TimelimitAction
        {
            get
            {
                if (SC.ContainsItem($"Turnover.TimeLimitTurnoverAction"))
                    return SC.GetValue<int>($"Turnover.TimeLimitTurnoverAction");
                return 60;
            }
        }
        public WaferSize DefaultWaferSize
        {
            get; set;
        } = WaferSize.WS12;
        public virtual WaferSize GetCurrentWaferSize()
        {
            return DefaultWaferSize;
        }

        public FlipperBaseDevice(string module, string name) : base()
        {
            Module = module;
            Name = name;
            FlipperModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
          


            InitializeFlipper();
        }

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

        private void InitializeFlipper()
        {
            BuildTransitionTable();
            SubscribeDataVariable();
            SubscribeOperation();
        }

        public WaferSize CurrentWaferSize
        {
            get
            {
                if (WaferManager.Instance.CheckHasWafer(FlipperModuleName, 0))
                    return WaferManager.Instance.GetWaferSize(FlipperModuleName, 0);
                return GetCurrentWaferSize();
            }
        }


        private void SubscribeDataVariable()
        {
            DATA.Subscribe($"{Module}.{Name}.State", () => CurrentState.ToString());
            DATA.Subscribe($"{Module}.{Name}.CurrentFlipperPosition", () => CurrentFlipperPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.CurrentGripperPositon", () => CurrentGripperPositon.ToString());

            DATA.Subscribe($"{Name}.WaferSize", () => CurrentWaferSize.ToString());

            EV.Subscribe(new EventItem("Event", EventWaferTurnOverStart, "Flipper start turn over."));
            EV.Subscribe(new EventItem("Event", EventWaferTurnOverEnd, "Flipper complete turn over."));

        }

        private void SubscribeOperation()
        {
            OP.Subscribe($"{Module}.{Name}.ResetError", (cmd, param) =>
            {
                if (!ExecuteReset(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not reset error, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Home", (cmd, param) =>
            {
                if (!ExecuteHome(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not home, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Grip", (cmd, param) =>
            {
                if (!ExecuteGrip(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not grip, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Ungrip", (cmd, param) =>
            {
                if (!ExecuteUnGrip(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not ungrip, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.TurnTo0", (cmd, param) =>
            {
                if (!ExecuteTurnTo0(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 0 degree, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.TurnTo180", (cmd, param) =>
            {
                if (!ExecuteTurnTo180(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 180 degree, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Stop", (cmd, param) =>
            {
                if (!ExecuteStop(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not turn to 180 degree, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Name}.SetWaferSize", (cmd, param) =>
            {
                if (param.Length < 2)
                {
                    return false;
                }

                WaferSize size = (WaferSize)Enum.Parse(typeof(WaferSize), param[1].ToString());
                return SetWaferSize(size);
            });
        }

        public virtual bool SetWaferSize(WaferSize size)
        {
            if (WaferManager.Instance.CheckHasWafer(FlipperModuleName, 0))
            {
                WaferManager.Instance.GetWafer(FlipperModuleName, 0).Size = size;
            }
            return true;
        }

        public bool ExecuteHome(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.StartHome, null);
        }

        public bool ExecuteGrip(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.Grip, null);
        }

        public bool ExecuteUnGrip(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.Ungrip, null);
        }

        public bool ExecuteTurnTo0(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.TurnTo0, null);
        }

        public bool ExecuteTurnTo180(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.TurnTo180, null);
        }

        public bool ExecuteStop(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.Abort, null);
        }

        public bool ExecuteReset(out string reason)
        {
            reason = "";
            return CheckToPostMessage(FlipperMsgEnum.Reset, null);
        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<FlipperBaseDevice>(Name + ".FlipperStateMachine", (int)FlipperStateEnum.Init, 50);
            AnyStateTransition(FlipperMsgEnum.Error, fError, FlipperStateEnum.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)FlipperStateEnum.Error);
            AnyStateTransition(FlipperMsgEnum.Abort, fStartAbort, FlipperStateEnum.Init);

            Transition(FlipperStateEnum.Init, FlipperMsgEnum.Reset, fStartReset, FlipperStateEnum.Resetting);
            Transition(FlipperStateEnum.Error, FlipperMsgEnum.Reset, fStartReset, FlipperStateEnum.Resetting);
            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.Reset, fStartReset, FlipperStateEnum.Resetting);

            Transition(FlipperStateEnum.Resetting, FlipperMsgEnum.ResetComplete, fCompleteReset, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Resetting, FlipperMsgEnum.ActionDone, fCompleteReset, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, FlipperStateEnum.Idle);

            Transition(FlipperStateEnum.Init, FlipperMsgEnum.StartHome, fStartHome, FlipperStateEnum.Homing);
            Transition(FlipperStateEnum.Error, FlipperMsgEnum.StartHome, fStartHome, FlipperStateEnum.Homing);
            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.StartHome, fStartHome, FlipperStateEnum.Homing);

            Transition(FlipperStateEnum.Homing, FlipperMsgEnum.HomeComplete, fCompleteHome, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Homing, FlipperMsgEnum.ActionDone, fCompleteHome, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Homing, FSM_MSG.TIMER, fMonitorHome, FlipperStateEnum.Idle);

            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.TurnTo0, fStartTurnTo0, FlipperStateEnum.TurningTo0);

            Transition(FlipperStateEnum.TurningTo0, FlipperMsgEnum.HomeComplete, fCompleteTurn, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.TurningTo0, FlipperMsgEnum.ActionDone, fCompleteTurn, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.TurningTo0, FSM_MSG.TIMER, fMonitorTurnTo0, FlipperStateEnum.Idle);

            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.TurnTo180, fStartTurnTo180, FlipperStateEnum.TurningTo180);

            Transition(FlipperStateEnum.TurningTo180, FlipperMsgEnum.TurnComplete, fCompleteTurn, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.TurningTo180, FlipperMsgEnum.ActionDone, fCompleteTurn, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.TurningTo180, FSM_MSG.TIMER, fMonitorTurnTo180, FlipperStateEnum.Idle);

            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.Grip, fStartGrip, FlipperStateEnum.Gripping);

            Transition(FlipperStateEnum.Gripping, FlipperMsgEnum.GripComplete, fCompleteGrip, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Gripping, FlipperMsgEnum.ActionDone, fCompleteGrip, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.Gripping, FSM_MSG.TIMER, fMonitorGrip, FlipperStateEnum.Idle);

            Transition(FlipperStateEnum.Idle, FlipperMsgEnum.Ungrip, fStartUnGrip, FlipperStateEnum.UnGripping);

            Transition(FlipperStateEnum.UnGripping, FlipperMsgEnum.UngripComplete, fCompleteUnGrip, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.UnGripping, FlipperMsgEnum.ActionDone, fCompleteUnGrip, FlipperStateEnum.Idle);
            Transition(FlipperStateEnum.UnGripping, FSM_MSG.TIMER, fMonitorUnGrip, FlipperStateEnum.Idle);
        }

        protected abstract bool fStartGrip(object[] param);

        protected virtual bool fCompleteGrip(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorGrip(object[] param)
        {
            return false;
        }

        protected abstract bool fStartUnGrip(object[] param);

        protected virtual bool fCompleteUnGrip(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorUnGrip(object[] param)
        {
            return false;
        }

        protected virtual bool fCompleteReset(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return false;
        }

        protected abstract bool fStartHome(object[] param);

        protected virtual bool fCompleteHome(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorHome(object[] param)
        {
            return false;
        }

        protected abstract bool fStartTurnTo0(object[] param);
        protected abstract bool fStartTurnTo180(object[] param);





        protected virtual bool fMonitorTurnTo0(object[] param)
        {



            return false;
        }

        protected virtual bool fCompleteTurn(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorTurnTo180(object[] param)
        {
            return false;
        }

        protected virtual bool fError(object[] param)
        {
            return true;
        }

        protected abstract bool fStartAbort(object[] param);

        protected abstract bool fStartReset(object[] param);

        public void OnError(string msg)
        {
            EV.PostAlarmLog($"Flipper", $"{FlipperModuleName} occurred error:{msg}");
            CheckToPostMessage(FlipperMsgEnum.Error, null);
        }

        public object[] CurrentParamter { get; private set; }

        public virtual bool HasAlarm { get; set; }

        public bool CheckToPostMessage(FlipperMsgEnum msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
                if ((FlipperMsgEnum)msg != FlipperMsgEnum.ActionDone)
                    EV.PostWarningLog(Name, $"{Name} is in { (FlipperStateEnum)fsm.State} state，can not do {(FlipperMsgEnum)msg}");
                return false;
            }
            CurrentParamter = args;
            if (msg == FlipperMsgEnum.Reset || msg== FlipperMsgEnum.StartHome || msg== FlipperMsgEnum.StartInit || msg== FlipperMsgEnum.TurnTo0
                || msg == FlipperMsgEnum.TurnTo180 || msg==FlipperMsgEnum.Grip || msg == FlipperMsgEnum.Ungrip)
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
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (FlipperStateEnum)fsm.State, (FlipperMsgEnum)msg);
                return false;
            }
            reason = "";

            return true;
        }

        public void Monitor()
        {
            
        }

        public void Reset()
        {
        }
    }




    public enum FlipperStateEnum
    {
        Init,
        Idle,
        Homing,
        TurningTo0,
        TurningTo180,
        

        Gripping,
        UnGripping,
        Error,
        OnErrorCleaning,
        Stopping,
        Stop,
        Resetting,
    }

    public enum FlipperMsgEnum
    {
        Reset,
        StartInit,
        StartHome,
        TurnTo0,
        TurnTo180,
        Grip,
        Ungrip,
        GripComplete,
        UngripComplete,
        Stop,
        Error,
        Abort,
        ActionDone,
        ResetComplete,
        HomeComplete,
        TurnComplete
    }

    public enum GripPosEnum
    {
        Unknow,
        Close,
        Open,
        
    }

    public enum FlipperPosEnum
    {
        Unknow,
        FrontSide,
        BackSide,
        

    }




}
