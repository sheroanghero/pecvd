using System;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace Aitex.Core.RT.Device.Unit
{
    public interface IGasStick
    {
        bool SetFlow(out string reason, int time, float flow);
    }

    public class IoGasStick : BaseDevice, IDevice, IGasStick
    {
        private IValve _valve;
        private IPressureMeter _pressure;
        private IMfc _mfc;
        private IValve _finalValve;

        public IoGasStick(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _valve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("valve")}") as IValve;
            _finalValve = DEVICE.GetDevice($"{Module}.{node.GetAttribute("final")}") as IValve;
            _pressure = DEVICE.GetDevice($"{Module}.{node.GetAttribute("pc")}") as IPressureMeter;
            _mfc = DEVICE.GetDevice($"{Module}.{node.GetAttribute("mfc")}") as IMfc;

            System.Diagnostics.Debug.Assert(_valve != null, "Valve setting not valid");
            System.Diagnostics.Debug.Assert(_finalValve != null, "Final Valve setting not valid");
            System.Diagnostics.Debug.Assert(_pressure != null, "Pressure switch setting not valid");
            System.Diagnostics.Debug.Assert(_mfc != null, "MFC setting not valid");
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

            if (!_valve.TurnValve(isOn, out reason))
                return false;

            //may other gas line is flowing, so we can not close the final valve here
            if (isOn && !_finalValve.TurnValve(isOn, out reason))
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
