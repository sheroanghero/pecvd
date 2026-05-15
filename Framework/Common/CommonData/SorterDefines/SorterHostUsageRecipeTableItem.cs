using System.Collections.Generic;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
    public class SorterHostUsageRecipeTableItem
    {
        public int PreAlignAngle { get; set; }
        public bool LaserMark1Read { get; set; }
        public bool LaserMark1Verify { get; set; }
        public bool LaserMark1VerifyCheckSum { get; set; }
        public int LaserMark1Reader { get; set; }
        public bool LaserMark2Read { get; set; }
        public bool LaserMark2Verify { get; set; }
        public bool LaserMark2VerifyCheckSum { get; set; }
        public int LaserMark2Reader { get; set; }
        public KeyValuePair<string, string> LaserMark1JobFile { get; set; }
        public KeyValuePair<string, string> LaserMark2JobFile { get; set; }
        public bool NeedPostAlign { get; set; }
        public int PostAlignAngle { get; set; }
    }
}