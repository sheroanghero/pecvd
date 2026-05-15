using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Device.Bases;

namespace Aitex.Core.RT.Device.Unit
{


    public class IoLid : BaseDevice, IDevice
    {
        private DIAccessor _diOpened;
        private DIAccessor _diClosed;

        private DOAccessor _doOpen;
        private DOAccessor _doClose;

        private LidState _operation;
        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();

        public int SetPoint   
        {
            get
            {
                if (_doOpen.Value && _doClose.Value) return (int) LidState.Error;
                if (_doOpen.Value && !_doClose.Value) return (int) LidState.Open;
                if (!_doOpen.Value && _doClose.Value) return (int) LidState.Close;
                if (!_doOpen.Value && !_doClose.Value) return (int) LidState.Unknown;

                return (int) LidState.Unknown;
            }
        }

        public int Status 
        {
            get
            {
                if (_diOpened.Value && _diClosed.Value)
                    return (int)LidState.Error;
                if (_diOpened.Value && !_diClosed.Value)
                    return (int)LidState.Open;
                if (!_diOpened.Value && _diClosed.Value)
                    return (int)LidState.Close;
                if (!_diOpened.Value && !_diClosed.Value)
                    return (int)LidState.Unknown;

                return (int)LidState.Unknown;
            }
        }
 
        public IoLid(string module, XmlElement node, string ioModule = "")
        {

            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diOpened = ParseDiNode("diOpen", node, ioModule);
            _diClosed = ParseDiNode("diClose", node, ioModule);
            _doOpen = ParseDoNode("doOpen", node, ioModule);
            _doClose = ParseDoNode("doClose", node, ioModule);

        }

        public bool Initialize()
        {
            DEVICE.Register(String.Format("{0}.{1}", Name, AITLidOperation.OpenLid), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLid(true, out reason);
                if (ret)
                {
                    reason = string.Format("Open Lid {0}", Name);
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITLidOperation.CloseLid), (out string reason, int time, object[] param) =>
            {
                bool ret = SetLid(false, out reason);
                if (ret)
                {
                    reason = string.Format("Close Lid {0}", Name);
                    return true;
                }

                return false;
            });

            return true;
        }

        public void Terminate()
        {
            _doOpen.Value = false;
            _doClose.Value = false;
        }

        public bool SetLid(bool isOpen, out string reason)
        {
            if (!_doOpen.Check(isOpen, out reason))
                return false;
            if (!_doClose.Check(!isOpen, out reason))
                return false;

            if (!_doOpen.SetValue(isOpen, out reason))
                return false;
            if (!_doClose.SetValue(!isOpen, out reason))
                return false;
            
            _operation = isOpen ? LidState.Open : LidState.Close;
            _timer.Start(1000 * 60 * 3);
            return true;
        }

        public void Monitor()
        {
            try
            {
                if (_timer.IsTimeout())
                {
                    _timer.Stop();

                    if (Status != (int)_operation)
                    {
                        if (_operation == LidState.Open)
                        {
                            string reason;
                            if (!_doOpen.Check(true, out reason))
                                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Open lid Failed for interlock, " + reason);
                            else
                                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Lid hold close status");
                        }
                        else
                        {
                            string reason;
                            if (!_doOpen.Check(false, out reason))
                                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Close lid Failed for interlock, " + reason);
                            else
                                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Lid hold open status");
                        }
                    }
                    _operation = (LidState)SetPoint;
                }
                else if (_timer.IsIdle())
                {
                    _trigReset.CLK = SetPoint != (int)_operation;   // fire event only check at first, SetPoint set by interlock
                    if (_trigReset.Q)
                    {
                        if (_operation == LidState.Open)
                        {
                            string reason;
                            if (!_doOpen.Check(true, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Close", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Close", "PLC kept"));
                        }
                        else
                        {
                            string reason;
                            if (!_doOpen.Check(false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason：{2}", Display, "Open", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Lid {0} was {1}，Reason {2}", Display, "Open", "PLC Kept"));
                        }
                        _operation = (LidState)SetPoint;
                    }
                }

                _trigError.CLK = Status == (int) LidState.Error;
                if (_trigError.Q)
                {
                    EV.PostMessage(Module, EventEnum.DefaultAlarm, "Lid in error status");
                }

                if ((SetPoint == Status) && (SetPoint == (int)LidState.Open || SetPoint == (int)LidState.Close))
                {
                    _doClose.Value = false;
                    _doOpen.Value = false;
                }


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            _trigReset.RST = true;
            _trigError.RST = true;
        }
    }
}
