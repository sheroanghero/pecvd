using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoThrottleValve2 : BaseDevice, IDevice
    {
        public PressureCtrlMode PressureMode
        {
            get
            {
                if (_aoPressureMode != null)
                {
                    foreach (var ioMode in _ctrlModeIoValue)
                    {
                        if (_isFloatAioType)
                        {
                            if ((int)_aoPressureMode.FloatValue == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }
                        else
                        {
                            if (_aoPressureMode.Value == ioMode.Value)
                            {
                                return ioMode.Key;
                            }
                        }

                    }
                    return PressureCtrlMode.Undefined;
                }

                return PressureCtrlMode.Undefined;
            }
            set
            {
                if (_isFloatAioType)
                    _aoPressureMode.FloatValue = _ctrlModeIoValue[value];
                else
                {
                    _aoPressureMode.Value = (short)_ctrlModeIoValue[value];
                }
            }
        }

        public float PositionSetpoint
        {
            get
            {
                return _aoPositionSetPoint == null ? 0 : (_isFloatAioType? _aoPositionSetPoint.FloatValue: _aoPositionSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPositionSetPoint.FloatValue = value;
                }
                else
                {
                    _aoPositionSetPoint.Value = (short)value;
                }
            }
        }

        public float PositionFeedback
        {
            get
            {
                return _aiPositionFeedback == null ? 0 : (_isFloatAioType ? _aiPositionFeedback.FloatValue: _aiPositionFeedback.Value);
            }
        }

        public float PressureSetpoint
        {
            get
            {
                return _aoPressureSetPoint == null ? 0 : (_isFloatAioType ? _aoPressureSetPoint.FloatValue:_aoPressureSetPoint.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPressureSetPoint.FloatValue = value;
                }
                else
                {
                    _aoPressureSetPoint.Value = (short)value;
                }
            }
        }

        public float PressureFeedback
        {
            get
            {
                return _aiPressureFeedback == null ? 0 : (_isFloatAioType ? _aiPressureFeedback.FloatValue:_aiPressureFeedback.Value);
            }
        }

        private AITThrottleValveData DeviceData
        {
            get
            {
                AITThrottleValveData data = new AITThrottleValveData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Mode = (int)PressureMode,
                    PositionFeedback = PositionFeedback,
                    PositionSetPoint = PositionSetpoint,

                    PressureFeedback = PressureFeedback,
                    PressureSetPoint = PressureSetpoint,

                    MaxValuePosition = 100,
                    MaxValuePressure = _unitPressure == "Torr" ? 760 : 760000,

                    UnitPressure = _unitPressure,
                };

                return data;
            }
        }

        private AOAccessor _aoPressureMode = null;

        private AIAccessor _aiPositionFeedback = null;

        private AOAccessor _aoPositionSetPoint = null;

        private AIAccessor _aiPressureFeedback = null;

        private AOAccessor _aoPressureSetPoint = null;

        private string _unitPressure;

        private Dictionary<PressureCtrlMode, int> _ctrlModeIoValue = new Dictionary<PressureCtrlMode, int>();
        
        private bool _isFloatAioType = false;

        public IoThrottleValve2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
            _unitPressure = node.GetAttribute("unit");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aoPressureMode = ParseAoNode("aoPressureMode", node, ioModule);

            _aiPositionFeedback = ParseAiNode("aiPositionFeedback", node, ioModule);
            _aoPositionSetPoint = ParseAoNode("aoPositionSetPoint", node, ioModule);

            _aiPressureFeedback = ParseAiNode("aiPressureFeedback", node, ioModule);
            _aoPressureSetPoint = ParseAoNode("aoPressureSetPoint", node, ioModule);

            EnumLoop<PressureCtrlMode>.ForEach(x => _ctrlModeIoValue[x] = -1);

            if (!string.IsNullOrEmpty(node.GetAttribute("modeValuePressure")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVPressureCtrl] = int.Parse(node.GetAttribute("modeValuePressure"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValuePosition")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVPositionCtrl] = int.Parse(node.GetAttribute("modeValuePosition"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValueOpen")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVOpen] = int.Parse(node.GetAttribute("modeValueOpen"));
            }
            if (!string.IsNullOrEmpty(node.GetAttribute("modeValueClose")))
            {
                _ctrlModeIoValue[PressureCtrlMode.TVClose] = int.Parse(node.GetAttribute("modeValueClose"));
            }
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.PositionSetPoint", () => PositionSetpoint);
            DATA.Subscribe($"{Module}.{Name}.PositionFeedback", () => PositionFeedback);
            DATA.Subscribe($"{Module}.{Name}.PressureSetPoint", () => PressureSetpoint);
            DATA.Subscribe($"{Module}.{Name}.PressureFeedback", () => PressureFeedback);
            DATA.Subscribe($"{Module}.{Name}.Mode", () => PressureMode);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetMode}", SetMode);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetPosition}", SetPosition);

            OP.Subscribe($"{Module}.{Name}.{AITThrottleValveOperation.SetPressure}", SetPressure);

            return true;
        }

        public bool SetMode(PressureCtrlMode mode, out string reason)
        {
            PressureMode = mode;

            reason = $"{Display} set to {mode}";

            EV.PostInfoLog(Module, reason);

            return true;
        }

        private bool SetMode(out string reason, int time, object[] param)
        {
            return SetMode((PressureCtrlMode)Enum.Parse(typeof(PressureCtrlMode), (string)param[0], true),
                out reason);
        }

        public bool SetPosition(float position, out string reason)
        {

            PositionSetpoint = position;

            reason = $"{Display} position set to {position}";

            EV.PostInfoLog(Module, reason);

            return true;
        }
        private bool SetPosition(out string reason, int time, object[] param)
        {
            return SetPosition(Convert.ToSingle(param[0].ToString()), out reason);
        }

        private bool SetPressure(out string reason, int time, object[] param)
        {
            return SetPressure(Convert.ToSingle(param[0].ToString()), out reason);
        }

        public bool SetPressure(float pressure, out string reason)
        {
            PressureSetpoint = pressure;

            reason = $"{Display}  pressure set to {pressure} {_unitPressure}";

            EV.PostInfoLog(Module, reason);

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }


        public void Reset()
        {

        }
    }
}
