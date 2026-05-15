using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoCylinder : BaseDevice, IDevice
    {
        private DIAccessor _diOpened;
        private DIAccessor _diClosed;

        private DOAccessor _doOpen;
        private DOAccessor _doClose;

        private CylinderState _operation;
        private DeviceTimer _timer = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();

        public bool EnableOpen { get; set; }
        public bool EnableClose { get; set; }

        public int SetPoint   
        {
            get
            {
                if (_doOpen.Value && _doClose.Value) return (int) CylinderState.Error;
                if (_doOpen.Value && !_doClose.Value) return (int) CylinderState.Open;
                if (!_doOpen.Value && _doClose.Value) return (int) CylinderState.Close;
                if (!_doOpen.Value && !_doClose.Value) return (int) CylinderState.Unknown;

                return (int) CylinderState.Unknown;
            }
        }

        public CylinderState State
        {
            get
            {
                if (_diOpened != null && _diClosed != null)
                {
                    if (OpenFeedback && _diClosed.Value)
                        return CylinderState.Error;

                    if (OpenFeedback && !_diClosed.Value)
                        return CylinderState.Open;
                    if (!OpenFeedback && _diClosed.Value)
                        return CylinderState.Close;
                    if (!OpenFeedback && !_diClosed.Value)
                        return CylinderState.Unknown;
                }
            return CylinderState.Unknown;
            }
        }

        public bool OpenFeedback
        {
            get { return _diOpened != null && _diOpened.Value; }
        }
        public bool CloseFeedback
        {
            get { return _diClosed != null && _diClosed.Value; }
        }
        public bool OpenSetPoint
        {
            get { return _doOpen != null && _doOpen.Value; }
            set
            {
                if (_doOpen != null && _doOpen.Check(value, out _))
                    _doOpen.Value = value;
            }
        }
        public bool CloseSetPoint
        {
            get { return _doClose != null && _doClose.Value; }
            set
            {
                if (_doClose != null && _doClose.Check(value, out _))
                    _doClose.Value = value;
            }
        }

        private AITCylinderData DeviceData
        {
            get
            {
                AITCylinderData deviceData = new AITCylinderData
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    OpenFeedback = OpenFeedback,
                    CloseFeedback = CloseFeedback,
                    OpenSetPoint = OpenSetPoint,
                    CloseSetPoint = CloseSetPoint
                };

                return deviceData;
            }
        }

        //public int Status 
        //{
        //    get
        //    {
        //        if (OpenFeedback && _diClosed.Value)
        //            return (int)CylinderState.Error;
        //        if (OpenFeedback && !_diClosed.Value)
        //            return (int)CylinderState.Open;
        //        if (!OpenFeedback && _diClosed.Value)
        //            return (int)CylinderState.Close;
        //        if (!OpenFeedback && !_diClosed.Value)
        //            return (int)CylinderState.Unknown;

        //        return (int)CylinderState.Unknown;
        //    }
        //}

        public IoCylinder(string module, XmlElement node, string ioModule = "")
        {
            base.Module = node.GetAttribute("module");
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
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            OP.Subscribe($"{Module}.{Name}.{AITCylinderOperation.Open}", InvokeOpenCylinder);
            OP.Subscribe($"{Module}.{Name}.{AITCylinderOperation.Close}", InvokeCloseCylinder);


            DEVICE.Register($"{Name}.{AITCylinderOperation.Open}", (out string reason, int time, object[] param) =>
            {
                bool ret = SetCylinder(true, out reason);
                if (ret)
                {
                    reason = string.Format("Open Cylinder {0}", Name);
                    return true;
                }

                return false;
            });

            DEVICE.Register($"{Name}.{AITCylinderOperation.Close}", (out string reason, int time, object[] param) =>
            {
                bool ret = SetCylinder(false, out reason);
                if (ret)
                {
                    reason = string.Format("Close Cylinder {0}", Name);
                    return true;
                }

                return false;
            });

            return true;
        }

        private bool InvokeOpenCylinder(string arg1, object[] arg2)
        {
            string reason;

            if (!SetCylinder(true, out reason))
            {
                EV.PostWarningLog(Module, $"Can not open cylinder {Module}.{Name}, {reason}");
                return false;
            }

            EV.PostInfoLog(Module, $"Open cylinder {Module}.{Name}");

            return true;
        }

        private bool InvokeCloseCylinder(string arg1, object[] arg2)
        {
            string reason;

            if (!SetCylinder(false, out reason))
            {
                EV.PostWarningLog(Module, $"Can not close cylinder {Module}.{Name}, {reason}");
                return false;
            }

            EV.PostInfoLog(Module, $"Close cylinder {Module}.{Name}");

            return true;
        }

        public void Terminate()
        {
            CloseSetPoint = false;
            OpenSetPoint = false;
        }

        public bool SetCylinder(bool isOpen, out string reason)
        {
            if (_doOpen != null)
            {
                if (!_doOpen.Check(isOpen, out reason))
                {
                    return false;
                }
            }
            if (_doClose != null)
            {
                if (!_doClose.Check(!isOpen, out reason))
                {
                    return false;
                }
            }

            reason = "";

            OpenSetPoint = isOpen;
            CloseSetPoint = !isOpen;
            
            _operation = isOpen ? CylinderState.Open : CylinderState.Close;
            _timer.Start(1000 * 60 * 3);

            return true;
        }

        public void Monitor()
        {
            try
            {
                if (_diOpened != null && _diClosed != null)
                {
                    MonitorSetAndResetValue();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        private void MonitorSetAndResetValue()
        {
            if (_timer.IsTimeout())
            {
                _timer.Stop();

                if (State != _operation)
                {
                    if (_operation == CylinderState.Open)
                    {
                        string reason;
                        if (!_doOpen.Check(true, out reason))
                            EV.PostMessage(Module, EventEnum.DefaultAlarm, "Open Cylinder Failed for interlock, " + reason);
                        else
                            EV.PostMessage(Module, EventEnum.DefaultAlarm, "Cylinder hold close status");
                    }
                    else
                    {
                        string reason;
                        if (!_doOpen.Check(false, out reason))
                            EV.PostMessage(Module, EventEnum.DefaultAlarm, "Close Cylinder Failed for interlock, " + reason);
                        else
                            EV.PostMessage(Module, EventEnum.DefaultAlarm, "Cylinder hold open status");
                    }
                }

                _operation = (CylinderState) SetPoint;
            }
            else if (_timer.IsIdle())
            {
                _trigReset.CLK = SetPoint != (int) _operation; // fire event only check at first, SetPoint set by interlock
                if (_trigReset.Q)
                {
                    if (_operation == CylinderState.Open)
                    {
                        string reason;
                        if (!_doOpen.Check(true, out reason))
                            EV.PostMessage(Module, EventEnum.SwInterlock, Module,
                                string.Format("Cylinder {0} was {1}，Reason：{2}", Display, "Close", reason));
                        else
                            EV.PostMessage(Module, EventEnum.SwInterlock, Module,
                                string.Format("Cylinder {0} was {1}，Reason {2}", Display, "Close", "PLC kept"));
                    }
                    else
                    {
                        string reason;
                        if (!_doOpen.Check(false, out reason))
                            EV.PostMessage(Module, EventEnum.SwInterlock, Module,
                                string.Format("Cylinder {0} was {1}，Reason：{2}", Display, "Open", reason));
                        else
                            EV.PostMessage(Module, EventEnum.SwInterlock, Module,
                                string.Format("Cylinder {0} was {1}，Reason {2}", Display, "Open", "PLC Kept"));
                    }

                    _operation = (CylinderState) SetPoint;
                }
            }

            _trigError.CLK = State == CylinderState.Error;
            if (_trigError.Q)
            {
                EV.PostMessage(Module, EventEnum.DefaultAlarm, "Cylinder in error status");
            }

            if ((SetPoint == (int) State) && (SetPoint == (int) CylinderState.Open || SetPoint == (int) CylinderState.Close))
            {
                CloseSetPoint = false;
                OpenSetPoint = false;
            }
        }

        public void Reset()
        {
            _trigReset.RST = true;
            _trigError.RST = true;
        }
    }
}
