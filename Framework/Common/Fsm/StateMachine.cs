using System;
using System.Diagnostics;
using System.Threading;

using System.Collections.Generic;
using System.Collections.Concurrent;

using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;

namespace Aitex.Core.RT.Fsm
{
    /// <summary>
    /// FSM transition function
    /// 返回值： true  需要转换状态
    ///          false 保持状态不变
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>


    public enum FSM_MSG
    {
        TIMER = 0x0FFFFFF0,
        NONE,
        WARNING,
        ALARM,

    }

    public enum FSM_STATE
    {
        UNDEFINE = 0x0FFFFFF0,
        SAME,
        ANY,
    }

    public class StateMachine : StateMachine<object>
    {
        public StateMachine(string name = "FSM", int initState = (int)FSM_STATE.UNDEFINE, int interval = 100) : base(name, initState, interval)
        {
        }
    }

    public class StateMachine<T> : IStateMachine
    {
        public string Name { get; set; }
        public int ElapsedTime
        {
            get
            {
                int ticket = Environment.TickCount;
                return ticket > enterTime ? ticket - enterTime : 0x7FFFFFFF + ticket - enterTime;
            }
        }
        public int EnterTime { set { enterTime = value; } }

        public int State { get; set; }
        public int PrevState { get; set; }
        public int LastMsg { get; private set; }

        private int enterTime;

        private int interval;

        private CancellationTokenSource cancelToken;
        private BlockingCollection<KeyValuePair<int, object[]>> _msgQueue;
        private object _lockerMsgQueue = new object();

        private KeyValuePair<int, object[]> timeoutMsg = new KeyValuePair<int, object[]>((int)FSM_MSG.TIMER, null);
        /// <summary>
        /// 状态迁移表           当前状态, 消息, 函数, 迁移状态
        /// AnyState迁移表       消息, 函数, 迁移状态
        /// 状态进入/退出迁移表  状态,进入函数,进入消息,退出函数, 
        /// </summary>
        private Dictionary<int, Dictionary<int, Tuple<FsmFunc, int>>> transition;
        private Dictionary<int, Tuple<FsmFunc, int>> anyStateTransition;
        private Dictionary<int, Tuple<FsmFunc, int, FsmFunc>> enterExitTransition;

        private Dictionary<int, string> _stringState = new Dictionary<int, string>();
        private Dictionary<int, string> _stringMessage = new Dictionary<int, string>();

        private KeyValuePair<int, object[]> _msgInProcess = new KeyValuePair<int, object[]>((int)FSM_MSG.TIMER, null);

        private long _msgCounter = 0;

