using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots
{
    public enum RobotArmEnum
    {
        Lower = 0,
        Upper = 1,        
        Both =2,
        Blade1 = 3,
        Blade2 = 4,
    }
    public enum RobotPostionEnum
    {
        PickReady,
        PickExtendLow,
        PickAtWafer,
        PickExtendUp,
        PickExtend,
        PickRetracted,
        PlaceReady =10,
        PlaceExtendUp,
        PlaceExtendAtWafer,
        PlaceExtendDown,
        PlaceExtend,
        PlaceRetract,
        ExchangeReady=20,
        ExchangeExtendPickDown,
        ExchangeExtendPickAtWafer,
        ExchangeExtendPickUp,
        ExchangeExtendPlaceUp,
        ExchangeExtendPlaceAtWafer,
        ExchangeExtendPlaceDown,
        ExchangeRetract,
        SpecifiedPosition =100,
    }
    public enum BladePostureEnum
    {
        Degree0=0,
        Degree90,
        Degree180,
        Degree270,

    }
}
