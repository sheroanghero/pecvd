using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class DummyLoadPort : LoadPort
    {
        public DummyLoadPort(string module, string name) : base(module, name)
        {
        }

        public override bool IsEnableMapWafer()
        {
            return false;
        }

        public override bool IsEnableTransferWafer()
        {
            return false;
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            return false;
        }

    }
}
