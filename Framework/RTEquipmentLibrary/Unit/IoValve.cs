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
    public interface IValve
    {
        bool TurnValve(bool isOn, out string reason);
    }

    public class IoValve : BaseDevice, IDevice, IValve
    {
        public string GVName { get { return Name; } }

        public string GVDeviceID { get { return DeviceID; } }

        public bool GVIsDefaultOpen { get { return _isDefaultOpen; } }


        [Subscription(AITValveDataPropertyName.SetPoint)]
        public bool SetPoint    //True：open| False：close
        {
            get
            {
                if (_doOpen == null)
                    return false;

                if (_doOpen != null && _doClose != null)
                {
                    return Status ? _doOpen.Value && !_doClose.Value : !_doOpen.Value && _doClose.Value;
                }

                return _isNc ? _doOpen.Value : !_doOpen.Value;
            }
            set
            {
                if (_doOpen != null)
                {
                    _doOpen.Value = _isNc ? value : !value;
                }
                if (_doClose != null)
                {
                    _doClose.Value = _isNc ? !value : value;
                }
            }
        }

        [Subscription(AITValveDataPropertyName.Status)]
        public bool Status    //True：open | False：close
        {
            get
            {
                if (_diOpenSensor != null && _diCloseSensor != null)
                    return _diOpenSensor.Value && !_diCloseSensor.Value;

                if (_diCloseSensor != null)
                    return !_diCloseSensor.Value;

                if (_diOpen != null)
                    return _isNc ? _diOpen.Value : !_diOpen.Value;

                if (_doOpen != null)
                    return _isNc ? _doOpen.Value : !_doOpen.Value;

                if (_doClose != null)
                    return _isNc ? !_doClose.Value : _doClose.Value;
                return false;
            }
        }

        private AITValveData DeviceData
        {
            get
            {
                AITValveData data = new AITValveData()
                {
                    UniqueName = _uniqueName,
                    DeviceName = GVName,
                    DefaultValue = GVIsDefaultOpen,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Feedback = Status,
                    SetPoint = SetPoint,
                };

                return data;
            }
        }

        /// <summary>
        /// normal closed, 0 关闭，1打开
        /// </summary>
        public bool _isNc;

        /// <summary>
        /// default open
        /// </summary>
        public bool _isDefaultOpen;

        private DIAccessor _diOpenSensor;
        private DIAccessor _diCloseSensor;

        private DIAccessor _diOpen;
        private DOAccessor _doOpen;
        private DOAccessor _doClose;

        private bool _operation;


        R_TRIG eventTrigger = new R_TRIG();
        R_TRIG _mutexSignalTrigger = new R_TRIG();
        DeviceTimer _timer = new DeviceTimer();
        DeviceTimer _mutexSignalTimer = new DeviceTimer();

        private string _uniqueName;

        public IoValve(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _isNc = Convert.ToBoolean(node.GetAttribute("isNc"));
            _isDefaultOpen = Convert.ToBoolean(node.GetAttribute("isDefaultOpen"));

            _diOpenSensor = ParseDiNode("diOpenSensor", node, ioModule);
            _diCloseSensor = ParseDiNode("diCloseSensor", node, ioModule);
            _doOpen = ParseDoNode("doOpen", node, ioModule);
            _diOpen = ParseDiNode("diOpen", node, ioModule);
            _doClose = ParseDoNode("doClose", node, ioModule);

            _uniqueName = $"{Module}.{Name}";
        }

        public bool Initialize()
        {
            DATA.Subscribe($"Device.{Module}.{GVName}", () => DeviceData);
            DATA.Subscribe($"{_uniqueName}.DeviceData", () => DeviceData);

            if (_diOpenSensor != null && _diCloseSensor != null)
            {
                DATA.Subscribe($"{_uniqueName}.DiOpen", () => _diOpenSensor.Value);
                DATA.Subscribe($"{_uniqueName}.DiClose", () => _diCloseSensor.Value);
            }
            if (_doOpen != null && _doClose != null)
            {
                DATA.Subscribe($"{_uniqueName}.DoOpen", () => _doOpen.Value);
                DATA.Subscribe($"{_uniqueName}.DoClose", () => _doClose.Value);
            }

            OP.Subscribe($"{_uniqueName}.{AITValveOperation.GVTurnValve}", InvokeOpenCloseValve);


            DEVICE.Register(String.Format("{0}.{1}", Name, AITValveOperation.GVTurnValve), (out string reason, int time, object[] param) =>
            {
                bool bOn = Convert.ToBoolean((string)param[0]);
                bool ret = TurnValve(bOn, out reason);
                if (ret)
                {
                    reason = string.Format("Valve {0}{1}", Name, bOn ? "Open" : "Close");
                    return true;
                }

                return false;
            });

            //for recipe
            DEVICE.Register(String.Format("{0}", Name), (out string reason, int time, object[] param) =>
            {
                bool bOn = Convert.ToBoolean((string)param[0]);
                bool ret = TurnValve(bOn, out reason);
                if (ret)
                {
                    reason = string.Format("Valve {0}{1}", Name, bOn ? "Open" : "Close");
                    return true;
                }

                return false;
            });

            return true;
        }

        public void Terminate()
        {
            string reason;
            TurnValve(_isDefaultOpen, out reason);
        }



        private bool InvokeOpenCloseValve(string method, object[] args)
        {
            string reason;
            bool op = Convert.ToBoolean(args[0]);
            string name = op ? "Open" : "Close";

            if (!TurnValve(op, out reason))
            {
                EV.PostWarningLog(Module, $"Can not {name} valve {Module}.{Name}, {reason}");
                return false;
            }

            EV.PostInfoLog(Module, $"{name} valve {Module}.{Name}");

            return true;
        }

        public void Monitor()
        {
            try
            {
                if (_diOpenSensor != null && _diCloseSensor != null)
                {
                    if (_diOpenSensor.Value != _diCloseSensor.Value)
                    {
                        _mutexSignalTimer.Start(4000);
                    }

                    _mutexSignalTrigger.CLK = _mutexSignalTimer.IsTimeout();

                    if (_mutexSignalTrigger.Q)
                    {
                        EV.PostWarningLog(Module, $"Valve {Name} was abnormal，Reason：diOpenSensor's value is {_diOpenSensor.Value} and diCloseSensor's value is {_diCloseSensor.Value} too.");
                    }
                }

                if (_timer.IsTimeout())
                {
                    _timer.Stop();

                    if (Status != _operation)
                    {
                        if (_operation)
                        {
                            string reason;
                            if (!_doOpen.Check(_isNc ? true : false, out reason))
                                EV.PostMessage(Module, EventEnum.ValveOperationFail, Module, Display, "Open", "：Failed for interlock " + reason);
                            else
                                EV.PostMessage(Module, EventEnum.ValveOperationFail, Module, Display, "Open", "：Valve keep closed ");

                        }
                        else
                        {
                            string reason;
                            if (!_doOpen.Check(_isNc ? true : false, out reason))
                                EV.PostMessage(Module, EventEnum.ValveOperationFail, Module, Display, "Close", "：Failed for interlock " + reason);
                            else
                                EV.PostMessage(Module, EventEnum.ValveOperationFail, Module, Display, "Close", "：Valve keep open");

                        }
                    }
                    _operation = SetPoint;
                }
                else if (_timer.IsIdle())
                {
                    eventTrigger.CLK = SetPoint != _operation;   // fire event only check at first, SetPoint set by interlock
                    if (eventTrigger.Q)
                    {
                        if (_operation)
                        {
                            string reason;
                            if (!_doOpen.Check(_isNc ? true : false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Valve {0} was {1}，Reason：{2}", Display, "Close", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Valve {0} was {1}，Reason {2}", Display, "Close", "PLC kept"));
                        }
                        else
                        {
                            string reason;
                            if (!_doOpen.Check(_isNc ? true : false, out reason))
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Valve {0} was {1}，Reason：{2}", Display, "Open", reason));
                            else
                                EV.PostMessage(Module, EventEnum.SwInterlock, Module, string.Format("Valve {0} was {1}，Reason {2}", Display, "Open", "PLC Kept"));
                        }
                        _operation = SetPoint;
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }

        }


        public bool TurnValve(bool isOn, out string reason)
        {
            bool bValue = _isNc ? isOn : !isOn;

            if (_doOpen != null)
            {
                if (!_doOpen.Check(bValue, out reason))
                {
                    return false;
                }
            }
            if (_doClose != null)
            {
                if (!_doClose.Check(!bValue, out reason))
                {
                    return false;
                }
            }

            var stringOnOff = isOn ? "On" : "Off";
            EV.PostInfoLog(Module, $"Turn {Display} {stringOnOff}");

            reason = "";
            SetPoint = isOn;

            _operation = isOn;
            _timer.Start(10 * 1000);         //5 seconds to monitor

            return true;
        }

        public void Reset()
        {
            eventTrigger.RST = true;
        }
    }


}
