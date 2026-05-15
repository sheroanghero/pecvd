using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
    public class SorterRecipeTransferTableItem
    {
        public ModuleName SourceStation { get; set; }
        public int SourceSlot { get; set; }
        public ModuleName DestinationStation { get; set; }
        public int DestinationSlot { get; set; }
        public bool IsReadLaserMarker { get; set; }
        public bool IsReadT7Code { get; set; }
        public bool IsVerifyLaserMarker { get; set; }
        public bool IsVerifyT7Code { get; set; }
        public bool IsAlign { get; set; }
        public bool IsTurnOver { get; set; } = false;
        public double AlignAngle { get; set; }
        public OrderByMode OrderBy { get; set; }

    }
}
