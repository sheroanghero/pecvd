using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPump : BaseDevice, IDevice, IPump
    {
        public bool IsRunning
        {
            get
            {
                if (_diRunning != null)
                    return _diRunning.Value;

                if (_diPowerOn != null)
                    return _diPowerOn.Value;

                return true; //默认常开
            }
        }

        public bool IsWarning
        {
            get
            {
                return _diWarning != null && _diWarning.Value;
            }
        }
 
 
        public bool IsError
        {
            get
            {
                return _diError != null && _diError.Value;
            }
        }
 

        public bool IsPumpOverloadAlarm
        {
            get
            {
                return _diOverloadAlarm != null && _diOverloadAlarm.Value;
            }
        }

        public bool HasError
        {
            get
            {
                return IsPumpOverloadAlarm || IsError;
            }
        }

        public bool IsN2OK
        {
            get
            {
                if (_diN2OK == null)
                    return true;
                return _diN2OK.Value;
            }
        }

        public bool IsExhaustPressureOK
        {
            get
            {
                if (_diExhaustPressureOK == null)
                    return true;
                return _diExhaustPressureOK.Value;
            }
        }

        private AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    DeviceModule = Module,
                    Module = Module,

                    IsOn = IsRunning,

                    IsError = IsError,

                    IsOverLoad = IsPumpOverloadAlarm,

                };


                return data;
            }
        }
 

        private DIAccessor _diRunning = null;

        private DIAccessor _diOverloadAlarm;
        private DIAccessor _diPowerOn;
        private DIAccessor _diStart;
        private DIAccessor _diStop;

        private DIAccessor _diN2OK;
        private DIAccessor _diExhaustPressureOK;

        private DIAccessor _diError;
        private DIAccessor _diWarning;

        private DOAccessor _doStart;
        private DOAccessor _doPowerOn;

        private R_TRIG _trigWarning = new R_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigOverload = new R_TRIG();
        private R_TRIG _trigN2OK = new R_TRIG();
        private R_TRIG _trigExhaustPressureOK = new R_TRIG();

        public IoPump(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diError = ParseDiNode("diAlarm", node, ioModule);

            _diRunning = ParseDiNode("diRunning", node, ioModule);

            _diWarning = ParseDiNode("diWarning", node, ioModule);

            _diOverloadAlarm = ParseDiNode("diOverloadAlarm", node, ioModule);

            _diPowerOn = ParseDiNode("diPowerOn", node, ioModule);

            _diStart = ParseDiNode("diStart", node, ioModule);

            _diStop = ParseDiNode("diStop", node, ioModule);

            _diN2OK = ParseDiNode("diN2OK", node, ioModule);
            _diExhaustPressureOK = ParseDiNode("diExhaustPressureOK", node, ioModule);

            _doStart = ParseDoNode("doStartStop", node, ioModule);
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);

        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.SetOnOff}" , SetPumpOnOff);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOn}", SetPumpOn);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOff}", SetPumpOff);

            return true;
        }

        private bool SetPumpOn(out string reason, int time, object[] param)
        {
            return SetPump(out reason, time, true);
        }

        private bool SetPumpOff(out string reason, int time, object[] param)
        {
            return SetPump(out reason, time, false);
        }

        private bool SetPumpOnOff(out string reason, int time, object[] param)
        {
            return SetPump(out reason, time, Convert.ToBoolean((string)param[0]));

        }

        public bool SetPump(out string reason, int time, bool isOn)
        {
            if (isOn)
            {
                if (IsError)
                {
                    reason = "Can not set pump on, pump in error";
                    return false;
                }

                if (IsPumpOverloadAlarm)
                {
                    reason = "Can not set pump on, pump is overload";
                    return false;
                }

                if (!IsN2OK)
                {
                    reason = "Can not set pump on, pump N2 is not OK";
                    return false;
                }

                if (!IsExhaustPressureOK)
                {
                    reason = "Can not set pump on, pump exhaust pressure is not OK";
                    return false;
                }
            }

            reason = string.Empty;

            if (_doStart != null)
            {
                _doStart.Value = isOn;
            }
            else if (_doPowerOn != null)
            {
                _doPowerOn.Value = isOn;
            }
            else
            {
                reason = "Do not support host control";
                return false;
            }

            return true;
        }

        public bool SetMainPowerOnOff(bool isOn, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Terminate()
        {
        }
 
        public void Monitor()
        {
            try
            {
                _trigWarning.CLK = IsWarning;
                if (_trigWarning.Q)
                {
                    EV.PostWarningLog(Module, $"{Name} waring");
                }

                _trigError.CLK = IsError;
                if (_trigError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Name} error");

                    if (_doPowerOn != null && _doPowerOn.Value)
                    {
                        _doPowerOn.Value = false;
                        EV.PostInfoLog(Module, $"set {Name} power off");
                    }

                    if (_doStart != null && _doStart.Value)
                    {
                        _doStart.Value = false;
                        EV.PostInfoLog(Module, $"set {Name} off");
                    }
                }

                _trigOverload.CLK = IsPumpOverloadAlarm;
                if (_trigOverload.Q)
                {
                    EV.PostAlarmLog(Module, $"{Name} overload");
                    if (_doPowerOn != null && _doPowerOn.Value)
                    {
                        _doPowerOn.Value = false;
                        EV.PostInfoLog(Module, $"set {Name} power off");
                    }

                    if (_doStart != null && _doStart.Value)
                    {
                        _doStart.Value = false;
                        EV.PostInfoLog(Module, $"set {Name} off");
                    }
                }

                _trigN2OK.CLK = !IsN2OK;
                if (_trigN2OK.Q)
                {
                    EV.PostWarningLog(Module, $"{Name} N2 is not OK");
                }

                _trigExhaustPressureOK.CLK = !IsExhaustPressureOK;
                if (_trigExhaustPressureOK.Q)
                {
                    EV.PostWarningLog(Module, $"{Name} exhaust pressure is not OK");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Reset()
        {
            _trigOverload.RST = true;
            _trigWarning.RST = true;
            _trigError.RST = true;
            _trigN2OK.RST = true;
            _trigExhaustPressureOK.RST = true;
        }
    }
}
