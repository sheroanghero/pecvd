using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots
{
 
    public interface IRobot  
    {
        RobotStateEnum RobotState { get; }


        RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm);

 
        string[] GetStationsInUse();

 
        bool Home(object[] param);


        //-------------------------------------------------------------------------
        // Single arm operations


        bool ApproachForPick(RobotArmEnum arm, string station, int slot);

        bool ApproachForPlace(RobotArmEnum arm, string station, int slot);

        bool Pick(RobotArmEnum arm, string station, int slot);

        bool Place(RobotArmEnum arm, string station, int slot);

        bool Swap(RobotArmEnum pickArm, string station, int slot);

        bool Transfer(RobotArmEnum arm, string sourceStation, int sourceSlot, string destStation, int destSlot);

        //-------------------------------------------------------------------------
        // Dual arm operations
 
        bool DualPick(string station, int lowerSlot);

        bool DualPlace(string station, int lowerSlot);
 
        bool DualSwap(string station, int lowerSlotPlaceTo, int lowerSlotPickFrom);
 
        bool DualTransfer(string sourceStation, int sourceLowerSlot, string destStation, int destLowerSlot);
    }
}
