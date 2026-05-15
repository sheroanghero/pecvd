using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Fortrend

{
    enum FortrendSMIFEventEnum
    {
        PodRemoved = 1,
        PodArrived = 2,
        AutoMode = 3,
        ManualMode = 4,
        PowerUp = 5,

        LoadStart = 11,
        LoadComplete = 12,
        LoadAborted = 13,
        UnloadStart = 14,
        UnloadComplete = 15,
        UnloadAborted = 16,

        HomeStart = 17,
        HomeComplete = 18,
        HomeAborted = 19,

        PortLockComplete = 31,
        PortUnlockComplete = 32,


    }
}