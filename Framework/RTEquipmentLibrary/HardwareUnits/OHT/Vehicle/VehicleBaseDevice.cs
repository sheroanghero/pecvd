using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using MECF.Framework.Common.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Vehicle
{
    public class VehicleBaseDevice : Entity, IEntity, IDevice, IVehicle
    {
        public VehicleBaseDevice(string module, string name) : base()
        {
            Module = module;
            Name = name;
            VehicleName = name;
            InitializeVehicleBase();
        }

        private void InitializeVehicleBase()
        {
            BuildTransitionTable();



        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<VehicleBaseDevice>(Module + Name + ".StateMachine", (int)VehicleStateEnum.Init, 10);
            AnyStateTransition(VehicleMsg.Error, fError, VehicleStateEnum.Error);
            AnyStateTransition((int)FSM_MSG.ALARM, fError, (int)VehicleStateEnum.Error);
            AnyStateTransition(VehicleMsg.Abort, fAbrot, VehicleStateEnum.Init);

            Transition(VehicleStateEnum.Idle, VehicleMsg.Reset, fStartReset, VehicleStateEnum.Resetting);
            Transition(VehicleStateEnum.Init, VehicleMsg.Reset, fStartReset, VehicleStateEnum.Resetting);
            Transition(VehicleStateEnum.Error, VehicleMsg.Reset, fStartReset, VehicleStateEnum.Resetting);
            Transition(VehicleStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, VehicleStateEnum.Idle);

            Transition(VehicleStateEnum.Idle, VehicleMsg.Init, fStartInit, VehicleStateEnum.Initializing);
            Transition(VehicleStateEnum.Init, VehicleMsg.Init, fStartInit, VehicleStateEnum.Initializing);
            Transition(VehicleStateEnum.Error, VehicleMsg.Init, fStartInit, VehicleStateEnum.Initializing);
            Transition(VehicleStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, VehicleStateEnum.Idle);

            Transition(VehicleStateEnum.Idle, FSM_MSG.TIMER, fMonitorIdle, VehicleStateEnum.Idle);
            Transition(VehicleStateEnum.Idle, VehicleMsg.Move, fStartMove, VehicleStateEnum.Moving);
            Transition(VehicleStateEnum.Idle, VehicleMsg.Pick, fStartPick, VehicleStateEnum.Picking);
            Transition(VehicleStateEnum.Idle, VehicleMsg.Place, fStartPlace, VehicleStateEnum.Placing);


            Transition(VehicleStateEnum.Moving, FSM_MSG.TIMER, fMonitorMoving, VehicleStateEnum.Idle);
            Transition(VehicleStateEnum.Picking, FSM_MSG.TIMER, fMonitorPicking, VehicleStateEnum.Idle);
            Transition(VehicleStateEnum.Placing, FSM_MSG.TIMER, fMonitorPlacing, VehicleStateEnum.Idle);

            Transition(VehicleStateEnum.Moving, VehicleMsg.Pause, fStartPause, VehicleStateEnum.Pausing);
            Transition(VehicleStateEnum.Picking, VehicleMsg.Pause, fStartPause, VehicleStateEnum.Pausing);
            Transition(VehicleStateEnum.Placing, VehicleMsg.Pause, fStartPause, VehicleStateEnum.Pausing);

            Transition(VehicleStateEnum.Pausing, FSM_MSG.TIMER, fMonitorPausing, VehicleStateEnum.Paused);



            Transition(VehicleStateEnum.Paused, VehicleMsg.ResumeToMove, fResumeToMove, VehicleStateEnum.Moving);
            Transition(VehicleStateEnum.Paused, VehicleMsg.ResumeToPick, fResumeToPick, VehicleStateEnum.Picking);
            Transition(VehicleStateEnum.Paused, VehicleMsg.ResumeToPlace, fResumeToPlace, VehicleStateEnum.Placing);

            Transition(VehicleStateEnum.Idle, VehicleMsg.SetMaitenance, fStartMaitenance, VehicleStateEnum.Maintenance);
            Transition(VehicleStateEnum.Maintenance, VehicleMsg.SetMaitenance, fStartIdle, VehicleStateEnum.Idle);

        }

       

        public bool IsBusy { get; protected set; }


        public bool ExecuteReset()
        {
            if(CheckToPostMessage( VehicleMsg.Reset,null))
            {
                IsBusy = true;
                return true;
            }
            return false;
            
        }

        public bool ExecuteInit()
        {
            if (CheckToPostMessage(VehicleMsg.Init, null))
            {
                IsBusy = true;
                return true;
            }
            return false;

        }

        public bool ExecuteMoveToPosition(uint targetPosition,uint[] viaPositions)
        {
            if (CheckToPostMessage(VehicleMsg.Move, new object[] {targetPosition,viaPositions }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }

        public bool ExecutePickFromPosition(uint targetPosition, uint[] viaPositions)
        {
            if (CheckToPostMessage(VehicleMsg.Pick, new object[] { targetPosition, viaPositions }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }
        public bool ExecutePlaceToPosition(uint targetPosition, uint[] viaPositions)
        {
            if (CheckToPostMessage(VehicleMsg.Place, new object[] { targetPosition, viaPositions }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }

        public bool ExecutePause()
        {
            if (CheckToPostMessage(VehicleMsg.Pause, null))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }

        public virtual bool ExecuteClearCommand()
        {
            return true;
        }

        public virtual bool ExecuteChangeRoute(uint targetPosition,uint[] viaPositions)
        {
            return true;
        }
        public virtual bool ExecuteReadCarrierID()
        {
            return true;

        }


        protected virtual bool fStartPause(object[] param)
        {
            return true;
        }
        protected virtual bool fMonitorPausing(object[] param)
        {
            return true;
        }

        protected virtual bool fStartInit(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorInit(object[] param)
        {
            return true;
        }


        protected virtual bool fAbrot(object[] param)
        {
            return true;
        }

        protected virtual bool fStartReset(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorIdle(object[] param)
        {
            return true;
        }

        protected virtual bool fStartMove(object[] param)
        {
            return true;
        }

        protected virtual bool fStartPick(object[] param)
        {
            return true;
        }

        protected virtual bool fStartPlace(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorMoving(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorPicking(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorPlacing(object[] param)
        {
            return true;
        }

        protected virtual bool fPauseMoving(object[] param)
        {
            return true;
        }

        protected virtual bool fPausePicking(object[] param)
        {
            return true;
        }

        protected virtual bool fPausePlacing(object[] param)
        {
            return true;
        }

        protected virtual bool fResumeToMove(object[] param)
        {
            return true;
        }

        protected virtual bool fResumeToPick(object[] param)
        {
            return true;
        }

        protected virtual bool fResumeToPlace(object[] param)
        {
            return true;
        }

        protected virtual bool fStartMaitenance(object[] param)
        {
            return true;
        }

        protected virtual bool fStartIdle(object[] param)
        {
            return true;
        }

        protected virtual bool fError(object[] param)
        {
            return true;
        }
        public virtual void OnError(string error = "")
        {
            EV.PostAlarmLog($"Vehicle {Name}", error);
            IsBusy = false;
            CheckToPostMessage(VehicleMsg.Error, null);

        }
        public string VehicleName { get; set; }
        public string Module { get; set; }
       
        public UInt32 CurrentPosition { get; set; }

        public string Name { get ; set; }

        public bool HasAlarm => throw new NotImplementedException();

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

        public bool Check(int msg, out string reason, params object[] args)
        {
            reason = "";
            return true;
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
               
                EV.PostWarningLog(Name, $"{Name} is in { (VehicleStateEnum)fsm.State} state，can not do {(VehicleMsg)msg}");
                return false;
            }
            fsm.PostMsg(msg, args);
            return true;
        }
        public bool CheckToPostMessage(VehicleMsg msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
               EV.PostWarningLog(Name, $"{Name} is in { (VehicleStateEnum)fsm.State} state，can not do {msg}");
                return false;
            }

            CurrentParameters = args;
            fsm.PostMsg((int)msg, args);
            return true;
        }

        public object[] CurrentParameters { get; private set; }

        public void Monitor()
        {
           
        }

        public void Reset()
        {
            
        }
    }
}
