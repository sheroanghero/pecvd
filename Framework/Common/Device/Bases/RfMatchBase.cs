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
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class RfMatchBase : BaseDevice, IDevice
    {
        public virtual bool IsConnected => true;
        public virtual EnumRfMatchTuneMode TuneMode1 { get; set; }
        public virtual EnumRfMatchTuneMode TuneMode2 { get; set; }
        public virtual EnumRfMatchTuneMode TuneMode1Setpoint { get; set; }
        public virtual EnumRfMatchTuneMode TuneMode2Setpoint { get; set; }

        public virtual float LoadPosition1 { get; set; }
        public virtual float LoadPosition2 { get; set; }
        public virtual float LoadPosition1Setpoint { get; set; }
        public virtual float LoadPosition2Setpoint { get; set; }

        public virtual float TunePosition1 { get; set; }
        public virtual float TunePosition2 { get; set; }
        public virtual float TunePosition1Setpoint { get; set; }
        public virtual float TunePosition2Setpoint { get; set; }

        public virtual float DCBias { get; set; }
        public virtual float BiasPeak { get; set; }
        public virtual float Capacitance { get; set; }
        public virtual float CapacitanceSetpoint { get; set; }

        public virtual AITRfMatchData DeviceData { get; set; }

        protected double _currentTuneFineTuningValue;
        protected double _currentLoadFineTuningValue;
        protected double _currentCapFineTuningValue;
        protected SCConfigItem _scFineTuningEnable;
        protected SCConfigItem _scTuneFineTuningValue;
        protected SCConfigItem _scLoadFineTuningValue;
        protected SCConfigItem _scCapFineTuningValue;

        private float _currentTuneWarningRange;
        private float _currentLoadWarningRange;
        private float _currentCapWarningRange;
        private float _currentTuneAlarmRange;
        private float _currentLoadAlarmRange;
        private float _currentCapAlarmRange;
        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scTuneAlarmTime;
        protected SCConfigItem _scTuneAlarmRange;
        protected SCConfigItem _scTuneWarningTime;
        protected SCConfigItem _scTuneWarningRange;
        protected ToleranceChecker _toleranceTuneAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceTuneWarningChecker = new ToleranceChecker();
        protected SCConfigItem _scLoadAlarmTime;
        protected SCConfigItem _scLoadAlarmRange;
        protected SCConfigItem _scLoadWarningTime;
        protected SCConfigItem _scLoadWarningRange;
        protected ToleranceChecker _toleranceLoadAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceLoadWarningChecker = new ToleranceChecker();
        protected SCConfigItem _scCapAlarmTime;
        protected SCConfigItem _scCapAlarmRange;
        protected SCConfigItem _scCapWarningTime;
        protected SCConfigItem _scCapWarningRange;
        protected SCConfigItem _scRecipeIgnoreTime;
        protected SCConfigItem _scStableCriteria;
        protected ToleranceChecker _toleranceCapAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceCapWarningChecker = new ToleranceChecker();
        protected DeviceTimer _recipeIgnoreTimer = new DeviceTimer();

        protected DeviceTimer _stableTimer = new DeviceTimer();
        protected float _stableTime = 2;//second

        public virtual double CapFineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentCapFineTuningValue != 0)
                    return 1 + _currentCapFineTuningValue / 100;

                return _scCapFineTuningValue != null ? 1 + _scCapFineTuningValue.DoubleValue / 100 : 1;
            }
        }

        public virtual double TuneFineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentTuneFineTuningValue != 0)
                    return 1 + _currentTuneFineTuningValue / 100;

                return _scTuneFineTuningValue != null ? 1 + _scTuneFineTuningValue.DoubleValue / 100 : 1;
            }
        }

        public virtual double FineTuningValue
        {
            get
            {
                if (_scFineTuningEnable == null || !_scFineTuningEnable.BoolValue)
                    return 1;

                if (_currentLoadFineTuningValue != 0)
                    return 1 + _currentLoadFineTuningValue / 100;

                return _scLoadFineTuningValue != null ? 1 + _scLoadFineTuningValue.DoubleValue / 100 : 1;
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

        public virtual double TuneAlarmTime
        {
            get
            {
                if (_scTuneAlarmTime != null)
                    return _scTuneAlarmTime.DoubleValue;
                return 0;
            }
        }

        public virtual double TuneAlarmRange
        {
            get
            {
                if (_currentTuneAlarmRange > 0)
                    return _currentTuneAlarmRange;

                if (_scTuneAlarmRange != null)
                    return _scTuneAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double TuneWarningTime
        {
            get
            {
                if (_scTuneWarningTime != null)
                    return _scTuneWarningTime.DoubleValue;
                return 0;
            }
        }

        public virtual double TuneWarningRange
        {
            get
            {
                if (_currentTuneWarningRange > 0)
                    return _currentTuneWarningRange;

                if (_scTuneWarningRange != null)
                    return _scTuneWarningRange.DoubleValue;
                return 0;
            }
        }

        public virtual double LoadAlarmTime
        {
            get
            {
                if (_scLoadAlarmTime != null)
                    return _scLoadAlarmTime.DoubleValue;
                return 0;
            }
        }

        public virtual double LoadAlarmRange
        {
            get
            {
                if (_currentLoadAlarmRange > 0)
                    return _currentLoadAlarmRange;

                if (_scLoadAlarmRange != null)
                    return _scLoadAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double LoadWarningTime
        {
            get
            {
                if (_scLoadWarningTime != null)
                    return _scLoadWarningTime.DoubleValue;
                return 0;
            }
        }

        public virtual double LoadWarningRange
        {
            get
            {
                if (_currentLoadWarningRange > 0)
                    return _currentLoadWarningRange;

                if (_scLoadWarningRange != null)
                    return _scLoadWarningRange.DoubleValue;
                return 0;
            }
        }

        public virtual double CapAlarmTime
        {
            get
            {
                if (_scCapAlarmTime != null)
                    return _scCapAlarmTime.DoubleValue;
                return 0;
            }
        }

        public virtual double CapAlarmRange
        {
            get
            {
                if (_currentCapAlarmRange > 0)
                    return _currentCapAlarmRange;

                if (_scCapAlarmRange != null)
                    return _scCapAlarmRange.DoubleValue;
                return 0;
            }
        }

        public virtual double CapWarningTime
        {
            get
            {
                if (_scCapWarningTime != null)
                    return _scCapWarningTime.DoubleValue;
                return 0;
            }
        }

        public virtual double CapWarningRange
        {
            get
            {
                if (_currentCapWarningRange > 0)
                    return _currentCapWarningRange;

                if (_scCapWarningRange != null)
                    return _scCapWarningRange.DoubleValue;
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
                return false;
            }
        }

        protected RfMatchBase(string module, string name) : base(module, name, name, name)
        {
            _scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
            _scTuneAlarmTime = SC.GetConfigItem($"{Module}.{Name}.TuneAlarmTime");
            _scTuneAlarmRange = SC.GetConfigItem($"{Module}.{Name}.TuneAlarmRange");
            _scTuneWarningTime = SC.GetConfigItem($"{Module}.{Name}.TuneWarningTime");
            _scTuneWarningRange = SC.GetConfigItem($"{Module}.{Name}.TuneWarningRange");
            _scLoadAlarmTime = SC.GetConfigItem($"{Module}.{Name}.LoadAlarmTime");
            _scLoadAlarmRange = SC.GetConfigItem($"{Module}.{Name}.LoadAlarmRange");
            _scLoadWarningTime = SC.GetConfigItem($"{Module}.{Name}.LoadWarningTime");
            _scLoadWarningRange = SC.GetConfigItem($"{Module}.{Name}.LoadWarningRange");
            _scTuneFineTuningValue = SC.GetConfigItem($"{Module}.FineTuning.{Name}Tune");
            _scLoadFineTuningValue = SC.GetConfigItem($"{Module}.FineTuning.{Name}Load");
            _scFineTuningEnable = SC.GetConfigItem($"{Module}.FineTuning.IsEnable");
            _scCapAlarmTime = SC.GetConfigItem($"{Module}.{Name}.CapAlarmTime");
            _scCapAlarmRange = SC.GetConfigItem($"{Module}.{Name}.CapAlarmRange");
            _scCapWarningTime = SC.GetConfigItem($"{Module}.{Name}.CapWarningTime");
            _scCapWarningRange = SC.GetConfigItem($"{Module}.{Name}.CapWarningRange");
            _scRecipeIgnoreTime = SC.GetConfigItem($"{Module}.{Name}.RecipeIgnoreTime");
            _scStableCriteria = SC.GetConfigItem($"{Module}.{Name}.StableCriteria");
        }
        protected RfMatchBase(string module, string name, XmlElement node = null, string ioModule = "") : base(module, name, name, name)
        {
            if (node != null)
            {
                _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{Module}.{Name}.EnableAlarm");
                _scTuneAlarmTime = ParseScNode("scTuneAlarmTime", node, ioModule, $"{Module}.{Name}.TuneAlarmTime");
                _scTuneAlarmRange = ParseScNode("scTuneAlarmRange", node, ioModule, $"{Module}.{Name}.TuneAlarmRange");
                _scTuneWarningTime = ParseScNode("scTuneWarningTime", node, ioModule, $"{Module}.{Name}.TuneWarningTime");
                _scTuneWarningRange = ParseScNode("scTuneWarningRange", node, ioModule, $"{Module}.{Name}.TuneWarningRange");
                _scLoadAlarmTime = ParseScNode("scLoadAlarmTime", node, ioModule, $"{Module}.{Name}.LoadAlarmTime");
                _scLoadAlarmRange = ParseScNode("scLoadAlarmRange", node, ioModule, $"{Module}.{Name}.LoadAlarmRange");
                _scLoadWarningTime = ParseScNode("scLoadWarningTime", node, ioModule, $"{Module}.{Name}.LoadWarningTime");
                _scLoadWarningRange = ParseScNode("scLoadWarningRange", node, ioModule, $"{Module}.{Name}.LoadWarningRange");
                _scTuneFineTuningValue = ParseScNode("scTuneFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}Tune");
                _scLoadFineTuningValue = ParseScNode("scLoadFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}Load");
                _scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, $"{Module}.FineTuning.IsEnable");
                _scCapAlarmTime = ParseScNode("scCapAlarmTime", node, ioModule, $"{Module}.{Name}.CapAlarmTime");
                _scCapAlarmRange = ParseScNode("scCapAlarmRange", node, ioModule, $"{Module}.{Name}.CapAlarmRange");
                _scCapWarningTime = ParseScNode("scCapWarningTime", node, ioModule, $"{Module}.{Name}.CapWarningTime");
                _scCapWarningRange = ParseScNode("scCapWarningRange", node, ioModule, $"{Module}.{Name}.CapWarningRange");
                _scRecipeIgnoreTime = ParseScNode("scRecipeIgnoreTime", node, ioModule, $"{Module}.{Name}.RecipeIgnoreTime");
                _scStableCriteria = ParseScNode("scStableCriteria", node, ioModule, $"{Module}.{Name}.StableCriteria");
            }
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.TuneMode1", () => TuneMode1.ToString());
            DATA.Subscribe($"{Module}.{Name}.TuneMode2", () => TuneMode2.ToString());

            DATA.Subscribe($"{Module}.{Name}.LoadPosition1", () => LoadPosition1);
            DATA.Subscribe($"{Module}.{Name}.LoadPosition2", () => LoadPosition2);

            DATA.Subscribe($"{Module}.{Name}.TunePosition1", () => TunePosition1);
            DATA.Subscribe($"{Module}.{Name}.TunePosition2", () => TunePosition2);

            DATA.Subscribe($"{Module}.{Name}.Capacitance", () => Capacitance);
            DATA.Subscribe($"{Module}.{Name}.CapacitanceSetpoint", () => CapacitanceSetpoint);

            OP.Subscribe($"{Module}.{Name}.SetTuneMode1", (function, args) =>
            {
                if (!Enum.TryParse((string)args[0], out EnumRfMatchTuneMode mode))
                {
                    EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
                    return false;
                }
                SetTuneMode1(mode);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetLoad1", (function, args) =>
            {
                SetLoad1(Convert.ToSingle(args[0]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetTune1", (function, args) =>
            {
                SetTune1(Convert.ToSingle(args[0]));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetTuneMode2", (function, args) =>
            {
                if (!Enum.TryParse((string)args[0], out EnumRfMatchTuneMode mode))
                {
                    EV.PostWarningLog(Module, $"Argument {args[0]}not valid");
                    return false;
                }
                SetTuneMode2(mode);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetLoad2", (function, args) =>
            {
                SetLoad2((float)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetTune2", (function, args) =>
            {
                SetTune2((float)args[0]);
                return true;
            });


            OP.Subscribe($"{Module}.{Name}.SetActivePresetNo1", (function, args) =>
            {
                SetActivePresetNo1((int)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetActivePresetNo2", (function, args) =>
            {
                SetActivePresetNo2((int)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPreSetsAndTrajectories1", (function, args) =>
            {
                SetPreSetsAndTrajectories1((Presets)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPreSetsAndTrajectories2", (function, args) =>
            {
                SetPreSetsAndTrajectories2((Presets)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.EnablePreset1", (function, args) =>
            {
                EnablePreset1((bool)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.EnablePreset2", (function, args) =>
            {
                EnablePreset2((bool)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.EnableCapacitorMove1", (function, args) =>
            {
                EnableCapacitorMove1((bool)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.EnableCapacitorMove2", (function, args) =>
            {
                EnableCapacitorMove2((bool)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetCapacitance", (function, args) =>
            {
                SetCapacitance(Convert.ToSingle(args[0]), out string reason);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetTolerance", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var tuneWarning = Convert.ToSingle(param[0]);
                var tuneAlarm = Convert.ToSingle(param[1]);
                var loadWarning = Convert.ToSingle(param[2]);
                var loadAlarm = Convert.ToSingle(param[3]);
                SetTolerance((float)tuneWarning, (float)tuneAlarm, (float)loadWarning, (float)loadAlarm);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetCapTolerance", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var capWarning = Convert.ToSingle(param[0]);
                var capAlarm = Convert.ToSingle(param[1]);
                SetTolerance((float)capWarning, (float)capAlarm);
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
            OP.Subscribe($"{Module}.{Name}.SetCapacitance", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                if (param.Length >= 2)
                    SetCapacitance(Convert.ToSingle(param[0]), Convert.ToSingle(param[1]), out reason);
                else
                    SetCapacitance(Convert.ToSingle(param[0]), out reason);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetCapFineTuning", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetFineTuning(Convert.ToSingle(param[0]));
                return true;
            });

            InitSc();

            return true;
        }

        protected virtual void InitSc()
        {

        }

        public virtual void SetTolerance(float capWarning, float capAlarm)
        {
            _currentCapWarningRange = capWarning;
            _currentCapAlarmRange = capAlarm;
            _toleranceCapAlarmChecker.Reset(CapAlarmTime);
            _toleranceCapWarningChecker.Reset(CapWarningTime);

            if (RecipeIgnoreTime > 0)
                _recipeIgnoreTimer.Start(0);
        }

        public virtual void SetTolerance(float tuneWarning, float tuneAlarm, float loadWarning, float loadAlarm)
        {
            _currentTuneWarningRange = tuneWarning;
            _currentTuneAlarmRange = tuneAlarm;
            _toleranceTuneAlarmChecker.Reset(TuneAlarmTime);
            _toleranceTuneWarningChecker.Reset(TuneWarningTime);

            _currentLoadWarningRange = loadWarning;
            _currentLoadAlarmRange = loadAlarm;
            _toleranceLoadAlarmChecker.Reset(LoadAlarmTime);
            _toleranceLoadWarningChecker.Reset(LoadWarningTime);

            if (RecipeIgnoreTime > 0)
                _recipeIgnoreTimer.Start(0);
        }

        public virtual void SetFineTuning(float capFineTuning)
        {
            _currentCapFineTuningValue = capFineTuning;
        }

        public virtual void SetFineTuning(float tuneFineTuning, float loadFineTuning)
        {
            _currentTuneFineTuningValue = tuneFineTuning;
            _currentLoadFineTuningValue = loadFineTuning;
        }

        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || TuneMode1 == EnumRfMatchTuneMode.Auto || (RecipeIgnoreTime > 0 && _recipeIgnoreTimer.GetElapseTime() < RecipeIgnoreTime * 1000))
                return;

            if (TunePosition1Setpoint != 0)
            {
                _toleranceTuneAlarmChecker.Monitor(TunePosition1, (TunePosition1Setpoint * (1 - TuneAlarmRange / 100)), (TunePosition1Setpoint * (1 + TuneAlarmRange / 100)), TuneAlarmTime);

                _toleranceTuneWarningChecker.Monitor(TunePosition1, (TunePosition1Setpoint * (1 - TuneWarningRange / 100)), (TunePosition1Setpoint * (1 + TuneWarningRange / 100)), TuneWarningTime);
            }

            if (LoadPosition1Setpoint != 0)
            {
                _toleranceLoadAlarmChecker.Monitor(LoadPosition1, (LoadPosition1Setpoint * (1 - LoadAlarmRange / 100)), (LoadPosition1Setpoint * (1 + LoadAlarmRange / 100)), LoadAlarmTime);

                _toleranceLoadWarningChecker.Monitor(LoadPosition1, (LoadPosition1Setpoint * (1 - LoadWarningRange / 100)), (LoadPosition1Setpoint * (1 + LoadWarningRange / 100)), LoadWarningTime);
            }

            if (CapacitanceSetpoint != 0)
            {
                _toleranceCapAlarmChecker.Monitor(Capacitance, (CapacitanceSetpoint * (1 - CapAlarmRange / 100)), (CapacitanceSetpoint * (1 + CapAlarmRange / 100)), CapAlarmTime);

                _toleranceCapWarningChecker.Monitor(Capacitance, (CapacitanceSetpoint * (1 - CapWarningRange / 100)), (CapacitanceSetpoint * (1 + CapWarningRange / 100)), CapWarningTime);
            }
        }

        public virtual bool CheckTuneToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceTuneAlarmChecker.Result;
        }

        public virtual bool CheckLoadToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceLoadAlarmChecker.Result;
        }

        public virtual bool CheckTuneToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceTuneWarningChecker.Result;
        }

        public virtual bool CheckLoadToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceLoadWarningChecker.Result;
        }

        public virtual bool CheckCapToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceCapWarningChecker.Result;
        }

        public virtual bool CheckCapToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceCapAlarmChecker.Result;
        }

        public virtual void SetPreSetsAndTrajectories1(Presets presets)
        {
            throw new NotImplementedException();
        }

        public virtual void SetActivePresetNo2(int v)
        {
            throw new NotImplementedException();
        }

        public virtual void SetPreSetsAndTrajectories2(Presets presets)
        {
            throw new NotImplementedException();
        }

        public virtual void EnablePreset1(bool v)
        {
            throw new NotImplementedException();
        }

        public virtual void EnablePreset2(bool v)
        {
            throw new NotImplementedException();
        }

        public virtual void EnableCapacitorMove1(bool v)
        {
            throw new NotImplementedException();
        }

        public virtual void EnableCapacitorMove2(bool v)
        {
            throw new NotImplementedException();
        }

        public virtual void SetActivePresetNo1(int v)
        {
            throw new NotImplementedException();
        }

        public virtual void SetTuneMode1(EnumRfMatchTuneMode enumRfMatchTuneMode)
        {

        }
        public virtual void SetLoadMode1(EnumRfMatchTuneMode enumRfMatchTuneMode)
        {

        }

        public virtual void SetLoad1(float load)
        {

        }

        public virtual void SetTune1(float tune)
        {

        }

        public virtual void SetTuneMode2(EnumRfMatchTuneMode enumRfMatchTuneMode)
        {

        }
        public virtual void SetLoad2(float load)
        {

        }

        public virtual void SetTune2(float tune)
        {

        }

        public virtual void SetLoadPresetPosition(float position)
        {

        }

        public virtual void SetTunePresetPosition(float position)
        {

        }

        public virtual bool SetCapacitance(float cap, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool SetCapacitance(float cap, float rampTimes, out string reason)
        {
            reason = string.Empty;
            return true;
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