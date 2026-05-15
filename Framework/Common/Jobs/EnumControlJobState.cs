using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Jobs
{
    //SEMI E94
    public enum EnumControlJobState
    {
        Queued,

        Selected,

        WaitingForStart,

        Executing,

        Paused,

        Completed,
    }

    public static class EnumControlJobState_Extension
    {
        public static bool IsQueued(this EnumControlJobState state) => state == EnumControlJobState.Queued;
        public static bool IsSelected(this EnumControlJobState state) => state == EnumControlJobState.Selected;
        public static bool IsWaitingForStart(this EnumControlJobState state) => state == EnumControlJobState.WaitingForStart;
        public static bool IsExecuting(this EnumControlJobState state) => state == EnumControlJobState.Executing;
        public static bool IsPaused(this EnumControlJobState state) => state == EnumControlJobState.Paused;
        public static bool IsCompleted(this EnumControlJobState state) => state == EnumControlJobState.Completed;
        public static bool IsBeforeExecute(this EnumControlJobState state) => state == EnumControlJobState.Queued
            || state == EnumControlJobState.Selected
            || state == EnumControlJobState.WaitingForStart;
    }
}
