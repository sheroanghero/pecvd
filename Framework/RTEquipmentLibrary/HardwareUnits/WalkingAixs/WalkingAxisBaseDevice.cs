using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.DataCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.Event;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs
{
    public abstract class WalkingAxisBaseDevice : Entity, IDevice
    {
        public bool IsBusy { get; protected set; }
        protected DateTime _dtActionStart;
        protected WalkingAxisBaseDevice(string module, string name)
            : base()
        {
            Module = module;
            Name = name;
            InitializeMotion();
                   
        }

        private void InitializeMotion()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            Running = true;

        }

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public bool HasAlarm { get; }

        public string Module { get; set; }
        public string Name { get; set ; }
        public WalkingAxisState DeviceState { get => (WalkingAxisState)fsm.State; }
        public abstract int GetCurrentStation(); 

        public bool IsArrivedTarget
        {
            get { return (TargetStation == GetCurrentStation()); }
        }
        public int TargetStation { get; set; }
        public abstract void Monitor();
        public virtual int TimeLimitHome
        {
            get
            {
                if (SC.ContainsItem($"WalkingAxis.{Name}.HomeTimeout"))
                    return SC.GetValue<int>($"WalkingAxis.{Name}.HomeTimeout");
                return 180;

            }
        }
        public virtual int TimeLimitMove
        {
            get
            {
                if (SC.ContainsItem($"WalkingAxis.{Name}.MoveTimeout"))
                    return SC.GetValue<int>($"WalkingAxis.{Name}.MoveTimeout");
                return 60;

            }
        }

        public void Reset()
        {
            
        }
        public enum WalkingAxisMsg
        {
            Reset,
            Init,
            StartMove,
            Stop,
            Error,
            ActionDone,
        }
        public enum WalkingAxisState
        {
            Init,
            Initializing,
            Idle,
            Moving,
            ERROR,
            Stopped,
            Busy,
            Resetting,
        }
        
      
        private void BuildTransitionTable()
        {
            fsm = new StateMachine<WalkingAxisBaseDevice>(Module + Name + ".StateMachine", (int)WalkingAxisState.Init, 50);

            AnyStateTransition(WalkingAxisMsg.Error, fError, WalkingAxisState.ERROR);
            AnyStateTransition(WalkingAxisMsg.Stop, fStop, WalkingAxisState.Stopped);

            Transition(WalkingAxisState.Init, WalkingAxisMsg.Init, fStartInit, WalkingAxisState.Initializing);
            Transition(WalkingAxisState.Initializing, FSM_MSG.TIMER, fMonitorInit, WalkingAxisState.Idle);
            Transition(WalkingAxisState.Initializing, WalkingAxisMsg.ActionDone, fInitComplete, WalkingAxisState.Idle);
            
            Transition(WalkingAxisState.Idle, WalkingAxisMsg.Init, fStartInit, WalkingAxisState.Initializing);
            Transition(WalkingAxisState.Idle, WalkingAxisMsg.StartMove, fStartMove, WalkingAxisState.Moving);

            Transition(WalkingAxisState.Moving, FSM_MSG.TIMER, fMonitorMove, WalkingAxisState.Idle);
            Transition(WalkingAxisState.Moving, WalkingAxisMsg.ActionDone, fMoveComplete, WalkingAxisState.Idle);
            
            Transition(WalkingAxisState.ERROR, WalkingAxisMsg.Reset, fStartReset, WalkingAxisState.Resetting);
            Transition(WalkingAxisState.Stopped, WalkingAxisMsg.Reset, fStartReset, WalkingAxisState.Resetting);
            Transition(WalkingAxisState.Idle, WalkingAxisMsg.Reset, fStartReset, WalkingAxisState.Resetting);
            Transition(WalkingAxisState.Init, WalkingAxisMsg.Reset, fStartReset, WalkingAxisState.Resetting);

            Transition(WalkingAxisState.Resetting, FSM_MSG.TIMER, fMonitorResetting, WalkingAxisState.Idle);


        }
        protected virtual bool fMonitorResetting(object[] param)
        {
            return true;
        }
        public virtual bool IsReady()
        {
            return ((!IsBusy) && (DeviceState == WalkingAxisState.Idle));
        }

        private void SubscribeDataVariable()
        {
            DATA.Subscribe($"{Module}.{Name}.State", () => DeviceState.ToString());
        }
        private void SubscribeOperation()
        {
            OP.Subscribe($"{Module}.{Name}.Home", (string cmd, object[] param) =>
            {
                string reason = "";
                if (!Home(null))
                {

                    EV.PostWarningLog(Module, $"{Name} can not home, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} home");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Reset", (string cmd, object[] param) =>
            {
                string reason = "";
                if (!Reset(null))
                {
                    EV.PostWarningLog(Module, $"{Name} can not clear alarm, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} reset alarm");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.MoveTo", (string cmd, object[] param) =>
            {
                string reason = "";                
                if (!MoveTo(param))
                {
                    EV.PostWarningLog(Module, $"{Name} can not move to {param[0].ToString()}, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} move to {param[0]}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Stop", (string cmd, object[] param) =>
            {
                string reason = "";
                if (!Stop(null))
                {

                    EV.PostWarningLog(Module, $"{Name} can not Stop, {reason}");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} Stop");
                return true;
            });

        }
        protected virtual bool fStop(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected abstract bool fStartReset(object[] param);       

        protected abstract bool fMonitorMove(object[] param);
        protected abstract bool fStartMove(object[] param);


        protected abstract bool fMonitorInit(object[] param);
        protected abstract bool fStartInit(object[] param);

        protected virtual bool fError(object[] param)
        {
            return true;
        }
        protected virtual bool fInitComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected virtual bool fMoveComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }
        public virtual bool Home(object[] param)
        {
            IsBusy = true;
            return CheckToPostMessage((int)WalkingAxisMsg.Init, param);
        }
        public virtual bool MoveTo(object[] param)
        {
            int temp;
            if (int.TryParse(param[0].ToString(), out temp))
            {
                TargetStation = temp;
                IsBusy = true;
                return CheckToPostMessage((int)WalkingAxisMsg.StartMove, new object[] { temp});
            }
            return false;


        }

        public virtual bool MoveTo(ModuleName module)
        {
            int temp;
            if (!SC.ContainsItem($"WalkingAxis.{Name}.{module}StationIndex")) return false;
            temp = SC.GetValue<int>($"WalkingAxis.{Name}.{module}StationIndex");
            TargetStation = temp;
            IsBusy = true;
            return CheckToPostMessage((int)WalkingAxisMsg.StartMove, new object[] { temp });
           
            
        }
        public virtual int GetStationIndex(ModuleName module)
        {
            if (!SC.ContainsItem($"WalkingAxis.{Name}.{module}StationIndex")) return 0;
            return SC.GetValue<int>($"WalkingAxis.{Name}.{module}StationIndex");
        }

        public virtual bool Reset(object[] param)
        {
            IsBusy = false;
            return CheckToPostMessage((int)WalkingAxisMsg.Reset, param);
        }
        public virtual bool Stop(object[] param)
        {
            IsBusy = true;
            return CheckToPostMessage((int)WalkingAxisMsg.Stop, param);
        }


        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            return CheckToPostMessage((int)WalkingAxisMsg.ActionDone, param);
        }
        public virtual bool OnError(object[] param)
        {
            return CheckToPostMessage((int)WalkingAxisMsg.Error, param);
        }

        public virtual bool OnError(string msg)
        {
            EV.PostAlarmLog("WalkingAxis", $"{Name} occurred error:{msg}.");
            return CheckToPostMessage((int)WalkingAxisMsg.Error, null);
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                EV.PostWarningLog(Name, $"{Name} is in { (WalkingAxisState)fsm.State} state，can not do {(WalkingAxisMsg)msg}");
                return false;
            }

            fsm.PostMsg(msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (WalkingAxisState)fsm.State, (WalkingAxisMsg)msg);
                return false;
            }

            reason = "";

            return true;
        }



    }
}
