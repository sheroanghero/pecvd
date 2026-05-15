using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using System;

namespace Aitex.Core.RT.Routine
{
    public class ModuleRoutine : SeqenecRoutine
    {
        public string Module { get; set; }
        public string Name { get; set; }

        public string StepName
        {
            get
            {
                return _stepName;
            }
        }
        protected string _stepName;
        public int CurrentStepID
        {
            get
            {
                return _currentStepID;
            }
        }
        protected int _currentStepID;
        
        public TimeSpan StepLeftTime
        {
            get
            {
                if (_stepSpan.TotalMilliseconds < 1)
                    return _stepSpan;
                return (_stepSpan - (DateTime.Now - _stepStartTime));
            }
        }

        public TimeSpan StepTime
        {
            get
            {
                if (_stepSpan.TotalMilliseconds < 1)
                    return _stepSpan;
                return DateTime.Now - _stepStartTime;
            }
        }

        public TimeSpan StepSpan
        {
            get
            {
                return _stepSpan;
            }
        }

        protected TimeSpan _stepSpan = new TimeSpan(0, 0, 0, 0);
        protected DateTime _stepStartTime;

        private int _timeoutMS;

        protected Result RoutineStepResult { get; set; }

        protected void Notify(string message)
        {
            EV.PostInfoLog(Module, $"{Module} {Name}, {message}");
        }

        protected void Stop(string failReason)
        {
            EV.PostAlarmLog(Module, $"{Module} {Name} failed, {failReason}");
        }
 
        public void Abort(string failReason)
        {
            EV.PostAlarmLog(Module, $"{Module} {Name}  {failReason}");
        }

        public void TimeDelay(int id, string stepName, double time)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                Notify($"Delay {time} seconds");

                _stepSpan = new TimeSpan(0, 0, 0, (int)time);
                _stepStartTime = DateTime.Now;
                _stepName = stepName;

                return true;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }


        public void TimeDelay(int id, double time)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                Notify($"Delay {time} seconds");

