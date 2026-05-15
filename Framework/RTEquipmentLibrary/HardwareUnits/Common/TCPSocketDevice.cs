using Aitex.Core.RT.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common
{

    public class TCPSocketDevice : BaseDevice, IDevice
    {

        public TCPSocketDevice(string module, string name)
        {
            Module = module;
            Name = name;
        }
        public virtual bool Initialize()
        {
            return true;
        }
        public virtual bool Initialize(string portName)
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

        public virtual bool Home(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }

}
