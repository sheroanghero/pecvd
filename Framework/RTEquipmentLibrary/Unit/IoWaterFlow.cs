using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoWaterFlow : BaseDevice, IDevice
    {
        public IoWaterFlow(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiWaterFlow = ParseAiNode("aiWaterFlow", node, ioModule);
            _aiTemperature = ParseAiNode("aiTemperature", node, ioModule);

            _doWaterFlowLow = ParseDoNode("doWaterFlowLow", node, ioModule);

            _pmInstalled = SC.GetValue<bool>($"System.SetUp.{Module}.IsInstalled");
            _ChillerIsInstalled = SC.GetValue<bool>($"System.SetUp.{Module}.ChillerIsInstalled");
            _pmType = SC.GetStringValue($"System.SetUp.{Module}.ChamberType");

            _scWaterFlowLowAlarm = SC.GetConfigItem($"System.WaterFlow.WaterFlowLowAlarm");
            _scTemperatureHighAlarm = SC.GetConfigItem($"System.WaterFlow.TemperatureHighAlarm");
        }

        private bool _pmInstalled;
        private bool _ChillerIsInstalled;
        private string _pmType;

        private AIAccessor _aiWaterFlow;
        private AIAccessor _aiTemperature;

        private DOAccessor _doWaterFlowLow;

        protected SCConfigItem _scWaterFlowLowAlarm;
        protected SCConfigItem _scTemperatureHighAlarm;

        private R_TRIG _rtrigWaterFlowLowAlarm = new R_TRIG();
        private R_TRIG _rtrigTemperatureHighAlarm = new R_TRIG();

        public double AiWaterFlow
        {
            get
            {
                if (_aiWaterFlow == null)
                    return 0;


                return _aiWaterFlow.Value / 10.0;
            }
        }

        public double AiTemperature
        {
            get
            {
                if (_aiTemperature == null)
                    return 0;


                return _aiTemperature.Value / 10.0;
            }
        }

        public bool DoWaterFlowLow
        {
            get
            {
                if (_doWaterFlowLow != null)
                    return _doWaterFlowLow.Value;

                return false;
            }
            set
            {
                if (_doWaterFlowLow != null)
                {
                    _doWaterFlowLow.Value = value;
                }
            }
        }

        //public AITHeaterData DeviceData
        //{
        //    get
        //    {
        //        return new AITHeaterData()
        //        {
        //            DeviceName = Name,
        //            DeviceSchematicId = DeviceID,
        //            DisplayName = Display,
        //            Module = Module,

        //            Scale = Scale,
        //            Unit = "℃",
        //            SetPoint = 0,
        //            FeedBack = Temperature,
        //            IsWarning = false,
        //        };
        //    }
        //}

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.WaterFlow", () => AiWaterFlow);
            DATA.Subscribe($"{Module}.{Name}.Temperature", () => AiTemperature);
            //DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);




            return true;
        }


        public void Monitor()
        {
            if (!_pmInstalled || !_ChillerIsInstalled || _pmType != "PVD")
            {
                return;
            }

            if (AiWaterFlow < _scWaterFlowLowAlarm.DoubleValue)
            {
                DoWaterFlowLow = true;
            }
            else
            {
                DoWaterFlowLow = false;
            }

            _rtrigWaterFlowLowAlarm.CLK = AiWaterFlow < _scWaterFlowLowAlarm.DoubleValue;
            if (_rtrigWaterFlowLowAlarm.Q)
            {
                EV.PostAlarmLog(Module, $"Water Flow Low {AiWaterFlow} < {_scWaterFlowLowAlarm.DoubleValue}");
            }

            _rtrigTemperatureHighAlarm.CLK = AiTemperature > _scTemperatureHighAlarm.DoubleValue;
            if (_rtrigTemperatureHighAlarm.Q)
            {
                EV.PostAlarmLog(Module, $"Chiller Temperature High {AiTemperature} > {_scTemperatureHighAlarm.DoubleValue}");
            }
        }

        public void Terminate()
        {
        }

        public void Reset()
        {
            _rtrigWaterFlowLowAlarm.RST = true;
            _rtrigTemperatureHighAlarm.RST = true;
        }
    }
}