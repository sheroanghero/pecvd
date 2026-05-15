using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Diagnostics;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPump3 : BaseDevice, IDevice
    {
        private DIAccessor _diStart;
        private DIAccessor _diError;

        private DOAccessor _doStart;

        

        private bool _isFloatAioType = true;

        public IoPump3(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diStart = ParseDiNode("diStart", node, ioModule);
            _diError = ParseDiNode("diError", node, ioModule);

            _doStart = ParseDoNode("doStart", node, ioModule);
           
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            if ($"{Module}.{Name}" == "TM.SystemDryPump")
            {
                DATA.Subscribe($"PM1.SystemDryPump.DeviceData", () => DeviceData);
                DATA.Subscribe($"PM2.SystemDryPump.DeviceData", () => DeviceData);
                DATA.Subscribe($"PM3.SystemDryPump.DeviceData", () => DeviceData);
                DATA.Subscribe($"PM4.SystemDryPump.DeviceData", () => DeviceData);
                DATA.Subscribe($"PM5.SystemDryPump.DeviceData", () => DeviceData);
                DATA.Subscribe($"PM6.SystemDryPump.DeviceData", () => DeviceData);
            }

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.SetOnOff}", SetPumpOnOff);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOn}", SetPumpOn);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOff}", SetPumpOff);

            return true;
        }

        public void Monitor()
        {

        }

        public void Reset()
        {

        }

        public void Terminate()
        {

        }

        private bool SetPumpOn(out string reason, int time, object[] param)
        {
            return SetPump(out reason, true);
        }

        private bool SetPumpOff(out string reason, int time, object[] param)
        {
            return SetPump(out reason, false);
        }

        public bool SetPumpOnOff(out string reason, int time, object[] param)
        {
            return SetPump(out reason, Convert.ToBoolean((string)param[0]));

        }

        public bool SetPump(out string reason, bool isOn)
        {
            reason = string.Empty;
            if (isOn)
            {
                //if (IsError)
                //{
                //    reason = "Can not set pump on, pump in error";
                //    return false;
                //}
            }



            if (!_doStart.Check(isOn, out reason))
            {
                return false;
            }
            if (!_doStart.SetValue(isOn, out reason))
            {
                return false;
            }



            return true;
        }

        private AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    DeviceModule = Module,
                    Module = Module,

                    IsOn = DiStart,
                    IsError = DiError

                };


                return data;
            }
        }

        #region DI
        

        public bool DiStart
        {
            get
            {
                return _diStart == null ? false : _diStart.Value;
            }
        }

        public bool DiError
        {
            get
            {
                return _diError == null ? false : _diError.Value;
            }
        }

        #endregion DI
        #region DO
        public bool DoStart
        {
            get
            {
                if (_doStart != null)
                    return _doStart.Value;

                return false;
            }
            set
            {
                if (_doStart != null)
                {
                    _doStart.Value = value;
                }
            }
        }
        #endregion DO
        #region AI
       
        #endregion AI

    }
}
