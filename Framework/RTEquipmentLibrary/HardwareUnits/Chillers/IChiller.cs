using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers
{
    public interface IChiller
    {
        bool IsRunning { get; }

        bool SetMainPowerOnOff(bool isOn, out string reason);
    }
}
