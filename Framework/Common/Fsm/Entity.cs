using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Fsm
{
    public class Entity
    {
        protected Thread thread = null;
        protected IStateMachine fsm = null;
        
        public bool Running { get; protected set;}

        public Entity()
        {
        }

        public bool Initialize()
        {
            Debug.Assert(fsm != null);

            Init();

            fsm.Start();
            thread = new Thread(new ThreadStart(fsm.Loop));
            thread.Name = fsm.Name;
            thread.Start();

            while (!thread.IsAlive)
                Thread.Sleep(1);

            return true;
        }

        public virtual void Terminate()
        {
            fsm.Stop();

            Term();
        

            if (thread != null && thread.IsAlive)
            {
                Thread.Sleep(100);
                if (thread.IsAlive)
                {
                    try
                    {
                        thread.Abort();
                    }
                    catch (Exception ex)
                    {
                        LOG.Error(String.Format("Entity terminate has exception."), ex);
                    }
                }
            }
            //Term();
        }

        protected virtual bool Init()
        {
            return true;
        }

        protected virtual void Term()
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
            if (fsm != null)
                fsm.Transition(state, msg, func, next);
        }

        protected void AnyStateTransition(int msg, FsmFunc func, int next)
        {
            if (fsm != null)
                fsm.AnyStateTransition(msg, func, next);
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
            if (fsm != null)
                fsm.EnterExitTransition(state, enter, msg, exit);
        }


        public void PostMsg<T>(T msg, params object[] args) where T : struct
        {
            Debug.Assert(typeof(T).IsEnum);
            int id = Convert.ToInt32(msg);
            PostMsg(id, args);
        }

        public void PostMsg(int msg, params object[] args)
        {
            if (fsm == null)
            {
                LOG.Error($"fsm is null, post msg {msg}");
                return;
            }
 
            fsm.PostMsg(msg, args);
        }

 
    }
}
