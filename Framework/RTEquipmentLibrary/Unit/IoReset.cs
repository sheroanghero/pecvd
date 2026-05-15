using System.Xml;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoReset : BaseDevice, IDevice
    {
        private DOAccessor _doReset = null;
        private DeviceTimer _timer = new DeviceTimer();

        public IoReset(string module, XmlElement node, string ioModule = "")
        {
 
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doReset = ParseDoNode("doReset", node, ioModule);

        }

        public bool Initialize()
        {
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            if (_timer.IsTimeout())
            {
                _doReset.Value = false;
            }
        }

        public void Reset()
        {
            _timer.Start(5000);
            _doReset.Value = true;
            
        }

    }
}
