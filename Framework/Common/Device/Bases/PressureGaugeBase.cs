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
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class PressureGaugeBase : BaseDevice, IDevice
    {
        public string Unit
        {
            get; set;
        }
 

        public virtual float SetPoint { get; set; }
       
        public virtual float FeedBack { get; set; }

        public virtual string FormatString { get; set; }


        public virtual  AITPressureMeterData DeviceData
        {
            get
            {
                AITPressureMeterData data = new AITPressureMeterData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    FeedBack = FeedBack,
                    FormatString = FormatString,
                };

                return data;
            }
        }
 
 

        public PressureGaugeBase( ):base()
        {
        }

        public PressureGaugeBase(string module, XmlElement node, string ioModule = "")
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
 

        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.FeedBack", () => FeedBack);
 

            return true;
        }
 

        public virtual void Monitor()
        {
 


        }

        public virtual void Reset()
        {
 
        }



        public virtual void Terminate()
        {
 
        }
 

    }
}
