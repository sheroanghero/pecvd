using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoEmo : BaseDevice, IDevice
    {
        public bool StopButtonSignal
        {
            get
            {
                return _diStopButton != null && _diStopButton.Value;
            }
        }

        public bool MainContactorSignal
        {
            get
            {
                return _diMainContactor == null || _diMainContactor.Value;
            }
        }

        private DIAccessor _diStopButton = null;
        private DIAccessor _diMainContactor = null;

        //private R_TRIG _trigStopButton = new R_TRIG();
        //private F_TRIG _trigMainContactor = new F_TRIG();

        private R_TRIG _trigEmoSignaled = new R_TRIG();

        public IoEmo(string module, XmlElement node, string ioModule = "")
        {

            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diStopButton = ParseDiNode("diStopButton", node, ioModule);
            _diMainContactor = ParseDiNode("diMainContactor", node, ioModule);

        }

        public bool Initialize()
        {
                                    DATA.Subscribe(string.Format("Device.{0}.{1}", Module, Name), () =>
                        {
                            AITEmoData data = new AITEmoData()
                            {
                                DeviceName =  Name,
 
                                DeviceSchematicId =  DeviceID,
                                DisplayName =  Display,

                                StopButtonSignal =  StopButtonSignal,
                                MainContactorSignal =  MainContactorSignal,
 
                            };

                            return data;
                        }, SubscriptionAttribute.FLAG.IgnoreSaveDB);
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            _trigEmoSignaled.CLK = StopButtonSignal || (!MainContactorSignal);

            if (_trigEmoSignaled.Q) //EMO被拍下
            {
                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Emergency Off button was pressed");
            }

            


        }

        public void Reset()
        {
            _trigEmoSignaled.RST = true;
        }

    }
}

