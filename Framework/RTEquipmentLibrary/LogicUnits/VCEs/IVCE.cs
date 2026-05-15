
namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCEs
{
    public interface IVCE
    {
        bool CheckAtm();
        bool CheckVacuum();
        bool SetFastVentValve(bool isOpen, out string reason);
        bool SetSlowVentValve(bool isOpen, out string reason);

        bool SetFastPumpValve(bool isOpen, out string reason);
        bool SetSlowPumpValve(bool isOpen, out string reason);
    }
}
