using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.FAServices.E40s
{
    [Serializable]
    [DataContract]
    public class FAProcessJob : NotifiableItem
    {
        [DataMember]
        public string ObjType { set; get; }
        [DataMember]
        public string ObjtID { set; get; }
        [DataMember]
        public List<int> PauseEvent { set; get; }
        [DataMember]
        public List<ProcessMaterialName> PRMtlNameList { set; get; }
        [DataMember]
        public MtlType PRMtlType { set; get; } //13=Carrier, 14= Wafer
        [DataMember]
        public bool PRProcessStart { set; get; } //TRUE - Automatic start FALSE - Manual start
        [DataMember]
        public ProcessRecipeMethod PRRecipeMethod { set; get; }
        [DataMember]
        public string RecID { set; get; }
        [DataMember]
        public Dictionary<string, object> RecVariableList { set; get; }
        [DataMember]
        public ProcessJobState PJstate { set; get; }
        [DataMember]
        public DateTime CreateTime { get; set; }
        [DataMember]
        public DateTime CompleteTime { get; set; }

    }
}
