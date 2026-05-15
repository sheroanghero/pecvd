using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.FAServices.E40s;

namespace MECF.Framework.Common.FAServices.E94s
{

    [Serializable]
    [DataContract]
    public class FAControlJob : NotifiableItem
    {
        [DataMember]
        public string ObjType { set; get; }
        [DataMember]
        public string ObjtID { set; get; }
        [DataMember]
        public List<string> CurrentPRJob { set; get; }
        [DataMember]
        public string DataCollectionPlan { set; get; }
        [DataMember]
        public List<string> CarrierIputSpec { set; get; }
        [DataMember]
        public List<MtrlOutSpecPair> MtrlOutSpec { set; get; }
        //public Dictionary<string,object> MtrlOutByStatus { set; get; }
        [DataMember]
        public List<int> PauseEvent { set; get; }
        [DataMember]
        public List<string> ProcessingCtrlSpec { set; get; }
        [DataMember]
        public ProcessOrderManagement ProcessingOrderMgmt { set; get; }//1=ARRIVAL 2=OPTIMIZE 3=LIST
        [DataMember]
        public bool StartMethod { set; get; }
        [DataMember]
        public CJState state { set; get; }
        [DataMember]
        public DateTime CreateTime { get; set; }
        [DataMember]
        public DateTime CompleteTime { get; set; }
    }
}
