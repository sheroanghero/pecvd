 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots
{
    public enum RobotStateEnum
    {
        Undefined = 0,
 
        Init,
 
        Initializing,

        Homing,
 
        Idle,
 
        Mapping,
 
        GoingToPosition,
 
        Picking,
 
        Placing,
 
        Swapping,
 
        Transferring,

        Faulted,
 
        ExtendingForPlace,
 
        ExtendingForPick,
 
        RetractingFromPlace,
 
        RetractingFromPick,
 
        Error,
 
        SettingServos,
 
        ReadingData,

        Gripping,

        UnGripping,

        Resetting,

        Stopped,

        Maintenance,
        Moving,

        PickingCassette,
        PlacingCassette,

        Executing,
        TransferBlock,
    };

}
