using System.Xml;
using Aitex.Core.RT.IOCore;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoTrigger : BaseDevice, IDevice
    {
        private DOAccessor _doTrigger = null;
        public DOAccessor DoTrigger => _doTrigger;

        public IoTrigger(string module, XmlElement node, string ioModule = "")
        {
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doTrigger = ParseDoNode("doTrigger", node, ioModule);


        }

        public bool Value => _doTrigger.Value;

        public bool SetTrigger(bool value, out string reason)
        {
            return _doTrigger.SetValue(value, out reason);

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
 

        }

        public void Reset()
        {
 
        }

    }
}
