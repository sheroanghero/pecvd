using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Jobs
{
    //Semi E40
    public enum EnumProcessJobState
    {
        Created,
        Queued,
        Canceled,

        SetUp,
        WaitingForStart,
        Processing,
        ProcessingComplete,

        //NotPaused,
        Pausing,
        Paused,

        //NotAborting,
        Aborting,

        //NotStopping,
        Stopping,

        Complete,
    }

    public static class EnumProcessJobState_Extension
    {
        public static bool IsCreated(this EnumProcessJobState state) => state == EnumProcessJobState.Created;
        public static bool IsQueued(this EnumProcessJobState state) => state == EnumProcessJobState.Queued;
        public static bool IsCanceled(this EnumProcessJobState state) => state == EnumProcessJobState.Canceled;
        public static bool IsSetUp(this EnumProcessJobState state) => state == EnumProcessJobState.SetUp;
        public static bool IsWaitingForStart(this EnumProcessJobState state) => state == EnumProcessJobState.WaitingForStart;
        public static bool IsProcessing(this EnumProcessJobState state) => state == EnumProcessJobState.Processing;
        public static bool IProcessingComplete(this EnumProcessJobState state) => state == EnumProcessJobState.ProcessingComplete;
        public static bool IsPausing(this EnumProcessJobState state) => state == EnumProcessJobState.Pausing;
        public static bool IsPaused(this EnumProcessJobState state) => state == EnumProcessJobState.Paused;
        public static bool IsAborting(this EnumProcessJobState state) => state == EnumProcessJobState.Aborting;
        public static bool IsStopping(this EnumProcessJobState state) => state == EnumProcessJobState.Stopping;
        public static bool IsComplete(this EnumProcessJobState state) => state == EnumProcessJobState.Complete;

        public static bool IsOver(this EnumProcessJobState state) => state == EnumProcessJobState.Complete || state == EnumProcessJobState.ProcessingComplete;
    }
}
