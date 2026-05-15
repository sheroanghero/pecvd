using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump2205
{
    public enum EdwardsPump2205MessageType
    {
        ReadMeas = 0x45,
        Command,
        ReadFailMess,
        ReadModFonct = 0x4D,
        ReadVersion,
        ReadCounters,
        ReadSetPoint,
        ReadMotorTemp,
        ReadStatus,
        ReadEvents,
        SetSpeedSetPoint,
        ReadSpeedSetPoint,
        ReadModFonctWithWarning,
        ReadMeasValue = 0x5B,
        ReadOptionFunc,
        SetOptionFunc,
        ReadCondition,
        ReadEventsWithTime,
    }
}
