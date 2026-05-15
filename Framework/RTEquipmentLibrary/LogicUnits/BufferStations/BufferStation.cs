using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.SubstrateTrackings;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.BufferStations
{
    public class BufferStation : BaseDevice, IDevice
    {
        public virtual bool IsMapped { get; set; }

        public BufferStation(string module, string name, int slotNumber)
        {
            Module = module;
            Name = name;

            WaferManager.Instance.SubscribeLocation(name, slotNumber);

        }


        public virtual bool Initialize()
        {
            return true;
        }

        public void Monitor()
        {


        }

        public void Terminate()
        {

        }


        public virtual void Reset()
        {

        }

        public virtual bool IsEnableTransferWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual bool IsEnableMapWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual void ConfirmWaferPresent()
        {

        }


        public virtual void OnSlotMapRead(string slotMap)
        {
        }
    }
}