                _stepSpan = new TimeSpan(0, 0, 0, (int)time);
                _stepStartTime = DateTime.Now;
                _currentStepID = id;
                return true;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }

        protected void ExecuteRoutine(int id, IRoutine routine)
        {
            Tuple<bool, Result> ret = ExecuteAndWait(id, routine);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw (new RoutineFaildException());
                }
                else if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }


        public void Loop(int id, int count)
        {
            Tuple<bool, Result> ret = Loop(id, () =>
            {
                Notify($"Start loop {LoopCounter + 1}/{count}");
                return true;
            }, count);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    throw new RoutineFaildException();
            }
        }

        public void Loop(int id, string name, int count)
        {
            Tuple<bool, Result> ret = Loop(id, () =>
            {
                Notify($"Start loop {LoopCounter + 1}/{count}");
                _stepName = name;
                return true;
            }, count);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    throw new RoutineFaildException();
            }
        }
        public void EndLoop(int id)
        {
            Tuple<bool, Result> ret = EndLoop(id, () =>
            {
                Notify($"Loop finished");
                return true;
            });

            if (ret.Item1)
            {
                throw new RoutineBreakException();
            }
        }

        public virtual Result Monitor()
        {
            RoutineStart();

            MonitorStep();

            RoutineStop();

            return RoutineStepResult;
        }

        protected virtual void MonitorStep()
        {

        }

        protected void RoutineStart()
        {
            if (_historySteps.Count == 0 && _currentStepId == -1)
            {
                Notify($"begin");

                RoutineStepResult = Result.RUN;

            }
            _waitSteps.Clear();
        }

        protected void RoutineStop()
        {
            if (_waitSteps.Count == 0 && _historySteps.Contains(_currentStepId) && RoutineStepResult == Result.RUN)
            {
                RoutineStepResult = Result.DONE;

                Notify($"end");
                return;
            }

            //中间没有任何步骤
            if (_waitSteps.Count == 0 && _currentStepId == -1 && _historySteps.Count == 0 && RoutineStepResult == Result.RUN)
            {
                RoutineStepResult = Result.DONE;

                Notify($"end");
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stepName"></param>
        /// <param name="stepAction"></param>
        /// <param name="timeoutAction">这里描述具体的错误信息和提示，以及超时错误后的安全收尾动作</param>
        /// <param name="failedAction">这里描述具体的错误信息和提示，以及超时错误后的安全收尾动作</param>
        protected void RoutineStep<T>(T stepName, Func<Result> stepAction, Action timeoutAction=null ,
    Action failedAction=null )
        {
            //如果超时，或者失败，避免重入；只有reset之后才能继续
            if (RoutineStepResult != Result.RUN)
                return;

            int id = Convert.ToInt32(stepName);

            //已经执行过的步骤，跳过去
            if (_historySteps.Contains(id))
                return;

            //上一步执行完毕，继续执行下一步
            if (_currentStepId == -1 || (_historySteps.Contains(_currentStepId) && _currentStepId != id))
            {
                _currentStepId = id;

                if (_waitSteps.Contains(id))
                {
                    _waitSteps.Remove(id);
                }

                LOG.Write($"{Module} {Name}, start step {id}={stepName}");
            }

            //待执行的步骤
            if (_currentStepId != id)
            {
                if (!_waitSteps.Contains(id))
                {
                    _waitSteps.Add(id);
                }
                return;
            }

            try
            {
                RoutineStepResult = stepAction.Invoke();

                if (RoutineStepResult == Result.TIMEOUT )
                {
                    if (timeoutAction != null)
                    {
                        timeoutAction();
                    }
                    else
                    {
                        Stop($"timeout, can not complete {stepName} in {_timeoutMS} ms.");
                    }

                    RoutineStepResult = Result.FAIL;
                }

                if (RoutineStepResult == Result.FAIL )
                {
                    if (failedAction != null)
                    {
                        failedAction();
                    }
                    else
                    {
                        Stop($"failed to do {stepName}");
                    }

                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public Result ExecuteAndWait(Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            bool? bExecute = false;

            if (_stepState == STATE.IDLE)
            {
                if (!execute())
                {
                    return Result.FAIL; //执行错误
                }

                _timeoutMS = (int)timeout;
                _timer.Start(timeout);
                _stepState = STATE.WAIT;
            }

            bExecute = check();

            if (bExecute == null)
            {
                return Result.FAIL; //Terminate
            }
            else
            {
                if (bExecute.Value) //检查Success, next
                {
                    NextStep();
                    return Result.RUN;
                }
            }

            if (_timer.IsTimeout())
                return Result.TIMEOUT;

            return Result.RUN;
        }

        public Result ExecuteAndWait(IRoutine routine)
        {
            if (_stepState == STATE.IDLE)
            {
                Result startRet = routine.Start();
                if (startRet == Result.FAIL)
                {
                    return Result.FAIL;   //执行错误
                }
                else if (startRet == Result.DONE)
                {
                    NextStep();
                    return Result.RUN;
                }
                _stepState = STATE.WAIT;
            }

            Result ret = routine.Monitor();

            if (ret == Result.DONE)
            {
                NextStep();
                return Result.RUN;
            }
            else if (ret == Result.FAIL || ret == Result.TIMEOUT)
            {
                return Result.FAIL;
            }
            else
            {
                return Result.RUN;
            }
        }


        public Result Delay(Func<bool> func, double timeMS)
        {
            if (_stepState == STATE.IDLE)
            {
                if ((func != null) && !func())
                {
                    return Result.FAIL;
                }

                _timer.Start(timeMS);
                _stepState = STATE.WAIT;
            }

            if (_timer.IsTimeout())
            {
                NextStep();
            }

            return Result.RUN;
        }


        public Result Loop(int count)
        {
            _loopStepStartId = _currentStepId;
            _loopTotalCountSetting = count;

            NextStep();

            EV.PostInfoLog(Module, $"{Name} loop begin, {_loopCounter + 1}/{_loopTotalCountSetting}");

            return Result.RUN;
        }

        public void RoutineLoop<T>(T stepId, int count)
        {
            RoutineStep(stepId, () => Loop(count), null, null);
        }

        public Result EndLoop()
        {
            EV.PostInfoLog(Module, $"{Name} loop end, {_loopCounter + 1}/{_loopTotalCountSetting}");

            ++_loopCounter;
            
            if (_loopCounter >= _loopTotalCountSetting) //Loop 结束
            {
                _loopCounter = 0;
                _loopTotalCountSetting = 0; // Loop 结束时，当前loop和loop总数都清零

                NextStep();
                return Result.RUN;
            }

            //继续下一LOOP

            NextStep(_loopStepStartId);



            return Result.RUN;
        }

        public void RoutineEndLoop<T>(T stepId)
        {
            RoutineStep(stepId, () => EndLoop(), null, null);
        }


        protected Result ExecuteRoutine(IRoutine routine)
        {
            return  ExecuteAndWait(routine);
        }
    }

}
