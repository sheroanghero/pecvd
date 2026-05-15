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

    public class IoMksRfPlasmaGenerator : IoPowerBase
    {
       
        private DIAccessor _diStatus;
        private DIAccessor _diCommunicationStatus;
       

        private DOAccessor _doPowerOn;
        private DOAccessor _doEnableflag;



        private AIAccessor _aiFAultInBinary;
        private AIAccessor _aiReversePower;
        private AIAccessor _aiDeliveredPower;
  


        private AOAccessor _aoSetPoint;
        private AOAccessor _aoSetPulseHighTime;
        private AOAccessor _aoSetPulseLowTime;


  


        private R_TRIG _trigAlam = new R_TRIG();

        private R_TRIG _trigErrorCode = new R_TRIG();




        public Func<bool, bool> FuncCheckInterLock;
        public Func<bool, bool> FuncForceAction;

        private R_TRIG _trigForceAction = new R_TRIG();

        private SCConfigItem _scPowerIsInstalled;




        private bool _isFloatAioType = false;


        private DeviceTimer _setTimer = new DeviceTimer();
        public IoMksRfPlasmaGenerator(string module, XmlElement node, string ioModule = "") : base(module, node, ioModule)
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _scPowerIsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.BiasRFIsInstalled");

            _diStatus = ParseDiNode("diStatus", node, ioModule);
            _diCommunicationStatus = ParseDiNode("diCommunicationStatus", node, ioModule);


 
            _doPowerOn = ParseDoNode("doPowerOn", node, ioModule);
            _doEnableflag = ParseDoNode("doEnableflag", node, ioModule);
            

            _aiFAultInBinary = ParseAiNode("aiFAultInBinary", node, ioModule);
            _aiReversePower = ParseAiNode("aiReversePower", node, ioModule);
            _aiDeliveredPower = ParseAiNode("aiDeliveredPower", node, ioModule);
 
          
            _aoSetPoint = ParseAoNode("aoSetPoint", node, ioModule);
            _aoSetPulseHighTime = ParseAoNode("aoSetPulseHighTime", node, ioModule);
            _aoSetPulseLowTime = ParseAoNode("aoSetPulseLowTime", node, ioModule);
     
        
        }


        public override bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.ReversePower", () => AiReversePower);
            DATA.Subscribe($"{Module}.{Name}.DeliveredPower", () => AiDeliveredPower);

            base.Initialize();




            return true;
        }

        public override void SetPower(float power)
        {
            AoSetPoint = power;

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
            get { return AiDeliveredPower; }
        }



        public override float PowerSetPoint
        {
            get { return AoSetPoint; }
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
        public float AiFAultInBinary
        {
            get
            {
                return _aiFAultInBinary == null ? 0 : (_isFloatAioType ? _aiFAultInBinary.FloatValue : _aiFAultInBinary.Value);
            }
        }
        public float AiReversePower
        {
            get
            {
                return _aiReversePower == null ? 0 : (_isFloatAioType ? _aiReversePower.FloatValue : _aiReversePower.Value);
            }
        }
        public float AiDeliveredPower
        {
            get
            {
                return _aiDeliveredPower == null ? 0 : (_isFloatAioType ? _aiDeliveredPower.FloatValue : _aiDeliveredPower.Value);
            }
        }



        #endregion AI
        #region AO

        private float _AoSetPoint;
        public float AoSetPoint
        {
            get
            {
                return _AoSetPoint;
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetPoint.FloatValue = value;
                }
                else
                {
                    _aoSetPoint.Value = (short)value;
                }

                _AoSetPoint = value;
            }
        }

        public float AoSetPulseHighTime
        {
            get
            {
                return _aoSetPulseHighTime == null ? 0 : (_isFloatAioType ? _aoSetPulseHighTime.FloatValue : _aoSetPulseHighTime.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetPulseHighTime.FloatValue = value;
                }
                else
                {
                    _aoSetPulseHighTime.Value = (short)value;
                }
            }
        }


        public float AoSetPulseLowTime
        {
            get
            {
                return _aoSetPulseLowTime == null ? 0 : (_isFloatAioType ? _aoSetPulseLowTime.FloatValue : _aoSetPulseLowTime.Value);
            }
            set
            {
                if (_isFloatAioType)
                {
                    _aoSetPulseLowTime.FloatValue = value;
                }
                else
                {
                    _aoSetPulseLowTime.Value = (short)value;
                }
            }
        }








        #endregion AO

        public override AITRfPowerData DeviceData
        {
            get
            {
                AITRfPowerData data = new AITRfPowerData()
                {
                    DeviceName = Name,
                    Module = Module,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    ForwardPower = ForwardPower,

                    ReflectPower = AiReversePower,
          
                    PowerSetPoint = PowerSetPoint,
    
                    ScalePower = _scalePower,

                    IsRfOn = DiStatus,
                  

                  

                    UnitPower = "W",
                };

                return data;
            }
        }







    }
}
