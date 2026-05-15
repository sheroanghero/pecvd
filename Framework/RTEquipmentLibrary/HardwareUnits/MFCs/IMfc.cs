using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs
{
    public interface IMfc
    {
        bool Ramp(double flowSetPoint, int time, out string reason);
    }
}
