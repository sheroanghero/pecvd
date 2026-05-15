using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoChiller : BaseDevice, IDevice, IChiller
    {
        public bool IsRunning
        {
            get
            {
                return _diRunning.Value;
            }
        }

        private bool IsError
        {
            get
            {
                return _diAlarm.Value;
            }
        }

        private bool PowerOnSetPoint
        {
            get
            {

                return _doPowerOn!=null && _doPowerOn.Value;
            }
            set
            {
                if (_doPowerOn != null)
                {
                    if (!_doPowerOn.SetValue(value, out string reason))
                    {
                        LOG.Write(reason);
                    }
                }

            }
        }

        private AITChillerData DeviceData
        {
            get
            {
                AITChillerData data = new AITChillerData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    IsRunning =  IsRunning,
                    IsError =  IsError,

                };

                return data;
            }
        }

        private DIAccessor _diRunning = null;
        private DIAccessor _diAlarm;

        private DOAccessor _doPowerOn = null;


        public AlarmEventItem AlarmHasError { get; set; }
 

        public IoChiller(string module, XmlElement node, string ioModule = "")
        {
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _diRunning = ParseDiNode("diRunning", node, ioModule);
 
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsError", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.IsRunning", () => DeviceData);

            OP.Subscribe($"{Module}.{Name}.{AITChillerOperation.ChillerOn}", SetChillerOn);
            OP.Subscribe($"{Module}.{Name}.{AITChillerOperation.ChillerOff}", SetChillerOff);


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


        private bool SetChillerOn(out string reason, int time, object[] param)
        {
            return SetMainPowerOnOff(true, out reason );
        }

        private bool SetChillerOff(out string reason, int time, object[] param)
        {
            return SetMainPowerOnOff(false, out reason );
        }

        public bool SetMainPowerOnOff(bool isOn, out string reason)
        {
            PowerOnSetPoint = isOn;
            reason = string.Empty;

            return true;
        }

    }
}