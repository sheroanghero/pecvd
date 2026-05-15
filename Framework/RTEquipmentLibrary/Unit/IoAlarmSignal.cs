using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using System.Diagnostics;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    ///  DO  reset
    /// DI alarm signal
    /// </summary>
    public class IoAlarmSignal : BaseDevice, IDevice
    {
        private bool SignalFeedback
        {
            get
            {
                if (_diSignal != null)
                    return _diSignal.Value;
                return false;
            }
            set
            {
            }
        }

        private bool SignalSetPoint
        {
            get
            {
                if (_doReset != null)
                    return _doReset.Value;
                return false;
            }
            set
            {
                if (_doReset != null)
                    _doReset.Value = value;
            }
        }
        private DIAccessor _diSignal = null;

        private DOAccessor _doReset = null;

        private Stopwatch _timer = new Stopwatch();

        public AlarmEventItem AlarmTriggered { get; set; }

        private R_TRIG _trigSignalOn = new R_TRIG();

        public IoAlarmSignal(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _doReset = ParseDoNode("doReset", node, ioModule);
            _diSignal = ParseDiNode("diSignal", node, ioModule);

        }

        public bool Initialize()
        {
            AlarmTriggered = SubscribeAlarm($"{Module}.{Name}.AlarmTriggered", $"{Name} alarm triggered", ResetAlarmChecker);


            return true;
        }

        private bool ResetAlarmChecker()
        {
            if (SignalFeedback)
            {
                if (!SignalSetPoint)
                {
                    _timer.Restart();
                    SignalSetPoint = true;
                }
            }

            return !SignalFeedback;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            _trigSignalOn.CLK = SignalFeedback ;
            if (_trigSignalOn.Q)
            {
                    AlarmTriggered.Set(Display);
            }

            if (SignalSetPoint)
            {
                if (!SignalFeedback)
                {
                    AlarmTriggered.Reset();
                    SignalSetPoint = false;
                    _timer.Stop();
                }

                if (SignalFeedback && _timer.IsRunning &&
                    _timer.ElapsedMilliseconds > 5 * 1000)
                {
                    _timer.Stop();
                    EV.PostWarningLog(Module, $"Can not reset {Display} in 5 seconds");
                    SignalSetPoint = false;
                }
            }
        }

        public void Reset()
        {
            _trigSignalOn.RST = true;

            AlarmTriggered.Reset();

        }

        public void SetIgnoreError(bool ignore)
        {
            AlarmTriggered.SetIgnoreError(ignore);
        }

    }
}