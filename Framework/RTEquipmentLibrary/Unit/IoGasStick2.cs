using System;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoGasStick2 : BaseDevice, IDevice, IGasStick
    {
        private IValve _frontValve;
        private IValve _behindValve;
        private IoFlowMeter _flowMeter;
        private IMfc _mfc;
        private IValve _purgeValve;
        private IValve _ventValve;
        private IValve _pumpValve;

        public IMfc MFC
        {
            set
            {
                _mfc = value;
            }
            get
            {
                return _mfc;
            }
        }

        public IoGasStick2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _frontValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("frontValve")}") as IValve;
            _behindValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("behindValve")}") as IValve;
            _purgeValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("purgeValve")}") as IValve;
            _ventValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("ventValve")}") as IValve;
            _pumpValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("pumpValve")}") as IValve;
            _flowMeter = DEVICE.GetDevice($"{Module}.{node.GetAttribute("flowMeter")}") as IoFlowMeter;
            _mfc = DEVICE.GetDevice($"{Module}.{node.GetAttribute("mfc")}") as IMfc;
        }

        public bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.Flow", SetFlow);
 
            return true;
        }

        private bool SetFlow(out string reason, int time, object[] param)
        {
            return SetFlow(out reason, time, Convert.ToSingle(param[0].ToString()));
        }

        public bool SetFlow(out string reason, int time, float flow)
        {
            bool isOn = flow > 0.1;

            if (_frontValve != null && !_frontValve.TurnValve(isOn, out reason))
                return false;

            if (_behindValve != null && !_behindValve.TurnValve(isOn, out reason))
                return false;

            if(_purgeValve!=null && _purgeValve.TurnValve(false, out reason))
                return false;

            if (_ventValve != null && _ventValve.TurnValve(false, out reason))
                return false;

            if (_pumpValve != null && _pumpValve.TurnValve(false, out reason))
                return false;

            if (!_mfc.Ramp(flow, time, out reason))
                return false;

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {

            }
            catch (Exception ex)
            {
                
                LOG.Write(ex);
            }

        }

        public void Reset()
        {

        }
    }
}
