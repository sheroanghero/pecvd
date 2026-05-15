using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs
{
    public interface ITM
    {
        bool CheckAtm();
        bool CheckVacuum();

        bool SetFastVentValve(bool isOpen, out string reason);
        bool SetSlowVentValve(bool isOpen, out string reason);

        bool SetFastPumpValve(bool isOpen, out string reason);
        bool SetSlowPumpValve(bool isOpen, out string reason);

        bool SetTurboPumpIsoValve(bool isOpen, out string reason);
        bool SetTurboPumpBackingValve(bool isOpen, out string reason);

        bool CheckTurboPumpStable();
        bool CheckTurboPumpOn();
        bool CheckTurboPumpOff();
        bool CheckTurboPumpError();
        bool TurboPumpOn();
        bool TurboPumpOff();

        bool SetSlitValve(ModuleName module, bool isOpen, out string reason);

        bool CheckSlitValveOpen(ModuleName module);
        bool CheckSlitValveClose(ModuleName module);

    }
}
