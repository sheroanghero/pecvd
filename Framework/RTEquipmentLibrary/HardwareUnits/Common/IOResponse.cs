using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common
{
    public class IOResponse
    {
        public string SourceCommandName { get; set; }
        public string SourceCommand { get; set; }
        public string SourceCommandType { get; set; }
        public string ResonseContent { get; set; }
        public DateTime ResonseRecievedTime { get; set; }
        public bool IsAck { get; set; }
    }
}
