using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPressureMeter2 : BaseDevice, IDevice, IPressureMeter
    {
        public enum UnitType
        {
            Torr,
            Pascal,
            Mbar,
            Pascal972B,
        }
        public double UnitValue
        {
            get
            {
                if (unitType == UnitType.Torr)
                    return GetTorr();
                else if (unitType == UnitType.Pascal)
                    return GetPascal();
                else if (unitType == UnitType.Pascal972B)
                    return GetPascal972B();
                else
                    return GetMbar();
            }
        }

        //Torr unit
        public double PressureValue
        {
            get
            {
                if (_type == "MKS.901P")
                    return PressureValue901P;
                if (_type == "MKS.974B")
                    return PressureValue974B;

                return RawValue;
            }
        }

        
        private double PressureValue901P
        {
            get
            {
                int offset = 0;
                int rawValve = RawValue;

                if (SC.ContainsItem("LoadLock.PressureOffset"))
                    offset = SC.GetConfigItem("LoadLock.PressureOffset").IntValue;

                rawValve -= offset;

                var voltage = Converter.Phy2Logic(rawValve, 1, 9, 3276.7, 29490.3);
                var torValue = Math.Pow(10.0, voltage - 6.0);
                return Unit == "mTorr" ? torValue * 1000 : torValue;
            }
        }

        private double PressureValue974B
        {
            get
            {
                var voltage = Converter.Phy2Logic(RawValue, 1.5, 7, 4915.05, 22936.9);
                var torValue = Math.Pow(10.0, 2 * voltage - 11.0);
                return Unit == "mTorr" ? torValue * 1000 : torValue;
            }
        }

        public short RawValue
        {
            get { return BitConverter.ToInt16(new byte[] {(byte) _aiLow.Value, (byte) _aiHigh.Value }, 0); }
        }

        private AITPressureMeterData DeviceData
        {
            get
            {
                AITPressureMeterData data = new AITPressureMeterData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    FeedBack = PressureValue,
                    Unit = Unit,
                    FormatString = _formatString,
                    DisplayWithUnit = true,
                };

                return data;
            }
        }


        public string Unit { get; set; }

        private AIAccessor _aiLow = null;
        private AIAccessor _aiHigh = null;

        //private SCConfigItem _scMinFlow;
        //private SCConfigItem _scMaxFlow;
 
        private UnitType unitType = UnitType.Pascal;

        private string _type = string.Empty;
        // 
        private double rangeMin = Int16.MinValue * 0.1;
        private double rangeMax = Int16.MaxValue * 0.9;
        private double min = Int16.MinValue * 0.1;
        private double max = Int16.MaxValue * 0.9;

        private string _formatString = "F5";

        public IoPressureMeter2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
  
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");
            

            _aiLow = ParseAiNode("aiLow", node, ioModule);
            _aiHigh = ParseAiNode("aiHigh", node, ioModule);

            _type = node.GetAttribute("type");

            if (node.HasAttribute("formatString"))
                _formatString = string.IsNullOrEmpty(node.GetAttribute("formatString")) ? "F5": node.GetAttribute("formatString");


            //Full Scale 0-10V
            this.min = Int16.MaxValue * min / 10.0;
            this.max = Int16.MaxValue * max / 10.0;
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.Value", () => UnitValue);
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

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

        public static double Voltage(double pressure)
        {
            double logic = 4.0 + Math.Log10(pressure);
            double min = Int16.MaxValue * 1 / 10.0;
            double max = Int16.MaxValue * 9 / 10.0;
            return Converter.Logic2Phy(logic, 1, 9, min, max);
        }
        public static double Voltage2(double pressure)
        {
            return Converter.Logic2Phy(pressure, 0, 1333 * 100, 0x0000, 0x5B6D);
        }



        private double GetPascal()
        {
            double voltage = Converter.Phy2Logic(RawValue, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 4.0);      //pascal
        }

        private double GetPascal972B()
        {
            double voltage = Converter.Phy2Logic(RawValue, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, 2 * voltage - 9);      //pascal
        }

        private double GetMbar()
        {
            double voltage = Converter.Phy2Logic(RawValue, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 6.0);      //mbar
        }
        private double GetTorr()
        {
            double voltage = Converter.Phy2Logic(RawValue, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 6.125);      //torr
        }

        private double GetAIScale()
        {
            return Converter.Phy2Logic(RawValue, 0, 1333, 0x0000, 0x7fff);
        }

    }
}


