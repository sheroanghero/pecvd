using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Schedulers
{
    //helper class
    public class MoveItem
    {
        public ModuleName SourceModule;
        public int SourceSlot;
        public ModuleName DestinationModule;
        public int DestinationSlot;
        public Hand RobotHand;

        public MoveItem(ModuleName sourceModule, int sourceSlot, ModuleName destinationModule, int destinationSlot, Hand robotHand)
        {
            this.SourceModule = sourceModule;
            this.SourceSlot = sourceSlot;
            this.DestinationModule = destinationModule;
            this.DestinationSlot = destinationSlot;
            this.RobotHand = robotHand;

        }
    }
}
