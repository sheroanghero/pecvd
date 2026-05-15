using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoServoMotor : BaseDevice, IDevice
    {
        [Subscription(AITServoMotorProperty.IsServoOn)]
        public bool IsServoOn
        {
            get
            {
                return _doServoOn != null && _doServoOn.Value;
            }
        }

        [Subscription(AITServoMotorProperty.IsStopped)]
        public bool IsStopped
        {
            get
            {
                return _diPulseOutputComplete != null && _diPulseOutputComplete.Value;
            }
        }

        [Subscription(AITServoMotorProperty.IsError)]
        public bool IsError
        {
            get
            {
                return _diAlarm != null && _diAlarm.Value;
            }
        }

        [Subscription(AITServoMotorProperty.CurrentPosition)]
        public float CurrentPosition
        {
            get
            {
                return _aiPulseFeedback.Value;
            }
        }

        [Subscription(AITServoMotorProperty.CurrentStatus)]
        public string CurrentStatus
        {
            get
            {
                return _state.ToString();
            }
        }
        

        private DIAccessor _diHome;
        private DIAccessor _diCWLimit;
        private DIAccessor _diCCWLimit;
        private DIAccessor _diLocationComplete;
        private DIAccessor _diAlarm;
        private DIAccessor _diPulseOutputOn;
        private DIAccessor _diPulseOutputComplete;
        private DIAccessor _diNotInitial;

        private DOAccessor _doBreakRelay;
        private DOAccessor _doServoOn;
        private DOAccessor _doMoveUp;
        private DOAccessor _doMoveDown;
        private DOAccessor _doMoveToPosition;
        private DOAccessor _doDeviationCounterReset;
        private DOAccessor _doAlarmReset;
        private DOAccessor _doStopMoveUp;
        private DOAccessor _doStopMoveDown;
        private DOAccessor _doHome;

        

        private AOAccessor _aoServoSpeedSetPoint;
        private AOAccessor _aoManualSpeedSetPoint;
        private AOAccessor _aoAcceleration;
        private AOAccessor _aoDeceleration;
        private AOAccessor _aoStartFrequency;
        private AOAccessor _aoPositionSetPoint;

        private AIAccessor _aiPulseFeedback;

        private DeviceTimer _timer = new DeviceTimer();
        
        private R_TRIG _trigError = new R_TRIG();

        private ServoState _state = ServoState.NotInitial;

        private F_TRIG _trigAlarmRecovered = new F_TRIG();

        public IoServoMotor(string module, XmlElement node, string ioModule = "")
        {

            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diHome = ParseDiNode("diHome", node, ioModule);
            _diCWLimit = ParseDiNode("diCWLimit", node, ioModule);
            _diCCWLimit = ParseDiNode("diCCWLimit", node, ioModule);
            _diLocationComplete = ParseDiNode("diLocationComplete", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _diPulseOutputOn = ParseDiNode("diPulseOutputOn", node, ioModule);
            _diPulseOutputComplete = ParseDiNode("diPulseOutputComplete", node, ioModule);
            _diNotInitial = ParseDiNode("diNotInitial", node, ioModule);

            _doBreakRelay = ParseDoNode("doBreakRelay", node, ioModule);
            _doServoOn = ParseDoNode("doServoOn", node, ioModule);
            _doMoveUp = ParseDoNode("doMoveUp", node, ioModule);
            _doMoveDown = ParseDoNode("doMoveDown", node, ioModule);
            _doMoveToPosition = ParseDoNode("doMoveToPosition", node, ioModule);
            _doDeviationCounterReset = ParseDoNode("doDeviationCounterReset", node, ioModule);
            _doAlarmReset = ParseDoNode("doAlarmReset", node, ioModule);
            _doStopMoveUp = ParseDoNode("doStopMoveUp", node, ioModule);
            _doStopMoveDown = ParseDoNode("doStopMoveDown", node, ioModule);
            _doHome = ParseDoNode("doHome", node, ioModule);

            _aoServoSpeedSetPoint = ParseAoNode("aoServoSpeedSetPoint", node, ioModule);
            _aoManualSpeedSetPoint = ParseAoNode("aoManualSpeedSetPoint", node, ioModule);
            _aoAcceleration = ParseAoNode("aoAcceleration", node, ioModule);
            _aoDeceleration = ParseAoNode("aoDeceleration", node, ioModule);
            _aoStartFrequency = ParseAoNode("aoStartFrequency", node, ioModule);
            _aoPositionSetPoint = ParseAoNode("aoPositionSetPoint", node, ioModule);

            _aiPulseFeedback = ParseAiNode("aiPulseFeedback", node, ioModule);

        }

        public bool Initialize()
        {
            DATA.Subscribe(string.Format("Device.{0}.{1}", Module, Name), () =>
                                                      {
                                                          AITServoMotorData data = new AITServoMotorData()
                                                                                 {
                                                                                     DeviceName = Name,
                                                                                     DeviceSchematicId = DeviceID,
                                                                                     DisplayName = Display,

                                                                                     IsServoOn = IsServoOn,
                                                                                     IsError = IsError,
                                                                                     IsStopped = IsStopped,

                                                                                     CurrentPosition = CurrentPosition,
                                                                                     CurrentStatus = CurrentStatus,

                                                                                     State = _state,
                                                                                 };
                                                          return data;
                                                      }, SubscriptionAttribute.FLAG.IgnoreSaveDB);


            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.Home), 
                (out string reason, int time, object[] param) =>
                {
                    reason = string.Format("{0} Home", Display);

                    if (!_doHome.SetValue(true, out reason))
                        return false;

                    return true;
            });

            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.SetServoOn),
                (out string reason, int time, object[] param) =>
                {
                    reason = string.Format("{0} Servo on", Display);

                    if (!_doServoOn.SetValue(true, out reason))
                        return false;

                    return true;
                });
            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.SetServoOff),
                (out string reason, int time, object[] param) =>
                {
                    reason = string.Format("{0} Servo off", Display);
                    if (!_doServoOn.SetValue(false, out reason))
                        return false;

                    return true;
                });

            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.MoveTo),
                (out string reason, int time, object[] param) =>
                {
                    
                    double speed = (double) param[0];
                    double position = (double) param[1];

                    reason = string.Format("{0} Move to {1} at speed {2}", Display, position, speed);

                    _aoPositionSetPoint.Value = (short)position;
                    _aoServoSpeedSetPoint.Value = (short)speed;

                    return true;
                });
            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.MoveBy),
                (out string reason, int time, object[] param) =>
                {
                    double speed = (double)param[0];
                    double by = (double)param[1];
                    double position = _aiPulseFeedback.Value;

                    reason = string.Format("{0} Move by {1} at speed {2}, from{3}", Display, @by, speed, position);
                    
                    _aoPositionSetPoint.Value = (short)position;
                    _aoServoSpeedSetPoint.Value = (short)speed;

                    _doMoveToPosition.Value = true;
                    return true;
                });
            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.Reset),
                (out string reason, int time, object[] param) =>
                {
                    reason = string.Format("{0} Reset", Display);
                    if (!_doAlarmReset.SetValue(true, out reason))
                        return false;

                    return true;
                });
            DEVICE.Register(string.Format("{0}.{1}", Name, AITServoMotorOperation.Stop),
                (out string reason, int time, object[] param) =>
                {
                    reason = string.Format("{0} Stop", Display);

                    if (!_doStopMoveDown.SetValue(false, out reason) && !_doStopMoveUp.SetValue(true, out reason))
                        return false;

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
                if (!_diAlarm.Value)
                {
                    _doAlarmReset.Value = false;
                }

                if (_diNotInitial.Value)
                {
                    _state = ServoState.NotInitial;
                }else if (_diAlarm.Value)
                {
                    _state = ServoState.Error;
                }else if (_diPulseOutputOn.Value)
                {
                    _state = ServoState.Moving;
                }
                else
                {
                    _state = ServoState.Idle;
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            _doAlarmReset.Value = true;
        }
    }
}
