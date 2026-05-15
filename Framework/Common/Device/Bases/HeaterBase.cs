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

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class HeaterBase : BaseDevice, IDevice
    {      
        public virtual bool IsPowerOn { get; set; }

        public virtual float TempSetPoint { get; set; }
        public virtual float TempFeedback { get; set; }
 
        public virtual AITHeaterData DeviceData { get; set; }

        private float _currentWarningRange;
        private float _currentAlarmRange;
        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scAlarmTime;
        protected SCConfigItem _scAlarmRange;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;
        protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

        protected double _currentFineTuningValue;
        protected SCConfigItem _scFineTuningEnable;
        protected SCConfigItem _scFineTuningValue;

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
                    return _scAlarmTime.DoubleValue;
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

        protected HeaterBase( ) : base( )
        {

        }
        protected HeaterBase(string module, string name, XmlElement node = null, string ioModule = "") : base(module, name, name, name)
        {
            if (node != null)
            {
                _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{Module}.{Name}.EnableAlarm");
                _scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, $"{Module}.{Name}.AlarmTime");
                _scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, $"{Module}.{Name}.AlarmRange");
                _scWarningTime = ParseScNode("scWarningTime", node, ioModule, $"{Module}.{Name}.WarningTime");
                _scWarningRange = ParseScNode("scWarningRange", node, ioModule, $"{Module}.{Name}.WarningRange");
                _scFineTuningValue = ParseScNode("scFineTuningValue", node, ioModule, $"{Module}.FineTuning.{Name}");
                _scFineTuningEnable = ParseScNode("scFineTuningEnable", node, ioModule, $"{Module}.FineTuning.IsEnable");
            }
        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.TempFeedback", () => TempFeedback);
            DATA.Subscribe($"{Module}.{Name}.TempSetPoint", () => TempSetPoint);

            DATA.Subscribe($"{Module}.{Name}.IsPowerOn", () => IsPowerOn);


            OP.Subscribe($"{Module}.{Name}.SetPowerOn", (function, args) =>
            {
                SetPowerOnOff(true);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOff", (function, args) =>
            {
                SetPowerOnOff(false);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOnOff", (function, args) =>
            {
                bool.TryParse(args[0].ToString(), out bool isOn);
                SetPowerOnOff(isOn);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetTemperature", (function, args) =>
            {
                SetTemperature((float)args[0]);
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

            return true;
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
        }

        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || TempSetPoint == 0)
                return;

            _toleranceAlarmChecker.Monitor(TempFeedback, (TempSetPoint * (1 - AlarmRange / 100)), (TempSetPoint * (1 + AlarmRange / 100)), AlarmTime);

            _toleranceWarningChecker.Monitor(TempFeedback, (TempSetPoint * (1 - WarningRange / 100)), (TempSetPoint * (1 + WarningRange / 100)), WarningTime);
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

        public virtual void SetPowerOnOff(bool isOn)
        {
             
        }

        public virtual void SetTemperature(float temperature)
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
            _toleranceAlarmChecker.Reset(AlarmTime);
            _toleranceWarningChecker.Reset(WarningTime);
        }
    }
}