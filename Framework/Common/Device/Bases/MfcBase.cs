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
using MECF.Framework.Common.DBCore;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class MfcBase : BaseDevice, IDevice
    {
        public string Unit
        {
            get; set;
        }

        public virtual float Scale
        {
            get
            {
                if (_scN2Scale == null || _scScaleFactor == null)
                    return 100;
                return (float)(_scN2Scale.DoubleValue * _scScaleFactor.DoubleValue);
            }
            set
            {

            }
        }
        public virtual float MinimumFlow
        {
            get;
            set;
        }
        public virtual float SetPoint { get; set; }

        public virtual float FeedBack { get; set; }

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

        public string DisplayName
        {
            get
            {
                if (_scGasName != null)
                    return _scGasName.StringValue;
                return Display;
            }
        }

        public virtual AITMfcData DeviceData
        {
            get
            {
                AITMfcData data = new AITMfcData()
                {
                    UniqueName = _uniqueName,
                    Type = "MFC",
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = DisplayName,
                    FeedBack = SetPoint == 0 ? 0 : FeedBack,
                    SetPoint = SetPoint,
                    Scale = Scale,

                };

                return data;
            }
        }

        private DeviceTimer rampTimer = new DeviceTimer();
        protected float rampTarget;
        private float rampInitValue;
        private int rampTime;
        private float _currentWarningRange;
        private float _currentAlarmRange;

        protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

        protected SCConfigItem _scGasName;
        protected SCConfigItem _scEnable;
        protected SCConfigItem _scN2Scale;
        protected SCConfigItem _scN2MinimumFlow;
        protected SCConfigItem _scScaleFactor;
        protected SCConfigItem _scAlarmRange;
        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scAlarmTime;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;
        protected SCConfigItem _scDefaultSetPoint;
        protected SCConfigItem _scRegulationFactor;
        private SCConfigItem _scFineTuningEnable;
        private SCConfigItem _scFineTuningValue;

        private R_TRIG _trigOffline = new R_TRIG();
        protected double _currentFineTuningValue;

        protected string _uniqueName;

        private float _percent10Calculate;
        private float _percent20Calculate;
        private float _percent30Calculate;
        private float _percent40Calculate;
        private float _percent50Calculate;
        private float _percent60Calculate;
        private float _percent70Calculate;
        private float _percent80Calculate;
        private float _percent90Calculate;
        private float _percent100Calculate;
        private float _verificationCalculate;
        private float _verificationSetpoint;
        private string _verificationResult = "";

        public MfcBase() : base()
        {
        }

        public MfcBase(string module, XmlElement node, string ioModule = "")
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _scGasName = ParseScNode("scGasName", node, ioModule, $"{Module}.{Name}.GasName");
            _scEnable = ParseScNode("scEnable", node);
            _scN2Scale = ParseScNode("scN2Scale", node, ioModule, $"{Module}.{Name}.N2Scale");

            _scScaleFactor = ParseScNode("scScaleFactor", node, ioModule, $"{Module}.{Name}.ScaleFactor");
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

            DATA.Subscribe($"{Module}.{Name}.FlowFeedback", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.FlowSetPoint", () => SetPoint);
            DATA.Subscribe($"{Module}.{Name}.VerificationResult", () => _verificationResult);

            OP.Subscribe($"{Module}.{Name}.SetMfcFlow", (function, args) =>
            {
                SetMfcFlow(Convert.ToSingle(args[0]));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Ramp", (function, args) =>
            {
                SetMfcFlow(Convert.ToSingle(args[0]));
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
            InitSc();

            return true;
        }

        public virtual void InitSc()
        {
            _scGasName = SC.GetConfigItem($"{ScBasePath}.{Name}.GasName");
            _scEnable = SC.GetConfigItem($"{ScBasePath}.{Name}.Enable");

            _scN2Scale = SC.GetConfigItem($"{ScBasePath}.{Name}.N2Scale");
            _scScaleFactor = SC.GetConfigItem($"{ScBasePath}.{Name}.ScaleFactor");

            _scEnableAlarm = SC.GetConfigItem($"{ScBasePath}.{Name}.EnableAlarm");
            _scAlarmRange = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmRange");
            _scAlarmTime = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmTime");
            _scWarningRange = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningRange");
            _scWarningTime = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningTime");
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

        public virtual void SetMfcFlow(float flow)
        {

        }

        public virtual bool SetMfcFlow(int time, float flow, out string reason)
        {
            reason = "";
            return true;
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

            _trigOffline.RST = true;
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
            rampInitValue = SetPoint;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            rampTime = time;
            rampTarget = target;
            rampTimer.Start(rampTime);
        }

        public void StopRamp()
        {
            Ramp(SetPoint, 0);
        }

        private void MonitorRamping()
        {
            if (rampTimer.IsTimeout() || rampTime == 0)
            {
                SetPoint = rampTarget;
            }
            else
            {
                SetPoint = (float)(rampInitValue + (rampTarget - rampInitValue) * rampTimer.GetElapseTime() / rampTime);
            }
        }

        protected virtual void MonitorTolerance()
        {
            CheckTolerance();
        }

        public virtual void SetVerificationResult(float setpoint, float calculateFlow, bool saveResult)
        {
            //ten points
            var delta = Scale / 10;
            _verificationResult += $"{setpoint},{calculateFlow};";
            if (delta - 0.1 < setpoint && delta + 0.1 > setpoint)
            {
                _percent10Calculate = calculateFlow;
            }
            else if (2 * delta - 0.1 < setpoint && 2 * delta + 0.1 > setpoint)
            {
                _percent20Calculate = calculateFlow;
            }
            else if (3 * delta - 0.1 < setpoint && 3 * delta + 0.1 > setpoint)
            {
                _percent30Calculate = calculateFlow;
            }
            else if (4 * delta - 0.1 < setpoint && 4 * delta + 0.1 > setpoint)
            {
                _percent40Calculate = calculateFlow;
            }
            else if (5 * delta - 0.1 < setpoint && 5 * delta + 0.1 > setpoint)
            {
                _percent50Calculate = calculateFlow;
            }
            else if (6 * delta - 0.1 < setpoint && 6 * delta + 0.1 > setpoint)
            {
                _percent60Calculate = calculateFlow;
            }
            else if (7 * delta - 0.1 < setpoint && 7 * delta + 0.1 > setpoint)
            {
                _percent70Calculate = calculateFlow;
            }
            else if (8 * delta - 0.1 < setpoint && 8 * delta + 0.1 > setpoint)
            {
                _percent80Calculate = calculateFlow;
            }
            else if (9 * delta - 0.1 < setpoint && 9 * delta + 0.1 > setpoint)
            {
                _percent90Calculate = calculateFlow;
            }
            else if (10 * delta - 0.1 < setpoint && 10 * delta + 0.1 > setpoint)
            {
                _percent100Calculate = calculateFlow;
            }
            else
            {
                _verificationCalculate = calculateFlow;
                _verificationSetpoint = setpoint;
            }

            if (saveResult)
            {
                SaveVerificationData();
            }
        }

        private void SaveVerificationData()
        {
            var delta = (float)(Scale / 10);
            MFCVerificationData data = new MFCVerificationData()
            {
                Module = Module,
                Name = DisplayName,
                Percent10Setpoint = delta,
                Percent10Calculate = _percent10Calculate,

                Percent20Setpoint = delta * 2,
                Percent20Calculate = _percent20Calculate,

                Percent30Setpoint = delta * 3,
                Percent30Calculate = _percent30Calculate,

                Percent40Setpoint = delta * 4,
                Percent40Calculate = _percent40Calculate,

                Percent50Setpoint = delta * 5,
                Percent50Calculate = _percent50Calculate,

                Percent60Setpoint = delta * 6,
                Percent60Calculate = _percent60Calculate,

                Percent70Setpoint = delta * 7,
                Percent70Calculate = _percent70Calculate,

                Percent80Setpoint = delta * 8,
                Percent80Calculate = _percent80Calculate,

                Percent90Setpoint = delta * 9,
                Percent90Calculate = _percent90Calculate,

                Percent100Setpoint = delta * 10,
                Percent100Calculate = _percent100Calculate,

                Setpoint = _verificationSetpoint,
                Calculate = _verificationCalculate,
            };
            MFCVerificationDataRecorder.Add(data);
        }

        public void ResetVerificationData()
        {
            _percent10Calculate = 0;
            _percent20Calculate = 0;
            _percent30Calculate = 0;
            _percent40Calculate = 0;
            _percent50Calculate = 0;
            _percent60Calculate = 0;
            _percent70Calculate = 0;
            _percent80Calculate = 0;
            _percent90Calculate = 0;
            _percent100Calculate = 0;
            _verificationCalculate = 0;
            _verificationSetpoint = 0;
            _verificationResult = "";
        }
    }
}
