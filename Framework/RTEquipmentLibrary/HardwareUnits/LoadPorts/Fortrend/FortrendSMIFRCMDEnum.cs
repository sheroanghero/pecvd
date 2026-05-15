using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Fortrend
{
    enum FortrendSMIFRCMDEnum
    {


        StartLoad = 9,
        StartUnload = 10,

        GoHome = 11,
        HostToUnlockPort = 13,
        HostToLockPort = 12,

    }
}