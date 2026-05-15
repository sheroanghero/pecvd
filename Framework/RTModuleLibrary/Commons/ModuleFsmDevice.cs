using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.RT.ModuleLibrary.Commons
{
    public class ModuleFsmDevice : FsmDevice
    {
        public bool IsInstalled
        {
            get
            {
                if (!SC.ContainsItem($"System.SetUp.Is{Module}Installed"))
                    return true;

                return SC.GetValue<bool>($"System.SetUp.Is{Module}Installed");
            }
        }

        public bool IsOnline { get; set; }

        public virtual  void SetOnline ()
        {
        }

        protected Queue<IRoutine> QueueRoutine
        {
            get { return _routine; }
        }

        private Queue<IRoutine> _routine = new Queue<IRoutine>();


        public ModuleFsmDevice() : base()
        {
        }


        public override bool Initialize()
        {
            return base.Initialize();
        }

        public Result StartRoutine(IRoutine routine, params object[] args)
        {
            QueueRoutine.Clear();

            QueueRoutine.Enqueue(routine);

            return QueueRoutine.Peek().Start(args);
        }

        public Result StartRoutine(IRoutine routine)
        {
            QueueRoutine.Clear();

            QueueRoutine.Enqueue(routine);

            return QueueRoutine.Peek().Start();
        }

        public Result StartRoutine()
        {
            if (_routine.Count == 0)
                return Result.DONE;

            Result ret = Result.DONE;
            var lst = _routine.ToList();
            for (int i = 0; i < lst.Count; i++)
            {
                ret = lst[i].Start();
                if (ret == Result.DONE)
                {
                    _routine.Dequeue();
                    continue;
                }
                else
                {
                    break;
                }
            }

            return Result.RUN;
        }

        public Result MonitorRoutine()
        {
            if (_routine.Count == 0)
                return Result.DONE;

            IRoutine routine = _routine.Peek();

            Result ret = routine.Monitor();
            if (ret == Result.DONE)
            {
                _routine.Dequeue();

                var lst = _routine.ToList();
                for (int i = 0; i < lst.Count; i++)
                {
                    ret = lst[i].Start();
                    if (ret == Result.DONE)
                    {
                        _routine.Dequeue();
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return ret;
        }

        public void AbortRoutine()
        {
            if (_routine != null)
            {
                _routine.Peek().Abort();
                _routine.Clear();
            }
        }

    }
}
