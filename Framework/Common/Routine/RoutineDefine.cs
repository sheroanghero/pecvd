using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Routine
{
    public enum  Result
    {
        RUN,
        DONE,
        FAIL,
        TIMEOUT,
        VERIFYFAIL,    //Only for Verification use
        PAUSE,    //Use When Process Needs Pause
        Succeed,
        WAIT,
    }

    public class RoutineFaildException : ApplicationException
    {
        public RoutineFaildException() { }
        public RoutineFaildException(string message)
            : base(message) { }
    }

    public class RoutineBreakException : ApplicationException
    {
        public RoutineBreakException() { }
        public RoutineBreakException(string message)
            : base(message) { }
    }

    public enum RoutineState
    {
        Running,
        Failed,
        Timeout,
        Finished,
    }

    public class RoutineResult
    {
        public RoutineState Result;
        public string ErrorMessage;
    }

}
