using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.ModuleLibrary.Commons
{
    public class FsmDevice : BaseDevice, IDevice
    {
        private Thread _thread = null;
        private IStateMachine _fsm = null;

        public int FsmState
        {
            get { return _fsm.State; }
        }

        public int FsmPreviousState
        {
            get { return _fsm.PrevState; }
        }

        public string StringFsmStatus
        {
            get
            {
                return _fsmStateMap.ContainsKey(FsmState) ? _fsmStateMap[FsmState] : FsmState.ToString();
            }
        }

        public int FsmLastMessage
        {
            get
            {
                return _fsm.LastMsg; 
            }
        }

        Dictionary<int, string> _fsmStateMap = new Dictionary<int, string>();
        Dictionary<int, string> _fsmMessageMap = new Dictionary<int, string>();

        public FsmDevice() : base()
        {

        }

        public void MapState(int state, string stringState)
        {
            _fsmStateMap[state] = stringState;
        }

        public void MapMessage(int msg, string stringMessage)
        {
            _fsmMessageMap[msg] = stringMessage;
        }

        public void EnableFsm(int fsmInterval, object initState)
        {
            EnableFsm(fsmInterval, (int)initState);
        }

        public void EnableFsm(int fsmInterval, int initState)
        {
            _fsm = new StateMachine($"{Module} {Name} FSM", initState, fsmInterval);

            _fsm.Start();
            _thread = new Thread(new ThreadStart(_fsm.Loop));
            _thread.Name = _fsm.Name;
            _thread.Start();

            while (!_thread.IsAlive)
                Thread.Sleep(1);
        }

        public virtual bool Initialize()
        {
            return true;
        }

        public virtual void Monitor()
        {

        }

        public virtual void Terminate()
        {
            if (_fsm != null)
            {
                _fsm.Stop();
            }


            if (_thread != null)
            {
                if (_thread.IsAlive)
                {
                    Thread.Sleep(100);
                    if (_thread.IsAlive)
                    {
                        try
                        {
                            _thread.Abort();
                        }
                        catch (Exception ex)
                        {
                            LOG.Error(String.Format("Entity terminate has exception."), ex);
                        }
                    }
                }
            }

            //Term();
        }

        public virtual void Reset()
        {

        }


        protected void Transition<T, V>(T state, V msg, FsmFunc func, T next)
        {
            Debug.Assert(typeof(T).IsEnum && typeof(V).IsEnum);

            int _state = Convert.ToInt32(state);
            int _next = Convert.ToInt32(next);
            int _msg = Convert.ToInt32(msg);

            Transition(_state, _msg, func, _next);
        }

        protected void Transition(int state, int msg, FsmFunc func, int next)
        {
            if (_fsm != null)
                _fsm.Transition(state, msg, func, next);
        }

        protected void AnyStateTransition(int msg, FsmFunc func, int next)
        {
            if (_fsm != null)
                _fsm.AnyStateTransition(msg, func, next);
        }

        protected void AnyStateTransition<T, V>(V msg, FsmFunc func, T next)
        {
            Debug.Assert(typeof(T).IsEnum && typeof(V).IsEnum);

            int _next = Convert.ToInt32(next);
            int _msg = Convert.ToInt32(msg);
            AnyStateTransition(_msg, func, _next);

        }

        protected void EnterExitTransition<T, V>(T state, FsmFunc enter, Nullable<V> msg, FsmFunc exit) where V : struct
        {
            Debug.Assert(typeof(T).IsEnum && ((msg == null) || typeof(V).IsEnum));

            int _state = Convert.ToInt32(state);

            int _msg = msg == null ? (int)FSM_MSG.NONE : Convert.ToInt32(msg);
            EnterExitTransition(_state, enter, _msg, exit);
        }

        protected void EnterExitTransition(int state, FsmFunc enter, int msg, FsmFunc exit)
        {
            if (_fsm != null)
                _fsm.EnterExitTransition(state, enter, msg, exit);
        }


        public void PostMsg<T>(T msg, params object[] args) where T : struct
        {
            Debug.Assert(typeof(T).IsEnum);
            int id = Convert.ToInt32(msg);
            PostMsg(id, args);
        }

        public void PostMsg(int msg, params object[] args)
        {
            if (_fsm == null)
            {
                LOG.Error($"fsm is null, post msg {msg}");
                return;
            }

            _fsm.PostMsgWithoutLock(msg, args);
        }

        public bool CheckAllMessageProcessed()
        {
            return _fsm.CheckExecuted();
        }

        public bool CheckExecuted(int msg)
        {
            return _fsm.CheckExecuted(msg);
        }

        public bool CheckToPostMessage<T>(T msg, params object[] args)
        {
            return CheckToPostMessage(Convert.ToInt32(msg), args);
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            int state = _fsm.State;
            string status = _fsmStateMap[state];
            if (!_fsm.FindTransition(_fsm.State, msg))
            {
                string message = string.Empty;
                if (_fsmMessageMap.ContainsKey(msg))
                    message = _fsmMessageMap[msg];
                else
                {
                    message = msg.ToString();
                }
                EV.PostWarningLog(Module, $"{Name} is in {status} state，can not do {message}");
                return false;
            }

            _fsm.PostMsg(msg, args);

            return true;
        }



    }

}
