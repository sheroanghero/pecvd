using System;
using System.Collections.Generic;
using Aitex.Core.Common;

namespace MECF.Framework.Common.Jobs
{

    [Serializable]
    public class ControlJobInfo
    {
        public string Name { get; set; }

        public Guid InnerId { get; set; }

        public string Module { get; set; }
 
        public List<string> ProcessJobNameList { get; set; }

        public EnumControlJobState State { get; set; }

        public string LotName { get; set; }
        public Guid LotInnerId { get; set; }
        public List<WaferInfo> LotWafers { get; set; }

        public string CarrierID { get; set; }

        public bool IsCycleMode { get; set; }
        public int CycleWaferCountSP { get; set; }
        public int CycledWaferCount { get; set; }

        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public ControlJobInfo()
        {
            ProcessJobNameList = new List<string>();
            State = EnumControlJobState.Queued;
            InnerId = Guid.NewGuid();
        }

        public void SetState(EnumControlJobState state)
        {
            State = state;
        }
    }
}
