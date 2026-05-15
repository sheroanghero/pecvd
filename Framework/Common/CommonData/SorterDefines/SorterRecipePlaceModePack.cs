using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum SorterRecipePlaceModePack
    {
        [Description("From Bottom Insert")]
        FromBottomInsert,

        [Description("From Bottom Shift")]
        FromBottomShift,

        [Description("From Top Insert")]
        FromTopInsert,

        [Description("From Top Shift")]
        FromTopShift,

    }
}
