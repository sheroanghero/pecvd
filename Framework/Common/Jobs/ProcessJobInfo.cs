using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;

namespace MECF.Framework.Common.Jobs
{

    [Serializable]
    [DataContract]
    public class ProcessJobInfo
    {
        [DataMember]
        public SequenceInfo Sequence { get; set; }

        [DataMember]
        public EnumProcessJobState State { get; private set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string LotName { get; set; }

        [DataMember]
        public string ControlJobName { get; set; }

        [DataMember]
        public Guid InnerId { get; set; }

        [DataMember]
        public List<Tuple<ModuleName, int>> SlotWafers { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        public ProcessJobInfo()
        {
             State = EnumProcessJobState.Created;
            InnerId = Guid.NewGuid();
        }


        public void SetState(EnumProcessJobState state)
        {
            State = state;
        }
    }
}
