using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class LidBase : BaseDevice, IDevice
    {
        public virtual bool OpenFeedback { get; set; }
        public virtual bool CloseFeedback { get; set; }

        public virtual bool OpenSetPoint { get; set; }
        public virtual bool CloseSetPoint { get; set; }

        public virtual AITCylinderData DeviceData { get; set; }

        protected LidBase() : base()
        {

        }
        protected LidBase(string module, string name) : base(module, name, name, name)
        {

        }


        public bool Initialize()
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


        public void Monitor()
        {

        }

        public void Terminate()
        {

        }

        public virtual bool SetSlitValve(bool open, out string reason)
        {
            reason = "";
            return true;
        }



        public void Reset()
        {
        }
    }
}

