using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public enum EnumLoadPortType
    {
        Undefined,

        //default open stage, with out door
        OpenStage,

        OpenStageWithDoor,

        //default load port, with map
        LoadPort,

        LoadPortNoMap,

        OpenStageWithWaferSize,
    }
}
