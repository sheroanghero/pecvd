using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPressureMeter3 : BaseDevice, IDevice, IPressureMeter
    {
        public double Value
        {
            get
            {
                return FeedBack;
            }
        }
        public double FeedBack
        {
            get
            {
                double value = _isFloatAioType ? _aiValue.FloatValue : _aiValue.Value;
                if (_tuningPercent > 0 && _tuningPercent < 100)
                {
                    value = value * (1 - _tuningPercent/100.0);
                }

                return value;
            }
        }
        public double Precision
        {
            get
            {
                return _scPrecision == null ? 1000 : _scPrecision.DoubleValue;
            }
        }

        private AITPressureMeterData DeviceData
        {
            get
            {
                AITPressureMeterData data = new AITPressureMeterData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    FeedBack = Value,
                    Unit = Unit,
                    FormatString = _formatString,
                    DisplayWithUnit = true,
                    IsError = IsError,
                    IsWarning = IsWarning,
                    Precision = Precision,
                };

                return data;
            }
        }

        public bool IsWarning
        {
            get
            {
                return _checkWarning.Result;
            }
        }

        public bool IsError
        {
            get
            {
                return _checkAlarm.Result;
            }
        }

        public double MinPressure
        {
            get
            {
                return _scMinValue == null ? 0 : _scMinValue.DoubleValue;
            }
        }

        public double MaxPressure
        {
            get
            {
                return _scMaxValue == null ? 0 : _scMaxValue.DoubleValue;
            }
        }

        public int WarningTime
        {
            get
            {
                return _scWarningTime == null ? 0 : _scWarningTime.IntValue;
            }
        }

        public int AlarmTime
        {
            get
            {
                return _scAlarmTime == null ? 0 : _scAlarmTime.IntValue;
            }
        }

        public bool EnableAlarm
        {
            get
            {
                return _scEnableAlarm == null ? true : _scEnableAlarm.BoolValue;
            }
        }

        public bool IsOutOfRange
        {
            get
            {
                if (MinPressure < 0.01 && MaxPressure < 0.01)
                    return false;

                return (Value < MinPressure) || (Value > MaxPressure);
            }
        }

        public string Unit { get; set; }

        private AIAccessor _aiValue = null;

        private string _formatString = "F5";

        private SCConfigItem _scMinValue;
        private SCConfigItem _scMaxValue;
        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scWarningTime;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scPrecision;

        private ToleranceChecker _checkWarning = new ToleranceChecker();
        private ToleranceChecker _checkAlarm = new ToleranceChecker();


        public AlarmEventItem AlarmToleranceWarning { get; set; }
        public AlarmEventItem AlarmToleranceError { get; set; }

        private bool _isFloatAioType = false;

        private float _tuningPercent;

        public IoPressureMeter3(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiValue = ParseAiNode("aiValue", node, ioModule);

            if (node.HasAttribute("formatString"))
                _formatString = string.IsNullOrEmpty(node.GetAttribute("formatString")) ? "F5" : node.GetAttribute("formatString");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scMinValue = ParseScNode("", node, "", $"{scBasePath}.{Name}.MinValue");
            _scMaxValue = ParseScNode("", node, "", $"{scBasePath}.{Name}.MaxValue");
            _scEnableAlarm = ParseScNode("", node, "", $"{scBasePath}.{Name}.EnableAlarm");
            _scWarningTime = ParseScNode("", node, "", $"{scBasePath}.{Name}.WarningTime");
            _scAlarmTime = ParseScNode("", node, "", $"{scBasePath}.{Name}.AlarmTime");
            _scPrecision = ParseScNode("", node, "", $"{scBasePath}.{Name}.Precision");
            
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.Value", () => Value);
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            AlarmToleranceWarning = SubscribeAlarm($"{Module}.{Name}.OutOfToleranceWarning", "", ResetWarningChecker, EventLevel.Warning);
            AlarmToleranceError = SubscribeAlarm($"{Module}.{Name}.OutOfToleranceError", "", ResetErrorChecker);

            return true;
        }

        public void Terminate()
        {
        }

        public void SetTuning(float percent)
        {
            if (percent>0 && percent<=100)
                _tuningPercent = percent;
        }

        public void UnsetTuning()
        {
            _tuningPercent = 0;
        }

        public void Monitor()
        {
            if (EnableAlarm && (WarningTime > 0))
            {
                _checkWarning.Monitor(Value, MinPressure, MaxPressure, WarningTime);

                if (_checkWarning.Trig)
                {
                    AlarmToleranceWarning.Description =
                        $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {WarningTime} seconds";
                    AlarmToleranceWarning.Set();
                }

                if (!_checkWarning.Result)
                {
                    AlarmToleranceWarning.Reset();
                }
            }
            else
            {
                AlarmToleranceWarning.Reset();
            }

            if (EnableAlarm && (AlarmTime > 0))
            {
                _checkAlarm.Monitor(Value, MinPressure, MaxPressure, AlarmTime);

                if (_checkAlarm.Trig)
                {
                    AlarmToleranceError.Description =
                        $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {AlarmTime} seconds";
                    AlarmToleranceError.Set();
                }

                if (!_checkAlarm.Result)
                {
                    AlarmToleranceError.Reset();
                }
            }
            else
            {
                AlarmToleranceError.Reset();
            }
        }

        public bool ResetWarningChecker()
        {
            _checkWarning.RST = true;

            return true;
        }

        public bool ResetErrorChecker()
        {
            _checkAlarm.RST = true;

            return true;
        }

        public void Reset()
        {
            AlarmToleranceWarning.Reset();
            AlarmToleranceError.Reset();
        }
    }
}


