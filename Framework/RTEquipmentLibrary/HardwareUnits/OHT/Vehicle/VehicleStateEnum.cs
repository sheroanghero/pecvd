using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Vehicle
{
    public enum VehicleStateEnum
    {
        Init,
        Initializing,
        Resetting,
        Idle,
        Moving,
        Picking,
        Placing,
        Pausing,
        Paused,
        Wait,
        //Auto_Idle,
        //Auto_Moving,
        //Auto_Picking,
        //Auto_Placing,
        //Auto_Paused,
        //Auto_Wait,
        Maintenance,
        Error,
    }
    public enum VehicleMsg
    {
        Init,
        Reset,
        Move,
        Pick,
        Place,
        Error,
        Abort,
        Pause,
        ResumeToMove,
        ResumeToPick,
        ResumeToPlace,
        SetMaitenance,
        SetIdle,
    }



}
