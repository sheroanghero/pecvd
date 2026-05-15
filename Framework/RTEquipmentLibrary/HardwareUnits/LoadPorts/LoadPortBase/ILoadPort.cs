using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase
{
    public interface ILoadPort : IDevice
    {
        IE87CallBack LPCallBack { get; set; }
        IE84CallBack LPE84Callback { get; set; }
        CIDReaderBaseDevice CurrentReader { get; }
        bool IsBypassCarrierIDReader { get;}
        bool IsAutoReadCarrierID { get;}
        DeviceState State { get; }
        string SlotMap { get; }
        FoupClampState ClampState { get; set; }
        FoupDockState DockState { get; set; }
        string CarrierId { get; }
        bool IsMapped { get; }
        bool IsPlacement { get; }
        bool IsPresent { get; }
        string InfoPadCarrierType { get; set; }

        bool FAUnload(out string reason);
        bool FALoad(out string reason);
        bool Unclamp(out string reason);
        bool WriteRfid(string cid, int startpage, int length, out string reason);
        bool ReadRfId(out string reason);
        bool SetIndicator(IndicatorType light, IndicatorState state);
        bool SetE84Available(out string reason);
        bool SetE84Unavailable(out string reason);
        void ProceedSetCarrierID(string carrierID);
    }
}