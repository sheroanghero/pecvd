using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.RT.Event;
using MECF.Framework.Common.Event;
using System;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPressureMeterTM : BaseDevice, IDevice, IPressureMeter
    {
        public double Value
        {
            get
            {
                return FeedBack;
            }
        }

        private float _originPressure;
        public double UnitValue
        {
            get
            {
                if (_formatString == "F1")
                {
                    if (_originPressure >= 1000)
                        Unit = "Torr";
                    else
                        Unit = "mTorr";
                }

                if (Unit == "Torr")
                {
                    return _originPressure / 1000; // 单位是Torr
                }

                return _originPressure; // 单位mTorr
            }
        }

        public double FeedBack
        {
            get
            {
                if (_aiValueL == null || _aiValueH == null)
                    return 0;

                short valueL = _aiValueL.Value;
                short valueH = _aiValueH.Value;
                //UInt32 tmp = (ushort)valueL + (UInt32)(((ushort)valueH) << 16);
                //double value = tmp > Int32.MaxValue ? (double)((~tmp + 1) * -1) : (double)tmp;

                byte[] byteL = BitConverter.GetBytes(valueL);
                byte[] byteH = BitConverter.GetBytes(valueH);

                float value = BitConverter.ToSingle(new byte[] { byteL[0], byteL[1], byteH[0], byteH[1] }, 0);

                if (Name == "Pressure2") // 真空压力计
                {
                    value = value * _factor; // 比例系数
                }

                _originPressure = value;

                if (_tuningPercent > 0 && _tuningPercent < 100)
                {
                    value = value * (1 - _tuningPercent / 100.0f);
                }

                return (double)value / 1000; // 单位Torr
            }
        }
        public double Precision
        {
            get
            {
                return _scPrecision == null ? 1000 : _scPrecision.DoubleValue;
            }
        }

        public AITPressureMeterData DeviceData
        {
            get
            {
                AITPressureMeterData data = new AITPressureMeterData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    FeedBack = UnitValue,
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
                return _aiWarning == null ? false : _aiWarning.Value > 0 ? true : false;
                //return _checkWarning.Result;
            }
        }

        public bool IsError
        {
            get
            {
                return _aiAlarm == null ? false : _aiAlarm.Value > 0 ? true : false;
                //return _checkAlarm.Result;
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

        private AIAccessor _aiValueL = null;
        private AIAccessor _aiValueH = null;
        private AIAccessor _aiAlarm = null;
        private AIAccessor _aiWarning = null;

        private string _formatString = "F5";

        private SCConfigItem _scMinValue;
        private SCConfigItem _scMaxValue;
        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scWarningTime;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scPrecision;

        private SCConfigItem _scVaccumGaugeScale;

        private ToleranceChecker _checkWarning = new ToleranceChecker();
        private ToleranceChecker _checkAlarm = new ToleranceChecker();


        public AlarmEventItem AlarmToleranceWarning { get; set; }
        public AlarmEventItem AlarmToleranceError { get; set; }

        private bool _isFloatAioType = false;

        private float _tuningPercent;

        private float _factor;

        public IoPressureMeterTM(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiValueL = ParseAiNode("aiValueL", node, ioModule);
            _aiValueH = ParseAiNode("aiValueH", node, ioModule);
            _aiAlarm = ParseAiNode("aiAlarm", node, ioModule);
            _aiWarning = ParseAiNode("aiWarning", node, ioModule);

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

            _scVaccumGaugeScale = SC.GetConfigItem($"{Module}.Pressure.VaccumGaugeScale");

            _factor = _scVaccumGaugeScale.IntValue / 100.0f;
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.Value", () => (float)Value);
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
            if (percent > 0 && percent <= 100)
                _tuningPercent = percent;
        }

        public void UnsetTuning()
        {
            _tuningPercent = 0;
        }

        public void Monitor()
        {
            //if (EnableAlarm && (WarningTime > 0))
            //{
            //    _checkWarning.Monitor(Value, MinPressure, MaxPressure, WarningTime);

            //    if (_checkWarning.Trig)
            //    {
            //        AlarmToleranceWarning.Description =
            //            $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {WarningTime} seconds";
            //        AlarmToleranceWarning.Set();
            //    }

            //    if (!_checkWarning.Result)
            //    {
            //        AlarmToleranceWarning.Reset();
            //    }
            //}
            //else
            //{
            //    AlarmToleranceWarning.Reset();
            //}

            //if (EnableAlarm && (AlarmTime > 0))
            //{
            //    _checkAlarm.Monitor(Value, MinPressure, MaxPressure, AlarmTime);

            //    if (_checkAlarm.Trig)
            //    {
            //        AlarmToleranceError.Description =
            //            $"{Display} out of range [{MinPressure},{MaxPressure}]{Unit} for {AlarmTime} seconds";
            //        AlarmToleranceError.Set();
            //    }

            //    if (!_checkAlarm.Result)
            //    {
            //        AlarmToleranceError.Reset();
            //    }
            //}
            //else
            //{
            //    AlarmToleranceError.Reset();
            //}
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
