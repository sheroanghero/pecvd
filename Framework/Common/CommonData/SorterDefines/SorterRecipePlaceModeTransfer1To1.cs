using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum SorterRecipePlaceModeTransfer1To1
    {
        [Description("From Bottom")]
        FromBottom,

        [Description("From Top")]
        FromTop,

        [Description("Same Slot")]
        SameSlot,

        [Description("Odd Slot From Bottom")]
        OddFromBotton,

        [Description("Odd Slot From Top")]
        OddFromTop,

        [Description("Even Slot From Bottom")]
        EvenFromBotton,

        [Description("Even Slot From Top")]
        EvenFromTop,

        [Description("Identify Slot by laser mark")]
        IdentifySlotByLaserMark,

        [Description("To Opposite Postion")]
        ToOppositePosition,

        [Description("To Fixed Slot")]
        ToFixedSlot,

    }
    public enum SorterPickMode
    {
        [Description("From Bottom")]
        FromBottom,

        [Description("From Top")]
        FromTop,
    }
}
