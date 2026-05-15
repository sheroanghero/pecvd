using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLocks
{
    public class LoadLock : BaseDevice, IDevice, ILoadLock
    {
        public virtual double ChamberPressure { get; }
        public virtual double ForelinePressure { get; }
        public virtual bool EnableLift { get; }

        public virtual double ForelinePressureDryPump { get; }

        public const int SlotCount = 2;

        public LoadLock(string module, int slotCount) : base(module, module, module, module)
        {
            //WaferManager.Instance.SubscribeLocation(module, slotCount);
            EnableLift = true;
        }

        public LoadLock(string module):this(module, SlotCount)
        {
            
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Name}.IsAtm", () => { return CheckAtm(); });
            DATA.Subscribe($"{Name}.IsVacuum", () => { return CheckVacuum(); });
            DATA.Subscribe($"{Name}.ChamberPressure", () => ChamberPressure);
            DATA.Subscribe($"{Name}.ForelinePressure", () => ForelinePressure);
            DATA.Subscribe($"{Name}.EnableLift", () => { return EnableLift; });

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
 
        public virtual bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckIsoValveState()
        {
            return true;
        }

        public virtual bool CheckTurboPumpState()
        {
            return true;
        }

        public virtual bool CheckTurboPumpError()
        {
            return true;
        }

        public virtual bool CheckTurboDryPumpState()
        {
            return true;
        }

        public virtual bool CheckTurboDryPumpError()
        {
            return true;
        }

        public virtual bool CheckBackValveState()
        {
            return true;
        }

        public virtual bool SetIsoValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetBackValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetTurboPump(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetEqualVentValve(bool isOpen, out string reason)
        {
            reason = string.Empty;
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

        public virtual bool SetDoor(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckDoorOpen()
        {
            return true;
        }

        public virtual bool CheckDoorClose()
        {
            return true;
        }

        public virtual bool SetPowerSV(float value)
        {
            return true;
        }

        public virtual bool SetLift(bool isUp, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetLift(int slot, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckLiftUp()
        {
            return true;
        }

        public virtual bool CheckLiftDown()
        {
            return true;
        }
        public virtual bool CheckLift(int slot)
        {
            return true;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type)
        {
            return false;
        }
    }
}
