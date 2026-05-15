using Aitex.Core.RT.Log;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.HineAutomation
{
    public class RobotConvertor
    {
        //position number = 01 through 15
        public static Dictionary<ModuleName, int> TeachThetaAxisAddressPosition = new Dictionary<ModuleName, int>()
        {
            {ModuleName.Cassette, 6},//4 inch = 4,6 inch = 6, 8 inch = 8
            {ModuleName.PM, 1},
            {ModuleName.Cooling, 2},          
            {ModuleName.TMRobot, 0},          
        };

        //position number = 01 through 99
        public static Dictionary<ModuleName, int> TeachZAxisAddressPosition = new Dictionary<ModuleName, int>()
        {
            {ModuleName.Cassette, -1},
            {ModuleName.PM, 1},
            {ModuleName.Cooling, 1},
        };

        public static int Chamber2ThetaAxisPosition(ModuleName chamber)
        {
            if (!TeachThetaAxisAddressPosition.ContainsKey(chamber))
            {
                LOG.Error($"not define TeachThetaAxisAddressPosition config {chamber}");
                return 0;
            }

            return TeachThetaAxisAddressPosition[chamber];
        }

        public static int ChamberSlot2ZAxisPosition(ModuleName chamber, int slot)
        {
            if (!TeachZAxisAddressPosition.ContainsKey(chamber))
            {
                LOG.Error($"not define TeachZAxisAddressPosition config {chamber}");
                return 0;
            }

            int sslot = TeachZAxisAddressPosition[chamber];
            if (sslot == -1)
            {
                sslot = slot + 1;
            }
            return sslot;
        }
    }
}
