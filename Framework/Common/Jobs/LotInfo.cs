using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Jobs
{
    [Serializable]
    public class LotInfo
    {
        [DataMember]
        public Guid InnerId { get; set; }

        [DataMember]
        public string Name { get; set; }
 
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string CarrierId { get; set; }

        public ModuleName PortIn { get; set; }

        public ModuleName PortOut { get; set; }

        public int TotalWafer { get; set; }

        public LotInfo()
        {
            InnerId = Guid.NewGuid();
        }
 
    }
}
