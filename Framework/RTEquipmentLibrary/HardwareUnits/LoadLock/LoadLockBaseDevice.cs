using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadLock
{
    public enum LoadLockDoorState
    {
        Opened,
        Closed,
        Unknown
    }

    public class LoadLockBaseDevice : BaseDevice, IDevice
    {
        public int SlotCount { get; set; }
        public virtual bool IsIdle { get; set; }     

        public virtual LoadLockDoorState LoadLockAtmDoorState
        {
            get;set;
        }
      
        public virtual LoadLockDoorState LoadLockVtmDoorState
        {
            get;set;            
        }

        public LoadLockBaseDevice(string module, string name, int slotNumber)
        {
            Module = module;
            Name = name;

            SlotCount = slotNumber;
            WaferManager.Instance.SubscribeLocation(name, slotNumber);            
        }

        
        public bool Initialize()
        {
          
            return true;
        }

        public bool IsEnableExtend
        {
            get;set;
        }
   
        public void Monitor()
        {
            
        }

        public void Terminate()
        {

        }

        public void Reset()
        {

        }

        

    }
}
