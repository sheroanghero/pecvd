using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.CommonData.DeviceData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MECF.Framework.Common.Device.Bases
{
    [Serializable]
    public abstract class ChillerBase : BaseDevice, IDevice
    {
        public virtual bool IsConnected => true;
        public virtual bool IsCH1Alarm { get; set; }
        public virtual bool IsCH2Alarm { get; set; }
        public virtual bool IsCH1Warning { get; set; }
        public virtual bool IsCH2Warning { get; set; }
        public virtual bool IsCH1On { get; set; }
        public virtual bool IsCH2On { get; set; }

        public virtual float CH1TemperatureSetpoint { get; set; }
        public virtual float CH1TemperatureFeedback { get; set; }
        public virtual float CH2TemperatureSetpoint { get; set; }
        public virtual float CH2TemperatureFeedback { get; set; }
        public virtual float CH1WaterFlow { get; set; }
        public virtual float CH2WaterFlow { get; set; }
        public virtual float TemperatureHighLimit { get; set; }
        public virtual float TemperatureLowLimit { get; set; }

        public virtual bool IsCH1Remote { get; set; }
        public virtual bool IsCH2Remote { get; set; }


        public virtual AITChillerData1 DeviceData { get; set; }

        protected ChillerBase() : base()
        {

        }
        protected ChillerBase(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsCH1On", () => IsCH1On);
            DATA.Subscribe($"{Module}.{Name}.IsCH2On", () => IsCH2On);
            DATA.Subscribe($"{Module}.{Name}.IsCH1Alarm", () => IsCH1Alarm);
            DATA.Subscribe($"{Module}.{Name}.IsCH2Alarm", () => IsCH2Alarm);
            DATA.Subscribe($"{Module}.{Name}.CH1TemperatureSetpoint", () => CH1TemperatureSetpoint);
            DATA.Subscribe($"{Module}.{Name}.CH2TemperatureSetpoint", () => CH2TemperatureSetpoint);
            DATA.Subscribe($"{Module}.{Name}.CH1TemperatureFeedback", () => CH1TemperatureFeedback);
            DATA.Subscribe($"{Module}.{Name}.CH2TemperatureFeedback", () => CH2TemperatureFeedback);
            DATA.Subscribe($"{Module}.{Name}.CH1WaterFlow", () => CH1WaterFlow);
            DATA.Subscribe($"{Module}.{Name}.CH2WaterFlow", () => CH2WaterFlow);

            OP.Subscribe($"{Module}.{Name}.SetChillerCH1On", (function, args) =>
            {
                SetChillerCH1OnOff(Convert.ToBoolean(args[0]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetChillerCH2On", (function, args) =>
            {
                SetChillerCH2OnOff(Convert.ToBoolean(args[0]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetChillerCH1Temperature", (function, args) =>
            {
                SetChillerCH1Temperature(Convert.ToSingle(args[0]));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetChillerCH2Temperature", (function, args) =>
            {
                SetChillerCH2Temperature(Convert.ToSingle(args[0]));
                return true;
            });
            return true;
        }

        public virtual void SetChillerCH1OnOff(bool isOn)
        {

        }

        public virtual void SetChillerCH2OnOff(bool isOn)
        {

        }

        public virtual void SetChillerCH1Temperature(float temperature)
        {

        }

        public virtual void SetChillerCH2Temperature(float temperature)
        {

        }

        public virtual void Terminate()
        {
        }

        public virtual void Monitor()
        {

        }

        public virtual void Reset()
        {

        }

    }
}
