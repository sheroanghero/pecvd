using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoLoopPump : BaseDevice, IDevice
    {


        public int SetPoint   
        {
            get
            {
                //if (_doOpen.Value && _doClose.Value) return (int) CylinderState.Error;
                if (_doOpen.Value  ) return (int) CylinderState.Open;
                if (!_doOpen.Value  ) return (int) CylinderState.Close;
                //if (!_doOpen.Value && !_doClose.Value) return (int) CylinderState.Unknown;

                return (int) CylinderState.Unknown;
            }
        }

        public CylinderState State
        {
            get
            {
                if (_diOpened.Value && _diClosed.Value)
                    return CylinderState.Error;
                if (_diOpened.Value && !_diClosed.Value)
                    return CylinderState.Open;
                if (!_diOpened.Value && _diClosed.Value)
                    return CylinderState.Close;
                if (!_diOpened.Value && !_diClosed.Value)
                    return CylinderState.Unknown;

                return CylinderState.Unknown;
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

                    OpenFeedback = _diOpened.Value,
                    CloseFeedback = _diClosed.Value,
                    OpenSetPoint = _doOpen.Value,
                    IsLoop = _cmdLoop,
                };

                return deviceData;
            }
        }

        //public int Status 
        //{
        //    get
        //    {
        //        if (_diOpened.Value && _diClosed.Value)
        //            return (int)CylinderState.Error;
        //        if (_diOpened.Value && !_diClosed.Value)
        //            return (int)CylinderState.Open;
        //        if (!_diOpened.Value && _diClosed.Value)
        //            return (int)CylinderState.Close;
        //        if (!_diOpened.Value && !_diClosed.Value)
        //            return (int)CylinderState.Unknown;

        //        return (int)CylinderState.Unknown;
        //    }
        //}

        //private DIAccessor _diLeak;
        private DIAccessor _diOpened;
        private DIAccessor _diClosed;

        private DOAccessor _doOpen;

        private bool _cmdLoop;
        private DeviceTimer _loopTimer = new DeviceTimer();
        private DeviceTimer _loopTimeout = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();
        private R_TRIG _trigOpenError = new R_TRIG();
        private R_TRIG _trigCloseError = new R_TRIG();

        private SCConfigItem _scLoopInterval;
        private SCConfigItem _scLoopTimeout;
        private R_TRIG _trigSetPointDone = new R_TRIG();

        private int _interval;

        public IoLoopPump(string module, XmlElement node, string ioModule = "")
        {
            base.Module = node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diOpened = ParseDiNode("diOpen", node, ioModule);
            _diClosed = ParseDiNode("diClose", node, ioModule);
            _doOpen = ParseDoNode("doOpen", node, ioModule);

            _scLoopInterval = SC.GetConfigItem($"Modules.{Module}.{Name}.PumpLoopInterval");
            _scLoopTimeout = SC.GetConfigItem($"Modules.{Module}.{Name}.PumpLoopTimeout");
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
                    reason = string.Format("Close {0}", Name);
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
                EV.PostWarningLog(Module, $"Can not open {Module}.{Name}, {reason}");
                return false;
            }

            EV.PostInfoLog(Module, $"Open {Module}.{Name}");

            return true;
        }

        private bool InvokeCloseCylinder(string arg1, object[] arg2)
        {
            string reason;

            if (!SetCylinder(false, out reason))
            {
                EV.PostWarningLog(Module, $"Can not close {Module}.{Name}, {reason}");
                return false;
            }

            EV.PostInfoLog(Module, $"Close {Module}.{Name}");

            return true;
        }

        public void Terminate()
        {
            _doOpen.Value = false;
         }

        public bool SetCylinder(bool isOpen, out string reason)
        {
            _cmdLoop = isOpen;

            _doOpen.Value = isOpen;

            _loopTimer.Start(_scLoopInterval.IntValue);
            _loopTimeout.Start(_scLoopTimeout.IntValue * 1000);

            _interval = _scLoopInterval.IntValue;

            if (_interval < 300)
                _interval = 300;

            reason = "";

            return true;
        }

        public void Monitor()
        {
            if (_cmdLoop)
            {
				if (_loopTimer.IsTimeout() && ((_doOpen.Value && State == CylinderState.Open)
						|| (!_doOpen.Value && State == CylinderState.Close)))
				{
					_loopTimer.Start(_interval);

					_doOpen.Value = !_doOpen.Value;

					_loopTimeout.Start(_scLoopTimeout.IntValue * 1000);
				}

                _trigOpenError.CLK = _doOpen.Value && State != CylinderState.Open && _loopTimeout.IsTimeout();
                if (_trigOpenError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name}, can not open in {_scLoopTimeout.IntValue} seconds");
                }

                _trigCloseError.CLK = !_doOpen.Value && State != CylinderState.Close && _loopTimeout.IsTimeout();
                if (_trigCloseError.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name}, can not close in {_scLoopTimeout.IntValue} seconds");
                }
            }
            else
            {
                _doOpen.Value = false;
            }

        }

        public void Reset()
        {
            _trigReset.RST = true;
            _trigOpenError.RST = true;
            _trigCloseError.RST = true;
        }
    }
}
