using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.Communications
{
    [Serializable]
    [DataContract]
    public class NotifiableConnectionItem : NotifiableItem
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public bool IsConnected { get; set; }
    }
}
