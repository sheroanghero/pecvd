using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase
{
    public abstract class CIDReaderBaseDevice : Entity, IDevice, ICarrierIDReader, IEntity
    {
        public event Action<ModuleName, string,string> OnCarrierIDReadEvent;
        public event Action<ModuleName, string,string> OnCarrierIDReadFailedEvent;
        public event Action<ModuleName, string,string> OnCarrierIDWriteEvent;
        public event Action<ModuleName,string> OnCarrierIDWriteFailedEvent;
        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public LoadPortBaseDevice ReaderOnLP { get; private set; }
        public ModuleName ReaderOnModule { get; private set; }
        public int CarrierIDIndex { get; protected set; }
        public int CarrierIDStartPage { get; protected set; }
        public int CarrierIDPageLength { get; protected set; }

        public bool IsUpdateLPCarrierID { get; set; }

        public string Module { get; set; }
        public string Name { get; set; }
        public string CarrierIDBeRead { get; set; }
        public string CarrierIDToBeWriten { get; set; }

        public bool HasAlarm { get; set; }
        public bool IsBusy { get; set; }
        public virtual bool IsNeedTrimSpace
        {
            get
            {
                if (SC.ContainsItem($"CarrierID.{Name}.CarrierIdNeedTrimSpace"))
                    return SC.GetValue<bool>($"CarrierID.{Name}.CarrierIdNeedTrimSpace");
                return true;
            }
        }

        public virtual int RetryTime
        {
            get
            {
                if (SC.ContainsItem($"CarrierID.{Name}.RetryTime"))
                    return SC.GetValue<int>($"CarrierID.{Name}.RetryTime");
                return 1;
            }
        }

        public virtual int ActionTimeLimit
        {
            get
            {
                if (SC.ContainsItem($"CarrierID.{Name}.ActionTimeLimit"))
                    return SC.GetValue<int>($"CarrierID.{Name}.ActionTimeLimit");
                return 10;
            }
        }

        public virtual bool IsReady => DeviceState == CIDReaderStateEnum.Idle && !IsBusy;



        public CIDReaderStateEnum DeviceState => (CIDReaderStateEnum)(int)fsm.State;

        public CIDReaderBaseDevice(string module,string name,LoadPortBaseDevice lp =null,int readerIndex=1,bool updateLpCarrierID = true):base()
        {
            
            Module = module;
            Name = name;
            ReaderOnLP = lp;
            if (ReaderOnLP != null)
            {
                ReaderOnModule = lp.LPModuleName;
            }
            else
                ReaderOnModule = ModuleName.System;

            CarrierIDIndex = readerIndex;

            IsUpdateLPCarrierID = updateLpCarrierID;
            InitializeCIDReader();
        }

        private void InitializeCIDReader()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();
            Running = true;
        }
        protected override bool Init()
        {
            return base.Init();
        }

        private void SubscribeDeviceOperation()
        {
            ;
        }

        private void SubscribeOperation()
        {
            
        }

        private void SubscribeDataVariable()
        {
            
        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<CIDReaderBaseDevice>(Module + Name + ".CIDReaderStateMachine", (int)CIDReaderStateEnum.Init, 50);
            AnyStateTransition(CIDMsg.Error, fError, CIDReaderStateEnum.Error);
            AnyStateTransition(CIDMsg.Reset, fStartReset, CIDReaderStateEnum.Resetting);

            Transition(CIDReaderStateEnum.Resetting, CIDMsg.ActionDone, fResetComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.Resetting, CIDMsg.ResetComplete, fResetComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, CIDReaderStateEnum.Idle);

            Transition(CIDReaderStateEnum.Init, CIDMsg.StartInit, fStartInit, CIDReaderStateEnum.Initializing);
            Transition(CIDReaderStateEnum.Initializing, CIDMsg.InitComplete, fInitComplete, CIDReaderStateEnum.Initializing);

            Transition(CIDReaderStateEnum.Initializing, CIDMsg.ActionDone, fInitComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, CIDReaderStateEnum.Idle);

            Transition(CIDReaderStateEnum.Error, CIDMsg.Clear, fStartClear, CIDReaderStateEnum.Idle);

            Transition(CIDReaderStateEnum.Idle, CIDMsg.SetParameter, fStartSetParameter, CIDReaderStateEnum.SetParameter);
            Transition(CIDReaderStateEnum.SetParameter, CIDMsg.SetComplete, fSetParameterComplete, CIDReaderStateEnum.SetParameter);
            Transition(CIDReaderStateEnum.SetParameter, CIDMsg.ActionDone, fSetParameterComplete, CIDReaderStateEnum.SetParameter);

            Transition(CIDReaderStateEnum.Idle, CIDMsg.ReadParameter, fStartReadParameter, CIDReaderStateEnum.ReadParameter);
            Transition(CIDReaderStateEnum.ReadParameter, CIDMsg.ReadParaComplete, fReadParameterComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.ReadParameter, CIDMsg.ActionDone, fReadParameterComplete, CIDReaderStateEnum.Idle);

            Transition(CIDReaderStateEnum.Idle, CIDMsg.ReadCarrierID, fStartReadCarrierID, CIDReaderStateEnum.ReadCarrierID);
            Transition(CIDReaderStateEnum.ReadCarrierID, CIDMsg.ReadCarrierIDComplete, fReadCarrierIDComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.ReadCarrierID, CIDMsg.ActionDone, fReadCarrierIDComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.ReadCarrierID, FSM_MSG.TIMER, fMonitorReadCarrierID, CIDReaderStateEnum.Idle);


            Transition(CIDReaderStateEnum.Idle, CIDMsg.WriteCarrierID, fStartWriteCarrierID, CIDReaderStateEnum.WriteCarrierID);
            Transition(CIDReaderStateEnum.WriteCarrierID, CIDMsg.WriteCarrierIDComplete, fWriteCarrierIDComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.WriteCarrierID, CIDMsg.ActionDone, fWriteCarrierIDComplete, CIDReaderStateEnum.Idle);
            Transition(CIDReaderStateEnum.WriteCarrierID, FSM_MSG.TIMER, fMonitorWriteCarrierID, CIDReaderStateEnum.Idle);

        }

        protected virtual bool fMonitorWriteCarrierID(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReadCarrierID(object[] param)
        {
            return false;
        }

        protected virtual bool fWriteCarrierIDComplete(object[] param)
        {
            return true; ;
        }

        protected virtual bool fStartWriteCarrierID(object[] param)
        {
            return true;
        }

        protected virtual bool fReadCarrierIDComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fReadParameterComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fSetParameterComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartReadCarrierID(object[] param)
        {
            return true;
        }

        protected virtual bool fStartReadParameter(object[] param)
        {
            return true;
        }

        protected virtual bool fStartSetParameter(object[] param)
        {
            return true;
        }

        protected virtual bool fStartClear(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorInit(object[] param)
        {
            return true;
        }

        protected virtual bool fInitComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartInit(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return true;
        }

        protected virtual bool fError(object[] param)
        {
            return true;
        }
        protected virtual bool fStartReset(object[] param)
        {
            return true;
        }
        protected virtual bool fResetComplete(object[] param)
        {
            return true;
        }

        public void OnCarrierIDRead(string carrierID)
        {
            if (OnCarrierIDReadEvent != null)
                OnCarrierIDReadEvent(ReaderOnModule, Name, carrierID);
            if (ReaderOnLP != null)
                ReaderOnLP.OnCarrierIdRead(ReaderOnModule, carrierID,CarrierIDIndex,CarrierIDStartPage,CarrierIDPageLength, IsUpdateLPCarrierID);
            OnActionDone();
        }
        public void OnCarrierIDReadFailed(string errorcode)
        {
            if (OnCarrierIDReadFailedEvent != null)
                OnCarrierIDReadFailedEvent(ReaderOnModule,Name,errorcode);
            if (ReaderOnLP != null)
                ReaderOnLP.OnCarrierIdReadFailed(ReaderOnModule,CarrierIDIndex);
            OnActionDone();
        }
        public void OnCarrierIDWrite()
        {
            if (OnCarrierIDWriteEvent != null)
                OnCarrierIDWriteEvent(ReaderOnModule, Name, CarrierIDToBeWriten);
            if (ReaderOnLP != null)
                ReaderOnLP.OnCarrierIdWrite(ReaderOnModule, CarrierIDToBeWriten,CarrierIDIndex, CarrierIDStartPage, CarrierIDPageLength, IsUpdateLPCarrierID);
            OnActionDone();
        }
        public void OnCarrierIDWriteFailed(string errorcode)
        {
            if (OnCarrierIDWriteFailedEvent != null)
                OnCarrierIDWriteFailedEvent(ReaderOnModule, Name);
            if (ReaderOnLP != null)
                ReaderOnLP.OnCarrierIdWriteFailed(ReaderOnModule, CarrierIDIndex);
            OnActionDone();
        }
        

        public void Monitor()
        {
            
        }

        public void Reset()
        {
            CheckToPostMessage((int)CIDMsg.Reset,null);
        }

        public bool ReadCarrierID()
        {
            return CheckToPostMessage((int)CIDMsg.ReadCarrierID, null); 
        }

        public bool WriteCarrierID(string carrierID)
        {
            return CheckToPostMessage((int)CIDMsg.WriteCarrierID, new object[] { carrierID});
        }

        public bool ReadParameter(string parameter)
        {
            return CheckToPostMessage((int)CIDMsg.ReadParameter, new object[] { parameter });
        }

        public bool SetParameter(string parameter, string value)
        {
            return CheckToPostMessage((int)CIDMsg.SetParameter, new object[] { parameter ,value});
        }

        public bool ReadCarrierID(int offset, int length)
        {
            return CheckToPostMessage((int)CIDMsg.ReadCarrierID, new object[] { offset, length });
        }

        public bool WriteCarrierID(int offset, int length, string carrierID)
        {
            return CheckToPostMessage((int)CIDMsg.WriteCarrierID, new object[] { carrierID, offset, length });
        }
        public void OnError()
        {
            CheckToPostMessage((int)CIDMsg.Error, null);
        }
        public void OnActionDone()
        {
            CheckToPostMessage((int)CIDMsg.ActionDone, null);
        }
        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {                
                LOG.Write($"{Name} is in { (CIDReaderStateEnum)fsm.State} state，can not do {(CIDMsg)msg}");
                return false;
            }
            switch((CIDMsg)msg)
            {
                case CIDMsg.ReadCarrierID:
                    IsBusy = true;
                    break;
                case CIDMsg.WriteCarrierID:
                    IsBusy = true;
                    break;
            }

            
            fsm.PostMsg(msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (CIDReaderStateEnum)fsm.State, (CIDMsg)msg);
                return false;
            }            
            reason = "";

            return true;
        }
    }

    public enum CIDReaderStateEnum
    {
        Undefined = 0,
        Init,
        Initializing,
        Idle,
        SetParameter,
        ReadParameter,
        ReadCarrierID,
        WriteCarrierID,
        Error,
        Resetting,
    }
    public enum CIDMsg
    {
        Reset,
        ResetComplete,
        Clear,
        StartInit,
        InitComplete,
        SetParameter,
        SetComplete,
        ReadParameter,
        ReadParaComplete,
        ReadCarrierID,
        ReadCarrierIDComplete,
        WriteCarrierID,
        WriteCarrierIDComplete,
        ActionDone,
        Error,

    }

}
