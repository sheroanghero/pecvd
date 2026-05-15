using System;
using System.Diagnostics;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 泵2对应的IO要求：
    /// diRunning   【如果未定义，默认泵为常开】
    /// diOverloadAlarm【可以不定义】
    ///
    /// doResetError【可以不定义】
    /// doStartStop【可以不定义】
    /// doPowerOn【可以不定义，默认主电源常供】
    /// </summary>
    public class IoPump2 : BaseDevice, IDevice, IPump
    {
        public bool IsRunning
        {
            get
            {
                if (_diRunning != null)
                    return _diRunning.Value;
                return true;
            }
        }

        public bool IsPumpOverloadAlarm
        {
            get
            {
                return _diOverloadAlarm != null && _diOverloadAlarm.Value;
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
                if (_doResetError!=null)
                    _doResetError.Value = value;
            }
        }

        public bool StartSetPoint
        {
            get
            {
                return _doStart != null && _doStart.Value;
            }
            set
            {
                if (_doStart != null)
                    _doStart.Value = value;
            }
        }
 
        public bool MainPowerOnSetPoint
        {
            get
            {
                return _doPowerOn != null && _doPowerOn.Value;
            }
            set
            {
                if (_doPowerOn != null)
                    _doPowerOn.Value = value;
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
                    IsError = HasAlarm,
                    IsOverLoad = IsPumpOverloadAlarm,
                };

                return data;
            }
        }
 

        private DIAccessor _diRunning = null;
        private DIAccessor _diOverloadAlarm;
 
        private DOAccessor _doStart;
        private DOAccessor _doPowerOn;
        private DOAccessor _doResetError;

        private R_TRIG _trigOverload = new R_TRIG();

        public AlarmEventItem AlarmOverload { get; set; }
        public AlarmEventItem AlarmFailedStartStop { get; set; }

        private Stopwatch _timerResetError = new Stopwatch();
        private Stopwatch _timerStartStop = new Stopwatch();

        private SCConfigItem _scStartTimeout;
        private SCConfigItem _scResetErrorTimeout;

        public IoPump2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diRunning = ParseDiNode("diRunning", node, ioModule);
            _diOverloadAlarm = ParseDiNode("diOverloadAlarm", node, ioModule);
 
            _doStart = ParseDoNode("doStartStop", node, ioModule);
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doResetError = ParseDoNode("doReset", node, ioModule);

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _scStartTimeout = ParseScNode("", node, ioModule, $"{scBasePath}.{Name}.StartTimeout");
            _scResetErrorTimeout = ParseScNode("", node, ioModule, $"{scBasePath}.{Name}.ResetErrorTimeout");

            System.Diagnostics.Debug.Assert(_scResetErrorTimeout != null, "SC not defined");
            System.Diagnostics.Debug.Assert(_scStartTimeout != null, "SC not defined");
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsOverload", () => IsPumpOverloadAlarm);
            DATA.Subscribe($"{Module}.{Name}.IsRunning", () => IsRunning);
            DATA.Subscribe($"{Module}.{Name}.StartSetPoint", () => StartSetPoint);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.SetOnOff}" , SetPumpOnOff);
            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOn}", SetPumpOn);
            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOff}", SetPumpOff);

            AlarmOverload = SubscribeAlarm($"{Module}.{Name}.OverloadAlarm", "", ResetOverload);
            AlarmFailedStartStop = SubscribeAlarm($"{Module}.{Name}.FailedStartStopAlarm", "", null);

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

        public bool SetMainPowerOnOff(bool isOn, out string reason)
        {
            MainPowerOnSetPoint = isOn;
            reason = string.Empty;
            return true;
        }

        public bool ResetOverload()
        {
            if (IsPumpOverloadAlarm)
            {
                if (!ResetErrorSetPoint)
                {
                    _timerResetError.Restart();
                    ResetErrorSetPoint = true;
                }
            }

            return !IsPumpOverloadAlarm;
        }

        public bool SetPump(out string reason, int time, bool isOn)
        {
            if (HasAlarm)
            {
                reason = $"{Display} Has active alarm, reset error first.";
                return false;
            }

            reason = string.Empty;

            _timerStartStop.Restart();
            StartSetPoint = isOn;

            return true;
        }
 
        public void Terminate()
        {
        }
 
        public void Monitor()
        {
            try
            {
                if (StartSetPoint != IsRunning)
                {
                    if (_timerStartStop.IsRunning &&
                        _timerStartStop.ElapsedMilliseconds > _scStartTimeout.IntValue * 1000)
                    {
                        var onoff = StartSetPoint ? "start up" : "shut down";
                        _timerStartStop.Stop();
                        AlarmFailedStartStop.Description =
                            $"{Display} can not {onoff} in {_scStartTimeout.IntValue} seconds";
                        AlarmFailedStartStop.Set();
                        StartSetPoint = IsRunning;
                    }
                }

                _trigOverload.CLK = IsPumpOverloadAlarm;
                if (_trigOverload.Q)
                {
                    AlarmOverload.Set("Pump Overload or Error");
                    StartSetPoint = false;
                }

                if (ResetErrorSetPoint)
                {
                    if (!IsPumpOverloadAlarm)
                    {
                        AlarmOverload.Reset();
                        ResetErrorSetPoint = false;
                        _timerResetError.Stop();
                    }

                    if (IsPumpOverloadAlarm && _timerResetError.IsRunning &&
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
            AlarmFailedStartStop.Reset();
            AlarmOverload.Reset();


        }
    }
}
