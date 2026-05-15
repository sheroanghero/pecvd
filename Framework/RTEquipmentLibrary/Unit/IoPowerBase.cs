using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;
using MECF.Framework.Common.DataCenter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoPowerBase : BaseDevice, IDevice
    {

        public virtual bool DiStatus { get; set; }
        public virtual bool CommunicationStatus { get; set; }
        public virtual float ForwardPower { get; set; }
        public virtual float PowerSetPoint { get; set; }

        public virtual bool IsError { get; set; }

        public string _unit;
        public float _scalePower;

        protected bool _chamberIsInstalled;
        protected string _chamberType;

        protected R_TRIG _trigCommunicationAlam = new R_TRIG();


        protected SCConfigItem _scEnableAlarm;
        protected SCConfigItem _scAlarmTime;
        protected SCConfigItem _scAlarmRange;
        protected SCConfigItem _scWarningTime;
        protected SCConfigItem _scWarningRange;

        protected ToleranceChecker _toleranceAlarmChecker = new ToleranceChecker();
        protected ToleranceChecker _toleranceWarningChecker = new ToleranceChecker();

    

        public IoPowerBase(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _chamberIsInstalled = SC.GetValue<bool>($"System.SetUp.{Module}.IsInstalled");
            _chamberType = SC.GetStringValue($"System.SetUp.{Module}.ChamberType");

            //_unit = SC.GetStringValue($"{Module}.{Name}.Unit");
            //_scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");

            //_scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
            //_scAlarmTime = SC.GetConfigItem($"{Module}.{Name}.AlarmTime");
            //_scAlarmRange = SC.GetConfigItem($"{Module}.{Name}.AlarmRange");
            //_scWarningTime = SC.GetConfigItem($"{Module}.{Name}.WarningTime");
            //_scWarningRange = SC.GetConfigItem($"{Module}.{Name}.WarningRange");

           
        
        }


        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

  
            DATA.Subscribe($"{Module}.{Name}.Status", () => DiStatus);
            DATA.Subscribe($"{Module}.{Name}.CommunicationStatus", () => CommunicationStatus);




            DATA.Subscribe($"{Module}.{Name}.ForwardPower", () => ForwardPower);
            DATA.Subscribe($"{Module}.{Name}.PowerSetPoint", () => PowerSetPoint);

 

            OP.Subscribe($"{Module}.{Name}.SetPowerOn", (function, args) =>
            {
                if (!SetPowerOnOff(true, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF on failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOff", (function, args) =>
            {
                if (!SetPowerOnOff(false, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF off failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerOnOff", (function, args) =>
            {
                if (!SetPowerOnOff(Convert.ToBoolean(args[0].ToString()), out string reason))
                {
                    EV.PostWarningLog(Module, $"{Module} {Name} RF off failed, for {reason}");
                    return false;
                }
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPower", (function, args) =>
            {
                SetPower(Convert.ToSingle(args[0].ToString()));
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPowerForRecipe", (function, args) =>
            {
                float value = Convert.ToSingle(args[0]);
                SetPower(value);
                bool isOn = value > 0;

                if (isOn ^ DiStatus)
                {
                    SetPowerOnOff(isOn, out _);
                }
                return true;
            });



            return true;
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


        public virtual void CheckTolerance()
        {
            if (!EnableAlarm || PowerSetPoint == 0 || !DiStatus )
            {
                _toleranceAlarmChecker.RST = true;
                _toleranceWarningChecker.RST = true;
                _toleranceAlarmChecker.Reset(AlarmTime);
                _toleranceWarningChecker.Reset(WarningTime);
                return;
            }

            _toleranceAlarmChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - AlarmRange / 100)), (PowerSetPoint * (1 + AlarmRange / 100)), AlarmTime);

            _toleranceWarningChecker.Monitor(ForwardPower, (PowerSetPoint * (1 - WarningRange / 100)), (PowerSetPoint * (1 + WarningRange / 100)), WarningTime);


        }

        public virtual bool CheckToleranceAlarm()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceAlarmChecker.Result;
        }

        public string GetToleranceAlarmLog()
        {
            return $"{Name} over alarm range of ±{AlarmRange}% for {AlarmTime}s";
        }

        public virtual bool CheckToleranceWarning()
        {
            if (!EnableAlarm)
                return false;

            return _toleranceWarningChecker.Result;
        }

        public string GetToleranceWarningLog()
        {
            return $"{Name} over warning range of ±{WarningRange}% for {WarningTime}s";
        }



        public virtual bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void SetPower(float power)
        {

        }




        public virtual void Monitor()
        {
            CheckTolerance();
        }

        public virtual void Reset()
        {
            _trigCommunicationAlam.RST = true;
        }

        public virtual void Terminate()
        {

        }




        public virtual AITRfPowerData DeviceData { get; set; }







    }
}
