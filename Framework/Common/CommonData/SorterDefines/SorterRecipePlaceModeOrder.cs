using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum SorterRecipePlaceModeOrder
    {
        Forward,

        [Description("Forward Pack")]
        ForwardPack,

        Reverse,

        [Description("Reverse Pack")]
        ReversePack,
    }
}