        public StateMachine(string name, int initState = (int)FSM_STATE.UNDEFINE, int interval = 100)
        {
            Name = name + " FSM";

            this.State = initState;
            this.PrevState = initState;
            this.interval = interval;

            cancelToken = new CancellationTokenSource();
            _msgQueue = new BlockingCollection<KeyValuePair<int, object[]>>();

            transition = new Dictionary<int, Dictionary<int, Tuple<FsmFunc, int>>>();
            anyStateTransition = new Dictionary<int, Tuple<FsmFunc, int>>();
            enterExitTransition = new Dictionary<int, Tuple<FsmFunc, int, FsmFunc>>();

            EnumLoop<FSM_STATE>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<FSM_MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });
        }

        #region Interface

        public void Init(int initState, int intervalTimeMs)
        {
            State = initState;
            interval = intervalTimeMs;
        }

        public void Start()
        {
            OnEnterState(this.State);
        }


        public void Loop()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                //_msgInProcess = timeoutMsg;
                try
                {
                    KeyValuePair<int, object[]> msg = timeoutMsg;
                    lock (_lockerMsgQueue)
                    {
                        if (!_msgQueue.TryTake(out msg, interval, cancelToken.Token))
                            _msgInProcess = timeoutMsg;
                        else
                        {
                            _msgInProcess = msg;
                        }
                    }

                    //Loop AnyState迁移表

                    if (anyStateTransition.ContainsKey(_msgInProcess.Key))
                    {
                        if (anyStateTransition[_msgInProcess.Key].Item1 != null)
                        {
                            if (anyStateTransition[_msgInProcess.Key].Item1(_msgInProcess.Value))  //need ChangeState
                            {
                                if (anyStateTransition[_msgInProcess.Key].Item2 != State && anyStateTransition[_msgInProcess.Key].Item2 != (int)FSM_STATE.SAME)
                                {
                                    OnExitState((int)FSM_STATE.ANY);
                                    OnExitState(State);

                                    enterTime = Environment.TickCount;

                                    LastMsg = msg.Key;
                                    PrevState = State;
                                    State = anyStateTransition[_msgInProcess.Key].Item2;

                                    OnEnterState((int)FSM_STATE.ANY);
                                    OnEnterState((int)State);
                                }
                            }
                        }

                        if (_msgInProcess.Key != timeoutMsg.Key)
                        {
                            Interlocked.Decrement(ref _msgCounter);

                            //System.Diagnostics.Trace.WriteLine($"message out1: {Name}--> {_msgCounter}");
                        }
                        continue;
                    }

                    if (!transition.ContainsKey(State))
                    {
                        if (_msgInProcess.Key != (int)FSM_MSG.TIMER)
                        {
                            LOG.Warning($"StateMachine [{Name}] no definition of state [{GetStringState(State)}] ");
                        }

                        if (_msgInProcess.Key != timeoutMsg.Key)
                        {
                            Interlocked.Decrement(ref _msgCounter);

                            //System.Diagnostics.Trace.WriteLine($"message out2: {Name} --> {_msgCounter}");
                        }
                        continue;
                    }

                    if (!transition[State].ContainsKey(_msgInProcess.Key))
                    {
                        if (_msgInProcess.Key != (int)FSM_MSG.TIMER)
                        {
                            LOG.Warning($"StateMachine {Name} no definition of [state={GetStringState(State)}], [message={GetStringMessage(_msgInProcess.Key)}]");
                        }

                        if (_msgInProcess.Key != timeoutMsg.Key)
                        {
                            Interlocked.Decrement(ref _msgCounter);

                            // System.Diagnostics.Trace.WriteLine($"message out3:{Name}--> {_msgCounter}");
                        }
                        continue;
                    }

                    //Loop Transition 
                    //if fun is null or fun return true need change
                    if (transition[State][_msgInProcess.Key].Item1 == null || transition[State][_msgInProcess.Key].Item1(_msgInProcess.Value))
                    {
                        if (transition[State][_msgInProcess.Key].Item2 != State && transition[State][_msgInProcess.Key].Item2 != (int)FSM_STATE.SAME)
                        {
                            OnExitState((int)FSM_STATE.ANY);
                            OnExitState(State);

                            enterTime = Environment.TickCount;
                            PrevState = State;
                            State = transition[State][_msgInProcess.Key].Item2;

                            OnEnterState((int)FSM_STATE.ANY);
                            OnEnterState((int)State);

                        }
                    }

                    if (_msgInProcess.Key != timeoutMsg.Key)
                    {
                        Interlocked.Decrement(ref _msgCounter);

                        //System.Diagnostics.Trace.WriteLine($"message out4: {Name}--> {_msgCounter}");
                    }
                    _msgInProcess = timeoutMsg;
                }
                catch (OperationCanceledException)
                {
                    LOG.Info($"FSM {Name} is canceled");
                }
                catch (Exception ex)
                {
                    LOG.Error(ex.StackTrace, ex);
                }
            }
        }

        public void Stop()
        {
            cancelToken.Cancel();
        }

        string GetStringState(int state)
        {
            if (_stringState.ContainsKey(state))
                return _stringState[state];
            return state.ToString();
        }

        string GetStringMessage(int message)
        {
            if (_stringMessage.ContainsKey(message))
                return _stringMessage[message];
            return message.ToString();

        }
        public void MapState(int state, string stringState)
        {
            System.Diagnostics.Debug.Assert(!_stringState.ContainsKey(state));

            _stringState[state] = stringState;
        }

        public void MapMessage(int message, string stringMessage)
        {
            System.Diagnostics.Debug.Assert(!_stringMessage.ContainsKey(message));

            _stringMessage[message] = stringMessage;
        }

        public bool FindTransition(int state, int msg)
        {
            if (anyStateTransition.ContainsKey(msg))
                return true;

            if (transition.ContainsKey(state) && transition[state] != null)
            {
                Dictionary<int, Tuple<FsmFunc, int>> table = transition[state];

                if (table.ContainsKey(msg))
                    return true;
            }

            return false;
        }

        public void Transition(int state, int msg, FsmFunc func, int next)
        {
            if (!transition.ContainsKey(state) || transition[state] == null)
            {
                transition[state] = new Dictionary<int, Tuple<FsmFunc, int>>();
            }

            transition[state][msg] = new Tuple<FsmFunc, int>(func, next);
        }

        public void AnyStateTransition(int msg, FsmFunc func, int next)
        {
            anyStateTransition[msg] = new Tuple<FsmFunc, int>(func, next);
        }

        public void EnterExitTransition(int state, FsmFunc enter, int msg, FsmFunc exit)
        {
            enterExitTransition[state] = new Tuple<FsmFunc, int, FsmFunc>(enter, msg, exit);
        }

        public void PostMsg(int msg, params object[] args)
        {
            Interlocked.Increment(ref _msgCounter);

            //lock (_lockerMsgQueue)
            {
                _msgQueue.Add(new KeyValuePair<int, object[]>(msg, args));
            }

            //System.Diagnostics.Trace.WriteLine($"PostMsg: {msg}--> {_msgCounter}");
        }
        public void PostMsgWithoutLock(int msg, params object[] args)
        {
            Interlocked.Increment(ref _msgCounter);

            _msgQueue.Add(new KeyValuePair<int, object[]>(msg, args));

            //System.Diagnostics.Trace.WriteLine($"PostMsgWithoutLock: {msg}--> {_msgCounter}");

        }
        #endregion

        private void OnEnterState(int state)
        {
            if (enterExitTransition.ContainsKey(state))
            {
                if (enterExitTransition[state].Item1 != null)
                {
                    enterExitTransition[state].Item1(null);
                }

                if (enterExitTransition[state].Item2 != (int)FSM_MSG.NONE)
                {
                    PostMsg(enterExitTransition[state].Item2, null);
                }
            }
        }

        private void OnExitState(int state)
        {
            if (enterExitTransition.ContainsKey(state))
            {
                if (enterExitTransition[state].Item3 != null)
                {
                    enterExitTransition[state].Item3(null);
                }

            }
        }

        public bool CheckExecuted(int msg)
        {
            return _msgCounter == 0;
            //lock (_lockerMsgQueue)
            //{
            //    foreach (var keyValuePair in _msgQueue)
            //    {
            //        if (keyValuePair.Key == msg)
            //            return false;
            //    }

            //    if (_msgInProcess.Key == msg)
            //        return false;
            //}

            //return true;
        }

        public bool CheckExecuted()
        {
            //System.Diagnostics.Trace.WriteLine($"CheckExecuted: --> {_msgCounter}");

            return _msgCounter == 0;
            //return Interlocked.CompareExchange(ref _msgCounter, 0, 0) == 0;
            ////lock (_lockerMsgQueue)
            //{
            //    return _msgQueue.Count == 0 && _msgInProcess.Key==(int)FSM_MSG.TIMER;
            //}

        }

        public bool CheckExecuted(int msg, out int currentMsg, out List<int> msgQueue)
        {
            currentMsg = 0;
            msgQueue = new List<int>();
            return _msgCounter == 0;

            //lock (_lockerMsgQueue)
            //{
            //    currentMsg = 0;
            //    msgQueue = new List<int>();

            //    foreach (var keyValuePair in _msgQueue)
            //    {
            //        if (keyValuePair.Key == msg)
            //            return false;

            //        msgQueue.Add(keyValuePair.Key);
            //    }

            //    if (_msgInProcess.Key == msg)
            //    {
            //        return false;
            //    }

            //    currentMsg = _msgInProcess.Key;
            //}

            //return true;
        }
    }
}
