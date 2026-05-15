using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoMfc2 : BaseDevice, IDevice, IMfc
    {
        public string Unit
        {
            get; set;
        }

        [Subscription(AITMfcDataPropertyName.Scale)]
        public double Scale
        {
            get
            {
                if (_scN2Scale == null || _scScaleFactor == null)
                    return 0;
                return _scN2Scale.DoubleValue * _scScaleFactor.DoubleValue;
            }
        }

        [Subscription(AITMfcDataPropertyName.SetPoint)]
        public double SetPoint
        {
            get
            {
                if (_aoFlow != null)
                {
                    return _aoFlow.Value;
                }
                return 0;
            }
            set
            {
                if (_aoFlow != null)
                {
                    _aoFlow.Value = (short)value;
                }
            }
        }

        [Subscription(AITMfcDataPropertyName.DefaultSetPoint)]
        public double DefaultSetPoint
        {
            get
            {
                if (_scDefaultSetPoint != null)
                    return _scDefaultSetPoint.DoubleValue;
                return 0;
            }
        }

        [Subscription(AITMfcDataPropertyName.FeedBack)]
        public double FeedBack
        {
            get
            {
                if (_aiFlow != null)
                    return (_scRegulationFactor!=null && _scRegulationFactor.DoubleValue > 0.001) ?  _aiFlow.Value / _scRegulationFactor.DoubleValue : _aiFlow.Value;
                return 0;
            }
        }

        [Subscription(AITMfcDataPropertyName.IsOutOfTolerance)]
        public bool IsOutOfTolerance
        {
            get
            {
                return _toleranceChecker.Result; 
            }
        }

        [Subscription(AITMfcDataPropertyName.IsEnableAlarm)]
        public bool EnableAlarm
        {
            get
            {
                if (_scEnableAlarm != null)
                    return _scEnableAlarm.BoolValue;
                return false;
            }
        }

        [Subscription(AITMfcDataPropertyName.AlarmRange)]
        public double AlarmRange
        {
            get
            {
                if (_scAlarmRange != null)
                    return _scAlarmRange.DoubleValue;
                return 0;
            }
        }

        [Subscription(AITMfcDataPropertyName.AlarmTime)]
        public double AlarmTime
        {
            get
            {
                if (_scAlarmTime != null)
                    return _scAlarmTime.IntValue;
                return 0;
            }
        }
 
        [Subscription(AITMfcDataPropertyName.IsOffline)]
        public bool IsOffline
        {
            get
            {
                if (_diOffline != null)
                    return _diOffline.Value;

                return false;
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

        private AITMfcData DeviceData
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
                    FeedBack = FeedBack,
                    SetPoint = SetPoint,
                    Scale = Scale,
                    IsOffline = IsOffline,
                };

                return data;
            }
        }


        private DeviceTimer rampTimer = new DeviceTimer();
        private double rampTarget;
        private double rampInitValue;  
        private int rampTime;

        private ToleranceChecker _toleranceChecker = new ToleranceChecker();

        private AIAccessor _aiFlow;
        private AOAccessor _aoFlow;
        private AOAccessor _aoRange;
        private DIAccessor _diOffline;

        private SCConfigItem _scGasName;
        private SCConfigItem _scEnable;
        private SCConfigItem _scN2Scale;
        private SCConfigItem _scScaleFactor;
        private SCConfigItem _scAlarmRange;
        private SCConfigItem _scEnableAlarm;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scDefaultSetPoint;
        private SCConfigItem _scRegulationFactor;
 
        private R_TRIG _trigOffline = new R_TRIG();

        private string _uniqueName;
        public IoMfc2(string module, XmlElement node, string ioModule="") 
        {
            Unit = node.GetAttribute("unit");
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ?  module :  node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");          

            _aoRange = ParseAoNode("aoRange", node, ioModule);
            _diOffline = ParseDiNode("diOffline", node, ioModule);

            _aiFlow = ParseAiNode("aiFlow", node, ioModule);
            _aoFlow = ParseAoNode("aoFlow", node, ioModule);
 
            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scGasName = ParseScNode("scGasName",node, ioModule, $"{scBasePath}.{Name}.GasName");
            _scEnable = ParseScNode("scEnable",node, ioModule, $"{scBasePath}.{Name}.Enable");
            _scN2Scale = ParseScNode("scN2Scale",node, ioModule, $"{scBasePath}.{Name}.N2Scale");
 
            _scScaleFactor = ParseScNode("scScaleFactor",node, ioModule, $"{scBasePath}.{Name}.ScaleFactor");
            _scAlarmRange = ParseScNode("scAlarmRange",node, ioModule, $"{scBasePath}.{Name}.AlarmRange");
            _scEnableAlarm = ParseScNode("scEnableAlarm",node, ioModule, $"{scBasePath}.{Name}.EnableAlarm");
            _scAlarmTime = ParseScNode("scAlarmTime",node, ioModule, $"{scBasePath}.{Name}.AlarmTime");
            _scDefaultSetPoint = ParseScNode("scDefaultSetPoint",node, ioModule, $"{scBasePath}.{Name}.DefaultSetPoint");

            _scRegulationFactor = ParseScNode("scFlowRegulationFactor",node, ioModule, $"{scBasePath}.{Name}.RegulationFactor");

            _uniqueName = $"{Module}.{Name}";

        }

        public bool Initialize()
        {
            DATA.Subscribe($"{_uniqueName}.DeviceData", () => DeviceData);
            DATA.Subscribe($"Device.{Module}.{Name}", () => DeviceData);

            OP.Subscribe($"{_uniqueName}.{AITMfcOperation.Ramp}", InvokeRamp);


            DEVICE.Register(String.Format("{0}.{1}", Name, AITMfcOperation.Ramp), (out string reason, int time, object[] param) =>
            {
                double target = Convert.ToDouble((string)param[0]);
                target = Math.Min(target, Scale);
                target = Math.Max(target, 0);

                Ramp(target, time);
                reason = string.Format("{0} ramp to {1}{2}", Display, target, Unit);
                return true;
            });

            //@AAA use recipe
            DEVICE.Register(String.Format("{0}", Name), (out string reason, int time, object[] param) =>
            {
                double target = Convert.ToDouble((string)param[0]);

                target = Math.Min(target, Scale);
                target = Math.Max(target, 0);

                Ramp(target, time);
                reason = string.Format("{0} ramp to {1}{2}", Display, target, Unit);
                return true;
            });

            return true;
        }

        private bool InvokeRamp(string method, object[] args)
        {
            double target = Convert.ToDouble((string)(args[0].ToString()));
            target = Math.Min(target, Scale);
            target = Math.Max(target, 0);

            int time = 0;
            if (args.Length >= 2)
                time  = Convert.ToInt32((string)(args[1].ToString()));

            Ramp(target, time);
  
            EV.PostInfoLog(Module, $"{_uniqueName} ramp to {target}{Unit} in {time} seconds");

            return true;
        }

        public void Monitor()
        {
 
            Ramping();
            CheckTolerance();

            if (_aoRange != null)
                _aoRange.Value = (short)Scale;

            _trigOffline.CLK = IsOffline;
            if (_trigOffline.Q)
            {
                EV.PostMessage(Module, EventEnum.DefaultAlarm, string.Format("{0} is offline", DisplayName));
            }

        }

        public void Reset()
        {
 
            _toleranceChecker.Reset(AlarmTime);

            _trigOffline.RST = true;
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

            EV.PostInfoLog(Module, $"{DisplayName} flow {flowSetPoint} {Unit} in {time} seconds");

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
            rampInitValue = SetPoint;    //ramp 初始值取当前设定值，而非实际读取值.零漂问题
            rampTime = time;
            rampTarget = target;
            rampTimer.Start(rampTime);
        }

        public void StopRamp()
        {
            Ramp(SetPoint, 0);
        }

        private void Ramping()
        {
            if (rampTimer.IsTimeout() || rampTime == 0)
            {
                SetPoint = rampTarget;
            }
            else
            {
                SetPoint = rampInitValue + (rampTarget - rampInitValue) * rampTimer.GetElapseTime() / rampTime;
            }
        }

        private void CheckTolerance()
        {
            if (!EnableAlarm)
            {
                _toleranceChecker.RST = true;
                return;
            }

            if (SetPoint < 0.1)
            {
                _toleranceChecker.RST = true;
                return;
            }

            _toleranceChecker.Monitor(FeedBack,  (SetPoint-Math.Abs(AlarmRange)), (SetPoint+Math.Abs(AlarmRange)), AlarmTime);

            if (_toleranceChecker.Trig)
            {
                EV.PostAlarmLog(Module, $"{Name} flow out of range {AlarmRange} sccm in {AlarmTime:F0} seconds" );
            }
        }
 
    }
}