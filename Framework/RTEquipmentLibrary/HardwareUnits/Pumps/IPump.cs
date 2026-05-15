using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps
{
    public interface IPump
    {
        bool IsRunning { get; }
        bool SetPump(out string reason, int time, bool isOn);
        bool SetMainPowerOnOff(bool isOn, out string reason);

    }
}
