using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.Device.Bases
{


    public abstract class RfPowerBase : BaseDevice, IDevice
    {
        public virtual bool IsConnected => true;
        public virtual bool IsPowerOn { get; set; }
        public virtual bool IsSetPowerOn { get; set; }
        public virtual bool IsError { get; set; }
        public virtual bool IsHighFrequentQueryMode { get; set; }
        public virtual EnumRfPowerClockMode ClockMode { get; set; }
        public virtual EnumRfPowerWorkMode WorkMode { get; set; }
        public virtual EnumRfPowerRegulationMode RegulationMode { get; set; }

        public virtual float ForwardPower { get; set; }
        public virtual float ReflectPower { get; set; }// 硬件反馈，反射功率
        public virtual float PowerSetPoint { get; set; }

        protected float _originalPowerSetPoint = 0;
        protected float _PowerSetPoint; // 下发到硬件
        public virtual float Frequency { get; set; }
        public virtual float PulsingFrequency { get; set; }
        public virtual float PulsingDutyCycle { get; set; }
        public virtual float PulsingReverseTime { get; set; }

        public virtual AITRfPowerData DeviceData { get; set; }

        private float _currentWarningRange;
        private float _currentAlarmRange;

        protected SCConfigItem _scReflectPowerAlarmTime;
        protected SCConfigItem _scReflectPowerAlarmValue;
        protected DeviceTimer _ReflectPowerAlarmTimer = new DeviceTimer();
        private R_TRIG _trigReflectPowerAlarm = new R_TRIG();

        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scAlarmTime;
        protected SCConfigItem _scAlarmRange;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;
        protected SCConfigItem _scReflectedPowerMonitorTime;
        protected SCConfigItem _scRecipeIgnoreTime;
        protected SCConfigItem _scPowerScale;
        protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();
        protected ToleranceChecker _prThresholdChecker = new ToleranceChecker();

        protected double _currentFineTuningValue;
        protected SCConfigItem _scFineTuningEnable;
        protected SCConfigItem _scFineTuningValue;
        protected float _PrThreshold;
        protected DeviceTimer _recipeIgnoreTimer = new DeviceTimer();

        protected DeviceTimer _rampTimer = new DeviceTimer();
        protected float _rampTarget;
        protected float _rampInitValue;
        protected int _rampTime;//unit ms

        private DeviceTimer _rampInternalTimer = new DeviceTimer();
        private readonly int _rampInterval = 200;

        //calibration
        protected SCConfigItem _scEnableCalibration;
        protected SCConfigItem _scCalibrationTable;

        private List<CalibrationItem> _calibrationTable = new List<CalibrationItem>();
        private string _previousSetting;

        public virtual bool EnableAlarm
        {
            get
            {
                if (_scEnableAlarm != null)
                    return _scEnableAlarm.BoolValue;
                return false;
            }
        }

        public virtual double ReflectPowerAlarmTime
        {
            get
            {
                if (_scReflectPowerAlarmTime != null)
                    return _scReflectPowerAlarmTime.IntValue;
                return 10;
            }
        }

        public virtual double ReflectPowerAlarmValue
        {
            get
            {

                if (_scReflectPowerAlarmValue != null)
                    return _scReflectPowerAlarmValue.DoubleValue;
                return 0;
            }
        }

        public virtual double AlarmTime
        {
            get
            {
                if (_scAlarmTime != null)
                    return _scAlarmTime.IntValue;
                return 0;
            }
        }

        public virtual double AlarmRange
        {
            get
            {
                if (_currentAlarmRange > 0)
                    return _currentAlarmRange;

                if (_scAlarmRange != null)
                    return _scAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double WarningTime
        {
            get
            {
                if (_scWarningTime != null)
                    return _scWarningTime.IntValue;
                return 0;
            }
        }

        public virtual double WarningRange
        {
            get
            {
                if (_currentWarningRange > 0)
                    return _currentWarningRange;

                if (_scWarningRange != null)
                    return _scWarningRange.DoubleValue;
                return 0;
            }
        }

        //unit second
        public virtual double PrThresholdMonitorTime
        {
            get
            {
                if (_scReflectedPowerMonitorTime != null)
                    return _scReflectedPowerMonitorTime.DoubleValue;
                return 0;
            }
        }

        //unit second
        public virtual double RecipeIgnoreTime
        {
            get
            {
                if (_scRecipeIgnoreTime != null)
                    return _scRecipeIgnoreTime.DoubleValue;
                return 0;
            }
        }

        public virtual double FineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentFineTuningValue != 0)
                    return 1 + _currentFineTuningValue / 100;

                return _scFineTuningValue != null ? 1 + _scFineTuningValue.DoubleValue / 100 : 1;
            }
        }

        public virtual int PowerRange
        {
            get
            {
                if (_scPowerScale == null)
                    return 1000;
                return _scPowerScale.IntValue;
            }
        }

        protected RfPowerBase(string module, string name) : base(module, name, name, name)
        {
            _scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
            _scAlarmTime = SC.GetConfigItem($"{Module}.{Name}.AlarmTime");
            _scAlarmRange = SC.GetConfigItem($"{Module}.{Name}.AlarmRange");
            _scWarningTime = SC.GetConfigItem($"{Module}.{Name}.WarningTime");
            _scWarningRange = SC.GetConfigItem($"{Module}.{Name}.WarningRange");
            _scFineTuningValue = SC.GetConfigItem($"{Module}.FineTuning.{Name}");
            _scFineTuningEnable = SC.GetConfigItem($"{Module}.FineTuning.IsEnable");

            _scReflectPowerAlarmTime = SC.GetConfigItem($"{Module}.{Name}.ReflectPowerAlarmTime");
            _scReflectPowerAlarmValue = SC.GetConfigItem($"{Module}.{Name}.ReflectPowerAlarmValue");
        }

        protected RfPowerBase() : base()
        {

        }
        protected RfPowerBase(string module, string name, XmlElement node = null, string ioModule = "") : base(module, name, name, name)
        {
            if (node != null)
            {
                _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{Module}.{Name}.EnableAlarm");
                _scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, $"{Module}.{Name}.AlarmTime");
                _scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, $"{Module}.{Name}.AlarmRange");
                _scWarningTime = ParseScNode("scWarningTime", node, ioModule, $"{Module}.{Name}.WarningTime");
                _scWarningRange = ParseScNode("scWarningRange", node, ioModule, $"{Module}.{Name}.WarningRange");
                _scRecipeIgnoreTime = ParseScNode("scRecipeIgnoreTime", node, ioModule, $"{Module}.{Name}.RecipeIgnoreTime");
                _scReflectedPowerMonitorTime = ParseScNode("scReflectedPowerMonitorTime", node, ioModule, $"{Module}.{Name}.ReflectedPowerMonitorTime");
                _scEnableCalibration = ParseScNode("scCalibrationTable", node, ioModule, $"{Module}.{Name}.EnableCalibration");
                _scCalibrationTable = ParseScNode("scCalibrationTable", node, ioModule, $"{Module}.{Name}.CalibrationTable");
                _scFineTuningValue = ParseScNode("scFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}");
                _scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, $"{Module}.FineTuning.IsEnable");
            }
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.IsPowerOn", () => IsPowerOn);
            DATA.Subscribe($"{Module}.{Name}.IsSetPowerOn", () => IsSetPowerOn);

            DATA.Subscribe($"{Module}.{Name}.WorkMode", () => WorkMode.ToString());
            DATA.Subscribe($"{Module}.{Name}.RegulationMode", () => RegulationMode.ToString());

            DATA.Subscribe($"{Module}.{Name}.ForwardPower", () => ForwardPower);
            DATA.Subscribe($"{Module}.{Name}.ReflectPower", () => ReflectPower);
            DATA.Subscribe($"{Module}.{Name}.PowerSetPoint", () => PowerSetPoint);

            DATA.Subscribe($"{Module}.{Name}.Frequency", () => Frequency);
            DATA.Subscribe($"{Module}.{Name}.PulsingFrequency", () => PulsingFrequency);
            DATA.Subscribe($"{Module}.{Name}.PulsingDutyCycle", () => PulsingDutyCycle);
            DATA.Subscribe($"{Module}.{Name}.PulsingReverseTime", () => PulsingReverseTime);

            OP.Subscribe($"{Module}.{Name}.SetPowerOn", (function, args) =>
            {
                if (!SetPowerOnOff(true, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF on failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOff", (function, args) =>
            {
                if (!SetPowerOnOff(false, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF off failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOnOff", (function, args) =>
            {
                if (!SetPowerOnOff(Convert.ToBoolean(args[0].ToString()), out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF off failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPower", (function, args) =>
            {
                SetPower(Convert.ToSingle(args[0].ToString()));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetRegulationMode", (function, args) =>
            {
                if (!Enum.TryParse((string)args[0], out EnumRfPowerRegulationMode mode))
                {
                    EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
                    return false;
                }
                SetRegulationMode(mode);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetRFParameters", (out string reason, int time, object[] param) => 
            {
                reason = string.Empty;
                SetRFParameters(param);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetTolerance", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var warning = Convert.ToSingle(param[0]);
                var alarm = Convert.ToSingle(param[1]);
                SetTolerance((float)warning, (float)alarm);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetFineTuning", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetFineTuning(Convert.ToSingle(param[0]));
                return true;
            });

            InitSc();
            UpdateCalibrationTable();

            return true;
        }

        public virtual void InitSc()
        {
        }

        public virtual void SetFineTuning(float fineTuning)
        {
            _currentFineTuningValue = fineTuning;
        }

        public virtual void SetTolerance(float warning, float alarm)
        {
            _currentWarningRange = warning;
            _currentAlarmRange = alarm;
            _toleranceAlarmChecker.Reset(AlarmTime);
            _toleranceWarningChecker.Reset(WarningTime);

            if (RecipeIgnoreTime > 0)
                _recipeIgnoreTimer.Start(0);
        }

        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || PowerSetPoint == 0 || !IsSetPowerOn || (RecipeIgnoreTime > 0 && _recipeIgnoreTimer.GetElapseTime() < RecipeIgnoreTime * 1000))
            {
                _toleranceAlarmChecker.RST = true;
                _toleranceWarningChecker.RST = true;
                _toleranceAlarmChecker.Reset(AlarmTime);
                _toleranceWarningChecker.Reset(WarningTime);

                _ReflectPowerAlarmTimer.Start(ReflectPowerAlarmTime * 1000);
                _trigReflectPowerAlarm.RST = true;
                return;
            }

            _toleranceAlarmChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - AlarmRange / 100)), (PowerSetPoint * (1 + AlarmRange / 100)), AlarmTime);

            _toleranceWarningChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - WarningRange / 100)), (PowerSetPoint * (1 + WarningRange / 100)), WarningTime);

            if (ReflectPower <= ReflectPowerAlarmValue)
            {
                _ReflectPowerAlarmTimer.Start(ReflectPowerAlarmTime * 1000);
            }
            else if (_ReflectPowerAlarmTimer.IsTimeout() && ReflectPowerAlarmValue != 0)
            {

                _trigReflectPowerAlarm.CLK = ReflectPower > ReflectPowerAlarmValue;
                if (_trigReflectPowerAlarm.Q)
                {
                    EV.PostAlarmLog(Module, $"{Name} ReflectPower:{ReflectPower}  > ReflectPowerAlarmValue:{ReflectPowerAlarmValue}  in {ReflectPowerAlarmTime}s");
                }
                else
                {
                    _trigReflectPowerAlarm.RST = true;
                    _ReflectPowerAlarmTimer.Start(ReflectPowerAlarmTime * 1000);
                }
            }

            if (_PrThreshold > 0)
                _prThresholdChecker.Monitor(ReflectPower, 0, _PrThreshold, PrThresholdMonitorTime);
        }

        public virtual bool CheckToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceAlarmChecker.Result;
        }

        public string GetToleranceAlarmLog()
        {
            return $"{Name} over alarm range of ±{AlarmRange}% for {AlarmTime}s";
        }

        public virtual bool CheckToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceWarningChecker.Result;
        }

        public string GetToleranceWarningLog()
        {
            return $"{Name} over warning range of ±{WarningRange}% for {WarningTime}s";
        }

        public virtual bool CheckPrThreshold()
        {
            if (!EnableAlarm)
                return false;

            return _prThresholdChecker.Result;
        }

        public virtual void SetRegulationMode(EnumRfPowerRegulationMode enumRfPowerControlMode)
        {

        }

        public virtual void SetWorkMode(EnumRfPowerWorkMode enumRfPowerWorkMode)
        {

        }

        public virtual void SetClockMode(EnumRfPowerClockMode clockMode)
        {

        }

        public virtual bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void SetPower(float power)
        {

        }

        public virtual void SetPower(float power, float rampTime, bool isCalibration = false)
        {

        }

        public virtual void SetFreq(float freq)
        {

        }

        public virtual void SetRFParameters(object[] param)
        {

        }

        public virtual void SetPrThreshold(float threshold)
        {
            _PrThreshold = threshold;
            _prThresholdChecker.Reset(PrThresholdMonitorTime);
        }

        public virtual void SetRampTime(float rampTime)
        {

        }

        public virtual void Terminate()
        {
        }

        public virtual void Monitor()
        {
            CheckTolerance();

            if (_scCalibrationTable != null)
            {
                if (string.IsNullOrEmpty(_previousSetting) || _previousSetting != _scCalibrationTable.StringValue)
                    UpdateCalibrationTable();
            }
        }

        public virtual void Reset()
        {

        }

        protected virtual void UpdateCalibrationTable()
        {
            if (_scCalibrationTable == null)
                return;

            if (_previousSetting == _scCalibrationTable.StringValue)
                return;

            _previousSetting = _scCalibrationTable.StringValue;

            if (string.IsNullOrEmpty(_previousSetting))
            {
                _calibrationTable = new List<CalibrationItem>();
                return;
            }

            var table = new List<Tuple<float, float>>();

            string[] items = _previousSetting.Split(';');

            for (int i = 0; i < items.Length; i++)
            {
                string itemValue = items[i];
                if (!string.IsNullOrEmpty(itemValue))
                {
                    string[] pairValue = itemValue.Split('#');
                    if (pairValue.Length == 2)
                    {
                        if (float.TryParse(pairValue[0], out float rawData)
                            && float.TryParse(pairValue[1], out float calibrationData))
                        {
                            table.Add(Tuple.Create(rawData, calibrationData));
                        }
                    }
                }
            }

            table = table.OrderBy(x => x.Item1).ToList();

            var calibrationTable = new List<CalibrationItem>();

            for (int i = 0; i < table.Count; i++)
            {
                if (i == 0 && table[0].Item1 > 0.001)
                {
                    calibrationTable.Add(new CalibrationItem()
                    {
                        RawFrom = 0,
                        CalibrationFrom = 0,

                        RawTo = table[0].Item1,
                        CalibrationTo = table[0].Item2,
                    });
                }

                if (i == table.Count - 1)
                {
                    float maxValue = (float)PowerRange;

                    calibrationTable.Add(new CalibrationItem()
                    {
                        RawFrom = table[i].Item1,
                        RawTo = table[i].Item2,

                        CalibrationFrom = maxValue,
                        CalibrationTo = maxValue,
                    });
                    continue;
                }

                calibrationTable.Add(new CalibrationItem()
                {
                    RawFrom = table[i].Item1,
                    CalibrationFrom = table[i].Item2,

                    RawTo = table[i + 1].Item1,
                    CalibrationTo = table[i + 1].Item2,
                });
            }

            _calibrationTable = calibrationTable;
        }


        protected virtual float CalibrationData(float value, bool output)
        {
            if (value.IsZero()) return value;

            //default enable
            if (_scEnableCalibration != null && !_scEnableCalibration.BoolValue)
                return value;

            if (_scCalibrationTable == null || !_calibrationTable.Any())
                return value;

            float ret = value;

            if (output)
            {
                var item = _calibrationTable.FirstOrDefault(x => x.RawFrom <= value && x.RawTo >= value);
                if (item != null && Math.Abs(item.RawTo - item.RawFrom) > 0.01)
                {
                    var slope = (item.CalibrationTo - item.CalibrationFrom) / (item.RawTo - item.RawFrom);
                    ret = (ret - item.RawFrom) * slope + item.CalibrationFrom;
                }
            }
            else
            {
                var item = _calibrationTable.FirstOrDefault(x => x.CalibrationFrom <= value && x.CalibrationTo >= value);
                if (item != null && Math.Abs(item.CalibrationTo - item.CalibrationFrom) > 0.01)
                {
                    var slope = (item.RawTo - item.RawFrom) / (item.CalibrationTo - item.CalibrationFrom);
                    ret = (ret - item.CalibrationFrom) * slope + item.RawFrom;
                }
            }

            if (ret < 0)
                return 0;

            if (ret >= float.MaxValue || ret > PowerRange)
                ret = value;

            return ret;
        }
        protected void MonitorRamping()
        {
            if (_originalPowerSetPoint == 0 && Math.Abs(PowerSetPoint - _rampTarget) < 0.000001) return;
            if (_rampTimer == null || _rampTime == 0) return;

            if (_rampTimer.IsTimeout())
            {
                if (Math.Abs(PowerSetPoint - _rampTarget) > 0.000001) SetRampPower(_rampTarget);
                _rampTimer = null;
            }
            else
            {
                float target = (float)(_rampInitValue + (_rampTarget - _rampInitValue) * _rampTimer.GetElapseTime() / _rampTime);
                if (_rampInternalTimer.IsIdle())
                    _rampInternalTimer.Start(_rampInterval);
                if (Math.Abs(PowerSetPoint - target) > 0.000001 && _rampInternalTimer.IsTimeout())
                {
                    SetRampPower(target);
                    _rampInternalTimer.Start(_rampInterval);
                }

                if (target.GreaterThanOrEqual(_rampTarget))
                {
                    _rampTimer = null;
                }
            }
        }
        protected virtual void SetRampPower(float power)
        {

        }
    }
}