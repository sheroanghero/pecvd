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
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class SlitValveBase : BaseDevice, IDevice
    {
        public virtual bool OpenFeedback { get; set; }
        public virtual bool CloseFeedback { get; set; }

        public virtual bool OpenSetPoint { get; set; }
        public virtual bool CloseSetPoint { get; set; }

        public virtual AITCylinderData DeviceData { get; set; }

        protected SlitValveBase() : base()
        {

        }
        protected SlitValveBase(string module, string name) : base(module, name, name, name)
        {

        }
 

        public virtual bool Initialize()
        {
 
            DATA.Subscribe($"{Module}.{Name}.OpenFeedback", () => OpenFeedback);
            DATA.Subscribe($"{Module}.{Name}.OpenSetPoint", () => OpenSetPoint);

            DATA.Subscribe($"{Module}.{Name}.CloseFeedback", () => CloseFeedback);
            DATA.Subscribe($"{Module}.{Name}.CloseSetPoint", () => CloseSetPoint);

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DEVICE.Register($"{Module}.{Name}.Open", (out string reason, int time, object[] param) =>
            {
                reason = "";

                return SetSlitValve(true, out reason);
            });

            DEVICE.Register($"{Module}.{Name}.Close", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return SetSlitValve(false, out reason);
            });

            return true;
        }


        public virtual void Monitor()
        {
 
        }

        public virtual void Terminate()
        {
 
        }

        public virtual bool CheckIsClose()
        {
            return CloseFeedback && !OpenFeedback;
        }
        public virtual bool CheckIsOpen()
        {
            return !CloseFeedback && OpenFeedback;
        }

        public virtual bool SetSlitValve(bool open, out string reason)
        {
            reason = "";
            return true;
        }



        public virtual void Reset()
        {
        }
    }
}

 