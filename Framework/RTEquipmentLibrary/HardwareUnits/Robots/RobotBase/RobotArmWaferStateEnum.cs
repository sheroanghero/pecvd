using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots
{
 
    public enum RobotArmWaferStateEnum
    {
        /// <summary>
        /// No location
        /// </summary>
        NoLocation,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,
        /// <summary>
        /// Present
        /// </summary>
        Present,
        /// <summary>
        /// Absent
        /// </summary>
        Absent,
        /// <summary>
        /// Doubled
        /// </summary>
        Doubled,
        /// <summary>
        /// Cross slotted
        /// </summary>
        CrossSlotted,
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// Arm invalid
        /// </summary>
        ArmInvalid,
    };

}
