using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Routine
{
    public class SeqenecRoutine
    {
        //timer, 计算routine时间
        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        protected enum STATE
        {
            IDLE,          
            WAIT,          
        }

        public int TokenId
        {
            get { return _currentStepId; }
        }
        protected int _currentStepId;         //step index

        /// <summary>
        /// already done steps
        /// </summary>
        protected List<int> _historySteps = new List<int>();

        /// <summary>
        /// wait run steps
        /// </summary>
        protected List<int> _waitSteps = new List<int>();

        protected STATE _stepState;    //step state //idel,wait,

        //loop control
        protected int  _loopCounter = 0;
        protected int  _loopTotalCountSetting = 0;
        protected int  _loopStepStartId = 0;

        protected DeviceTimer _timer = new DeviceTimer();

        public int LoopCounter { get { return _loopCounter; } }
        public int LoopTotalTime { get { return _loopTotalCountSetting; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(_timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        public void Reset()
        {
            _currentStepId = -1;
            _historySteps.Clear();
            _waitSteps.Clear();

            _loopCounter  = 0;
			_loopTotalCountSetting = 0;

            _stepState = STATE.IDLE;
            counter.Start(60*60*100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
        }

        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!ActiveStep(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            _loopCounter = _loopTotalCountSetting;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count) 
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);            

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                _loopStepStartId = idx;
                _loopTotalCountSetting = count;

                NextStep();
                return Tuple.Create(true, Result.RUN);     
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);

            if (bActive)
            {
                if (++_loopCounter >= _loopTotalCountSetting)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    _loopCounter  = 0;
                    _loopTotalCountSetting = 0;  // Loop 结束时，当前loop和loop总数都清零

                    NextStep();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                NextStep(_loopStepStartId);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
          
            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }else if (startRet == Result.DONE)
                    {
                        NextStep();
                        return Tuple.Create(true, Result.DONE);
                    }
                    _stepState = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    NextStep();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }

        
        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);

            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    _stepState = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    NextStep();

                    if(bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
            bool? bExecute = false;

            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    if (!execute())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    _timer.Start(timeout);
                    _stepState = STATE.WAIT;
                }
                
                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute.Value)       //检查Success, next
                    {
                        NextStep();
                        return Tuple.Create(true, Result.RUN);    
                    }
                }

                if(_timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);   

                return Tuple.Create(true, Result.RUN);     
            }

            return Tuple.Create(false, Result.RUN);
        }
         
        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
            bool? bExecute = false;
            double timeout = 0;
            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    timeout = time();
                    if (!execute())
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    _timer.Start(timeout);
                    _stepState = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }
                    if (bExecute.Value)       //检查Success, next
                    {
                        NextStep();
                        return Tuple.Create(true, Result.RUN);
                    }

                if (_timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);

            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    rt.Start();               
                    _stepState = STATE.WAIT;
                }

                Result ret = rt.Monitor();            

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN); 
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    _timer.Start(time);
                    _stepState = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (_timer.IsTimeout())
                {
                    NextStep();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                        
                    _timer.Start(time);
                    _stepState = STATE.WAIT;
                }

                if (_timer.IsTimeout())
                {
                    NextStep();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = ActiveStep(idx);
            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {                   
                    _timer.Start(time);
                    _stepState = STATE.WAIT;
                }

                if (_timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    NextStep();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
#endregion


        private Tuple<bool,bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive  = ActiveStep(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                if (bExecute)
                {
                    NextStep();
                }
            }

            return Tuple.Create(bActive, bExecute); 
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = ActiveStep(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                NextStep();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>
       
        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool,bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive  = ActiveStep(id);
            bool bExecute = false; 
            bool bTimeout = false;

            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    _timer.Start(timeout);
                    _stepState = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    NextStep();                  
                }

                bTimeout = _timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);  
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = ActiveStep(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (_stepState == STATE.IDLE)
                {
                    _timer.Start(timeout);
                    _stepState = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    NextStep();
                }

                bTimeout = _timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout); 
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool,Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    return Tuple.Create(true, Result.FAIL);
                }

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);  
        }
        
        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool ActiveStep(int id) //
        {
            if (_historySteps.Contains(id))
                return false;

            this._currentStepId = id;
            return true;
        }

        protected void NextStep()
        {
            _historySteps.Add(_currentStepId);
            _stepState = STATE.IDLE;
        }

        protected void NextStep(int step) //loop
        {
            if (!_historySteps.Contains(step))
            {
                System.Diagnostics.Trace.Assert(false, $"Error, no step {step}");
                LOG.Write($"Error, no step {step}");
                return;
            }

            int[] steps = _historySteps.ToArray();

            for (int i = steps.Length - 1; i >= 0; i--)
            {
                _historySteps.RemoveAt(i);

                if (steps[i] == step)
                {
                    _currentStepId = step;
                    break;
                }

                _waitSteps.Insert(0,steps[i]);
            }

            _stepState = STATE.IDLE;
        }

        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
                {
                    return true;
                }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }



        public bool IsActived(int id)
        {
            return _historySteps.Contains(id);
        }
    }



}

