using System.Net.Configuration;
using System.Xml;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Device.Bases;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoSignalLight : BaseDevice, IDevice
    {
        protected DOAccessor _do = null;
        public bool Value
        {
            get
            {
                if (_do != null)
                    return _do.Value;

                return false;
            }
        }

        public int Interval
        {
            get { return _timeout; }
            set
            {
                if (value < 200)
                {
                    _timeout = 200;
                }
                else if (value > 20000)
                {
                    _timeout = 20000;
                }
                else
                {
                    _timeout = value;
                }

                
            }
        }
        public TowerLightStatus StateSetPoint { get; set; }
 
        private DeviceTimer _timer = new DeviceTimer();

        private bool _blinking = false;
        private int _timeout = 500;

        public IoSignalLight(string module, XmlElement node, string ioModule = "")
        {
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _do = ParseDoNode("doSet", node, ioModule);
        }

        public IoSignalLight(string module, string id, string display, string deviceId, DOAccessor doItem)
        {
            base.Module = module;
            base.Name = id;
            base.Display = display;
            base.DeviceID = deviceId;

            _do = doItem;
        }

        protected virtual void SetIoValue(bool value)
        {
            string reason;
            _do.SetValue(value, out reason);
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
            if (_timer.IsIdle()) _timer.Start(_timeout);

            if (_timer.IsTimeout())
            {
                _timer.Start(_timeout);
                _blinking = !_blinking;
            }

            switch (StateSetPoint)
            {
                case TowerLightStatus.On: SetIoValue(true); break;
                case TowerLightStatus.Off: SetIoValue(false); break;
                case TowerLightStatus.Blinking: SetIoValue(_blinking); break;
            }
        }

        public void Reset()
        {
	        StateSetPoint = TowerLightStatus.Off;
	        SetIoValue(false);
        }
    }


    public class IoSwitchableSignalLight : IoSignalLight
    {
        private SCConfigItem _scUsingOption = null;

        private DOAccessor _doDefault = null;
        private DOAccessor _doOption = null;

        public IoSwitchableSignalLight(string module, XmlElement node):base(module,node)
        {
            _doDefault = ParseDoNode("doSet", node);
            _doOption = ParseDoNode("doSetOption", node);
            _scUsingOption = ParseScNode("scUsingOption", node);
        }


        protected override void SetIoValue(bool value)
        {
            string reason;

            if (_scUsingOption.BoolValue)
            {
                _doOption.SetValue(value, out reason);
            }
            else
            {
                _doDefault.SetValue(value, out reason);
            }
 
        }
    }

}