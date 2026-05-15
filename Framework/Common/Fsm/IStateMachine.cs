using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Fsm
{
    public delegate bool FsmFunc(object[] param);

    public interface IStateMachine
    {
        string Name { get; set; }
        int ElapsedTime { get; }
        int EnterTime { set; }
        int State { get; }
        int LastMsg { get; }
        int PrevState { get; }

        void Init(int initState, int interval);

        void Start();
        void Loop();
        void Stop();

        void Transition(int state, int msg, FsmFunc func, int next);
        void AnyStateTransition(int msg, FsmFunc func, int next);
        void EnterExitTransition(int state, FsmFunc enter, int msg, FsmFunc exit);

        void PostMsg(int msg, object[] args);
        void PostMsgWithoutLock(int msg, object[] args);

        void MapState(int state, string stringState);
        void MapMessage(int message, string stringMessage);
        bool FindTransition(int state, int msg);

        bool CheckExecuted();
        bool CheckExecuted(int msg);
        bool CheckExecuted(int msg, out int currentMsg, out List<int> msgQueue);
    }
}
