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

    public class IoDCPower : IoPowerBase
    {
       
        private DIAccessor _diStatus;
        private DIAccessor _diCommunicationStatus;
       

        private DOAccessor _doPowerOn;
        private DOAccessor _doEnableflag;
        private DOAccessor _doEnablePulseMode;



        private AIAccessor _aiQuerystatus;
        private AIAccessor _aiQueryforwardpower;
        private AIAccessor _aiQueryforwardvoltage;
        private AIAccessor _aiQueryforwardcurrent;

        private AIAccessor _aiQueryregulationmode;
        private AIAccessor _aiQuerySetpoint;
        private AIAccessor _aiQueryRegulationmode;
        private AIAccessor _aiQuerypulsefrequencyindex;
        private AIAccessor _aiQuerypulsereversetime;


        private AOAccessor _aoSetPowerValue;
        private AOAccessor _aoRegulationMode;
        private AOAccessor _aoPulseFrequencyindex;
        private AOAccessor _aoPulseReverseTime;

  


        private R_TRIG _trigAlam = new R_TRIG();

        private R_TRIG _trigErrorCode = new R_TRIG();




        private float _factor;

        private SCConfigItem _scPowerType;
        private SCConfigItem _scPulseType;
        private SCConfigItem _scPulseReverseTime;


        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();

        private RD_TRIG _trigRfOnOff = new RD_TRIG();
        public bool _RetryOK;
        private int _RetryCount;
        public SCConfigItem _scRetryCount;
        public SCConfigItem _scRetryAlarmRange;
        private DeviceTimer _RetryDelayChecktimer = new DeviceTimer();
        private DeviceTimer _RetryFailedDelaytimer = new DeviceTimer();
        private R_TRIG _trigRetryDelayCheck = new R_TRIG();
        private R_TRIG _trigRetryFailedDelay = new R_TRIG();



        private bool _isFloatAioType = false;

    

        private DeviceTimer _setTimer = new DeviceTimer();
        public IoDCPower(string module, XmlElement node, string ioModule = ""):base( module,  node, ioModule)
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");



            

            _scPowerType = SC.GetConfigItem($"System.SetUp.{Module}.PowerType");
            _scPulseType = SC.GetConfigItem($"System.SetUp.{Module}.PulseType");
            _scPulseReverseTime = SC.GetConfigItem($"System.SetUp.{Module}.PulseReverseTime");

           

            _diStatus = ParseDiNode("diStatus", node, ioModule);
            _diCommunicationStatus = ParseDiNode("diCommunicationStatus", node, ioModule);


 
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doEnableflag = ParseDoNode("doEnableflag", node, ioModule);
            _doEnablePulseMode = ParseDoNode("doEnablePulseMode", node, ioModule);


            _aiQuerystatus = ParseAiNode("aiQuerystatus", node, ioModule);
            _aiQueryforwardpower = ParseAiNode("aiQueryforwardpower", node, ioModule);
            _aiQueryforwardvoltage = ParseAiNode("aiQueryforwardvoltage", node, ioModule);
            _aiQueryforwardcurrent = ParseAiNode("aiQueryforwardcurrent", node, ioModule);

            _aiQueryregulationmode = ParseAiNode("aiQueryregulationmode", node, ioModule);
            _aiQuerySetpoint = ParseAiNode("aiQuerySetpoint", node, ioModule);
            _aiQueryRegulationmode = ParseAiNode("aiQueryRegulationmode", node, ioModule);
            _aiQuerypulsefrequencyindex = ParseAiNode("aiQuerypulsefrequencyindex", node, ioModule);
            _aiQuerypulsereversetime = ParseAiNode("aiQuerypulsereversetime", node, ioModule);


          
            _aoSetPowerValue = ParseAoNode("aoSetPowerValue", node, ioModule);
            _aoRegulationMode = ParseAoNode("aoRegulationMode", node, ioModule);
            _aoPulseFrequencyindex = ParseAoNode("aoPulseFrequencyindex", node, ioModule);
            _aoPulseReverseTime = ParseAoNode("aoPulseReverseTime", node, ioModule);
           
       
        
        }


        public override bool Initialize()
        {
            base.Initialize();

            DATA.Subscribe($"{Module}.{Name}.PowerOnOff", () => DoPowerOn);

            DATA.Subscribe($"{Module}.{Name}.Current", () => AiQueryforwardcurrent);
            DATA.Subscribe($"{Module}.{Name}.Voltage", () => AiQueryforwardvoltage);

      
            DATA.Subscribe($"{Module}.{Name}.PulsingFrequency", () => AiQuerypulsefrequencyindex);
            DATA.Subscribe($"{Module}.{Name}.PulsingReverseTime", () => AiQuerypulsereversetime);


 



            OP.Subscribe($"{Module}.{Name}.SetPulseFrequency", (function, args) =>
            {
                int value = Convert.ToInt32(args[0]);
                SetPulseFrequency(value);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPulseReverseTime", (function, args) =>
            {
                float value = Convert.ToSingle(args[0]);
                SetPulseReverseTime(value);
                return true;
            });

            return true;
        }



     


        public override  void SetPower(float power)
        {
            AoSetPowerValue = power;

        }

        public override bool SetPowerOnOff(bool isOn, out string reason)
        {
            reason = "";

            if (FuncCheckInterLock != null)
            {
                if (!FuncCheckInterLock(isOn))
                {
                    return false;
                }
            }

            DoPowerOn = isOn;

            LOG.Write($"{Module} Set DCPower " + (isOn ? "On" : "Off"));

            return true;
        }


        public void SetPulseFrequency(float frequency)
        {
            AoPulseFrequencyindex = frequency;


        }

        public void SetPulseReverseTime(float time)
        {
            AoPulseReverseTime = time;

  
        }

        public override void Monitor()
        {
            base.Monitor();

            if (_chamberIsInstalled && _chamberType == "PVD" &&  _scEnableAlarm == null && SC.ContainsItem($"{Module}.{Name}.EnableAlarm"))
            {
                _unit = SC.GetStringValue($"{Module}.{Name}.Unit");
                _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");
                _factor = SC.GetValue<int>($"{Module}.{Name}.Factor");

                _scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
                _scAlarmTime = SC.GetConfigItem($"{Module}.{Name}.AlarmTime");
                _scAlarmRange = SC.GetConfigItem($"{Module}.{Name}.AlarmRange");
                _scWarningTime = SC.GetConfigItem($"{Module}.{Name}.WarningTime");
                _scWarningRange = SC.GetConfigItem($"{Module}.{Name}.WarningRange");

                _scRetryCount = SC.GetConfigItem($"{Module}.{Name}.RetryCount");
                _scRetryAlarmRange = SC.GetConfigItem($"{Module}.{Name}.RetryAlarmRange");


            }

            if (_chamberIsInstalled && _chamberType == "PVD" && !DoEnableflag)
            {
                DoEnableflag = true;
            }

            if (_chamberIsInstalled && _chamberType == "PVD" && _scPowerType.StringValue == "DCPulse" && !DoEnablePulseMode)
            {
                DoEnablePulseMode = true;
            }

            if (_chamberIsInstalled && _chamberType == "PVD")
            {
                IsError = !CommunicationStatus;

                _trigCommunicationAlam.CLK = !CommunicationStatus;
                if (_trigCommunicationAlam.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module} {Name}  Communication Err");
                }



                _trigRfOnOff.CLK = DiStatus;
                if (_trigRfOnOff.R)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is on");

                    if (_scRetryCount.IntValue > 0 && _RetryCount <= _scRetryCount.IntValue && !_RetryOK)
                    {
                        _RetryDelayChecktimer.Start(2000);

                    }
                    else
                    {
                        _RetryCount = 0;
                    }
                }

                if (_trigRfOnOff.T)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is off");
                    _RetryOK = false;
                    if (_RetryCount > 0)
                    {
                        _RetryFailedDelaytimer.Start(500);
                    }

                }

                #region Retry

                _trigRetryDelayCheck.CLK = _RetryDelayChecktimer.IsTimeout();
                if (_trigRetryDelayCheck.Q)
                {
                    _RetryCount++;

                    if (ForwardPower >= PowerSetPoint * (1 - _scRetryAlarmRange.DoubleValue / 100f) && ForwardPower <= PowerSetPoint * (1 + _scRetryAlarmRange.DoubleValue / 100f))
                    {
                        _RetryCount = 0;
                        _RetryOK = true;

                    }
                    else
                    {
                        if (_RetryCount > _scRetryCount.IntValue)
                        {
                            _RetryCount = 0;
                            EV.PostAlarmLog(Module, $"{Module}.{Name} Retry: {_scRetryCount.IntValue} Failed, PowerSetPoint: {PowerSetPoint}W, ForwardPower: {ForwardPower}W");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }
                        else
                        {
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry: {_RetryCount} , PowerSetPoint: {PowerSetPoint}W, ForwardPower: {ForwardPower}W");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }


                    }


                    _RetryDelayChecktimer.Stop();
                }

                _trigRetryFailedDelay.CLK = _RetryFailedDelaytimer.IsTimeout();
                if (_trigRetryFailedDelay.Q)
                {
                    SetPower(AoSetPowerValue);
                    SetPowerOnOff(true, out _);
                    EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power On");


                }

                #endregion

            }


            if (FuncForceAction != null)
            {
                _trigForceAction.CLK = FuncForceAction(DiStatus);
                if (_trigForceAction.Q)
                {
                    SetPower(0);
                    SetPowerOnOff(false, out string reason);
                    //EV.PostAlarmLog(Module, $"Force set {Name} off for interlock");
                }
            }

        }

        public override void Reset()
        {
            base.Reset();

            _trigErrorCode.RST = true;
            _trigAlam.RST = true;

            _trigForceAction.RST = true;
        }

        public override void Terminate()
        {

        }


        public override float ForwardPower
        {
            get { return AiQueryforwardpower; }
        }



        public override float PowerSetPoint
        {
            get { return AoSetPowerValue; }
        }


        #region DI




        public override bool DiStatus
        {
            get
            {
                return _diStatus == null ? false : _diStatus.Value;
            }
        }

        public override bool CommunicationStatus
        {
            get
            {
                return _diCommunicationStatus == null ? false : _diCommunicationStatus.Value;
            }
        }




        #endregion DI
        #region DO

        private bool _DoPowerOn;
        public bool DoPowerOn
        {
            get
            {
              

                return _DoPowerOn;
            }
            set
            {
                if (_doPowerOn != null)
                {
                    _doPowerOn.Value = value;
                    _DoPowerOn = value;
                }
            }
        }

        public bool DoEnableflag
        {
            get
            {
                if (_doEnableflag != null)
                    return _doEnableflag.Value;

                return false;
            }
            set
            {
                if (_doEnableflag != null)
                {
                    _doEnableflag.Value = value;
                }
            }
        }

        public bool DoEnablePulseMode
        {
            get
            {
                if (_doEnablePulseMode != null)
                    return _doEnablePulseMode.Value;

                return false;
            }
            set
            {
                if (_doEnablePulseMode != null)
                {
                    _doEnablePulseMode.Value = value;
                }
            }
        }




        #endregion DO
        #region AI
        public float AiQuerystatus
        {
            get
            {
                return _aiQuerystatus == null ? 0 : (_isFloatAioType ? _aiQuerystatus.FloatValue : _aiQuerystatus.Value);
            }
        }
        public float AiQueryforwardpower
        {
            get
            {
                return _aiQueryforwardpower == null ? 0 : (_isFloatAioType ? _aiQueryforwardpower.FloatValue*_factor : _aiQueryforwardpower.Value*_factor);
            }
        }
        public float AiQueryforwardvoltage
        {
            get
            {
                return _aiQueryforwardvoltage == null ? 0 : (_isFloatAioType ? _aiQueryforwardvoltage.FloatValue : _aiQueryforwardvoltage.Value);
            }
        }
        public float AiQueryforwardcurrent
        {
            get
            {
                return _aiQueryforwardcurrent == null ? 0 : (_isFloatAioType ? _aiQueryforwardcurrent.FloatValue/100 : (float)_aiQueryforwardcurrent.Value/100);
            }
        }
        public float AiQueryregulationmode
        {
            get
            {
                return _aiQueryregulationmode == null ? 0 : (_isFloatAioType ? _aiQueryregulationmode.FloatValue : _aiQueryregulationmode.Value);
            }
        }
        public float AiQuerySetpoint
        {
            get
            {
                return _aiQuerySetpoint == null ? 0 : (_isFloatAioType ? _aiQuerySetpoint.FloatValue*_factor : _aiQuerySetpoint.Value*_factor);
            }
        }




        public float AiQueryRegulationmode
        {
            get
            {
                return _aiQueryRegulationmode == null ? 0 : (_isFloatAioType ? _aiQueryRegulationmode.FloatValue : _aiQueryRegulationmode.Value);
            }

        }
   
        public float AiQuerypulsefrequencyindex
        {
            get
            {
                return _aiQuerypulsefrequencyindex == null ? 0 : (_isFloatAioType ? _aiQuerypulsefrequencyindex.FloatValue*5 : _aiQuerypulsefrequencyindex.Value*5);
            }

        }
        public float AiQuerypulsereversetime
        {
            get
            {
                return _aiQuerypulsereversetime == null ? 0 : (_isFloatAioType ? _aiQuerypulsereversetime.FloatValue/10 : (float)_aiQuerypulsereversetime.Value/10);
            }

        }




        #endregion AI
        #region AO

        private float _AoSetPowerValue;
        public float AoSetPowerValue
        {
            get
            {
                return _AoSetPowerValue;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetPowerValue.FloatValue = value/_factor;
                }
                else
                {
                    _aoSetPowerValue.Value = (short)(value/_factor);
                }

                _AoSetPowerValue = value;
            }
        }

        public float AoRegulationMode
        {
            get
            {
                return _aoRegulationMode == null ? 0 : (_isFloatAioType ? _aoRegulationMode.FloatValue : _aoRegulationMode.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoRegulationMode.FloatValue = value;
                }
                else
                {
                    _aoRegulationMode.Value = (short)value;
                }
            }
        }

        private float _AoPulseFrequencyindex;
        public float AoPulseFrequencyindex
        {
            get
            {
                return _AoPulseFrequencyindex;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPulseFrequencyindex.FloatValue = value/5;
                }
                else
                {
                    _aoPulseFrequencyindex.Value = (short)(value/5);
                }

                _AoPulseFrequencyindex = value;
            }
        }

        private float _AoPulseReverseTime;
        public float AoPulseReverseTime
        {
            get
            {
                return _AoPulseReverseTime;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoPulseReverseTime.FloatValue = value*10;
                }
                else
                {
                    _aoPulseReverseTime.Value = (short)(value*10);
                }

                _AoPulseReverseTime = value;
            }
        }







        #endregion AO

        public override AITRfPowerData DeviceData
        {
            get
            {
                AITRfPowerData data = new AITRfPowerData()
                {
                    Module = Module,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    UnitPower = _unit,

                    ForwardPower = AiQueryforwardpower,
                    //ReflectPower = ReflectPower,
                    PowerSetPoint = AoSetPowerValue,
                    //RegulationMode = AiQueryRegulationmode,
                    Voltage = AiQueryforwardvoltage,
                    Current = AiQueryforwardcurrent,

                    IsRfOn = DiStatus,
                    //IsRfAlarm = IsError,

                    //Frequency = Frequency,
                    PulsingFrequency = AiQuerypulsefrequencyindex,
                    //PulsingDutyCycle = PulsingDutyCycle,
                    PulsingReverseTime = AiQuerypulsereversetime,

                    ScalePower = _scalePower,
                    IsPulseType = _scPowerType.StringValue == "DCPulse",
                };



                return data;
            }
        }




  
     

    }
}
