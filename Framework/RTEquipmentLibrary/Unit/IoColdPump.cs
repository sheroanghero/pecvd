using System;
using System.Collections.Generic;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.DataCenter;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoColdPump : BaseDevice, IDevice
    {
        private DIAccessor _diStart;
        private DIAccessor _diError;
        private DIAccessor _diCold;
        private DIAccessor _diPurgeValve;
        private DIAccessor _diRoughValve;
        private DIAccessor _diPumpTCState;
        private DIAccessor _diAUXTCState;

        private DIAccessor _diGateValveClose;

        private DOAccessor _doManualPumpOn;
        private DOAccessor _doManualPumpOff;
        private DOAccessor _doAutoPumpOn;
        private DOAccessor _doAutoPumpOff;
        private DOAccessor _doPumpTcOn;
        private DOAccessor _doPumpTcOff;
        private DOAccessor _doAuxTcOn;
        private DOAccessor _doAuxTcOff;
        private DOAccessor _doRoughValve;
        private DOAccessor _doRoughValveOff;
        private DOAccessor _doPurgeValve;
        private DOAccessor _doPurgeValveOff;


        private AIAccessor _aiFstStageTem;
        private AIAccessor _aiSecStageTem;
        private AIAccessor _aiPumpTCPressure;
        private AIAccessor _aiAUXTCPressure;
        private AIAccessor _aiRegenErrorID;
        private AIAccessor _aiCurrentRegenerationStep;

        private AIAccessor _aiPumpRestartDelay;
        private AIAccessor _aiExtendedPurgeTime;
        private AIAccessor _aiRepurgeCycles;
        private AIAccessor _aiRoughToPressure;
        private AIAccessor _aiRateOfRise;
        private AIAccessor _aiRORCycles;
        private AIAccessor _aiStartUpTemperature;
        private AIAccessor _aiRepurgeTime;
        private AIAccessor _aiPumpDelayToStartOfRegenerationCycle;

        private AOAccessor _aoPumpRestartDelay;
        private AOAccessor _aoExtendedPurgeTime;
        private AOAccessor _aoRepurgeCycles;
        private AOAccessor _aoRoughToPressure;
        private AOAccessor _aoRateOfRise;
        private AOAccessor _aoRORCycles;
        private AOAccessor _aoStartUpTemperature;
        private AOAccessor _aoRepurgeTime;
        private AOAccessor _aoPumpDelayToStartOfRegenerationCycle;

        protected SCConfigItem _scSecStageTempNeedLessThan;

        protected SCConfigItem _scCryoPumpType;

        private R_TRIG _trigAlam = new R_TRIG();

        private R_TRIG _trigErrorCode = new R_TRIG();

        private R_TRIG _trigRegenComplete = new R_TRIG();



        protected DateTime _StartTime;
        protected DateTime _EndTime;

        private Dictionary<int, string> _errorCode;
        private Dictionary<int, string> _Step;


        private bool _isFloatAioType = false;

        private bool _regenIsRunning;
        private bool _regenIsComplete;

        private bool _notFirstRun;

        private bool _pmIsInstalled;

        private DeviceTimer _setTimer = new DeviceTimer();
        public IoColdPump(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diStart = ParseDiNode("diStart", node, ioModule);
            _diError = ParseDiNode("diError", node, ioModule);
            _diPurgeValve = ParseDiNode("diPurgeValve", node, ioModule);
            _diRoughValve = ParseDiNode("diRoughValve", node, ioModule);
            _diPumpTCState = ParseDiNode("diPumpTCState", node, ioModule);
            _diAUXTCState = ParseDiNode("diAUXTCState", node, ioModule);

            _diGateValveClose = ParseDiNode("diGateValveClose", node, ioModule);

            _doAutoPumpOn = ParseDoNode("doAutoPumpOn", node, ioModule);
            _doAutoPumpOff = ParseDoNode("doAutoPumpOff", node, ioModule);
            _doManualPumpOn = ParseDoNode("doManualPumpOn", node, ioModule);
            _doManualPumpOff = ParseDoNode("doManualPumpOff", node, ioModule);
            _doPumpTcOn = ParseDoNode("doPumpTcOn", node, ioModule);
            _doPumpTcOff = ParseDoNode("doPumpTcOff", node, ioModule);
            _doAuxTcOn = ParseDoNode("doAuxTcOn", node, ioModule);
            _doAuxTcOff = ParseDoNode("doAuxTcOff", node, ioModule);
            _doRoughValve = ParseDoNode("doRoughValve", node, ioModule);
            _doRoughValveOff = ParseDoNode("doRoughValveOff", node, ioModule);
            _doPurgeValve = ParseDoNode("doPurgeValve", node, ioModule);
            _doPurgeValveOff = ParseDoNode("doPurgeValveOff", node, ioModule);

            _aiFstStageTem = ParseAiNode("aiFstStageTem", node, ioModule);
            _aiSecStageTem = ParseAiNode("aiSecStageTem", node, ioModule);
            _aiPumpTCPressure = ParseAiNode("aiPumpTCPressure", node, ioModule);
            _aiAUXTCPressure = ParseAiNode("aiAUXTCPressure", node, ioModule);
            _aiRegenErrorID = ParseAiNode("aiRegenErrorID", node, ioModule);
            _aiCurrentRegenerationStep = ParseAiNode("aiCurrentRegenerationStep", node, ioModule);


            _aiPumpRestartDelay = ParseAiNode("aiPumpRestartDelay", node, ioModule);
            _aiExtendedPurgeTime = ParseAiNode("aiExtendedPurgeTime", node, ioModule);
            _aiRepurgeCycles = ParseAiNode("aiRepurgeCycles", node, ioModule);
            _aiRoughToPressure = ParseAiNode("aiRoughToPressure", node, ioModule);
            _aiRateOfRise = ParseAiNode("aiRateOfRise", node, ioModule);
            _aiRORCycles = ParseAiNode("aiRORCycles", node, ioModule);
            _aiStartUpTemperature = ParseAiNode("aiStartUpTemperature", node, ioModule);
            _aiRepurgeTime = ParseAiNode("aiRepurgeTime", node, ioModule);
            _aiPumpDelayToStartOfRegenerationCycle = ParseAiNode("aiPumpDelayToStartOfRegenerationCycle", node, ioModule);


            _aoPumpRestartDelay = ParseAoNode("aoPumpRestartDelay", node, ioModule);
            _aoExtendedPurgeTime = ParseAoNode("aoExtendedPurgeTime", node, ioModule);
            _aoRepurgeCycles = ParseAoNode("aoRepurgeCycles", node, ioModule);
            _aoRoughToPressure = ParseAoNode("aoRoughToPressure", node, ioModule);
            _aoRateOfRise = ParseAoNode("aoRateOfRise", node, ioModule);
            _aoRORCycles = ParseAoNode("aoRORCycles", node, ioModule);
            _aoStartUpTemperature = ParseAoNode("aoStartUpTemperature", node, ioModule);
            _aoRepurgeTime = ParseAoNode("aoRepurgeTime", node, ioModule);
            _aoPumpDelayToStartOfRegenerationCycle = ParseAoNode("aoPumpDelayToStartOfRegenerationCycle", node, ioModule);

            _scSecStageTempNeedLessThan = ParseScNode("scSecStageTempNeedLessThan", node, ioModule, $"System.CryoPump.SecStageTempNeedLessThan");

            _scCryoPumpType = ParseScNode("scCryoPumpType", node, ioModule, $"System.CryoPump.CryoPumpType");


            _errorCode = new Dictionary<int, string>()
            {
                {10, "warm up timeout" },
                {20, "cool down timeout" },
                {30, "Error in rate of roughing" },
                {40, "ROR cycle limit was reached" },
                {50, "manual abort" },
                {60, "rough valve timeout" },
                {70, "pump system bug" },
            };

            _Step = new Dictionary<int, string>()
            {
                {10, "Warm Up" },
                {20, "Purge" },
                {30, "Rough" },
                {40, "ROR" },
                {50, "Cool Down" },
                {60, "Regen Complete" },
                {70, "Regen Aborted" },
                {80, "Delay Restart" },
                {90, "Purge Fail" },
                {100, "Power Fail" },

            };

            if (Module != "System" && Module != "TM")
            {
                _pmIsInstalled = SC.GetValue<bool>($"System.SetUp.{Module}.IsInstalled");
            }
        }


        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.RegenIsRunning", () => _regenIsRunning);

            DATA.Subscribe($"{Module}.{Name}.DiError", () => DiError);

            DATA.Subscribe($"{Module}.{Name}.CurrentStepString", () => CurrentStepString);
            DATA.Subscribe($"{Module}.{Name}.RegenCompleteTime", () => RegenCompleteTime);

            DATA.Subscribe($"{Module}.{Name}.Start", () => DiStart);
            DATA.Subscribe($"{Module}.{Name}.PurgeValve", () => DiPurgeValve);
            DATA.Subscribe($"{Module}.{Name}.RoughValve", () => DiRoughValve);
            DATA.Subscribe($"{Module}.{Name}.PumpTCState", () => DiPumpTCState);
            DATA.Subscribe($"{Module}.{Name}.AUXTCState", () => DiAUXTCState);

            DATA.Subscribe($"{Module}.{Name}.AutoPumpOn", () => DoAutoPumpOn);

            DATA.Subscribe($"{Module}.{Name}.FstStageTem", () => AiFstStageTem);
            DATA.Subscribe($"{Module}.{Name}.SecStageTem", () => AiSecStageTem);
            DATA.Subscribe($"{Module}.{Name}.PumpTCPressure", () => AiPumpTCPressure);
            DATA.Subscribe($"{Module}.{Name}.AUXTCPressure", () => AiAUXTCPressure);
            DATA.Subscribe($"{Module}.{Name}.RegenErrorID", () => AiRegenErrorID);
            DATA.Subscribe($"{Module}.{Name}.CurrentRegenerationStep", () => AiCurrentRegenerationStep);

            DATA.Subscribe($"{Module}.{Name}.AiPumpRestartDelay", () => AiPumpRestartDelay);
            DATA.Subscribe($"{Module}.{Name}.AiRoughToPressure", () => AiRoughToPressure);
            DATA.Subscribe($"{Module}.{Name}.AiExtendedPurgeTime", () => AiExtendedPurgeTime);
            DATA.Subscribe($"{Module}.{Name}.AiRepurgeCycles", () => AiRepurgeCycles);
            DATA.Subscribe($"{Module}.{Name}.AiStartUpTemperature", () => AiStartUpTemperature);
            DATA.Subscribe($"{Module}.{Name}.AiRateOfRise", () => AiRateOfRise);
            DATA.Subscribe($"{Module}.{Name}.AiRORCycles", () => AiRORCycles);
            DATA.Subscribe($"{Module}.{Name}.AiRepurgeTime", () => AiRepurgeTime);
            DATA.Subscribe($"{Module}.{Name}.AiPumpDelayToStartOfRegenerationCycle", () => AiPumpDelayToStartOfRegenerationCycle);


            DATA.Subscribe($"{Module}.{Name}.AoPumpRestartDelay", () => AoPumpRestartDelay);
            DATA.Subscribe($"{Module}.{Name}.AoRoughToPressure", () => AoRoughToPressure);
            DATA.Subscribe($"{Module}.{Name}.AoExtendedPurgeTime", () => AoExtendedPurgeTime);
            DATA.Subscribe($"{Module}.{Name}.AoRepurgeCycles", () => AoRepurgeCycles);
            DATA.Subscribe($"{Module}.{Name}.AoStartUpTemperature", () => AoStartUpTemperature);
            DATA.Subscribe($"{Module}.{Name}.AoRateOfRise", () => AoRateOfRise);
            DATA.Subscribe($"{Module}.{Name}.AoRORCycles", () => AoRORCycles);
            DATA.Subscribe($"{Module}.{Name}.AoRepurgeTime", () => AoRepurgeTime);
            DATA.Subscribe($"{Module}.{Name}.AoPumpDelayToStartOfRegenerationCycle", () => AoPumpDelayToStartOfRegenerationCycle);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOn}", SetPumpOn);

            OP.Subscribe($"{Module}.{Name}.{AITPumpOperation.PumpOff}", SetPumpOff);

            OP.Subscribe($"{Module}.{Name}.SetCold", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetCold(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetManualPumpOn", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetManualPumpOn(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPumpTcOn", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetPumpTcOn(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetAuxTcOn", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetAuxTcOn(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetRoughValve", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetRoughValve(enable, out string reason);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPurgeValve", (function, args) =>
            {
                bool enable = Convert.ToBoolean(args[0].ToString());
                SetPurgeValve(enable, out string reason);
                return true;
            });



            OP.Subscribe($"{Module}.{Name}.SetPumpRestartDelay", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetPumpRestartDelay(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetExtendedPurgeTime", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetExtendedPurgeTime(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRepurgeCycles", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetRepurgeCycles(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRoughToPressure", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetRoughToPressure(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRateOfRise", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetRateOfRise(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetRORCycles", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetRORCycles(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetStartUpTemperature", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetStartUpTemperature(target);
                return true;
            });


            OP.Subscribe($"{Module}.{Name}.SetRepurgeTime", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetRepurgeTime(target);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPumpDelayToStartOfRegenerationCycle", (function, args) =>
            {
                float target = Convert.ToSingle(args[0].ToString());
                SetPumpDelayToStartOfRegenerationCycle(target);
                return true;
            });


            return true;
        }

        public void Monitor()
        {

            if (Module!="TM" && Module != "System")
            {
                if (_scCryoPumpType!=null)
                {
                    if (_scCryoPumpType.StringValue == "ZY" && Name!= "ZYColdPump")
                    {
                        return;
                    }
                    else if (_scCryoPumpType.StringValue == "Cti" && Name != "ColdPump")
                    {
                        return;
                    }
                }
            }

            if (_setTimer.IsTimeout())
            {
                _setTimer.Stop();
                if (DoAutoPumpOn)
                {
                    DoAutoPumpOn = false;
                }

                if (DoAutoPumpOff)
                {
                    DoAutoPumpOff = false;
                }
            }
            else if (!_setTimer.IsIdle())
            {
                //if (DiCold == DoCold)
                //{
                //    _setTimer.Stop();
                //}
            }

            _trigErrorCode.CLK = AiRegenErrorID > 0 || DiError;
            if (_trigErrorCode.Q)
            {
                if (DiError)
                {
                    EV.PostAlarmLog(Module == "System" ? "TM" : Module, $"{Module} {Name} Communication Outage ");
                }
                else if (AiRegenErrorID == 50)
                {
                    EV.PostWarningLog(Module == "System" ? "TM" : Module, $"{Module} {Name}  ErrorCode:{AiRegenErrorID}  {ErrorCodeString}");
                }
                else
                {
                    EV.PostAlarmLog(Module == "System" ? "TM" : Module, $"{Module} {Name}  ErrorCode:{AiRegenErrorID}  {ErrorCodeString}");
                }

            }

            if (Module == "TM" || _pmIsInstalled)
            {
                _trigAlam.CLK = AiCurrentRegenerationStep >= 90;
                if (_trigAlam.Q)
                {
                    EV.PostAlarmLog(Module == "System" ? "TM" : Module, $"{Module} {Name}  {AiCurrentRegenerationStep}  {CurrentStepString}");
                }
            }

            _trigRegenComplete.CLK = AiCurrentRegenerationStep == 60;
            if (_trigRegenComplete.Q)
            {

                _EndTime = DateTime.Now;
                if (_StartTime == DateTime.MinValue)
                {
                    _StartTime = _EndTime;
                }
                _regenIsRunning = false;
                _regenIsComplete = true;

                if (Module != "System" && _notFirstRun)
                {
                    StatsDataManager.Instance.SetValue($"{Module}.CyroLife", 0);
                    _notFirstRun = false;
                }

            }
        }

        public void Reset()
        {
            _trigErrorCode.RST = true;
            _trigAlam.RST = true;
        }

        public void Terminate()
        {

        }

        public string RegenCompleteTime
        {
            get
            {
                if (_regenIsRunning && !_regenIsComplete)
                {
                    return (DateTime.Now - _StartTime).ToString(@"hh\:mm\:ss");
                }
                return (_EndTime - _StartTime).ToString(@"hh\:mm\:ss");
            }
        }

        public string CurrentStepString
        {
            get
            {
                if (_Step.ContainsKey((int)AiCurrentRegenerationStep))
                {
                    return _Step[(int)AiCurrentRegenerationStep];
                }
                else
                {
                    return AiCurrentRegenerationStep.ToString();
                }
            }
        }

        public string ErrorCodeString
        {
            get
            {
                if (_errorCode.ContainsKey((int)AiRegenErrorID))
                {
                    return _errorCode[(int)AiRegenErrorID];
                }
                else
                {
                    return "";
                }
            }
        }

        public bool IsError
        {
            get
            {
                if (DiError || (AiRegenErrorID > 0 && AiRegenErrorID != 50) || AiCurrentRegenerationStep >= 90)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SecTempAllowOpenGateValve
        {
            get
            {
                if (AiSecStageTem <= Convert.ToSingle(_scSecStageTempNeedLessThan.Value))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public float AiSecStageTempNeedLessThan
        {
            get
            {
                return _scSecStageTempNeedLessThan == null ? 0 : Convert.ToSingle(_scSecStageTempNeedLessThan.Value);
            }
        }

        #region DI

        public bool DiStart
        {
            get
            {
                return _diStart == null ? false : _diStart.Value;
            }
        }

        public bool DiError
        {
            get
            {
                return _diError == null ? false : _diError.Value;
            }
        }


        public bool DiCold
        {
            get
            {
                return _diCold == null ? false : _diCold.Value;
            }
        }

        public bool DiPurgeValve
        {
            get
            {
                return _diPurgeValve == null ? false : _diPurgeValve.Value;
            }
        }


        public bool DiRoughValve
        {
            get
            {
                return _diRoughValve == null ? false : _diRoughValve.Value;
            }
        }

        public bool DiPumpTCState
        {
            get
            {
                return _diPumpTCState == null ? false : _diPumpTCState.Value;
            }
        }
        public bool DiAUXTCState
        {
            get
            {
                return _diAUXTCState == null ? false : _diAUXTCState.Value;
            }
        }

        public bool DiGateValveClose
        {
            get
            {
                return _diGateValveClose == null ? false : _diGateValveClose.Value;
            }
        }

        #endregion DI
        #region DO
        public bool DoManualPumpOn
        {
            get
            {
                if (_doManualPumpOn != null)
                    return _doManualPumpOn.Value;

                return false;
            }
            set
            {
                if (_doManualPumpOn != null)
                {
                    _doManualPumpOn.Value = value;
                }
            }
        }


        public bool DoAutoPumpOn
        {
            get
            {
                if (_doAutoPumpOn != null)
                    return _doAutoPumpOn.Value;

                return false;
            }
            set
            {
                if (_doAutoPumpOn != null)
                {
                    _doAutoPumpOn.Value = value;
                }
            }
        }

        public bool DoAutoPumpOff
        {
            get
            {
                if (_doAutoPumpOff != null)
                    return _doAutoPumpOff.Value;

                return false;
            }
            set
            {
                if (_doAutoPumpOff != null)
                {
                    _doAutoPumpOff.Value = value;
                }
            }
        }

        public bool DoPumpTcOn
        {
            get
            {
                if (_doPumpTcOn != null)
                    return _doPumpTcOn.Value;

                return false;
            }
            set
            {
                if (_doPumpTcOn != null)
                {
                    _doPumpTcOn.Value = value;
                }
            }
        }
        public bool DoAuxTcOn
        {
            get
            {
                if (_doAuxTcOn != null)
                    return _doAuxTcOn.Value;

                return false;
            }
            set
            {
                if (_doAuxTcOn != null)
                {
                    _doAuxTcOn.Value = value;
                }
            }
        }
        public bool DoRoughValve
        {
            get
            {
                if (_doRoughValve != null)
                    return _doRoughValve.Value;

                return false;
            }
            set
            {
                if (_doRoughValve != null)
                {
                    _doRoughValve.Value = value;
                }
            }
        }
        public bool DoPurgeValve
        {
            get
            {
                if (_doPurgeValve != null)
                    return _doPurgeValve.Value;

                return false;
            }
            set
            {
                if (_doPurgeValve != null)
                {
                    _doPurgeValve.Value = value;
                }
            }
        }

        #endregion DO
        #region AI
        public float AiFstStageTem
        {
            get
            {
                return _aiFstStageTem == null ? 0 : (_isFloatAioType ? _aiFstStageTem.FloatValue : _aiFstStageTem.Value);
            }
        }
        public float AiSecStageTem
        {
            get
            {
                return _aiSecStageTem == null ? 0 : (_isFloatAioType ? _aiSecStageTem.FloatValue : _aiSecStageTem.Value);
            }
        }
        public float AiPumpTCPressure
        {
            get
            {
                return _aiPumpTCPressure == null ? 0 : (_isFloatAioType ? _aiPumpTCPressure.FloatValue : _aiPumpTCPressure.Value);
            }
        }
        public float AiAUXTCPressure
        {
            get
            {
                return _aiAUXTCPressure == null ? 0 : (_isFloatAioType ? _aiAUXTCPressure.FloatValue : _aiAUXTCPressure.Value);
            }
        }
        public float AiRegenErrorID
        {
            get
            {
                return _aiRegenErrorID == null ? 0 : (_isFloatAioType ? _aiRegenErrorID.FloatValue : _aiRegenErrorID.Value);
            }
        }
        public float AiCurrentRegenerationStep
        {
            get
            {
                return _aiCurrentRegenerationStep == null ? 0 : (_isFloatAioType ? _aiCurrentRegenerationStep.FloatValue : _aiCurrentRegenerationStep.Value);
            }
        }




        public float AiPumpRestartDelay
        {
            get
            {
                return _aiPumpRestartDelay == null ? 0 : (_isFloatAioType ? _aiPumpRestartDelay.FloatValue : _aiPumpRestartDelay.Value);
            }

        }
        public float AiRoughToPressure
        {
            get
            {
                return _aiRoughToPressure == null ? 0 : (_isFloatAioType ? _aiRoughToPressure.FloatValue : _aiRoughToPressure.Value);
            }

        }
        public float AiExtendedPurgeTime
        {
            get
            {
                return _aiExtendedPurgeTime == null ? 0 : (_isFloatAioType ? _aiExtendedPurgeTime.FloatValue : _aiExtendedPurgeTime.Value);
            }

        }
        public float AiRepurgeCycles
        {
            get
            {
                return _aiRepurgeCycles == null ? 0 : (_isFloatAioType ? _aiRepurgeCycles.FloatValue : _aiRepurgeCycles.Value);
            }

        }
        public float AiStartUpTemperature
        {
            get
            {
                return _aiStartUpTemperature == null ? 0 : (_isFloatAioType ? _aiStartUpTemperature.FloatValue : _aiStartUpTemperature.Value);
            }

        }
        public float AiRateOfRise
        {
            get
            {
                return _aiRateOfRise == null ? 0 : (_isFloatAioType ? _aiRateOfRise.FloatValue : _aiRateOfRise.Value);
            }

        }
        public float AiRORCycles
        {
            get
            {
                return _aiRORCycles == null ? 0 : (_isFloatAioType ? _aiRORCycles.FloatValue : _aiRORCycles.Value);
            }

        }

        public float AiRepurgeTime
        {
            get
            {
                return _aiRepurgeTime == null ? 0 : (_isFloatAioType ? _aiRepurgeTime.FloatValue : _aiRepurgeTime.Value);
            }

        }

        public float AiPumpDelayToStartOfRegenerationCycle
        {
            get
            {
                return _aiPumpDelayToStartOfRegenerationCycle == null ? 0 : (_isFloatAioType ? _aiPumpDelayToStartOfRegenerationCycle.FloatValue : _aiPumpDelayToStartOfRegenerationCycle.Value);
            }

        }



        #endregion AI
        #region AO
        public float AoPumpRestartDelay
        {
            get
            {
                return _aoPumpRestartDelay == null ? 0 : (_isFloatAioType ? _aoPumpRestartDelay.FloatValue : _aoPumpRestartDelay.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPumpRestartDelay.FloatValue = value;
                }
                else
                {
                    _aoPumpRestartDelay.Value = (short)value;
                }
            }
        }
        public float AoRoughToPressure
        {
            get
            {
                return _aoRoughToPressure == null ? 0 : (_isFloatAioType ? _aoRoughToPressure.FloatValue : _aoRoughToPressure.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRoughToPressure.FloatValue = value;
                }
                else
                {
                    _aoRoughToPressure.Value = (short)value;
                }
            }
        }
        public float AoExtendedPurgeTime
        {
            get
            {
                return _aoExtendedPurgeTime == null ? 0 : (_isFloatAioType ? _aoExtendedPurgeTime.FloatValue : _aoExtendedPurgeTime.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoExtendedPurgeTime.FloatValue = value;
                }
                else
                {
                    _aoExtendedPurgeTime.Value = (short)value;
                }
            }
        }
        public float AoRepurgeCycles
        {
            get
            {
                return _aoRepurgeCycles == null ? 0 : (_isFloatAioType ? _aoRepurgeCycles.FloatValue : _aoRepurgeCycles.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRepurgeCycles.FloatValue = value;
                }
                else
                {
                    _aoRepurgeCycles.Value = (short)value;
                }
            }
        }
        public float AoStartUpTemperature
        {
            get
            {
                return _aoStartUpTemperature == null ? 0 : (_isFloatAioType ? _aoStartUpTemperature.FloatValue : _aoStartUpTemperature.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoStartUpTemperature.FloatValue = value;
                }
                else
                {
                    _aoStartUpTemperature.Value = (short)value;
                }
            }
        }
        public float AoRateOfRise
        {
            get
            {
                return _aoRateOfRise == null ? 0 : (_isFloatAioType ? _aoRateOfRise.FloatValue : _aoRateOfRise.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRateOfRise.FloatValue = value;
                }
                else
                {
                    _aoRateOfRise.Value = (short)value;
                }
            }
        }
        public float AoRORCycles
        {
            get
            {
                return _aoRORCycles == null ? 0 : (_isFloatAioType ? _aoRORCycles.FloatValue : _aoRORCycles.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRORCycles.FloatValue = value;
                }
                else
                {
                    _aoRORCycles.Value = (short)value;
                }
            }
        }

        public float AoRepurgeTime
        {
            get
            {
                return _aoRepurgeTime == null ? 0 : (_isFloatAioType ? _aoRepurgeTime.FloatValue : _aoRepurgeTime.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRepurgeTime.FloatValue = value;
                }
                else
                {
                    _aoRepurgeTime.Value = (short)value;
                }
            }
        }

        public float AoPumpDelayToStartOfRegenerationCycle
        {
            get
            {
                return _aoPumpDelayToStartOfRegenerationCycle == null ? 0 : (_isFloatAioType ? _aoPumpDelayToStartOfRegenerationCycle.FloatValue : _aoPumpDelayToStartOfRegenerationCycle.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPumpDelayToStartOfRegenerationCycle.FloatValue = value;
                }
                else
                {
                    _aoPumpDelayToStartOfRegenerationCycle.Value = (short)value;
                }
            }
        }

        #endregion AO

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

                    IsOn = DiStart,

                    FstStageTemp = AiFstStageTem,
                    SecStageTemp = AiSecStageTem,
                    TCPress = AiPumpTCPressure,

                    IsError = IsError

                };


                return data;
            }
        }

        private bool SetPumpOn(out string reason, int time, object[] param)
        {
            return SetCold(true, out reason);
        }

        private bool SetPumpOff(out string reason, int time, object[] param)
        {
            return SetCold(false, out reason);
        }

        public bool SetCold(bool enable, out string reason)
        {
            //if (!DiGateValveClose)
            //{
            //    reason = string.Empty;
            //    EV.PostWarningLog(Module, "Gate valve not close,can not operation auto regen");
            //    return false;
            //}

            if (!_doAutoPumpOn.Check(enable, out reason) || !_doAutoPumpOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doAutoPumpOn.SetValue(enable, out reason) || !_doAutoPumpOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _trigRegenComplete.RST = true;

            _regenIsComplete = false;

            if (enable)
            {
                _StartTime = DateTime.Now;
                _regenIsRunning = true;
                _notFirstRun = true;
            }
            else
            {
                _StartTime = DateTime.Now;
                _EndTime = DateTime.Now;
                _regenIsRunning = false;
            }




            _setTimer.Start(500 * 1);
            return true;
        }

        public bool SetManualPumpOn(bool enable, out string reason)
        {
            if (!_doManualPumpOn.Check(enable, out reason) || !_doManualPumpOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doManualPumpOn.SetValue(enable, out reason) || !_doManualPumpOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _setTimer.Start(1000 * 10);
            return true;
        }
        public bool SetPumpTcOn(bool enable, out string reason)
        {
            if (!_doPumpTcOn.Check(enable, out reason) || !_doPumpTcOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doPumpTcOn.SetValue(enable, out reason) || !_doPumpTcOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _setTimer.Start(1000 * 10);
            return true;
        }
        public bool SetAuxTcOn(bool enable, out string reason)
        {
            if (!_doAuxTcOn.Check(enable, out reason) || !_doAuxTcOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doAuxTcOn.SetValue(enable, out reason) || !_doAuxTcOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _setTimer.Start(1000 * 10);
            return true;
        }
        public bool SetRoughValve(bool enable, out string reason)
        {
            if (!_doRoughValve.Check(enable, out reason) || !_doRoughValveOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doRoughValve.SetValue(enable, out reason) || !_doRoughValveOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _setTimer.Start(1000 * 10);
            return true;
        }
        public bool SetPurgeValve(bool enable, out string reason)
        {
            if (!_doPurgeValve.Check(enable, out reason) || !_doPurgeValveOff.Check(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }
            if (!_doPurgeValve.SetValue(enable, out reason) || !_doPurgeValveOff.SetValue(!enable, out reason))
            {
                EV.PostWarningLog(Module, reason);
                return false;
            }

            _setTimer.Start(1000 * 10);
            return true;
        }





        public bool SetPumpRestartDelay(float fValue)
        {
            //if (!DiCold)
            //{
            //    EV.PostWarningLog(Module, $"{Module} {Name} Can not Set Volt While EscPower is not Start");
            //    return false;
            //}
            AoPumpRestartDelay = fValue;


            return true;
        }
        public bool SetRoughToPressure(float fValue)
        {

            AoRoughToPressure = fValue;

            return true;
        }
        public bool SetExtendedPurgeTime(float fValue)
        {

            AoExtendedPurgeTime = fValue;

            return true;
        }
        public bool SetRepurgeCycles(float fValue)
        {

            AoRepurgeCycles = fValue;

            return true;
        }
        public bool SetRateOfRise(float fValue)
        {
            AoRateOfRise = fValue;


            return true;
        }
        public bool SetStartUpTemperature(float fValue)
        {

            AoStartUpTemperature = fValue;

            return true;
        }
        public bool SetRORCycles(float fValue)
        {

            AoRORCycles = fValue;

            return true;
        }

        public bool SetRepurgeTime(float fValue)
        {

            AoRepurgeTime = fValue;
            return true;
        }

        public bool SetPumpDelayToStartOfRegenerationCycle(float fValue)
        {

            AoPumpDelayToStartOfRegenerationCycle = fValue;
            return true;
        }

    }

}
