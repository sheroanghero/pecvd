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

    public class IoRFPower : IoPowerBase
    {
       
        private DIAccessor _diStatus;
        private DIAccessor _diCommunicationStatus;
       

        private DOAccessor _doPowerOn;
        private DOAccessor _doEnableflag;

        private DOAccessor _doErrorReset;

        private AIAccessor _aiQuerySetpoint;
        private AIAccessor _aiActualForward;
        private AIAccessor _aiActualRefected;
        private AIAccessor _aiErrorCode;
    



        private AOAccessor _aoSetpoint;
 

  


        private R_TRIG _trigAlam = new R_TRIG();

        private R_TRIG _trigErrorCode = new R_TRIG();




        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();




        private bool _isFloatAioType = false;


        private float _factor;


        private DeviceTimer _setTimer = new DeviceTimer();
        public IoRFPower(string module, XmlElement node, string ioModule = "") : base(module, node, ioModule)
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _diStatus = ParseDiNode("diStatus", node, ioModule);
            _diCommunicationStatus = ParseDiNode("diCommunicationStatus", node, ioModule);


 
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doEnableflag = ParseDoNode("doEnableflag", node, ioModule);
            _doErrorReset = ParseDoNode("doErrorReset", node, ioModule);

            _aiQuerySetpoint = ParseAiNode("aiQuerySetpoint", node, ioModule);
            _aiActualForward = ParseAiNode("aiActualForward", node, ioModule);
            _aiActualRefected = ParseAiNode("aiActualRefected", node, ioModule);
            _aiErrorCode = ParseAiNode("aiErrorCode", node, ioModule);
          
            
          
            _aoSetpoint = ParseAoNode("aoSetpoint", node, ioModule);
    
        
        }

        public override void SetPower(float power)
        {
            AoSetpoint = power;

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


        public override bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.ActualForward", () => AiActualForward);
            DATA.Subscribe($"{Module}.{Name}.ActualRefected", () => AiActualRefected);

            base.Initialize();

            return true;
        }

        public override void Monitor()
        {
            base.Monitor();

            if (_chamberIsInstalled && _chamberType == "Clean" && _scEnableAlarm == null && SC.ContainsItem($"{Module}.{Name}.EnableAlarm"))
            {
                _unit = SC.GetStringValue($"{Module}.{Name}.Unit");
                _scalePower = (float)SC.GetValue<double>($"{Module}.{Name}.ScalePower");
                _factor = (float)SC.GetValue<double>($"{Module}.{Name}.Factor");

                _scEnableAlarm = SC.GetConfigItem($"{Module}.{Name}.EnableAlarm");
                _scAlarmTime = SC.GetConfigItem($"{Module}.{Name}.AlarmTime");
                _scAlarmRange = SC.GetConfigItem($"{Module}.{Name}.AlarmRange");
                _scWarningTime = SC.GetConfigItem($"{Module}.{Name}.WarningTime");
                _scWarningRange = SC.GetConfigItem($"{Module}.{Name}.WarningRange");

            }

            if (_chamberIsInstalled && _chamberType == "Clean" && !DoEnableflag)
            {
                DoEnableflag = true;
            }

            if (_chamberIsInstalled && _chamberType == "Clean" )
            {
                IsError = !CommunicationStatus;

                _trigCommunicationAlam.CLK = !CommunicationStatus;
                if (_trigCommunicationAlam.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module} {Name}  Communication Err");
                }
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
            get { return AiActualForward/_factor; }
        }

        public override float PowerSetPoint
        {
            get { return AoSetpoint; }
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
        public float AiActualForward
        {
            get
            {
                return _aiActualForward == null ? 0 : (_isFloatAioType ? _aiActualForward.FloatValue : _aiActualForward.Value);
            }
        }
        public float AiActualRefected
        {
            get
            {
                return _aiActualRefected == null ? 0 : (_isFloatAioType ? _aiActualRefected.FloatValue : _aiActualRefected.Value);
            }
        }
        public float AiErrorCode
        {
            get
            {
                return _aiErrorCode == null ? 0 : (_isFloatAioType ? _aiErrorCode.FloatValue : _aiErrorCode.Value);
            }
        }
      
   
        public float AiQuerySetpoint
        {
            get
            {
                return _aiQuerySetpoint == null ? 0 : (_isFloatAioType ? _aiQuerySetpoint.FloatValue : _aiQuerySetpoint.Value);
            }
        }










        #endregion AI
        #region AO

        private float _AoSetpoint;
        public float AoSetpoint
        {
            get
            {
                return _AoSetpoint;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetpoint.FloatValue = value*_factor;
                }
                else
                {
                    _aoSetpoint.Value = (short)(value*_factor);
                }

                _AoSetpoint = value;
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
                    ReflectPower = AiActualRefected,
                    PowerSetPoint = PowerSetPoint,


                    IsRfOn = DiStatus,



                    ScalePower = _scalePower
                };

                return data;
            }
        }







    }
}
