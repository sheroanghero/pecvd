using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class GasSplitterBase : BaseDevice, IDevice
    {
        public string Unit
        {
            get; set;
        }

        public virtual float Scale
        {
            get
            {
                if (_scFullScale == null)
                    return 100;
                return (float)(_scFullScale.DoubleValue);
            }
        }

        public virtual float SetPoint { get; set; }

        public virtual float FeedBack { get; set; }
        public virtual float SwitchFeedBack { get; set; }

        public bool IsOutOfTolerance
        {
            get
            {
                return _toleranceAlarmChecker.Result;
            }
        }

        public virtual bool EnableAlarm
        {
            get
            {
                if (_scEnableAlarm != null)
                    return _scEnableAlarm.BoolValue;
                return false;
            }
        }

        public virtual double AlarmTime
        {
            get
            {
                if (_scAlarmTime != null)
                    return _scAlarmTime.IntValue != 0 ? _scAlarmTime.IntValue : _scAlarmTime.DoubleValue;
                return 10;
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
                    return _scWarningTime.DoubleValue;
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

        public virtual AITGasSplitterData DeviceData
        {
            get
            {
                AITGasSplitterData data = new AITGasSplitterData()
                {
                    UniqueName = _uniqueName,
                    Type = "Splitter",
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = "",
                    FeedBack = FeedBack,
                    SetPoint = SetPoint,
                    Scale = Scale,
                };

                return data;
            }
        }

        private DeviceTimer _rampTimer = new DeviceTimer();
        protected float _rampTarget;
        private float _rampInitValue;
        private int _rampTime;
        private float _currentWarningRange;
        private float _currentAlarmRange;

        protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

        protected SCConfigItem _scFullScale;
        protected SCConfigItem _scAlarmRange;
        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scAlarmTime;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;
        protected SCConfigItem _scDefaultSetPoint;
        protected SCConfigItem _scRegulationFactor;
        private SCConfigItem _scFineTuningEnable;
        private SCConfigItem _scFineTuningValue;

        protected double _currentFineTuningValue;

        protected string _uniqueName;

        public GasSplitterBase() : base()
        {
        }

        public GasSplitterBase(string module, XmlElement node, string ioModule = "")
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _scFullScale = ParseScNode("scFullScale", node, ioModule, $"{Module}.{Name}.FullScale");

            _scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, $"{Module}.{Name}.AlarmRange");
            _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{Module}.{Name}.EnableAlarm");
            _scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, $"{Module}.{Name}.AlarmTime");
            _scWarningTime = ParseScNode("scWarningTime", node, ioModule, $"{Module}.{DeviceID}.WarningTime");
            _scWarningRange = ParseScNode("scWarningRange", node, ioModule, $"{Module}.{DeviceID}.WarningRange");
            _scFineTuningValue = ParseScNode("scFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}");
            _scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, $"{Module}.FineTuning.IsEnable");
            _scDefaultSetPoint = ParseScNode("scDefaultSetPoint", node);

            _scRegulationFactor = ParseScNode("scFlowRegulationFactor", node, ioModule, $"{Module}.{Name}.RegulationFactor");

            _uniqueName = $"{Module}.{Name}";
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.RatioFeedback", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.RatioSetPoint", () => SetPoint);

            OP.Subscribe($"{Module}.{Name}.SetRatio", (function, args) =>
            {
                SetRatio(Convert.ToSingle(args[0]));
                return true;
            });
            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetRatio", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetRatio(Convert.ToSingle(param[0]));
                return true;
            });
            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetFineTuning", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetFineTuning(Convert.ToSingle(param[0]));
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

            return true;
        }

        public virtual void SetTolerance(float warning, float alarm)
        {
            _currentWarningRange = warning;
            _currentAlarmRange = alarm;
            _toleranceAlarmChecker.Reset(AlarmTime);
            _toleranceWarningChecker.Reset(WarningTime);
        }

        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || SetPoint == 0)
                return;

            _toleranceAlarmChecker.Monitor(FeedBack, (SetPoint * (1 - AlarmRange / 100)), (SetPoint * (1 + AlarmRange / 100)), AlarmTime);

            _toleranceWarningChecker.Monitor(FeedBack, (SetPoint * (1 - WarningRange / 100)), (SetPoint * (1 + WarningRange / 100)), WarningTime);
        }

        public virtual bool CheckToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceAlarmChecker.Result;
        }

        public virtual bool CheckToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceWarningChecker.Result;
        }

        public virtual void SetFineTuning(float fineTuning)
        {
            _currentFineTuningValue = fineTuning;
        }

        public virtual void SetRatio(float ratio)
        {
            _rampTarget = ratio;
        }

        private bool InvokeRamp(string method, object[] args)
        {
            float target = Convert.ToSingle((string)(args[0].ToString()));
            target = Math.Min(target, Scale);
            target = Math.Max(target, 0);

            int time = 0;
            if (args.Length >= 2)
                time = Convert.ToInt32((string)(args[1].ToString()));

            Ramp(target, time);

            EV.PostInfoLog(Module, $"{_uniqueName} ramp to {target}{Unit} in {time} seconds");

            return true;
        }

        public virtual void Monitor()
        {
            MonitorRamping();

            MonitorTolerance();
        }

        public virtual void Reset()
        {
            _toleranceAlarmChecker.Reset(AlarmTime);
            _toleranceWarningChecker.Reset(WarningTime);
        }

        public virtual void Terminate()
        {
            Ramp(0, 0);
        }

        public void Ramp(int time)
        {
            Ramp(0, time);
        }

        public void Ramp(float target, int time)
        {
            target = Math.Max(0, target);
            target = Math.Min(Scale, target);
            _rampInitValue = SetPoint;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            _rampTime = time;
            _rampTarget = target;
            _rampTimer.Start(_rampTime);
        }

        public void StopRamp()
        {
            Ramp(SetPoint, 0);
        }

        private void MonitorRamping()
        {
            if (_rampTimer.IsTimeout() || _rampTime == 0)
            {
                SetPoint = _rampTarget;
            }
            else
            {
                SetPoint = (float)(_rampInitValue + (_rampTarget - _rampInitValue) * _rampTimer.GetElapseTime() / _rampTime);
            }
        }

        protected virtual void MonitorTolerance()
        {
            CheckTolerance();
        }

    }
}
