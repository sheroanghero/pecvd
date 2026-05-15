using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks
{
    enum BrooksSMIFEventEnum
    {
        PodRemoved = 1,
        PodArrived = 2,
        AutoMode = 3,
        ManualMode = 4,
        PowerUp = 5,
        GotoStagePos = 9,





        ReachHome = 18,
        AbortHome = 20,
        ExitHome = 21,
        CompleteLock = 31,
        CompleteUnlock = 32,
        AbortLock = 33,
        AbortUnlock = 34,
        CompleteCalibration = 49,
        AbortCal = 50,
        ReachStage = 53,
        AbortStage = 54,

        ReachPosition = 55,
        AbortPos = 56,
        CompleteMap = 57,
        CompleteOpenGrip = 81,
        AbortOpenGrip = 82,
        CompleteCloseGrip = 83,
        AbortCloseGrip = 84,
        BeginFetch = 85,
        CompleteFetch = 86,
        AbortFetch = 87,
        BeginLoad = 88,
        CompleteLoad = 89,
        AbortLoad = 90,
        BeginHome = 91,
        BeginOpen = 92,
        CompleteOpen = 93,
        AbortOpen = 94,
        BeginReceiveCassette = 95,
        AbortReceiveCassette = 97,
        BeginUnload = 98,
        CompleteUnload = 99,
        AbortUnload = 100,
        CassetteArrived = 101,
        CassetteRemoved = 102,
        CompleteErrorHome = 103,
        CompleteErrorLoad = 104,
        CompleteErrorUnload = 105,
        BeginErrorHome = 106,
        AbortErrorHome = 107,
        BeginErrorLoad = 108,
        AbortErrorLoad = 109,
        BeginErrorUnload = 110,
        AbortErrorUnload = 111,
        AbortLockLatch = 143,
        AbortUnlockLatch = 145,
    }
}
