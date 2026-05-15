using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoMicrowave : BaseDevice, IDevice
    {
        public float PowerRange
        {
            get
            {
                return _scPowerRange == null ? 99999 : (float)_scPowerRange.IntValue;
            }
        }

        public float ScaleFrequency
        {
            get
            {
                return 1000;
            }
        }



        public string UnitPower
        {
            get
            {
                return "W";
            }
        }

        public bool IsRfOn    //True：on
        {
            get
            {
                if (_diStatus != null)
                    return _diStatus.Value;

                return _doPowerOn.Value;
            }
        }

        public float PowerSetPoint
        {
            get
            {
                float tuned = _isFloatAioType ? GetTuneValue(_aoPower.FloatValue, false) : GetTuneValue(_aoPower.Value, false);
                float calibrated = CalibrationPower(tuned, false);
                if (calibrated < 0)
                    calibrated = 0;

                return calibrated;
            }
            set
            {
                float tuned = GetTuneValue(value, true);
                float calibrated = CalibrationPower(tuned, true);
                if (calibrated < 0)
                    calibrated = 0;

                if (_isFloatAioType)
                    _aoPower.FloatValue = calibrated;
                else
                    _aoPower.Value = (short)calibrated;
            }
        }

        public float ImpulseSetPoint
        {
            get
            {
                if (_aoImpulseTime == null)
                    return 0;
                return _isFloatAioType ? _aoImpulseTime.FloatValue : _aoImpulseTime.Value;
            }
            set
            {
                if (_aoImpulseTime == null)
                    return;

                if (_isFloatAioType)
                    _aoImpulseTime.FloatValue = value;
                else
                    _aoImpulseTime.Value = (short)value;
            }
        }

        public float PauseTimeSetPoint
        {
            get
            {
                if (_aoPauseTime == null)
                    return 0;

                return _isFloatAioType ? _aoPauseTime.FloatValue : _aoPauseTime.Value;
            }
            set
            {
                if (_aoPauseTime == null)
                    return;

                if (_isFloatAioType)
                    _aoPauseTime.FloatValue = value;
                else
                    _aoPauseTime.Value = (short)value;
            }
        }

        public float RFForwardPower
        {
            get
            {
                if (_aiForwardPower == null)
                    return 0;


                float tuned = _isFloatAioType
                    ? GetTuneValue(_aiForwardPower.FloatValue, false)
                    : GetTuneValue(_aiForwardPower.Value, false);

                float calibrated = CalibrationPower(tuned, false);
                if (calibrated < 0)
                    calibrated = 0;

                return calibrated;
            }
        }

        public float RFReflectPower
        {
            get
            {
                return _aiReflectedPower == null ? 0 : (_isFloatAioType ? GetTuneValue(_aiReflectedPower.FloatValue, false) : GetTuneValue(_aiReflectedPower.Value, false));
            }
        }

        public bool RFInterlock
        {
            get
            {
                return _diIntlk == null || _diIntlk.Value;
            }
        }

        public bool MainPowerOnSetPoint
        {
            get
            {
                if (_doPowerOn != null)
                    return _doPowerOn.Value;
                return false;
            }
            set
            {
                if (_doPowerOn != null)
                {
                    if (!_doPowerOn.SetValue(value, out string reason))
                    {
                        LOG.Write(reason);
                    }

                }
            }
        }

        public bool HeatOnSetPoint
        {
            get
            {
                if (_doHeaterOn != null)
                    return _doHeaterOn.Value;
                return false;
            }
            set
            {
                if (_doHeaterOn != null)
                {
                    if (!_doHeaterOn.SetValue(value, out string reason))
                    {
                        LOG.Write(reason);
                    }

                }
            }
        }

        public bool HeatOnComplete
        {
            get
            {
                if (_diHeatComplete != null)
                    return _diHeatComplete.Value;
                return false;
            }
        }

        private DateTime _powerOnStartTime;
        private TimeSpan _powerOnElapsedTime;
        public string PowerOnTime
        {
            get
            {
                if (IsRfOn)
                    _powerOnElapsedTime = DateTime.Now - _powerOnStartTime;

                return string.Format("{0}:{1}:{2}", ((int)_powerOnElapsedTime.TotalHours).ToString("00"),
                    _powerOnElapsedTime.Minutes.ToString("00"), (_powerOnElapsedTime.Seconds > 0 ? (_powerOnElapsedTime.Seconds + 1) : 0).ToString("00"));
            }
        }

        public int ErrorCode
        {
            get
            {
                return _isFloatAioType ? (int)_aiErrorCode.FloatValue : _aiErrorCode.Value;
            }
        }

        private Dictionary<int, string> _errorCodeMap = new Dictionary<int, string>()
        {
            {0, "No Error"},
            {128, "Communication error"},
            {129, "Software interlock error"},
            {130, "Hardware interlock error"},
            {131, "Cooling error power supply and magnetron head"},
            {134, "Anode current error"},
            {135, "Anode voltage error"},
            {137, "Low-power error"},
            {138, "ARC fault"},
            {255, "Service error or CANbus error"},
        };

        public string ErrorCodeString
        {
            get
            {
                if (_errorCodeMap.ContainsKey(ErrorCode))
                    return _errorCodeMap[ErrorCode];
                return "";
            }
        }

        public bool IsAlarm
        {
            get
            {
                return _diAlarm.Value || _diGroundFault.Value || _diOverVoltageCurrent.Value || _diCoolingError.Value ||
                       _diInterlockFault.Value;
            }
        }

        public bool ResetErrorSetPoint
        {
            get
            {
                return _doResetError != null && _doResetError.Value;
            }
            set
            {
                if (_doResetError != null)
                    _doResetError.Value = value;
            }
        }

        public string AlarmText
        {
            get
            {
                if (!IsAlarm)
                    return "";
                string result = "";
                if (_diGroundFault.Value) result += "Ground Fault;";
                if (_diOverVoltageCurrent.Value) result += "Over Voltage Current;";
                if (_diCoolingError.Value) result += "Cooling Error;";
                if (_diInterlockFault.Value) result += "Interlock Fault;";
                if (ErrorCode > 0)
                    result += ErrorCodeString;
                return result;
            }
        }

        private AITRfData DeviceData
        {
            get
            {
                AITRfData data = new AITRfData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    ForwardPower = RFForwardPower,
                    ReflectPower = RFReflectPower,
                    IsInterlockOk = RFInterlock,
                    IsRfOn = IsRfOn,
                    IsRfAlarm = IsAlarm,

                    PowerSetPoint = PowerSetPoint,

                    ScalePower = PowerRange,

                    UnitPower = UnitPower,

                    PowerOnElapsedTime = PowerOnTime,

                    EnablePulsing = false,
                    EnableVoltageCurrent = false,

                    IsToleranceError = !AlarmPowerToleranceAlarm.IsAcknowledged || !AlarmReflectPowerToleranceAlarm.IsAcknowledged,
                    IsToleranceWarning = !AlarmPowerToleranceWarning.IsAcknowledged || !AlarmReflectPowerToleranceWarning.IsAcknowledged,

                    WorkMode = (int)RfMode.ContinuousWaveMode,
                };

                data.AttrValue["HeatOnSetPoint"] = HeatOnSetPoint;
                data.AttrValue["HeatOnComplete"] = HeatOnComplete;

                return data;
            }
        }


        private DIAccessor _diStatus = null;
        private DIAccessor _diIntlk = null;
        private DIAccessor _diAlarm;
        private DIAccessor _diHeatComplete;
        private DIAccessor _diControlVoltageOn;
        private DIAccessor _diGroundFault;
        private DIAccessor _diOverVoltageCurrent;
        private DIAccessor _diCoolingError;
        private DIAccessor _diInterlockFault;

        private DOAccessor _doPowerOn = null;
        private DOAccessor _doOn = null;
        private DOAccessor _doHeaterOn = null;
        private DOAccessor _doResetError = null;

        private AIAccessor _aiReflectedPower = null;
        private AIAccessor _aiForwardPower = null;
        private AIAccessor _aiErrorCode = null;

        private AOAccessor _aoPower = null;
        private AOAccessor _aoImpulseTime = null;
        private AOAccessor _aoPauseTime = null;

        //SC
        private SCConfigItem _scEnablePowerAlarm;
        private SCConfigItem _scPowerAlarmRange = null;
        private SCConfigItem _scPowerAlarmTime = null;
        private SCConfigItem _scPowerWarningRange = null;
        private SCConfigItem _scPowerWarningTime = null;

        private SCConfigItem _scEnableReflectPowerAlarm;
        private SCConfigItem _scReflectPowerAlarmRange = null;
        private SCConfigItem _scReflectPowerAlarmTime = null;
        private SCConfigItem _scReflectPowerWarningRange = null;
        private SCConfigItem _scReflectPowerWarningTime = null;

        private SCConfigItem _scPowerRange;
        private SCConfigItem _scCoefficient;
        private SCConfigItem _scPowerOnTimeout;

        private SCConfigItem _scImpulseTime;
        private SCConfigItem _scPauseTime;


        private SCConfigItem _scPowerTuneEnable;
        private SCConfigItem _scPowerTuneIsPercent;
        private SCConfigItem _scPowerTuneTable;

        private SCConfigItem[] _scPowerCalibrationTable;
        private SCConfigItem _scPowerCalibrationEnable;

        private F_TRIG _trigInterlock = new F_TRIG();
        private R_TRIG _trigOn = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();

        readonly DeviceTimer _powerOnTimer = new DeviceTimer();

        private const string RFHighReflect = "RFHighReflect";
        private const string RFHardwareInterlock = "RFHardwareInterlock";
        private const string RFOutOfTolerance = "RFOutOfTolerance";

        public AlarmEventItem AlarmPowerOnFailed { get; set; }

        public AlarmEventItem AlarmPowerToleranceWarning { get; set; }
        public AlarmEventItem AlarmPowerToleranceAlarm { get; set; }

        public AlarmEventItem AlarmReflectPowerToleranceWarning { get; set; }
        public AlarmEventItem AlarmReflectPowerToleranceAlarm { get; set; }

        public AlarmEventItem AlarmDeviceAlarm { get; set; }

        private ToleranceChecker _checkerPowerWarning;
        private ToleranceChecker _checkerPowerAlarm;
        private ToleranceChecker _checkerReflectPowerWarning;
        private ToleranceChecker _checkerReflectPowerAlarm;

        private bool _isFloatAioType = false;

        private Stopwatch _timerResetError = new Stopwatch();
        private SCConfigItem _scResetErrorTimeout;

        private List<RfTuneItem> _tuneTable;

        //calibration
        private List<CalbrationParas> _calibrationFormla;
        private List<RFCalibrationItem> _rfCalibrationDatas = new List<RFCalibrationItem>();
        private const int TenPoints = 10;

        public IoMicrowave(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }
            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

            _diStatus = ParseDiNode("diOn", node, ioModule);
            _diIntlk = ParseDiNode("diInterlock", node, ioModule);
            _diAlarm = ParseDiNode("diAlarm", node, ioModule);
            _diHeatComplete = ParseDiNode("diHeatComplete", node, ioModule);
            _diControlVoltageOn = ParseDiNode("diControlVoltageOn", node, ioModule);
            _diGroundFault = ParseDiNode("diGroundFault", node, ioModule);
            _diOverVoltageCurrent = ParseDiNode("diOverVoltageCurrent", node, ioModule);
            _diCoolingError = ParseDiNode("diCoolingError", node, ioModule);
            _diInterlockFault = ParseDiNode("diInterlockFault", node, ioModule);

            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doOn = ParseDoNode("doOn", node, ioModule);
            _doHeaterOn = ParseDoNode("doHeaterOn", node, ioModule);
            _doResetError = ParseDoNode("doReset", node, ioModule);

            _aiReflectedPower = ParseAiNode("aiReflectPower", node, ioModule);
            _aiForwardPower = ParseAiNode("aiForwardPower", node, ioModule);
            _aiErrorCode = ParseAiNode("aiErrorCode", node, ioModule);

            _aoPower = ParseAoNode("aoPower", node, ioModule);
            _aoImpulseTime = ParseAoNode("aoImpulseTime", node, ioModule);
            _aoPauseTime = ParseAoNode("aoPauseTime", node, ioModule);

            _scEnablePowerAlarm = ParseScNode("scEnablePowerAlarm", node, ioModule, $"{scBasePath}.{Name}.EnablePowerAlarm");
            _scPowerAlarmRange = ParseScNode("scPowerAlarmRange", node, ioModule, $"{scBasePath}.{Name}.PowerAlarmRange");
            _scPowerAlarmTime = ParseScNode("scPowerAlarmTime", node, ioModule, $"{scBasePath}.{Name}.PowerAlarmTime");
            _scPowerWarningRange = ParseScNode("scPowerWarningRange", node, ioModule, $"{scBasePath}.{Name}.PowerWarningRange");
            _scPowerWarningTime = ParseScNode("scPowerWarningTime", node, ioModule, $"{scBasePath}.{Name}.PowerWarningTime");

            _scEnableReflectPowerAlarm = ParseScNode("scEnableReflectPowerAlarm", node, ioModule, $"{scBasePath}.{Name}.EnableReflectPowerAlarm");
            _scReflectPowerAlarmRange = ParseScNode("scReflectPowerAlarmRange", node, ioModule, $"{scBasePath}.{Name}.ReflectPowerAlarmRange");
            _scReflectPowerAlarmTime = ParseScNode("scReflectPowerAlarmTime", node, ioModule, $"{scBasePath}.{Name}.ReflectPowerAlarmTime");
            _scReflectPowerWarningRange = ParseScNode("scReflectPowerWarningRange", node, ioModule, $"{scBasePath}.{Name}.ReflectPowerWarningRange");
            _scReflectPowerWarningTime = ParseScNode("scReflectPowerWarningTime", node, ioModule, $"{scBasePath}.{Name}.ReflectPowerWarningTime");

            _scPowerRange = ParseScNode("scPowerRange", node, ioModule, $"{scBasePath}.{Name}.PowerRange");
            _scCoefficient = ParseScNode("scPowerCoefficient", node, ioModule, $"{scBasePath}.{Name}.PowerCoefficient");

            _scPowerOnTimeout = ParseScNode("scPowerOnTimeout", node, ioModule, $"{scBasePath}.{Name}.PowerOnTimeout");

            _scResetErrorTimeout = ParseScNode("scResultErrorTimeout", node, ioModule, $"{scBasePath}.{Name}.ResetErrorTimeout");

            _scImpulseTime = ParseScNode("scImpulseTime", node, ioModule, $"{scBasePath}.{Name}.ImpulseTime");
            _scPauseTime = ParseScNode("scPauseTime", node, ioModule, $"{scBasePath}.{Name}.PauseTime");


            _scPowerTuneEnable = SC.GetConfigItem($"{scBasePath}.{Name}.PowerTuneEnable");
            _scPowerTuneIsPercent = SC.GetConfigItem($"{scBasePath}.{Name}.PowerTuneIsPercent");
            _scPowerTuneTable = SC.GetConfigItem($"{scBasePath}.{Name}.PowerTuneTable");

            _scPowerCalibrationTable = new SCConfigItem[10];
            for (int i = 0; i < _scPowerCalibrationTable.Length; i++)
            {
                _scPowerCalibrationTable[i] = SC.GetConfigItem($"{scBasePath}.{Name}.Rate{i + 1}MeterPower");
            }
            _scPowerCalibrationEnable = SC.GetConfigItem($"{scBasePath}.{Name}.PowerCalibrationEnable");


        }

        public bool Initialize()
        {
            UpdateTuneTable();
            InitializeCalalibration();

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.PowerSetPoint", () => PowerSetPoint);
            DATA.Subscribe($"{Module}.{Name}.ForwardPower", () => RFForwardPower);
            DATA.Subscribe($"{Module}.{Name}.ReflectPower", () => RFReflectPower);
            DATA.Subscribe($"{Module}.{Name}.IsRfOn", () => _diStatus.Value);
            DATA.Subscribe($"{Module}.{Name}.IsInterlockOK", () => RFInterlock);
            DATA.Subscribe($"{Module}.{Name}.RfOnSetPoint", () => _doOn.Value);
            DATA.Subscribe($"{Module}.{Name}.IsAlarm", () => _diAlarm.Value);

            OP.Subscribe($"{Module}.{Name}.{AITRfOperation.SetPowerOnOff}", SetPowerOnOff);
            OP.Subscribe($"{Module}.{Name}.{AITRfOperation.SetPower}", SetPower);
            OP.Subscribe($"{Module}.{Name}.{AITRfOperation.SetHeatOnOff}", SetHeatOnOff);

            AlarmPowerToleranceWarning = SubscribeAlarm($"{Module}.{Name}.PowerToleranceWarning", "", ResetPowerWarningChecker, EventLevel.Warning);
            AlarmPowerToleranceAlarm = SubscribeAlarm($"{Module}.{Name}.PowerToleranceAlarm", "", ResetPowerAlarmChecker);

            AlarmReflectPowerToleranceWarning = SubscribeAlarm($"{Module}.{Name}.ReflectPowerToleranceWarning", "", ResetReflectPowerWarningChecker, EventLevel.Warning);
            AlarmReflectPowerToleranceAlarm = SubscribeAlarm($"{Module}.{Name}.ReflectPowerToleranceAlarm", "", ResetReflectPowerAlarmChecker);

            AlarmPowerOnFailed = SubscribeAlarm($"{Module}.{Name}.PowerOnFailed", $"Failed turn {Display} on", null);
            AlarmDeviceAlarm = SubscribeAlarm($"{Module}.{Name}.DeviceAlarm", $"{Display} alarmed", ResetDeviceAlarmChecker);


            _checkerPowerWarning = new ToleranceChecker(_scPowerAlarmTime.IntValue);
            _checkerPowerAlarm = new ToleranceChecker(_scReflectPowerAlarmTime.IntValue);

            _checkerReflectPowerWarning = new ToleranceChecker(_scPowerAlarmTime.IntValue);
            _checkerReflectPowerAlarm = new ToleranceChecker(_scReflectPowerAlarmTime.IntValue);

            return true;
        }

        private bool SetPowerOnOff(out string reason, int time, object[] param)
        {
            return SetPowerOnOff(Convert.ToBoolean((string)param[0]), out reason);
        }


        public bool SetHeatOnOff(bool isOn, out string reason)
        {
            HeatOnSetPoint = isOn;
            reason = string.Empty;
            return true;
        }

        private bool SetHeatOnOff(out string reason, int time, object[] param)
        {
            HeatOnSetPoint = Convert.ToBoolean((string)param[0]);
            reason = string.Empty;
            return true;
        }

        public bool SetMainPowerOnOff(bool isOn, out string reason)
        {
            MainPowerOnSetPoint = isOn;
            reason = string.Empty;
            return true;
        }

        private bool SetPower(out string reason, int time, object[] param)
        {
            reason = string.Empty;

            float power = (float)Convert.ToDouble((string)param[0]);

            if (power < 0 || power > PowerRange)
            {
                reason = $"{Name} setpoint value {power} is out of power range, should between 0 and {PowerRange}";
                EV.PostWarningLog(Module, reason);
                return false;
            }

            PowerSetPoint = power;

            reason = $"{Name} set power to {power}";

            EV.PostInfoLog(Module, reason);

            return true;
        }


        public void Stop()
        {
            string reason = String.Empty;

            _aoPower.Value = 0;
        }

        public void Terminate()
        {
        }


        public void Monitor()
        {
            try
            {
                if (!IsRfOn)
                {
                    if (_scImpulseTime != null)
                        ImpulseSetPoint = _scImpulseTime.IntValue;
                    if (_scPauseTime != null)
                        PauseTimeSetPoint = _scPauseTime.IntValue;
                }

                if (_powerOnTimer.IsTimeout() && !_diStatus.Value)
                {
                    AlarmPowerOnFailed.Description =
                        $"Can not turn {Display} power on in {(int)(_powerOnTimer.GetElapseTime() / 1000)} seconds";
                    AlarmPowerOnFailed.Set();
                    SetPowerOnOff(false, out string _);
                    _powerOnTimer.Stop();
                }

                _trigOn.CLK = IsRfOn;

                if (_trigOn.Q)
                {
                    _powerOnStartTime = DateTime.Now;

                    _checkerPowerAlarm.Reset(_scPowerAlarmTime.IntValue);
                    _checkerPowerWarning.Reset(_scPowerWarningTime.IntValue);
                    _checkerReflectPowerAlarm.Reset(_scReflectPowerAlarmTime.IntValue);
                    _checkerReflectPowerWarning.Reset(_scReflectPowerWarningTime.IntValue);
                }

                if (_trigOn.M)
                {
                    _checkerPowerWarning.Monitor(RFForwardPower, PowerSetPoint - _scPowerWarningRange.IntValue, PowerSetPoint + _scPowerWarningRange.IntValue, _scPowerWarningTime.IntValue);
                    if (_checkerPowerWarning.Trig)
                    {
                        AlarmPowerToleranceWarning.Description = $"{Display} Forward power {RFForwardPower} out of range[{(PowerSetPoint - _scPowerWarningRange.IntValue)},{(PowerSetPoint + _scPowerWarningRange.IntValue)}] in {_scPowerWarningTime.IntValue} seconds";
                        AlarmPowerToleranceWarning.Set();
                    }

                    _checkerPowerAlarm.Monitor(RFForwardPower, PowerSetPoint - _scPowerAlarmRange.IntValue, PowerSetPoint + _scPowerAlarmRange.IntValue, _scPowerAlarmTime.IntValue);
                    if (_checkerPowerAlarm.Trig)
                    {
                        AlarmPowerToleranceAlarm.Description = $"{Display} Forward power {RFForwardPower} out of range[{(PowerSetPoint - _scPowerAlarmRange.IntValue)},{(PowerSetPoint + _scPowerAlarmRange.IntValue)}] in {_scPowerAlarmTime.IntValue} seconds";
                        AlarmPowerToleranceAlarm.Set();

                        SetPowerOnOff(false, out _);
                    }

                    _checkerReflectPowerWarning.Monitor(RFReflectPower, 0, _scReflectPowerWarningRange.IntValue, _scReflectPowerWarningTime.IntValue);
                    if (_checkerReflectPowerWarning.Trig)
                    {
                        AlarmReflectPowerToleranceWarning.Description = $"{Display} reflect power {RFReflectPower} out of range[0,{_scReflectPowerWarningRange.IntValue}] in {_scReflectPowerWarningTime.IntValue} seconds";
                        AlarmReflectPowerToleranceWarning.Set();
                    }

                    _checkerReflectPowerAlarm.Monitor(RFReflectPower, 0, _scReflectPowerAlarmRange.IntValue, _scReflectPowerAlarmTime.IntValue);
                    if (_checkerReflectPowerAlarm.Trig)
                    {
                        AlarmReflectPowerToleranceAlarm.Description = $"{Display} reflect power {RFReflectPower} out of range[0,{_scReflectPowerAlarmRange.IntValue}] in {_scReflectPowerAlarmTime.IntValue} seconds";
                        AlarmReflectPowerToleranceAlarm.Set();

                        SetPowerOnOff(false, out _);
                    }
                }

                _trigError.CLK = IsAlarm;
                if (_trigError.Q)
                {
                    AlarmDeviceAlarm.Set(AlarmText);
                    SetPowerOnOff(false, out string _);
                }

                if (ResetErrorSetPoint)
                {
                    if (!IsAlarm)
                    {
                        AlarmDeviceAlarm.Reset();
                        ResetErrorSetPoint = false;
                        _timerResetError.Stop();
                    }

                    if (IsAlarm && _timerResetError.IsRunning &&
                        _timerResetError.ElapsedMilliseconds > _scResetErrorTimeout.IntValue * 1000)
                    {
                        _timerResetError.Stop();
                        EV.PostWarningLog(Module, $"Can not reset {Display} error in {_scResetErrorTimeout.IntValue} seconds");
                        ResetErrorSetPoint = false;
                    }
                }

 
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }

        public void Reset()
        {
            _trigInterlock.RST = true;
            _trigError.RST = true;
            _trigOn.RST = true;

            AlarmDeviceAlarm.Reset();
            AlarmPowerOnFailed.Reset();

            AlarmPowerToleranceAlarm.Reset();
            AlarmPowerToleranceWarning.Reset();

            AlarmReflectPowerToleranceAlarm.Reset();
            AlarmReflectPowerToleranceWarning.Reset();
        }

        public bool ResetDeviceAlarmChecker()
        {
            if (IsAlarm)
            {
                if (!ResetErrorSetPoint)
                {
                    _timerResetError.Restart();
                    ResetErrorSetPoint = true;
                }
            }

            return !IsAlarm;
        }

        public bool ResetPowerWarningChecker()
        {
            _checkerPowerWarning.Reset(_scPowerWarningTime.IntValue);

            return true;
        }

        public bool ResetPowerAlarmChecker()
        {
            _checkerPowerAlarm.Reset(_scPowerAlarmTime.IntValue);

            return true;
        }

        public bool ResetReflectPowerWarningChecker()
        {
            _checkerReflectPowerWarning.Reset(_scReflectPowerWarningTime.IntValue);

            return true;
        }

        public bool ResetReflectPowerAlarmChecker()
        {
            _checkerReflectPowerAlarm.Reset(_scReflectPowerAlarmTime.IntValue);

            return true;
        }

        public bool SetPowerOnOff(bool isOn, out string reason)
        {
            if (isOn)
            {
                reason = string.Empty;
                if (!_diIntlk.Value)
                {
                    reason = $"{Name} interlock is not satisfied, can not be on";
                    EV.PostWarningLog(Module, reason);
                    return false;
                }

                if (!HeatOnComplete)
                {
                    reason = $"{Name} heat on not complete";
                    EV.PostWarningLog(Module, reason);
                    return false;
                }
                
            }


            bool result = true;
            if (!isOn)
            {
                PowerSetPoint = 0;
            }
            else
            {
                if (PowerSetPoint < 0.01)
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(200);
                        result = _doOn.SetValue(isOn, out string reason1);
                        if (!result)
                        {
                            EV.PostWarningLog(Module, reason1);
                        }
                        if (result)
                        {
                            string info = $"Set {Display} power " + (isOn ? "On" : "Off");
                            if (isOn)
                                _powerOnTimer.Start(Math.Min(Math.Max(2, _scPowerOnTimeout.IntValue), 30) * 1000);
                            else
                                _powerOnTimer.Stop();

                            EV.PostInfoLog(Module, info);
                        }
                    });

                    reason = string.Empty;
                    return result;
                }
            }

            result = _doOn.SetValue(isOn, out reason);
            if (result)
            {
                reason = $"Set {Display} power " + (isOn ? "On" : "Off");
                if (isOn)
                    _powerOnTimer.Start(Math.Min(Math.Max(2, _scPowerOnTimeout.IntValue), 30) * 1000);
                else
                    _powerOnTimer.Stop();

                EV.PostInfoLog(Module, reason);
            }

            return result;
        }


        public void UpdateTuneTable()
        {
            _tuneTable = new List<RfTuneItem>();

            // 100#200#5#-6;
            string scValue = _scPowerTuneTable.StringValue;

            if (string.IsNullOrEmpty(scValue))
                return;

            string[] items = scValue.Split(';');

            for (int i = 0; i < items.Length; i++)
            {
                string itemValue = items[i];
                if (!string.IsNullOrEmpty(itemValue))
                {
                    string[] pairValue = itemValue.Split('#');
                    if (pairValue.Length == 4)
                    {
                        if (int.TryParse(pairValue[0], out int rangeItem1)
                            && int.TryParse(pairValue[1], out int rangeItem2)
                            && int.TryParse(pairValue[2], out int setItem1)
                            && int.TryParse(pairValue[3], out int setItem2))
                        {
                            _tuneTable.Add(new RfTuneItem()
                            {
                                Percent = setItem1,
                                RangeFrom = rangeItem1,
                                RangeTo = rangeItem2,
                                Value = setItem2,
                            });
                        }
                    }
                }
            }
        }

        public float GetTuneValue(float raw, bool isOutput)
        {
            if (_scPowerTuneEnable == null || !_scPowerTuneEnable.BoolValue)
                return raw;

            float tuned = raw;

            foreach (var rfTuneItem in _tuneTable)
            {
                if (raw >= rfTuneItem.RangeFrom && raw <= rfTuneItem.RangeTo)
                {
                    if (isOutput)
                    {
                        double percent = rfTuneItem.Percent / 100.0;
                        tuned = (float)(_scPowerTuneIsPercent.BoolValue ? (raw + raw * percent) : (raw + rfTuneItem.Value));
                    }
                    else
                    {
                        double percent = rfTuneItem.Percent / 100.0;
                        percent = 1 + percent;
                        if (Math.Abs(percent) < 0.01)
                            percent = 1;

                        tuned = (float)(_scPowerTuneIsPercent.BoolValue ? (raw / percent) : (raw - rfTuneItem.Value));
                    }
                }
            }

            if (tuned < 0)
            {
                tuned = 0;
            }

            if (tuned > PowerRange)
            {
                tuned = PowerRange;
            }

            return tuned;
        }




        public void InitializeCalalibration()
        {
            var records = new List<RFCalibrationItem>();
            for (int i = 0; i < 10; i++)
            {
                records.Add(new RFCalibrationItem()
                {
                    Setpoint = (float)(PowerRange / 10 * (i + 1)),
                    Meter = (float)_scPowerCalibrationTable[i].DoubleValue,
                });
            }

            _calibrationFormla = new List<CalbrationParas>();
            for (int i = 0; i < records.Count; i++)
            {
                if (i + 1 >= records.Count)
                    break;

                var para = Caculate(records[i], records[i + 1]);
                _calibrationFormla.Add(para);
            }

            var para0 = new CalbrationParas()
            {
                upperLimit = (float)(PowerRange / 10),
                lowerLimit = 0,
                paraA = _calibrationFormla[0].paraA,
                paraB = _calibrationFormla[0].paraB,
            };

            _calibrationFormla.Add(para0);
            _calibrationFormla = _calibrationFormla.OrderBy(x => x.lowerLimit).ToList();
        }

        public void UpdateCalibrationInfo(List<float> meterList)
        {
            for (int i = 0; i < meterList.Count; i++)
            {
                _scPowerCalibrationTable[i].DoubleValue = meterList[i];
            }

            InitializeCalalibration();
        }

        private CalbrationParas Caculate(RFCalibrationItem record1, RFCalibrationItem record2)
        {
            var para = new CalbrationParas();
            para.paraA = (record1.Setpoint - record2.Setpoint) / (record1.Meter - record2.Meter);
            para.paraB = record1.Setpoint - record1.Meter * para.paraA;
            para.upperLimit = record1.Setpoint > record2.Setpoint ? record1.Setpoint : record2.Setpoint;
            para.lowerLimit = record1.Setpoint < record2.Setpoint ? record1.Setpoint : record2.Setpoint;
            return para;
        }

        private float CalibrationPower(float power, bool output)
        {
            //default enable
            if (_scPowerCalibrationEnable != null && !_scPowerCalibrationEnable.BoolValue)
                return power;

            float ret = power;

            if (output)
            {
                if (_calibrationFormla != null && _calibrationFormla.Any())
                {
                    var para = _calibrationFormla.FirstOrDefault(x => x.lowerLimit <= power && x.upperLimit >= power);
                    ret = ret * para.paraA + para.paraB;
                }

                return ret;
            }

            if (_calibrationFormla != null && _calibrationFormla.Any())
            {
                var para = _calibrationFormla.FirstOrDefault(x => x.lowerLimit <= power && x.upperLimit >= power);
                ret = (ret - para.paraB) / para.paraA;
            }

            return ret < 0 || ret > float.MaxValue ? 0 : ret;
        }


        public void SaveRFCalibrationData()
        {
            if (_rfCalibrationDatas.Count != TenPoints)
            {
                EV.PostInfoLog(Module, $"Save Rf calibration data failed, for some points have not been calibrated.");
                return;
            }
            _rfCalibrationDatas = _rfCalibrationDatas.OrderBy(x => x.Setpoint).ToList();
            //RFCalibrationData data = new RFCalibrationData()
            //{
            //    Module = Module,
            //    Name = Name,
            //    Rate1Setpoint = _rfCalibrationDatas[0].Setpoint,
            //    Rate1Feedback = _rfCalibrationDatas[0].Feedback,
            //    Rate1Meter = _rfCalibrationDatas[0].Meter,

            //    Rate2Setpoint = _rfCalibrationDatas[1].Setpoint,
            //    Rate2Feedback = _rfCalibrationDatas[1].Feedback,
            //    Rate2Meter = _rfCalibrationDatas[1].Meter,

            //    Rate3Setpoint = _rfCalibrationDatas[2].Setpoint,
            //    Rate3Feedback = _rfCalibrationDatas[2].Feedback,
            //    Rate3Meter = _rfCalibrationDatas[2].Meter,

            //    Rate4Setpoint = _rfCalibrationDatas[3].Setpoint,
            //    Rate4Feedback = _rfCalibrationDatas[3].Feedback,
            //    Rate4Meter = _rfCalibrationDatas[3].Meter,

            //    Rate5Setpoint = _rfCalibrationDatas[4].Setpoint,
            //    Rate5Feedback = _rfCalibrationDatas[4].Feedback,
            //    Rate5Meter = _rfCalibrationDatas[4].Meter,

            //    Rate6Setpoint = _rfCalibrationDatas[5].Setpoint,
            //    Rate6Feedback = _rfCalibrationDatas[5].Feedback,
            //    Rate6Meter = _rfCalibrationDatas[5].Meter,

            //    Rate7Setpoint = _rfCalibrationDatas[6].Setpoint,
            //    Rate7Feedback = _rfCalibrationDatas[6].Feedback,
            //    Rate7Meter = _rfCalibrationDatas[6].Meter,

            //    Rate8Setpoint = _rfCalibrationDatas[7].Setpoint,
            //    Rate8Feedback = _rfCalibrationDatas[7].Feedback,
            //    Rate8Meter = _rfCalibrationDatas[7].Meter,

            //    Rate9Setpoint = _rfCalibrationDatas[8].Setpoint,
            //    Rate9Feedback = _rfCalibrationDatas[8].Feedback,
            //    Rate9Meter = _rfCalibrationDatas[8].Meter,

            //    Rate10Setpoint = _rfCalibrationDatas[9].Setpoint,
            //    Rate10Feedback = _rfCalibrationDatas[9].Feedback,
            //    Rate10Meter = _rfCalibrationDatas[9].Meter,
            //};
            //RFCalibrationDataRecorder.Add(data);


            for (int i = 0; i < _rfCalibrationDatas.Count; i++)
            {
                SC.SetItemValue($"{Module}.RF.Rate{i + 1}MeterPower", (double)_rfCalibrationDatas[i].Meter);
            }

            InitializeCalalibration();

            EV.PostInfoLog(Module, $"Save RF calibration data successed.");
        }

        public void SaveRFCalibrationPoint(float setpoint, float meterValue)
        {
            if (_rfCalibrationDatas.Any(x => x.Setpoint == setpoint))
            {
                _rfCalibrationDatas.RemoveAll(x => x.Setpoint == setpoint);
            }

            var data = new RFCalibrationItem()
            {
                Setpoint = setpoint,
                Meter = meterValue,
                Feedback = RFForwardPower,
            };
            _rfCalibrationDatas.Add(data);
        }

        private struct CalbrationParas
        {
            //y = a*x + b, x is setpoint
            public float paraA;
            public float paraB;
            public float upperLimit;
            public float lowerLimit;
        }

        private class RFCalibrationItem
        {
            public float Setpoint { get; set; }
            public float Feedback { get; set; }
            public float Meter { get; set; }
        }
    }
}