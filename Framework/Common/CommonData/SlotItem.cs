using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Schedulers
{
    //helper class
    public class SlotItem
    {
        public ModuleName Module { get; set; }
        public int Slot { get; set; }

        public SlotItem(ModuleName module, int slot)
        {
            Module = module;
            Slot = slot;

        }
    }
}
