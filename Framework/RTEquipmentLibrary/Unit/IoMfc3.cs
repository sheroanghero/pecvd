using System;
using System.Xml;
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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 
    /// </summary>
    public class IoMfc3 : BaseDevice, IDevice, IMfc
    {
        public string Unit
        {
            get; set;
        }

        public double Scale
        {
            get
            {
                if (_scN2Scale == null || _scScaleFactor == null)
                    return 0;
                return _scN2Scale.DoubleValue * _scScaleFactor.DoubleValue;
            }
        }

        public double SetPoint
        {
            get
            {
                if (_aoFlowL == null || _aoFlowH == null)
                    return 0;

                short valueL = _aoFlowL.Value;
                short valueH = _aoFlowH.Value;
                UInt32 tmp = (ushort)valueL + (UInt32)(((ushort)valueH) << 16);
                double value = tmp > Int32.MaxValue ? (double)((~tmp + 1) * -1) : (double)tmp;

                return value;
            }
            set
            {
                if (_aoFlowL != null && _aoFlowH != null) 
                {
                    _aoFlowL.Value = (short)((int)value & 0xFFFF);
                    _aoFlowH.Value = (short)((int)value >> 16 & 0xFFFF);
                }
            }
        }

        public double DefaultSetPoint
        {
            get
            {
                if (_scDefaultSetPoint != null)
                    return _scDefaultSetPoint.DoubleValue;
                return 0;
            }
        }

        public double FeedBack
        {
            get
            {
                if (_aiFlowL == null || _aiFlowH == null)
                    return 0;

                short valueL = _aiFlowL.Value;
                short valueH = _aiFlowH.Value;
                UInt32 tmp = (ushort)valueL + (UInt32)(((ushort)valueH) << 16);
                double value = tmp > Int32.MaxValue ? (double)((~tmp + 1) * -1) : (double)tmp;

                 return (_scRegulationFactor != null && _scRegulationFactor.DoubleValue > 0.001) ? value / _scRegulationFactor.DoubleValue : value;
                
                
            }
        }

        public bool EnableAlarm
        {
            get
            {
                if (_scEnableAlarm != null)
                    return _scEnableAlarm.BoolValue;
                return false;
            }
        }

        public double AlarmRange
        {
            get
            {
                if (_scAlarmRange != null)
                    return _scAlarmRange.DoubleValue;
                return 0;
            }
        }

        public double AlarmTime
        {
            get
            {
                if (_scAlarmTime != null)
                    return _scAlarmTime.IntValue;
                return 0;
            }
        }
        public double WarningRange
        {
            get
            {
                if (_scWarningRange != null)
                    return _scWarningRange.DoubleValue;
                return 0;
            }
        }

        public double WarningTime
        {
            get
            {
                if (_scWarningTime != null)
                    return _scWarningTime.IntValue;
                return 0;
            }
        }
 
        private AITMfcData DeviceData
        {
            get
            {
                AITMfcData data = new AITMfcData()
                {
                    UniqueName = $"{Module}.{Name}",
                    Type = "MFC",
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = DisplayName,
                    FeedBack = FeedBack,
                    SetPoint = SetPoint,
                    Scale = Scale,
                    IsWarning = !AlarmToleranceWarning.IsAcknowledged,
                    IsError = !AlarmToleranceAlarm.IsAcknowledged,
                };

                return data;
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

        private DeviceTimer _rampTimer = new DeviceTimer();
        private double _rampTarget;
        private double _rampInitValue;
        private int _rampTime;

        private ToleranceChecker _toleranceCheckerWarning = new ToleranceChecker();
        private ToleranceChecker _toleranceCheckerAlarm = new ToleranceChecker();

        private AIAccessor _aiFlowL;
        private AIAccessor _aiFlowH;
        private AOAccessor _aoFlowL;
        private AOAccessor _aoFlowH;
        private DOAccessor _doOpen;
        private DOAccessor _doClose;

        protected SCConfigItem _scGasName;
        protected SCConfigItem _scEnable;
        private SCConfigItem _scN2Scale;
        private SCConfigItem _scScaleFactor;
        private SCConfigItem _scAlarmRange;
        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scDefaultSetPoint;
        private SCConfigItem _scRegulationFactor;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;

        public AlarmEventItem AlarmToleranceWarning { get; set; }
        public AlarmEventItem AlarmToleranceAlarm { get; set; }

        private bool _isFloatAioType = false;

        public IoMfc3(string module, XmlElement node, string ioModule = "")
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _aiFlowL = ParseAiNode("aiFlowL", node, ioModule);
            _aiFlowH = ParseAiNode("aiFlowH", node, ioModule);
            _aoFlowL = ParseAoNode("aoFlowL", node, ioModule);
            _aoFlowH = ParseAoNode("aoFlowH", node, ioModule);
            _doOpen = ParseDoNode("doOpen", node, ioModule);
            _doClose = ParseDoNode("doClose", node, ioModule);

            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType"))  && (node.GetAttribute("aioType")=="float");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scGasName = SC.GetConfigItem($"{scBasePath}.{Name}.GasName");
            _scEnable = SC.GetConfigItem($"{scBasePath}.{Name}.Enable");

            _scN2Scale = ParseScNode("scN2Scale", node, ioModule, $"{scBasePath}.{Name}.N2Scale");

            _scScaleFactor = ParseScNode("scScaleFactor", node, ioModule, $"{scBasePath}.{Name}.ScaleFactor");
            _scAlarmRange = ParseScNode("scAlarmRange", node, ioModule, $"{scBasePath}.{Name}.AlarmRange");
            _scEnableAlarm = ParseScNode("scEnableAlarm", node, ioModule, $"{scBasePath}.{Name}.EnableAlarm");
            _scAlarmTime = ParseScNode("scAlarmTime", node, ioModule, $"{scBasePath}.{Name}.AlarmTime");
            _scDefaultSetPoint = ParseScNode("scDefaultSetPoint", node, ioModule, $"{scBasePath}.{Name}.DefaultSetPoint");

 
            _scWarningRange = SC.GetConfigItem($"{scBasePath}.{Name}.WarningRange");
            _scWarningTime = SC.GetConfigItem($"{scBasePath}.{Name}.WarningTime");

            _scRegulationFactor = ParseScNode("scFlowRegulationFactor", node, ioModule, $"{scBasePath}.{Name}.RegulationFactor");

        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.Feedback", () => FeedBack);
            DATA.Subscribe($"{Module}.{Name}.SetPoint", () => SetPoint);

            OP.Subscribe($"{Module}.{Name}.Ramp", (out string reason, int time, object[] param) =>
            {
                double target = Convert.ToDouble(param[0].ToString());

                if (target < 0 || target > Scale)
                {
                    reason = $"set {Display} value {target} out of range [0, {Scale}] {Unit}";
                    return false;
                }

                Ramp(target, time);

                if (time > 0)
                {
                    reason = $"{Display} ramp to {target} {Unit} in {time} seconds";
                }
                else
                {
                    reason = $"{Display} ramp to {target} {Unit}";
                }
                return true;
            });

            AlarmToleranceWarning = SubscribeAlarm($"{Module}.{Name}.ToleranceWarning", "", ResetWarningChecker, EventLevel.Warning);
            AlarmToleranceAlarm = SubscribeAlarm($"{Module}.{Name}.ToleranceAlarm", "", ResetAlarmChecker);

            return true;
        }

        public void Monitor()
        {
            MonitorRamping();

            MonitorTolerance();
        }

        public bool ResetWarningChecker()
        {
            _toleranceCheckerWarning.Reset(WarningTime);

            return true;
        }

        public bool ResetAlarmChecker()
        {
            _toleranceCheckerAlarm.Reset(AlarmTime);

            return true;
        }

        public void Reset()
        {
            AlarmToleranceWarning.Reset();
            AlarmToleranceAlarm.Reset();
        }



        public void Terminate()
        {
            Ramp(DefaultSetPoint, 0);
        }

        public bool Ramp(double flowSetPoint, int time, out string reason)
        {
            if (HasAlarm)
            {
                reason = $"{DisplayName} in error status, can not flow";
                return false;
            }

            if (flowSetPoint < 0 || flowSetPoint > Scale)
            {
                reason = $"{DisplayName} range is [0, {Scale}], can not flow {flowSetPoint}";
                return false;
            }

            if (time > 0)
            {
                EV.PostInfoLog(Module, $"Set {DisplayName} flow to {flowSetPoint} {Unit} in {time/1000:F0} seconds");
            }
            else
            {
                EV.PostInfoLog(Module, $"Set {DisplayName} flow to {flowSetPoint} {Unit}");
            }

            Ramp(flowSetPoint, time);
            reason = string.Empty;
            return true;
        }

        public void Ramp(int time)
        {
            Ramp(DefaultSetPoint, time);
        }

        public void Ramp(double target, int time)
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
            if (!_rampTimer.IsIdle())
            {
                if (_rampTimer.IsTimeout() || _rampTime == 0)
                {
                    _rampTimer.Stop();
                    SetPoint = _rampTarget;
                }
                else
                {
                    SetPoint = _rampInitValue + (_rampTarget - _rampInitValue) * _rampTimer.GetElapseTime() / _rampTime;
                }
            }
        }
        public bool SetOpen(bool isOpen, out string reason)
        {
            reason = string.Empty;

            if (_doOpen != null)
            {
                if (!_doOpen.SetValue(isOpen, out reason))
                {
                    LOG.Write(reason);
                    return false;
                }
            }
            if (_doClose != null)
            {
                if (!_doClose.SetValue(!isOpen, out reason))
                {
                    LOG.Write(reason);
                    return false;
                }
            }
            return true;
        }

        private void MonitorTolerance()
        {
            if (!EnableAlarm || SetPoint < 0.01)
            {
                _toleranceCheckerWarning.RST = true;
                _toleranceCheckerAlarm.RST = true;
                return;
            }

            _toleranceCheckerWarning.Monitor(FeedBack, (SetPoint - Math.Abs(WarningRange)), (SetPoint + Math.Abs(WarningRange)), WarningTime);
            if (_toleranceCheckerWarning.Trig)
            {
                AlarmToleranceWarning.Description = $"{Display} flow out of range {WarningRange} {Unit} in {WarningTime:F0} seconds";
                AlarmToleranceWarning.Set();
            }

            _toleranceCheckerAlarm.Monitor(FeedBack, (SetPoint - Math.Abs(AlarmRange)), (SetPoint + Math.Abs(AlarmRange)), AlarmTime);
            if (_toleranceCheckerAlarm.Trig)
            {
                AlarmToleranceAlarm.Description = $"{Display} flow out of range {AlarmRange} {Unit} in {AlarmTime:F0} seconds";
                AlarmToleranceAlarm.Set();
            }



        }

    }
}