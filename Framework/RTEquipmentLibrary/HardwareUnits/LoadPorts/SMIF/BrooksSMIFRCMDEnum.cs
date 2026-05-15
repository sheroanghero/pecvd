using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks
{
    enum BrooksSMIFRCMDEnum
    {
        HostToUnlockPort = 1,
        HostToLockPort = 2,

        GotoStagePosition = 9,
        GetWaferMap = 11,
        GoHome = 13,

        Reset = 16,

        SwitchOperationMode = 18,

        EmergencyStop = 20,

        EnableCassetteFetch = 30,
        EnableLoad,
        EnableHome,
        EnableOpenForUnload,
        EnableUnload,
        EnableClose,
        EnableErrorHome,
        EnableErrorLoad,
        EnableErrorUnload,
        EnableAll,
        DisableCassetteFetch,
        DisableLoad,
        DisableHome,
        DisableOpenForUnload,
        DisableUnload,
        DisableClose,
        DisableErrorHome,
        DisableErrorLoad,
        DisableErrorUnload,
        DisableAll,
        FetchCassetteFromPod,
        LoadCassette,
        OpenPodForUnload,
        UnloadCassette,
        ClosePodFOrUnload,
        ErrorHome,
        ErrorLoad,
        ErrorUnload,

        GetIOPortStatus = 134,

        GetWaferMapReport = 140,
        LockPodDoor = 141,
        UnlockPodDoor = 142

    }
}
