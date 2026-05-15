using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.FAServices.SecsDataAttribute;

namespace MECF.Framework.Common.FAServices.E40s
{
    public enum ProcessJobState
    {
        NONE = 0,
        POOLED,
        SETTING_UP,
        WAITING_FOR_START,
        PROCESSING,
        PROCESS_COMPLETE,
        STOPPING,
        PAUSING,
        PAUSED,
        ABORTING,
    }
    public enum MtlType
    {
        CARRIER = 0,
        WAFER,
    }
    public enum ProcessRecipeMethod
    {
        RECIEP = 0,
        RECIPE_WITH_VAR_TUNING,
    }
    public enum CJState
    {
        QUEUED = 0,
        SELECTED,
        WAITING_FOR_START,
        EXECUTING,
        PAUSED,
        COMPLETED,
    }
    public struct ProcessMaterialName
    {
        [SecsDataElement]
        public string CarrierID { set; get; }
        [SecsDataElement]
        public List<int> SlotID { set; get; }
    }
    public enum PJOperation
    {
        ABORT = 0,
        CANCEL,
        PAUSE,
        RESUME,
        START_PROCESS,
        STOP,
        CREATEENH,
        DUPLICATE_CREATE,
        MULTI_CREATE,
        DEQUEUE,
        PR_GETALLJOBS,
        PR_GETSPACE,
        PR_SET_RECIPE_VARIABLE,
        PR_SET_START_METHOD,
    }
    public enum CJOperation
    {
        CREAT = 0,
        START,
        PAUSE,
        RESUME,
        CANCEL,
        DESELECT,
        STOP,
        ABORT,
        HOQ,
    }


    public enum ProcessOrderManagement
    {
        ARRIVAL = 1,
        OPTIMIZE,
        LIST,
    }
    public struct MtrlOutSpecPair
    {
        public string sourceCarID { set; get; }
        public string DestCarID { set; get; }
        public List<int> sourceSlots { set; get; }
        public List<int> destSlots { set; get; }
    }    
}
