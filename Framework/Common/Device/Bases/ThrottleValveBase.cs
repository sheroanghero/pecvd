using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
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
    public abstract class ThrottleValveBase : BaseDevice, IDevice
    {
        public virtual PressureCtrlMode Mode { get; set; }

        public virtual float PositionFeedback { get; set; }

        public virtual float PressureFeedback { get; set; }

        public virtual float PressureSetPoint { get; set; }

        public virtual float PositionSetPoint { get; set; }

        public virtual bool IsConnected => true;

        public virtual AITThrottleValveData DeviceData { get; set; }

        public uint MaxValuePressure = 100;
        public float PressureParam = 0.1f;
        public uint MaxValuePosi = 5000;
        public float PosiParam = 0.1f;

        protected double _currentPressureFineTuningValue;
        protected double _currentPositionFineTuningValue;
        protected SCConfigItem _scFineTuningEnable;
        protected SCConfigItem _scPressureFineTuningValue;
        protected SCConfigItem _scPositionFineTuningValue;

        private float _currentPressureWarningRange;
        private float _currentPressureAlarmRange;
        private float _currentPositionWarningRange;
        private float _currentPositionAlarmRange;
        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scPressureAlarmTime;
        protected SCConfigItem _scPressureAlarmRange;
        protected SCConfigItem _scPressureWarningTime;
        protected SCConfigItem _scPressureWarningRange;
        protected SCConfigItem _scStableCriteria;
        protected SCConfigItem _scRecipeIgnoreTime;
        protected SCConfigItem _scPositionAlarmTime;
        protected SCConfigItem _scPositionAlarmRange;
        protected SCConfigItem _scPositionWarningTime;
        protected SCConfigItem _scPositionWarningRange;
        protected ToleranceChecker _tolerancePressureAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _tolerancePressureWarningChecker = new ToleranceChecker();
        protected ToleranceChecker _tolerancePositionAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _tolerancePositionWarningChecker = new ToleranceChecker();
        protected DeviceTimer _recipeIgnoreTimer = new DeviceTimer();

        protected DeviceTimer _stableTimer = new DeviceTimer();
        protected float _stableTime = 2;//second

        public virtual double PressureFineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentPressureFineTuningValue != 0)
                    return 1 + _currentPressureFineTuningValue / 100;

                return _scPressureFineTuningValue != null ? 1 + _scPressureFineTuningValue.DoubleValue / 100 : 1;
            }
        }

        public virtual double PositionFineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentPositionFineTuningValue != 0)
                    return 1 + _currentPositionFineTuningValue / 100;

                return _scPositionFineTuningValue != null ? 1 + _scPositionFineTuningValue.DoubleValue / 100 : 1;
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

        public virtual double PressureAlarmTime
        {
            get
            {
                if (_scPressureAlarmTime != null)
                    return _scPressureAlarmTime.DoubleValue;
                return 0;
            }
        }

        public virtual double PressureAlarmRange
        {
            get
            {
                if (_currentPressureAlarmRange > 0)
                    return _currentPressureAlarmRange;

                if (_scPressureAlarmRange != null)
                    return _scPressureAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double PressureWarningTime
        {
            get
            {
                if (_scPressureWarningTime != null)
                    return _scPressureWarningTime.DoubleValue;
                return 0;
            }
        }

        public virtual double PressureWarningRange
        {
            get
            {
                if (_currentPressureWarningRange > 0)
                    return _currentPressureWarningRange;

                if (_scPressureWarningRange != null)
                    return _scPressureWarningRange.DoubleValue;
                return 0;
            }
        }
        public virtual double PositionAlarmTime
        {
            get
            {
                if (_scPositionAlarmTime != null)
                    return _scPositionAlarmTime.DoubleValue;
                return 0;
            }
        }

        public virtual double PositionAlarmRange
        {
            get
            {
                if (_currentPositionAlarmRange > 0)
                    return _currentPositionAlarmRange;

                if (_scPositionAlarmRange != null)
                    return _scPositionAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double PositionWarningTime
        {
            get
            {
                if (_scPositionWarningTime != null)
                    return _scPositionWarningTime.DoubleValue;
                return 0;
            }
        }

        public virtual double PositionWarningRange
        {
            get
            {
                if (_currentPositionWarningRange > 0)
                    return _currentPositionWarningRange;

                if (_scPositionWarningRange != null)
                    return _scPositionWarningRange.DoubleValue;
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

        public virtual bool IsStable
        {
            get
            {
                if (_scStableCriteria == null || _scStableCriteria.DoubleValue == 0)
                    return false;

                if (Mode == PressureCtrlMode.TVPressureCtrl)
                {
                    if (PressureSetPoint == 0)
                        return true;

                    if (100 * PressureFeedback / PressureSetPoint < _scStableCriteria.DoubleValue)
                        _stableTimer.Stop();

                    if (_stableTimer.IsIdle() && 100 * PressureFeedback / PressureSetPoint >= _scStableCriteria.DoubleValue)
                        _stableTimer.Start(_stableTime * 1000);

                    return _stableTimer.IsTimeout();
                }

                if (Mode == PressureCtrlMode.TVPositionCtrl)
                {
                    if (PositionSetPoint == 0)
                        return true;

                    if (100 * PositionFeedback / PositionSetPoint < _scStableCriteria.DoubleValue)
                        _stableTimer.Stop();

                    if (_stableTimer.IsIdle() && 100 * PositionFeedback / PositionSetPoint >= _scStableCriteria.DoubleValue)
                        _stableTimer.Start(_stableTime * 1000);

                    return _stableTimer.IsTimeout();
                }

                return true;
            }
        }

        protected ThrottleValveBase() : base()
        {

        }
        protected ThrottleValveBase(string module, string name, XmlElement node = null, string ioModule = "") : base(module, name, name, name)
        {
            if (node != null)
            {
                _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{Module}.{Name}.EnableAlarm");
                _scPressureAlarmTime = ParseScNode("scPressureAlarmTime", node, ioModule, $"{Module}.{Name}.PressureAlarmTime");
                _scPressureAlarmRange = ParseScNode("scPressureAlarmRange", node, ioModule, $"{Module}.{Name}.PressureAlarmRange");
                _scPressureWarningTime = ParseScNode("scPressureWarningTime", node, ioModule, $"{Module}.{Name}.PressureWarningTime");
                _scPressureWarningRange = ParseScNode("scPressureWarningRange", node, ioModule, $"{Module}.{Name}.PressureWarningRange");
                _scPositionAlarmTime = ParseScNode("scPositionAlarmTime", node, ioModule, $"{Module}.{Name}.PositionAlarmTime");
                _scPositionAlarmRange = ParseScNode("scPositionAlarmRange", node, ioModule, $"{Module}.{Name}.PositionAlarmRange");
                _scPositionWarningTime = ParseScNode("scPositionWarningTime", node, ioModule, $"{Module}.{Name}.PositionWarningTime");
                _scPositionWarningRange = ParseScNode("scPositionWarningRange", node, ioModule, $"{Module}.{Name}.PositionWarningRange");
                _scPressureFineTuningValue = ParseScNode("scPressureFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}Pressure");
                _scPositionFineTuningValue = ParseScNode("scPositionFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}Position");
                _scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, $"{Module}.FineTuning.IsEnable");
                _scStableCriteria = ParseScNode("scStableCriteria", node, ioModule, $"{Module}.{Name}.StableCriteria");
                _scRecipeIgnoreTime = ParseScNode("scRecipeIgnoreTime", node, ioModule, $"{Module}.{Name}.RecipeIgnoreTime");
            }
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.ControlMode", () => (int)Mode);

            DATA.Subscribe($"{Module}.{Name}.PositionFeedback", () => PositionFeedback);
            DATA.Subscribe($"{Module}.{Name}.PressureFeedback", () => PressureFeedback);

            DATA.Subscribe($"{Module}.{Name}.PositionSetPoint", () => PositionSetPoint);
            DATA.Subscribe($"{Module}.{Name}.PressureSetPoint", () => PressureSetPoint);

            OP.Subscribe($"{Module}.{Name}.SetPressure", (function, args) =>
            {
                SetPressure(Convert.ToSingle(args[0]));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPosition", (function, args) =>
            {
                SetPosition(Convert.ToSingle(args[0]));
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetOpen", (function, args) =>
            {
                SetOpen();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetClose", (function, args) =>
            {
                SetClose();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMode", (function, args) =>
            {
                if (!Enum.TryParse((string)args[0], out PressureCtrlMode mode))
                {
                    EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
                    return false;
                }
                SetControlMode(mode);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Learn", (function, args) =>
            {
                Learn();
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetFineTuning", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetFineTuning(Convert.ToSingle(param[0]), Convert.ToSingle(param[1]));
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetTolerance", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var pressureWarning = Convert.ToSingle(param[0]);
                var pressureAlarm = Convert.ToSingle(param[1]);
                var positionWarning = Convert.ToSingle(param[2]);
                var positionAlarm = Convert.ToSingle(param[3]);
                SetTolerance((float)pressureWarning, (float)pressureAlarm, (float)positionWarning, (float)positionAlarm);
                return true;
            });

            InitSc();

            return true;
        }

        protected virtual void InitSc()
        {

        }

        public virtual void SetFineTuning(float pressureFineTuning, float positionFineTuning)
        {
            _currentPressureFineTuningValue = pressureFineTuning;
            _currentPositionFineTuningValue = positionFineTuning;
        }

        public virtual void SetTolerance(float pressureWarning, float pressureAlarm, float positionWarning, float positionAlarm)
        {
            _currentPressureWarningRange = pressureWarning;
            _currentPressureAlarmRange = pressureAlarm;
            _tolerancePressureAlarmChecker.Reset(PressureAlarmTime);
            _tolerancePressureWarningChecker.Reset(PressureWarningTime);

            _currentPositionWarningRange = positionWarning;
            _currentPositionAlarmRange = positionAlarm;
            _tolerancePositionAlarmChecker.Reset(PositionAlarmTime);
            _tolerancePositionWarningChecker.Reset(PositionWarningTime);

            if (RecipeIgnoreTime > 0)
                _recipeIgnoreTimer.Start(0);
        }

        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || (RecipeIgnoreTime > 0 && _recipeIgnoreTimer.GetElapseTime() < RecipeIgnoreTime * 1000))
                return;

            if (Mode == PressureCtrlMode.TVPressureCtrl && PressureSetPoint > 0)
            {
                _tolerancePressureAlarmChecker.Monitor(PressureFeedback, (PressureSetPoint * (1 - PressureAlarmRange / 100)), (PressureSetPoint * (1 + PressureAlarmRange / 100)), PressureAlarmTime);

                _tolerancePressureWarningChecker.Monitor(PressureFeedback, (PressureSetPoint * (1 - PressureWarningRange / 100)), (PressureSetPoint * (1 + PressureWarningRange / 100)), PressureWarningTime);
            }
            if (Mode == PressureCtrlMode.TVPositionCtrl && PositionSetPoint > 0)
            {
                _tolerancePressureAlarmChecker.Monitor(PositionFeedback, (PositionSetPoint * (1 - PositionAlarmRange / 100)), (PositionSetPoint * (1 + PositionAlarmRange / 100)), PositionAlarmTime);

                _tolerancePressureWarningChecker.Monitor(PositionFeedback, (PositionSetPoint * (1 - PositionWarningRange / 100)), (PositionSetPoint * (1 + PositionWarningRange / 100)), PositionWarningTime);
            }
        }

        public virtual bool CheckPressureToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _tolerancePressureAlarmChecker.Result;
        }

        public virtual bool CheckPressureToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _tolerancePressureWarningChecker.Result;
        }

        public virtual bool CheckPositionToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _tolerancePositionAlarmChecker.Result;
        }

        public virtual bool CheckPositionToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _tolerancePositionWarningChecker.Result;
        }

        public virtual void SetClose()
        {
            throw new NotImplementedException();
        }

        public virtual void SetOpen()
        {
            throw new NotImplementedException();
        }

        public virtual void SetControlMode(PressureCtrlMode mode)
        {

        }

        public virtual void SetPressure(float pressure)
        {

        }

        public virtual void SetPosition(float position)
        {

        }

        public virtual void Learn()
        {

        }

        public virtual void Terminate()
        {
        }

        public virtual void Monitor()
        {
            CheckTolerance();
        }

        public virtual void Reset()
        {

        }
        public void StopStableTimer()
        {
            _stableTimer.Stop();
        }
    }
}