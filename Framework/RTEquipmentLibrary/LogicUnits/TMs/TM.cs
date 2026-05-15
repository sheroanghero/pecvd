using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs
{
    public class TM : BaseDevice, IDevice, ITM
    {
        public virtual double ChamberPressure { get; }
        public virtual double ForelinePressure { get; }

        public TM(string module ) : base(module, module, module, module)
        {
         }
 

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Name}.IsAtm", () => { return CheckAtm(); });
            DATA.Subscribe($"{Name}.IsVacuum", () => { return CheckVacuum(); });
            DATA.Subscribe($"{Name}.ChamberPressure", () => ChamberPressure);
            DATA.Subscribe($"{Name}.ForelinePressure", () => ForelinePressure);

            return true;
        }


        public virtual bool CheckAtm()
        {
            return false;
        }

        public virtual bool CheckVacuum()
        {
            return false;
        }

        public virtual bool CheckIsPumping()
        {
            return false;
        }

        public virtual bool SetFastVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetSlowVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetFastPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetIsolationValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetTurboPumpIsoValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetTurboPumpBackingValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public virtual bool SetMfcVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public virtual bool SetAllValves(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetVentMfc(double flow, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetVentMfcFullFlow(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckTurboPumpStable()
        {
            return true;
        }

        public virtual bool CheckTurboPumpOn()
        {
            return true;
        }
        public virtual bool CheckTurboPumpOff()
        {
            return true;
        }
        public virtual bool CheckTurboPumpError()
        {
            return true;
        }
        public virtual bool TurboPumpOn()
        {
            return true;
        }
        public virtual bool TurboPumpOff()
        {
            return true;
        }
        public virtual IoSlitValve GetSlitValve(ModuleName module)
        {
            return null;
        }

        public virtual bool SetSlitValve(ModuleName module, bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckSlitValveOpen(ModuleName module)
        {
            return true;
        }

        public virtual bool CheckSlitValveClose(ModuleName module)
        {
            return true;
        }

        public virtual bool CheckIsoValveClose()
        {
            return true;
        }

        public virtual bool CheckIsoValveOpen()
        {
            return true;
        }
        public virtual bool CheckventValveOnOff()
        {
           
            return true;
        }
        public virtual bool TMCheck(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool CheckpumpValveOnOff()
        {
            
            return true;
        }
        public virtual void Monitor()
        {

        }

        public virtual void Terminate()
        {

        }

        public virtual void Reset()
        {

        }

  

    }
}
