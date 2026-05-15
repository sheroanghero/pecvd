using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.DataCenter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Aitex.Core.RT.Device.Unit
{

    public class IoDxkdpDCPower : IoPowerBase
    {
       
        private DIAccessor _diStatus;
        private DIAccessor _diCommunicationStatus;
       

        private DOAccessor _doPowerOn;
        private DOAccessor _doEnableflag;



        private AIAccessor _aiQuerystatus;
        private AIAccessor _aiActualVoltage;
        private AIAccessor _aiActualCurrent;

        private AIAccessor _aiQueryforwardpower;

       



        private AOAccessor _aoSetElectricity;
        private AOAccessor _aoSetVoltage;
    

  


        private R_TRIG _trigAlam = new R_TRIG();

        private R_TRIG _trigErrorCode = new R_TRIG();



        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();


        private SCConfigItem _scPowerIsInstalled;

        private RD_TRIG _trigRfOnOff = new RD_TRIG();
        public bool _RetryOK;
        private int _RetryCount;
        public SCConfigItem _scRetryCount;
        public SCConfigItem _scRetryAlarmRange;
        private DeviceTimer _RetryDelayChecktimer = new DeviceTimer();
        private DeviceTimer _RetryFailedDelaytimer = new DeviceTimer();
        private R_TRIG _trigRetryDelayCheck = new R_TRIG();
        private R_TRIG _trigRetryFailedDelay = new R_TRIG();


        private float _scaleCurrent;

        private bool _isFloatAioType = false;



        private DeviceTimer _setTimer = new DeviceTimer();
        public IoDxkdpDCPower(string module, XmlElement node, string ioModule = "") : base(module, node, ioModule)
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _scPowerIsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.MagnetCoilIsInstalled");


            _diStatus = ParseDiNode("diStatus", node, ioModule);
            _diCommunicationStatus = ParseDiNode("diCommunicationStatus", node, ioModule);


 
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doEnableflag = ParseDoNode("doEnableflag", node, ioModule);
            

            _aiQuerystatus = ParseAiNode("aiQueryStatus", node, ioModule);
            _aiQueryforwardpower = ParseAiNode("aiQueryforwardpower", node, ioModule);
            _aiActualVoltage = ParseAiNode("aiActualVoltage", node, ioModule);
            _aiActualCurrent = ParseAiNode("aiActualCurrent", node, ioModule);

    
            _aoSetElectricity = ParseAoNode("aoSetElectricity", node, ioModule);
            _aoSetVoltage = ParseAoNode("aoSetVoltage", node, ioModule);
      
        
        }


        public override bool  Initialize()
        {
            base.Initialize();


            DATA.Subscribe($"{Module}.{Name}.Current", () => AiActualCurrent);
            DATA.Subscribe($"{Module}.{Name}.Voltage", () => AiActualVoltage);

            OP.Subscribe($"{Module}.{Name}.SetVoltage", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!SetVoltage(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set Voltage, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set Voltage to {args[0]}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetCurrent", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                SetCurrent(value);

                EV.PostInfoLog(Module, $"{Module}.{Name} set Current to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetCurrentForRecipe", (out string reason, int time, object[] args) =>
            {
                float value = Convert.ToSingle(args[0]);
                bool isOn = value > 0;
                reason = string.Empty;

                SetCurrent(value);

                if (isOn ^ DiStatus)
                {
                    SetPowerOnOff(isOn, out _);
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set Current to {value}");
                return true;
            });


            return true;
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

            return true;
        }

        public void SetCurrent(float current)
        {
            AoSetElectricity = current;

         
        }


        public bool SetVoltage(out string reason, int time, float voltage)
        {
            reason = string.Empty;

            AoSetVoltage = voltage;

            return true;
        }


        public override void Monitor()
        {
            base.Monitor();

            if (_chamberIsInstalled && _chamberType == "PVD" && _scPowerIsInstalled.BoolValue && _scEnableAlarm == null && SC.ContainsItem($"{Module}.{Name}.EnableAlarm"))
            {
                _unit = SC.GetStringValue($"{Module}.{Name}.Unit");
                _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");


                _scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
                _scAlarmTime = SC.GetConfigItem($"{Module}.{Name}.AlarmTime");
                _scAlarmRange = SC.GetConfigItem($"{Module}.{Name}.AlarmRange");
                _scWarningTime = SC.GetConfigItem($"{Module}.{Name}.WarningTime");
                _scWarningRange = SC.GetConfigItem($"{Module}.{Name}.WarningRange");

                _scaleCurrent = (float)SC.GetValue<double>($"{Module}.{Name}.ScaleCurrent");

                _scRetryCount = SC.GetConfigItem($"{Module}.{Name}.RetryCount");
                _scRetryAlarmRange = SC.GetConfigItem($"{Module}.{Name}.RetryAlarmRange");

            }

            if (_chamberIsInstalled && _chamberType == "PVD" && _scPowerIsInstalled.BoolValue && !DoEnableflag)
            {
                DoEnableflag = true;
            }

            if (_chamberIsInstalled && _chamberType == "PVD" && _scPowerIsInstalled.BoolValue)
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

                    if (AiActualCurrent >= AoSetElectricity * (1 - _scRetryAlarmRange.DoubleValue / 100f) && AiActualCurrent <= AoSetElectricity * (1 + _scRetryAlarmRange.DoubleValue / 100f))
                    {
                        _RetryCount = 0;
                        _RetryOK = true;

                    }
                    else
                    {
                        if (_RetryCount > _scRetryCount.IntValue)
                        {
                            _RetryCount = 0;
                            EV.PostAlarmLog(Module, $"{Module}.{Name} Retry: {_scRetryCount.IntValue} Failed, SetCurrent: {AoSetElectricity}A, Current: {AiActualCurrent}A");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }
                        else
                        {
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry: {_RetryCount} , SetCurrent: {AoSetElectricity}A, Current: {AiActualCurrent}A");
                            SetPowerOnOff(false, out _);
                            EV.PostInfoLog(Module, $"{Module}.{Name} Retry Set Power Off");

                        }


                    }


                    _RetryDelayChecktimer.Stop();
                }

                _trigRetryFailedDelay.CLK = _RetryFailedDelaytimer.IsTimeout();
                if (_trigRetryFailedDelay.Q)
                {
                    SetCurrent(AoSetElectricity);
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
                    SetCurrent(0);
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
                return _aiQueryforwardpower == null ? 0 : (_isFloatAioType ? _aiQueryforwardpower.FloatValue : _aiQueryforwardpower.Value);
            }
        }
        public float AiActualVoltage
        {
            get
            {
                return _aiActualVoltage == null ? 0 : (_isFloatAioType ? _aiActualVoltage.FloatValue/100 : (float)_aiActualVoltage.Value/100);
            }
        }
        public float AiActualCurrent
        {
            get
            {
                return _aiActualCurrent == null ? 0 : (_isFloatAioType ? _aiActualCurrent.FloatValue/1000 : (float)_aiActualCurrent.Value/1000);
            }
        }










        #endregion AI
        #region AO

        private float _AoSetElectricity;
        public float AoSetElectricity
        {
            get
            {
                return _AoSetElectricity;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetElectricity.FloatValue = value*1000;
                }
                else
                {
                    _aoSetElectricity.Value = (short)(value*1000);
                }

                _AoSetElectricity = value;
            }
        }

        private float _AoSetVoltage;
        public float AoSetVoltage
        {
            get
            {
                return _AoSetVoltage;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetVoltage.FloatValue = value*100;
                }
                else
                {
                    _aoSetVoltage.Value = (short)(value*100);
                }

                _AoSetVoltage = value;
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

                    ForwardPower = ForwardPower,
                 
                    PowerSetPoint = PowerSetPoint,
  
                    Voltage = AiActualVoltage,
                    Current = AiActualCurrent,

                    CurrentSetPoint = AoSetElectricity,
                    VoltageSetPoint = AoSetVoltage,

                    ScaleCurrent = _scaleCurrent,

                    UnitCurrent = "A",
                    UnitVoltage = "V",

                    IsRfOn = DiStatus,
                    

        
                };

                return data;
            }
        }







    }
}
