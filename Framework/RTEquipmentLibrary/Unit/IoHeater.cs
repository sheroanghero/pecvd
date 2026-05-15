using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoHeater : BaseDevice, IDevice
    {
        public double SetPointLimit
        {
            get
            {
                if (_aoSetPointLimit != null)
                    return _isFloatAioType ? _aoSetPointLimit.FloatValue : _aoSetPointLimit.Value;

                if (_scRange != null)
                    return _scRange.DoubleValue;

                return 100;
            }
            set
            {
                if (_aoSetPointLimit != null)
                {
                    if (_isFloatAioType)
                        _aoSetPointLimit.FloatValue = (float)value;
                    else
                    {
                        _aoSetPointLimit.Value = (short)value;
                    }
                }
            }
        }

        [Subscription(AITHeaterPropertyName.MonitorTcFeedback)]
        public float MonitorTcFeedback
        {
            get
            {
                if (_aiMonitorTcFeedback == null)
                    return 0;

                float value = _isFloatAioType ? _aiMonitorTcFeedback.FloatValue : _aiMonitorTcFeedback.Value;

                float calibrated = CalibrationData(value, false);
                if (calibrated < 0)
                    calibrated = 0;

                return calibrated;
            }
        }

        [Subscription(AITHeaterPropertyName.ControlTcFeedback)]
        public float ControlTcFeedback
        {
            get
            {
                if (_aiControlTcFeedback == null)
                    return 0;

                float value = _isFloatAioType ? _aiControlTcFeedback.FloatValue : _aiControlTcFeedback.Value;

                float calibrated = CalibrationData(value, false);
                if (calibrated < 0)
                    calibrated = 0;

                return calibrated;
            }
        }

        [Subscription(AITHeaterPropertyName.ControlTcSetPoint)]
        public float ControlTcSetPoint
        {
            get
            {
                if (_aoSetPoint == null)
                    return 0;

                float value = _isFloatAioType ? _aoSetPoint.FloatValue : _aoSetPoint.Value;

                float calibrated = CalibrationData(value, false);
                if (calibrated < 0)
                    calibrated = 0;

                return calibrated;
            }
            set
            {
                if (_aoSetPoint != null)
                {
                    float calibrated = CalibrationData(value, true);
                    if (calibrated < 0)
                        calibrated = 0;
 
                    if (_isFloatAioType)
                        _aoSetPoint.FloatValue = (float)calibrated;
                    else
                    {
                        _aoSetPoint.Value = (short)calibrated;
                    }
                }
            }
        }

        [Subscription(AITHeaterPropertyName.IsMonitorTcBroken)]
        public bool IsMonitorTcBroken
        {
            get
            {
                if (_diMonitorTcBroken != null)
                    return _diMonitorTcBroken.Value;

                return false;
            }
        }

        [Subscription(AITHeaterPropertyName.IsControlTcBroken)]
        public bool IsControlTcBroken
        {
            get
            {
                if (_diControlTcBroken != null)
                    return _diControlTcBroken.Value;

                return false;
            }
        }

        [Subscription(AITHeaterPropertyName.IsPowerOnFeedback)]
        public bool IsPowerOnFeedback
        {
            get
            {
                if (_diPowerOnFeedback != null)
                    return _diPowerOnFeedback.Value;

                if (_doPowerOn != null)
                    return _doPowerOn.Value;

                return false;
            }
        }

        [Subscription(AITHeaterPropertyName.IsPowerOnSetPoint)]
        public bool IsPowerOnSetPoint
        {
            get
            {
                if (_doPowerOn != null)
                    return _doPowerOn.Value;

                return false;
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

                    FeedBack = ControlTcFeedback,
                    MonitorTcFeedBack = MonitorTcFeedback,

                    Scale = SetPointLimit,
                    SetPoint = ControlTcSetPoint,

                    IsPowerOn = IsPowerOnFeedback,
                    IsPowerOnSetPoint = IsPowerOnSetPoint,

                    IsControlTcBroken = IsControlTcBroken,
                    IsMonitorTcBroken = IsMonitorTcBroken,

                    Unit = Unit,

                    DisplayWithUnit = true,
                    FormatString = "F2",
                };

                return data;
            }
        }

        private DIAccessor _diPowerOnFeedback = null;
        private DIAccessor _diControlTcBroken;
        private DIAccessor _diMonitorTcBroken;

        private DOAccessor _doPowerOn = null;

        private AIAccessor _aiControlTcFeedback = null;
        private AIAccessor _aiMonitorTcFeedback = null;

        private AOAccessor _aoSetPoint = null;
        private AOAccessor _aoSetPointLimit = null;

        private R_TRIG _trigMonitorTcBroken = new R_TRIG();
        private R_TRIG _trigControlTcBroken = new R_TRIG();

        private bool _isFloatAioType = false;

        private SCConfigItem _scRange;

        private SCConfigItem _scEnableCalibration;
        private SCConfigItem _scCalibrationTable;

        private List<CalibrationItem> _calibrationTable = new List<CalibrationItem>();
        private string _previousSetting;

        public IoHeater(string module, XmlElement node, string ioModule = "")
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

            _diPowerOnFeedback = ParseDiNode("diPowerOnFeedback", node, ioModule);
            _diControlTcBroken = ParseDiNode("diControlTcBroken", node, ioModule);
            _diMonitorTcBroken = ParseDiNode("diMonitorTcBroken", node, ioModule);

            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);

            _aiControlTcFeedback = ParseAiNode("aiFeedback", node, ioModule);
            _aiMonitorTcFeedback = ParseAiNode("aiMonitor", node, ioModule);

            _aoSetPoint = ParseAoNode("aoSetPoint", node, ioModule);
            _aoSetPointLimit = ParseAoNode("aoSetPointLimit", node, ioModule);

            _scRange = ParseScNode("scRange", node, ioModule, $"{scBasePath}.{Name}.Range");

            _scEnableCalibration = SC.GetConfigItem($"{scBasePath}.{Name}.EnableCalibration");
            _scCalibrationTable = SC.GetConfigItem($"{scBasePath}.{Name}.CalibrationTable");
        }

        public void UpdateConfig(double setPointLimit)
        {
            SetPointLimit = setPointLimit;
        }


        public bool Initialize()
        {
            DATA.Subscribe(string.Format("Device.{0}.{1}", Module, Name), () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DEVICE.Register(String.Format("{0}.{1}", Name, AITHeaterOperation.SetPowerOnOff), (out string reason, int time, object[] param) =>
                                                                                              {
                                                                                                  reason = "";
                                                                                                  bool isEnable = Convert.ToBoolean((string)param[0]);

                                                                                                  bool result = true;
                                                                                                  if (_doPowerOn != null)
                                                                                                  {
                                                                                                      result &= _doPowerOn.SetValue(isEnable, out reason);
                                                                                                  }

                                                                                                  if (result)
                                                                                                      reason = string.Format("Set Heater {0} Power {1}", Name, isEnable ? "On" : "Off");

                                                                                                  return result;
                                                                                              });

            DEVICE.Register(String.Format("{0}.{1}", Name, AITHeaterOperation.Ramp), (out string reason, int time, object[] param) =>
            {
                float setpoint = (float)Convert.ToDouble((string)param[0]);

                if (setpoint > SetPointLimit || setpoint < 0)
                {
                    reason = string.Format("Heater {0} temperature setpoint {1} is not valid, should be (0, {2})", Display, setpoint, SetPointLimit);
                    return false;
                }

                ControlTcSetPoint = setpoint;

                reason = string.Format("Heater {0} Set to {1}", Display, setpoint);

                return true;
            });


            OP.Subscribe($"{Module}.{Name}.{AITHeaterOperation.Ramp}", (out string reason, int time, object[] param) =>
            {
                float setpoint = (float)Convert.ToDouble((string)param[0]);

                if (setpoint > SetPointLimit || setpoint < 0)
                {
                    reason = string.Format("Heater {0} temperature setpoint {1} is not valid, should be (0, {2})", Display, setpoint, SetPointLimit);
                    return false;
                }

                ControlTcSetPoint = setpoint;

                reason = string.Format("Heater {0} Set to {1}", Display, setpoint);

                return true;
            });


            UpdateCalibrationTable();

            return true;
        }

        public void Stop()
        {
            ControlTcSetPoint = 0;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            _trigControlTcBroken.CLK = IsControlTcBroken;
            if (_trigControlTcBroken.Q)
            {
                EV.PostMessage(Module, EventEnum.DefaultAlarm, string.Format("{0}, found control TC broken", Display));
            }

            _trigMonitorTcBroken.CLK = IsMonitorTcBroken;
            if (_trigMonitorTcBroken.Q)
            {
                EV.PostMessage(Module, EventEnum.DefaultAlarm, string.Format("{0}, found monitor TC broken", Display));
            }
        }

        public void Reset()
        {
            _trigControlTcBroken.RST = true;
            _trigMonitorTcBroken.RST = true;

            UpdateCalibrationTable();
        }


        public void UpdateCalibrationTable()
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
                    float maxValue = _scRange != null ? (float)_scRange.DoubleValue : table[i].Item2 * 2;

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


        private float CalibrationData(float value, bool output)
        {
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
                if (item != null && item.CalibrationTo == 0 && item.CalibrationFrom == 0 && value > 0)
                    item = _calibrationTable[_calibrationTable.Count - 1];
                if (item != null && Math.Abs(item.CalibrationTo - item.CalibrationFrom) > 0.01)
                {
                    var slope = (item.RawTo - item.RawFrom) / (item.CalibrationTo - item.CalibrationFrom);
                    ret = (ret - item.CalibrationFrom) * slope + item.RawFrom;
                }
            }

            if (ret < 0)
                return 0;

            if (ret >= float.MaxValue || (_scRange != null && ret > _scRange.DoubleValue))
                ret = value;

            return ret;
        }
    }
}