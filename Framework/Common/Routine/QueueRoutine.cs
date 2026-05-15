using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Routine;

namespace Aitex.Core.RT.Routine
{
    public class QueueRoutine
    {
        private Queue<IRoutine> _steps = new Queue<IRoutine>();

        private IRoutine _currentStep = null;


        public void Add(IRoutine step)
        {
            _steps.Enqueue(step);
        }

        public void Reset()
        {
            _steps.Clear();
            _currentStep = null;
        }

        public void Abort()
        {
            if (_currentStep != null)
                _currentStep.Abort();

            Reset();
        }

        public Result Start(params object[] objs)
        {
            if (_steps.Count == 0)
                return Result.DONE;

            _currentStep = _steps.Dequeue();

            return _currentStep.Start(objs);
        }


        public Result Monitor(params object[] objs)
        {
            Result ret = Result.FAIL;
            if (_currentStep != null)
            {
                ret = _currentStep.Monitor();
            }

            if (ret == Result.DONE)
            {
                _currentStep = _steps.Count > 0 ? _steps.Dequeue() : null;

                if (_currentStep != null)
                {
                    return _currentStep.Start(objs);
                }
            }

            return ret;
        }
    }
}
