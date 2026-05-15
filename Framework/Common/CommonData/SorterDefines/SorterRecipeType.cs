using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum SorterRecipeType
    {
        [Description("Transfer 1 To 1")]
        Transfer1To1,
        [Description("Transfer N To 1")]
        TransferNTo1,
        [Description("Transfer N To N")]
        TransferNToN,
        Pack,
        Order,
        Align,
        [Description("Read Wafer Id")]
        ReadWaferId,
        [Description("Host Usage")]
        HostNToN,

    }
}
