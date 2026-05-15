using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Schedulers;
using System;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCEs
{
    public class VCE : BaseDevice, IDevice, IVCE
    {
        public virtual double ChamberPressure { get; }
        public virtual double ForelinePressure { get; }

        public VCE(string module) : base(module, module, module, module)
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


        public virtual void Monitor()
        {
            
        }

        public virtual void Reset()
        {
        }

        public virtual bool SetFastPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetFastVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetSlowVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type, int slot, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void Terminate()
        {
            
        }
    }
}
