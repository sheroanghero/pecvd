using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase
{
    public enum AlignerStateEnum
    {
        Undefined = 0,

        Init,

        Initializing,

        Idle,
        Gripping,

        UnGripping,
        Aligning,
        Error,
        SettingServos,
        ReadingData,  
        Resetting,
        Stopped,
        LiftingUp,
        LiftingDown,
        Maintenance,
        Homing,
        PrepareAccept,
        SetAligning,
    }
}
