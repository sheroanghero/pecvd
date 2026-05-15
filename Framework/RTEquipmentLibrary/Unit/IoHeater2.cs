using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using MECF.Framework.Common.Event;
using System;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    ///
    ///
    /// 1个DO控制开关， 1个AI显示当前反馈。根据反馈值和设定值的差别，来自动调整开或者关
    ///
    /// AI 大于 设定值，关闭DO
    /// AI 小于 设定值, 打开DO
    /// 
    /// </summary>
    public class IoHeater2 : BaseDevice, IDevice
    {
        public bool HeaterOnSetPoint
        {
            get
            {
                if (_doOn != null)
                    return _doOn.Value;
                return false;
            }

            set
            {
                if (_doOn != null)
                    _doOn.Value = value;
            }
        }

        public float Feedback
        {
            get
            {
                return _aiFeedback.FloatValue;
            }
        }

        public string Unit
        {
            get; set;
        }

        private AITHeaterData DeviceData
        {
            get
            {
                AITHeaterData data = new AITHeaterData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    FeedBack = Feedback,

                    IsPowerOn = HeaterOnSetPoint,
                    IsPowerOnSetPoint = HeaterOnSetPoint,

                    Unit = Unit,

                    DisplayWithUnit = true,
                    FormatString = "F2",
                };

                return data;
            }
        }

        private DOAccessor _doOn = null;

        private AIAccessor _aiFeedback = null;

        private bool _isFloatAioType = false;

        private SCConfigItem _scAutoAdjustMinValue;
        private SCConfigItem _scAutoAdjustMaxValue;
        private SCConfigItem _scEnableAutoAdjust;

        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scAlarmMaxValue;
        private SCConfigItem _scAlarmMinValue;

        private bool _enableHeatUp;

        public AlarmEventItem AlarmOutOfRange { get; set; }

        private ToleranceChecker _outOfRangeChecker = new ToleranceChecker();

        public IoHeater2(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            Unit = node.GetAttribute("unit");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _aiFeedback = ParseAiNode("aiFeedback", node, ioModule);
            _doOn = ParseDoNode("doOn", node, ioModule);

            _scAutoAdjustMinValue = SC.GetConfigItem($"{scBasePath}.{Name}.AutoAdjustMinValue");
            _scAutoAdjustMaxValue = SC.GetConfigItem($"{scBasePath}.{Name}.AutoAdjustMaxValue");
            _scEnableAutoAdjust = SC.GetConfigItem($"{scBasePath}.{Name}.EnableAutoAdjust");
 
            _scEnableAlarm = SC.GetConfigItem($"{scBasePath}.{Name}.EnableAlarm");
            _scAlarmTime = SC.GetConfigItem($"{scBasePath}.{Name}.AlarmTime");
            _scAlarmMaxValue = SC.GetConfigItem($"{scBasePath}.{Name}.AlarmMaxValue");
            _scAlarmMinValue = SC.GetConfigItem($"{scBasePath}.{Name}.AlarmMinValue");

            _enableHeatUp = true;
        }

        public bool Initialize()
        {
            AlarmOutOfRange = SubscribeAlarm($"{Module}.{Name}.OutOfRangeAlarm", $"{Name} temperature out of range", ResetOutOfRangeChecker);

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.Feedback", () => Feedback);


            OP.Subscribe($"{Module}.{Name}.{AITHeaterOperation.Ramp}", (out string reason, int time, object[] param) =>
            {

                reason = "do not support set temperature";
                EV.PostWarningLog(Module, reason);

                return true;
            });

            OP.Subscribe($"{Module}.{Name}.{AITHeaterOperation.SetPowerOnOff}", (out string reason, int time, object[] param) =>
            {
                bool isOn = Convert.ToBoolean((string)param[0]);

                if (_doOn != null)
                {
                    if (!_doOn.SetValue(isOn, out reason))
                        return false;
                }

                reason = $"set {Module}.{Name} heater on";
                return true;
            });

            return true;
        }

        public void EnableControl(bool enable)
        {
            if (_enableHeatUp == enable)
                return;

            _enableHeatUp = enable;

            if (!_enableHeatUp)
            {
                EV.PostInfoLog(Module, "Disable control heater temperature");
            }
        }

        public void Stop()
        {

        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            MonitorAutoHeaterUp();

            MonitorOutOfRange();
        }

        private void MonitorAutoHeaterUp()
        {
            if (!_enableHeatUp)
            {
                HeaterOnSetPoint = false;
                return;
            }

            if (_scEnableAlarm.BoolValue && Feedback > _scAlarmMaxValue.DoubleValue)
            {
                HeaterOnSetPoint = false;
                return;
            }

            if (_scEnableAutoAdjust.BoolValue)
            {
                if (Feedback > _scAutoAdjustMaxValue.DoubleValue)
                {
                    HeaterOnSetPoint = false;
                }

                if (Feedback < _scAutoAdjustMinValue.DoubleValue)
                {
                    HeaterOnSetPoint = true;
                }
            }
        }

        private void MonitorOutOfRange()
        {
            if (!_scEnableAlarm.BoolValue)
            {
                _outOfRangeChecker.RST = true;
                AlarmOutOfRange.Reset();
                return;
            }

            _outOfRangeChecker.Monitor(Feedback, _scAlarmMinValue.DoubleValue, _scAlarmMaxValue.DoubleValue, _scAlarmTime.IntValue);
            if (_outOfRangeChecker.Trig)
            {
                AlarmOutOfRange.Description = $"{Display} out of range [{_scAlarmMinValue.DoubleValue:F1},{_scAlarmMaxValue.DoubleValue:F1}] {Unit} for {_scAlarmTime.IntValue:F0} seconds";
                AlarmOutOfRange.Set();
            }
        }

        public void Reset()
        {
            AlarmOutOfRange.Reset();
        }

        public bool ResetOutOfRangeChecker()
        {
            _outOfRangeChecker.Reset(_scAlarmTime.IntValue);

            return true;
        }
    }
}