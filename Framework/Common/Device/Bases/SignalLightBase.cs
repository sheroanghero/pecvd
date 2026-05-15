using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class SignalLightBase : BaseDevice, IDevice
    {
        public virtual bool Value
        {
            get
            {
                if (StateSetPoint == TowerLightStatus.Blinking)
                    return _blinkingToken;

                return StateSetPoint != TowerLightStatus.Off;
            }
        }

        public int Interval
        {
            get { return _timeout; }
            set
            {
                if (value < 200)
                {
                    _timeout = 200;
                }
                else if (value > 20000)
                {
                    _timeout = 20000;
                }
                else
                {
                    _timeout = value;
                }
            }
        }

        public TowerLightStatus StateSetPoint { get; set; }

        private DeviceTimer _timer = new DeviceTimer();

        private bool _blinkingToken = false;
        private int _timeout = 1000;


        public SignalLightBase(string module, string name) : base(module, name, name, name)
        {
            if (SC.ContainsItem("System.SignalLightBlinkingTimeout"))
            {
                _timeout = SC.GetValue<int>("System.SignalLightBlinkingTimeout");
            }
        }

        protected abstract void SetOn();
        protected abstract void SetOff();
        protected abstract void SetBlinking(bool token);


        public bool Initialize()
        {
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            if (_timer.IsIdle()) _timer.Start(_timeout);

            if (_timer.IsTimeout())
            {
                _timer.Start(_timeout);
                _blinkingToken = !_blinkingToken;
            }

            switch (StateSetPoint)
            {
                case TowerLightStatus.On:
                    SetOn();
                    break;
                case TowerLightStatus.Off:
                    SetOff();
                    break;
                case TowerLightStatus.Blinking:
                    SetBlinking(_blinkingToken);
                    break;
            }
        }

        public virtual void Reset()
        {
            StateSetPoint = TowerLightStatus.Off;
        }
    }
}
