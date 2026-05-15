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
    public interface IPressureMeter
    {

    }

    public class IoPressureMeter : BaseDevice, IDevice, IPressureMeter
    {
        public enum UnitType
        {
            Torr,
            Pascal,
            Mbar,
            Pascal972B,
        }
        public double Value
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



        public string Unit { get; set; }

        private AIAccessor ai = null;


        //private SCConfigItem _scMinFlow;
        //private SCConfigItem _scMaxFlow;
  
        private RD_TRIG _trigValveOpenClose = new RD_TRIG();

        private UnitType unitType = UnitType.Pascal;

        // 
        private double rangeMin = Int16.MinValue * 0.1;
        private double rangeMax = Int16.MaxValue * 0.9;
        private double min = Int16.MinValue * 0.1;
        private double max = Int16.MaxValue * 0.9;

        public IoPressureMeter(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");

            ai = ParseAiNode("aiValue", node, ioModule);

            string[] range = node.GetAttribute("range").Split(',');
            double.TryParse(range[0], out rangeMin);
            double.TryParse(range[1], out rangeMax);

            //Full Scale 0-10V
            //this.min = Int16.MaxValue * min / 10.0;
            //this.max = Int16.MaxValue * max / 10.0;
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.Value", () => Value);

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
            double voltage = Converter.Phy2Logic(ai.Value, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 4.0);      //pascal
        }

        private double GetPascal972B()
        {
            double voltage = Converter.Phy2Logic(ai.Value, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, 2 * voltage - 9);      //pascal
        }

        private double GetMbar()
        {
            double voltage = Converter.Phy2Logic(ai.Value, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 6.0);      //mbar
        }
        private double GetTorr()
        {
            double voltage = Converter.Phy2Logic(ai.Value, rangeMin, rangeMax, min, max);
            return Math.Pow(10.0, voltage - 6.125);      //torr
        }

        private double GetAIScale()
        {
            return Converter.Phy2Logic(ai.Value, 0, 1333, 0x0000, 0x7fff);
        }

    }

}


