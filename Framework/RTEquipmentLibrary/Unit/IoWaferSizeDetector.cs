using System;
using System.Security.AccessControl;
using System.Xml;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoWaferSizeDetector : BaseDevice, IDevice
    {
        public WaferSize Value
        {
            get
            {
                if (_diSensorInch3 != null && _diSensorInch3.Value)
                    return WaferSize.WS3;
                if (_diSensorInch4 != null && _diSensorInch4.Value)
                    return WaferSize.WS4;
                if (_diSensorInch6 != null && _diSensorInch6.Value)
                    return WaferSize.WS6;

                return WaferSize.WS0;
            }
        }

        public bool NotPresent3
        {
            get { return _diSensorInch3.Value; }
        }

        public bool NotPresent4
        {
            get { return _diSensorInch4.Value; }
        }

        public bool NotPresent6
        {
            get { return _diSensorInch6.Value; }
        }
        public bool HasCassette
        {
            get { return !_diSensorInch3.Value || !_diSensorInch4.Value || !_diSensorInch6.Value; }
        }
        private DIAccessor _diSensorInch3 = null;
        private DIAccessor _diSensorInch4 = null;
        private DIAccessor _diSensorInch6 = null;


        public IoWaferSizeDetector(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diSensorInch3 = ParseDiNode("diSensorInch3", node, ioModule);
            _diSensorInch4 = ParseDiNode("diSensorInch4", node, ioModule);
            _diSensorInch6 = ParseDiNode("diSensorInch6", node, ioModule);

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
