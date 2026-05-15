using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoThrottleValve : BaseDevice, IDevice
    {
        public struct Context
        {
            public string tvName;

            public string aoPressureModeName;

            public string aoPressureSetPointName;
            public string aoPositionSetPointName;
            public string aiPressureFeedbackName;
            public string aiPositionFeedbackName;

            public string aiStateName;
        };

        public PressureCtrlMode PressureMode
        {
            get
            {
                if (_aoPressureMode == null)
                    return PressureCtrlMode.TVPositionCtrl;

                return Math.Abs(_aoPressureMode.Value - 2) < 0.1 ? PressureCtrlMode.TVPositionCtrl : PressureCtrlMode.TVPressureCtrl;
            }
            set
            {
                if (_aoPositionSetPoint == null || _aoPressureSetPoint == null || _aoPressureMode == null)
                    return;

                float setpoint = value == PressureCtrlMode.TVPositionCtrl ? 2 : 1;
                if (Math.Abs(_aoPressureMode.Value - setpoint) > 0.01)
                {
                    if (value == PressureCtrlMode.TVPositionCtrl)
                    {
                        _aoPositionSetPoint.Value = (short)PositionFeedback;
                    }
                    else
                    {
                        _aoPressureSetPoint.Value = (short)PressureFeedback;
                    }
                    _aoPressureMode.Value = (short)setpoint;
                }
            }
        }


        public int State
        {
            get
            {
                return _aiState == null ? 1 : (int)_aiState.Value;
            }
        }

        [Subscription(AITThrottleValvePropertyName.TVPositionSetPoint)]
        public float PositionSetpoint
        {
            get
            {
                return _aoPositionSetPoint == null ? 0 : _aoPositionSetPoint.Value;
            }
            set
            {
                if (_aoPositionSetPoint == null)
                    return;

                _aoPositionSetPoint.Value = (short)value;
            }
        }

        [Subscription(AITThrottleValvePropertyName.TVPosition)]
        public float PositionFeedback
        {
            get
            {
                return _aiPositionFeedback == null ? 0 : _aiPositionFeedback.Value;
            }
        }

        [Subscription(AITThrottleValvePropertyName.TVPressureSetPoint)]
        public float PressureSetpoint
        {
            get
            {
                return _aoPressureSetPoint == null ? 0 : _aoPressureSetPoint.Value;
            }
            set
            {
                if (_aoPressureSetPoint == null)
                    return;

                _aoPressureSetPoint.Value = (short)value;
            }
        }


        private AITThrottleValveData DeviceData
        {
            get
            {
                AITThrottleValveData data = new AITThrottleValveData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Mode = (int)PressureMode,
                    PositionFeedback = PositionFeedback,
                    PositionSetPoint = PositionSetpoint,
                    PressureFeedback = PressureFeedback,
                    PressureSetPoint = PressureSetpoint,
                    State = State,

                };


                return data;
            }
        }

        [Subscription(AITThrottleValvePropertyName.TVPressure)]
        public float PressureFeedback
        {
            get
            {
                return _aiPressureFeedback == null ? 0 : _aiPressureFeedback.Value;
            }
        }

        public bool IsPumpMode
        {
            get; set;
        }

        public bool IsIndependent
        {
            get; set;
        }

        public bool IsOffline
        {
            get
            {
                return _diOffline != null && _diOffline.RawData;
            }
        }

        private DIAccessor _diOffline;

        private AIAccessor _aiPressureFeedback = null;
        private AIAccessor _aiPositionFeedback = null;

        private AOAccessor _aoPressureSetPoint = null;
        private AOAccessor _aoPositionSetPoint = null;

        private AOAccessor _aoPressureMode = null;
        private AIAccessor _aiState = null;

        private R_TRIG _tvStatusAlmTrig = new R_TRIG();
        private R_TRIG _trigOffline = new R_TRIG();

        public IoThrottleValve(string module, XmlElement node, string ioModule = "")
        {
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiPositionFeedback = ParseAiNode("aiPositionFeedback", node);
            _aiPressureFeedback = ParseAiNode("aiPressureFeedback", node);
            _aoPositionSetPoint = ParseAoNode("aoPositionSetPoint", node);
            _aoPressureSetPoint = ParseAoNode("aoPressureSetPoint", node);

            _aiState = ParseAiNode("aiState", node);

            _aoPressureMode = ParseAoNode("aoPressureMode", node);

            _diOffline = ParseDiNode("diOffline", node);
        }


        public bool Initialize()
        {
            DATA.Subscribe(string.Format("Device.{0}.{1}", Module, Name), () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            PressureMode = PressureCtrlMode.TVPressureCtrl;

            DEVICE.Register(String.Format("{0}.{1}", Name, AITThrottleValveOperation.SetMode), (out string reason, int time, object[] param) =>
            {

                PressureMode = (PressureCtrlMode)Enum.Parse(typeof(PressureCtrlMode), (string)param[0], true);
                reason = string.Format("Throttle valve set to {0} mode", PressureMode);

                return true;

            });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITThrottleValveOperation.SetPosition), (out string reason, int time, object[] param) =>
            {
                double target = Convert.ToDouble((string)param[0]);

                if (PressureMode != PressureCtrlMode.TVPositionCtrl)
                {
                    reason = "Throttle valve in pressure mode, can not set position";
                    return false;
                }

                PositionSetpoint = (float)target;
                reason = string.Format("position set to {0}%", target.ToString("F1"));
                return true;
            });


            DEVICE.Register(String.Format("{0}.{1}", Name, AITThrottleValveOperation.SetPressure), (out string reason, int time, object[] param) =>
            {
                if (PressureMode == PressureCtrlMode.TVPositionCtrl)
                {
                    reason = "Throttle valve is in positon conrol mode, can not set pressure";
                    return false;
                }

                double target = Convert.ToDouble((string)param[0]);

                PressureSetpoint = (float)target;
                reason = string.Format("pressure set {0} mTorr", target);
                return true;
            });

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                _tvStatusAlmTrig.CLK = State != 1;
                if (_tvStatusAlmTrig.Q)
                {
                    EV.PostMessage(Module, EventEnum.ThrottleValveAbnormal, Module);
                }

                _trigOffline.CLK = IsOffline;
                if (_trigOffline.Q)
                {
                    EV.PostMessage(Module, EventEnum.DefaultAlarm, "Throttle Valve Offline");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

 
        public void Reset()
        {
            _tvStatusAlmTrig.RST = true;
            _trigOffline.RST = true;
        }

        public void SetPositionMode(int position)
        {
            PressureMode = PressureCtrlMode.TVPositionCtrl;
            PositionSetpoint = (float)position;
        }

    }
}
