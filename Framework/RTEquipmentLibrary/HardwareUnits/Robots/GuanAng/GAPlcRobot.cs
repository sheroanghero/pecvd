using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Threading;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System.Xml;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Routine;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using MECF.Framework.Common.CommonData;
using EventType = Aitex.Core.RT.Event.EventType;

namespace Aitex.Core.RT.Device.Unit
{
    public class GAPlcRobot:RobotBaseDevice
    {
        public GAPlcRobot(string module,XmlElement node,string ioModule =""): base(node.GetAttribute("module"), node.GetAttribute("id"))
        {
            base.Module = node.GetAttribute("module");//string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            ioModule = node.GetAttribute("ioModule");

            _aiCurrentXPos_L = ParseAiNode("AI_CurrentXPos_L", node, ioModule);
            _aiCurrentXPos_H = ParseAiNode("AI_CurrentXPos_H", node, ioModule);
            _aiCurrentYPos_L = ParseAiNode("AI_CurrentYPos_L", node, ioModule);
            _aiCurrentYPos_H = ParseAiNode("AI_CurrentYPos_H", node, ioModule);
            _aiCurrentZPos_L = ParseAiNode("AI_CurrentZPos_L", node, ioModule);
            _aiCurrentZPos_H = ParseAiNode("AI_CurrentZPos_H", node, ioModule);

            _aiZAxisPitch_L = ParseAiNode("AI_ZAxisPitch_L", node, ioModule);
            _aiZAxisPitch_H = ParseAiNode("AI_ZAxisPitch_H", node, ioModule);
            _aiZAxisFirstPos_L = ParseAiNode("AI_ZAxisFirstPos_L", node, ioModule);
            _aiZAxisFirstPos_H = ParseAiNode("AI_ZAxisFirstPos_H", node, ioModule);
            _aiZAxisPlacePos_L = ParseAiNode("AI_ZAxisPlacePos_L", node, ioModule);
            _aiZAxisPlacePos_H = ParseAiNode("AI_ZAxisPlacePos_H", node, ioModule);
            _aiXAxisMoveDistance_L = ParseAiNode("AI_XAxisMoveDistance_L", node, ioModule);
            _aiXAxisMoveDistance_H = ParseAiNode("AI_XAxisMoveDistance_H", node, ioModule);
            _aiYAxisMoveDistance_L = ParseAiNode("AI_YAxisMoveDistance_L", node, ioModule);
            _aiYAxisMoveDistance_H = ParseAiNode("AI_YAxisMoveDistance_H", node, ioModule);
            _aiZAxisMoveSpeed_L = ParseAiNode("AI_ZAxisMoveSpeed_L", node, ioModule);
            _aiZAxisMoveSpeed_H = ParseAiNode("AI_ZAxisMoveSpeed_H", node, ioModule);
            _aiXAxisMoveSpeed_L = ParseAiNode("AI_XAxisMoveSpeed_L", node, ioModule);
            _aiXAxisMoveSpeed_H = ParseAiNode("AI_XAxisMoveSpeed_H", node, ioModule);
            _aiYAxisMoveSpeed_L = ParseAiNode("AI_YAxisMoveSpeed_L", node, ioModule);
            _aiYAxisMoveSpeed_H = ParseAiNode("AI_YAxisMoveSpeed_H", node, ioModule);



            _aiTargetPos = ParseAiNode("AI_TargetPos", node, ioModule);






            _aoZAxisPitch_L = ParseAoNode("AO_ZAxisPitch_L", node, ioModule);
            _aoZAxisPitch_H = ParseAoNode("AO_ZAxisPitch_H", node, ioModule);
            _aoZAxisFirstPos_L = ParseAoNode("AO_ZAxisFirstPos_L", node, ioModule);
            _aoZAxisFirstPos_H = ParseAoNode("AO_ZAxisFirstPos_H", node, ioModule);
            _aoZAxisPlacePos_L = ParseAoNode("AO_ZAxisPlacePos_L", node, ioModule);
            _aoZAxisPlacePos_H = ParseAoNode("AO_ZAxisPlacePos_H", node, ioModule);
            _aoXAxisMoveDistance_L = ParseAoNode("AO_XAxisMoveDistance_L", node, ioModule);
            _aoXAxisMoveDistance_H = ParseAoNode("AO_XAxisMoveDistance_H", node, ioModule);
            _aoYAxisMoveDistance_L = ParseAoNode("AO_YAxisMoveDistance_L", node, ioModule);
            _aoYAxisMoveDistance_H = ParseAoNode("AO_YAxisMoveDistance_H", node, ioModule);
            _aoZAxisMoveSpeed_L = ParseAoNode("AO_ZAxisMoveSpeed_L", node, ioModule);
            _aoZAxisMoveSpeed_H = ParseAoNode("AO_ZAxisMoveSpeed_H", node, ioModule);
            _aoXAxisMoveSpeed_L = ParseAoNode("AO_XAxisMoveSpeed_L", node, ioModule);
            _aoXAxisMoveSpeed_H = ParseAoNode("AO_XAxisMoveSpeed_H", node, ioModule);
            _aoYAxisMoveSpeed_L = ParseAoNode("AO_YAxisMoveSpeed_L", node, ioModule);
            _aoYAxisMoveSpeed_H = ParseAoNode("AO_YAxisMoveSpeed_H", node, ioModule);
            _aoTargetPos = ParseAoNode("AO_TargetPos", node, ioModule);
            _aoXMoveFwdDistance_L = ParseAoNode("AO_XMoveFwdDistance_L", node, ioModule);
            _aoXMoveFwdDistance_H = ParseAoNode("AO_XMoveFwdDistance_H", node, ioModule);
            _aoYMoveFwdDistance_L = ParseAoNode("AO_YMoveFwdDistance_L", node, ioModule);
            _aoYMoveFwdDistance_H = ParseAoNode("AO_YMoveFwdDistance_H", node, ioModule);
            _aoZMoveFwdDistance_L = ParseAoNode("AO_ZMoveFwdDistance_L", node, ioModule);
            _aoZMoveFwdDistance_H = ParseAoNode("AO_ZMoveFwdDistance_H", node, ioModule);

            _aoXMoveRevDistance_L = ParseAoNode("AO_XMoveRevDistance_L", node, ioModule);
            _aoXMoveRevDistance_H = ParseAoNode("AO_XMoveRevDistance_H", node, ioModule);
            _aoYMoveRevDistance_L = ParseAoNode("AO_YMoveRevDistance_L", node, ioModule);
            _aoYMoveRevDistance_H = ParseAoNode("AO_YMoveRevDistance_H", node, ioModule);
            _aoZMoveRevDistance_L = ParseAoNode("AO_ZMoveRevDistance_L", node, ioModule);
            _aoZMoveRevDistance_H = ParseAoNode("AO_ZMoveRevDistance_H", node, ioModule);

            _diReset_Ack = ParseDiNode("DI_Reset_Ack", node, ioModule);
            _diHome_Ack = ParseDiNode("DI_Home_Ack", node, ioModule);
            _diSave_Ack = ParseDiNode("DI_Save_Ack", node, ioModule);
            _diPick_Ack = ParseDiNode("DI_Pick_Ack", node, ioModule);
            _diPlace_Ack = ParseDiNode("DI_Place_Ack", node, ioModule);
            _diX_Fwd_Ack = ParseDiNode("DI_X_Fwd_Ack", node, ioModule);
            _diX_Rev_Ack = ParseDiNode("DI_X_Rev_Ack", node, ioModule);
            _diY_Fwd_Ack = ParseDiNode("DI_Y_Fwd_Ack", node, ioModule);
            _diY_Rev_Ack = ParseDiNode("DI_Y_Rev_Ack", node, ioModule);
            _diZ_Fwd_Ack = ParseDiNode("DI_Z_Fwd_Ack", node, ioModule);
            _diZ_Rev_Ack = ParseDiNode("DI_Z_Rev_Ack", node, ioModule);
            _diPlugError = ParseDiNode("DI_PlugError", node, ioModule);
            _diZMotionError = ParseDiNode("DI_ZMotionError", node, ioModule);
            _diXMotionError = ParseDiNode("DI_XMotionError", node, ioModule);
            _diYMotionError = ParseDiNode("DI_YMotionError", node, ioModule);
            _diZMotionOutLimit = ParseDiNode("DI_ZMotionOutLimit", node, ioModule);
            _diXMotionOutLimit = ParseDiNode("DI_XMotionOutLimit", node, ioModule);
            _diYMotionOutLimit = ParseDiNode("DI_YMotionOutLimit", node, ioModule);
            _diPLCReady = ParseDiNode("DI_PLCReady", node, ioModule);
            _diMotionIdle = ParseDiNode("DI_MotionIdle", node, ioModule);

            _doResetCmd = ParseDoNode("DO_ResetCmd", node, ioModule);
            _doHomeCmd = ParseDoNode("DO_HomeCmd", node, ioModule);
            _doSaveCmd = ParseDoNode("DO_SaveCmd", node, ioModule);
            _doPickCmd = ParseDoNode("DO_PickCmd", node, ioModule);
            _doPlaceCmd = ParseDoNode("DO_PlaceCmd", node, ioModule);
            _doX_FwdCmd = ParseDoNode("DO_X_FwdCmd", node, ioModule);
            _doX_RevCmd = ParseDoNode("DO_X_RevCmd", node, ioModule);
            _doY_FwdCmd = ParseDoNode("DO_Y_FwdCmd", node, ioModule);
            _doY_RevCmd = ParseDoNode("DO_Y_RevCmd", node, ioModule);
            _doZ_FwdCmd = ParseDoNode("DO_Z_FwdCmd", node, ioModule);
            _doZ_RevCmd = ParseDoNode("DO_Z_RevCmd", node, ioModule);


            EV.Subscribe(new EventItem("Alarm", AlarmRobotError, $"Robot occurred error.", EventLevel.Alarm, EventType.EventUI_Notify));

            DATA.Subscribe($"{Name}.PLCReady", () => _diPLCReady.Value);
            DATA.Subscribe($"{Name}.MotionIdle", () => _diMotionIdle.Value);

            DATA.Subscribe($"{Name}.SlotPitch", () => _slotPitch);
            DATA.Subscribe($"{Name}.FirstZPosition", () => _zFirstPosition);
            DATA.Subscribe($"{Name}.PlaceWaferZPosition", () => _zPutWaferPosition);
            DATA.Subscribe($"{Name}.XMoveDistance", () => _xMoveDistance);
            DATA.Subscribe($"{Name}.YMoveDistance", () => _yMoveDistance);
            DATA.Subscribe($"{Name}.ZMoveSpeed", () => _zMoveSpeed);
            DATA.Subscribe($"{Name}.YMoveSpeed", () => _yMoveSpeed);
            DATA.Subscribe($"{Name}.XMoveSpeed", () => _xMoveSpeed);

            DATA.Subscribe($"{Name}.CurrentXPosition", () => _currentXPosition);
            DATA.Subscribe($"{Name}.CurrentYPosition", () => _currentYPosition);
            DATA.Subscribe($"{Name}.CurrentZPosition", () => _currentZPosition);


            _trigErrorOccurred = new R_TRIG();
            _thread = new PeriodicJob(10, OnTimer, $"{Module}.{Name} MonitorHandler", true);

        }
        private string AlarmRobotError = "RobotOccurredError";

        

        private AIAccessor _aiZAxisPitch_L;
        private AIAccessor _aiZAxisPitch_H;
        private AIAccessor _aiZAxisFirstPos_L;
        private AIAccessor _aiZAxisFirstPos_H;
        private AIAccessor _aiZAxisPlacePos_L;
        private AIAccessor _aiZAxisPlacePos_H;
        private AIAccessor _aiXAxisMoveDistance_L;
        private AIAccessor _aiXAxisMoveDistance_H;
        private AIAccessor _aiYAxisMoveDistance_L;
        private AIAccessor _aiYAxisMoveDistance_H;
        private AIAccessor _aiZAxisMoveSpeed_L;
        private AIAccessor _aiZAxisMoveSpeed_H;
        private AIAccessor _aiXAxisMoveSpeed_L;
        private AIAccessor _aiXAxisMoveSpeed_H;
        private AIAccessor _aiYAxisMoveSpeed_L;
        private AIAccessor _aiYAxisMoveSpeed_H;
        private AIAccessor _aiTargetPos;

        private AIAccessor _aiCurrentXPos_L;
        private AIAccessor _aiCurrentXPos_H;

        private AIAccessor _aiCurrentYPos_L;
        private AIAccessor _aiCurrentYPos_H;

        private AIAccessor _aiCurrentZPos_L;
        private AIAccessor _aiCurrentZPos_H;

        private AOAccessor _aoZAxisPitch_L;
        private AOAccessor _aoZAxisPitch_H;
        private AOAccessor _aoZAxisFirstPos_L;
        private AOAccessor _aoZAxisFirstPos_H;
        private AOAccessor _aoZAxisPlacePos_L;
        private AOAccessor _aoZAxisPlacePos_H;
        private AOAccessor _aoXAxisMoveDistance_L;
        private AOAccessor _aoXAxisMoveDistance_H;
        private AOAccessor _aoYAxisMoveDistance_L;
        private AOAccessor _aoYAxisMoveDistance_H;
        private AOAccessor _aoZAxisMoveSpeed_L;
        private AOAccessor _aoZAxisMoveSpeed_H;
        private AOAccessor _aoXAxisMoveSpeed_L;
        private AOAccessor _aoXAxisMoveSpeed_H;
        private AOAccessor _aoYAxisMoveSpeed_L;
        private AOAccessor _aoYAxisMoveSpeed_H;
        private AOAccessor _aoTargetPos;
        private AOAccessor _aoXMoveFwdDistance_L;
        private AOAccessor _aoXMoveFwdDistance_H;
        private AOAccessor _aoYMoveFwdDistance_L;
        private AOAccessor _aoYMoveFwdDistance_H;
        private AOAccessor _aoZMoveFwdDistance_L;
        private AOAccessor _aoZMoveFwdDistance_H;

        private AOAccessor _aoXMoveRevDistance_L;
        private AOAccessor _aoXMoveRevDistance_H;
        private AOAccessor _aoYMoveRevDistance_L;
        private AOAccessor _aoYMoveRevDistance_H;
        private AOAccessor _aoZMoveRevDistance_L;
        private AOAccessor _aoZMoveRevDistance_H;


        private DIAccessor _diReset_Ack;
        private DIAccessor _diHome_Ack;
        private DIAccessor _diSave_Ack;
        private DIAccessor _diPick_Ack;
        private DIAccessor _diPlace_Ack;
        private DIAccessor _diX_Fwd_Ack;
        private DIAccessor _diX_Rev_Ack;
        private DIAccessor _diY_Fwd_Ack;
        private DIAccessor _diY_Rev_Ack;
        private DIAccessor _diZ_Fwd_Ack;
        private DIAccessor _diZ_Rev_Ack;
        private DIAccessor _diPlugError;
        private DIAccessor _diZMotionError;
        private DIAccessor _diXMotionError;
        private DIAccessor _diYMotionError;
        private DIAccessor _diZMotionOutLimit;
        private DIAccessor _diXMotionOutLimit;
        private DIAccessor _diYMotionOutLimit;
        private DIAccessor _diPLCReady;
        private DIAccessor _diMotionIdle;

        private DOAccessor _doResetCmd;
        private DOAccessor _doHomeCmd;
        private DOAccessor _doSaveCmd;
        private DOAccessor _doPickCmd;
        private DOAccessor _doPlaceCmd;
        private DOAccessor _doX_FwdCmd;
        private DOAccessor _doX_RevCmd;
        private DOAccessor _doY_FwdCmd;
        private DOAccessor _doY_RevCmd;
        private DOAccessor _doZ_FwdCmd;
        private DOAccessor _doZ_RevCmd;

        private PeriodicJob _thread;
        private R_TRIG _trigErrorOccurred;
        private int _currentXPosition
        {
            get
            {
                return TwoShortConvInt(_aiCurrentXPos_L.Value, _aiCurrentXPos_H.Value);
            }
        }
        private int _currentYPosition
        {
            get
            {
                return TwoShortConvInt(_aiCurrentYPos_L.Value, _aiCurrentYPos_H.Value);
            }
        }
        private int _currentZPosition
        {
            get
            {
                return TwoShortConvInt(_aiCurrentZPos_L.Value, _aiCurrentZPos_H.Value);
            }
        }

        private int _slotPitch
        {
            get
            {
                return TwoShortConvInt(_aiZAxisPitch_L.Value, _aiZAxisPitch_H.Value);
            }
        }

        private int _zFirstPosition
        {
            get
            {
                return TwoShortConvInt(_aiZAxisFirstPos_L .Value, _aiZAxisFirstPos_H.Value);
            }
        }

        private int _zPutWaferPosition
        {
            get
            {
                return TwoShortConvInt(_aiZAxisPlacePos_L.Value, _aiZAxisPlacePos_H.Value);
            }
        }


        private int _xMoveDistance
        {
            get
            {
                return TwoShortConvInt(_aiXAxisMoveDistance_L.Value, _aiXAxisMoveDistance_H.Value);
            }
        }
        private int _yMoveDistance
        {
            get
            {
                return TwoShortConvInt(_aiYAxisMoveDistance_L.Value, _aiYAxisMoveDistance_H.Value);
            }
        }
        private int _zMoveSpeed
        {
            get
            {
                return TwoShortConvInt(_aiZAxisMoveSpeed_L.Value, _aiZAxisMoveSpeed_H.Value);
            }
        }
        private int _yMoveSpeed
        {
            get
            {
                return TwoShortConvInt(_aiYAxisMoveSpeed_L.Value, _aiYAxisMoveSpeed_H.Value);
            }
        }
        private int _xMoveSpeed
        {
            get
            {
                return TwoShortConvInt(_aiXAxisMoveSpeed_L.Value, _aiXAxisMoveSpeed_H.Value);
            }
        }
        private bool OnTimer()
        {
            _trigErrorOccurred.CLK =
                _diPlugError.Value || _diZMotionError.Value
                || _diXMotionError.Value || _diYMotionError.Value || _diZMotionOutLimit.Value
                || _diXMotionOutLimit.Value || _diYMotionOutLimit.Value;

            if(_trigErrorOccurred.Q)
            {
                EV.Notify(AlarmRobotError);
                if (_diPlugError.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diPlugError.");
                }
                if (_diZMotionError.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diZMotionError.");
                }
                if (_diXMotionError.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diXMotionError.");
                }
                if (_diYMotionError.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diYMotionError.");
                }
                if (_diZMotionOutLimit.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diZMotionOutLimit.");
                }
                if (_diYMotionOutLimit.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diYMotionOutLimit.");
                }
                if (_diXMotionOutLimit.Value)
                {
                    EV.PostAlarmLog("Robot", $"Robot occurred error: _diXMotionOutLimit.");
                }

                OnError("Robot Error");

            }



            return true;
        }


        #region Override Robot base function

        private enum RobotStepEnum
        {
            ActionStep1,
            ActionStep2,
            ActionStep3,
            ActionStep4,
            ActionStep5,
            ActionStep6,
            ActionStep7,
            ActionStep8,

            ActionStep9,
            ActionStep10,
            ActionStep11,
            ActionStep12,

            ActionStep13,
            ActionStep14,
            ActionStep15,
            ActionStep16,
        }

        private DateTime _dtActionStart;
        
        protected override bool fClear(object[] param)
        {
            return true;
        }

        protected override bool fStartReadData(object[] param)
        {
            return true;
        }

        protected override bool fStartSetParameters(object[] param)
        {
            
            try
            {
                _aoZAxisPitch_L.Value = _aiZAxisPitch_L.Value;
                _aoZAxisPitch_H.Value = _aiZAxisPitch_H.Value;
                _aoZAxisFirstPos_L.Value = _aiZAxisFirstPos_L.Value;
                _aoZAxisFirstPos_H.Value = _aiZAxisFirstPos_H.Value;
                _aoZAxisPlacePos_L.Value = _aiZAxisPlacePos_L.Value;
                _aoZAxisPlacePos_H.Value = _aiZAxisPlacePos_H.Value;
                _aoXAxisMoveDistance_L.Value = _aiXAxisMoveDistance_L.Value;
                _aoXAxisMoveDistance_H.Value = _aiXAxisMoveDistance_H.Value;
                _aoYAxisMoveDistance_L.Value = _aiYAxisMoveDistance_L.Value;
                _aoYAxisMoveDistance_H.Value = _aiYAxisMoveDistance_H.Value;
                _aoZAxisMoveSpeed_L.Value = _aiZAxisMoveSpeed_L.Value;
                _aoZAxisMoveSpeed_H.Value = _aiZAxisMoveSpeed_H.Value;
                _aoXAxisMoveSpeed_L.Value = _aiXAxisMoveSpeed_L.Value;
                _aoXAxisMoveSpeed_H.Value = _aiXAxisMoveSpeed_H.Value;
                _aoYAxisMoveSpeed_L.Value = _aiYAxisMoveSpeed_L.Value;
                _aoYAxisMoveSpeed_H.Value = _aiYAxisMoveSpeed_H.Value;




                string setcommand = param[0].ToString();
                int setvalue = Convert.ToInt32(param[1].ToString());

                short lowbit=0, highbit=0;

                IntConvTwoShort(setvalue, ref lowbit, ref highbit);
                _dtActionStart = DateTime.Now;
                ResetRoutine();


                switch (setcommand)
                {
                    case "SlotPitch":
                        _aoZAxisPitch_L.Value = lowbit;
                        _aoZAxisPitch_H.Value = highbit;
                        break;
                    case "FirstZPosition":
                        _aoZAxisFirstPos_L.Value = lowbit;
                        _aoZAxisFirstPos_H.Value = highbit;
                        break;
                    case "PlaceWaferZPosition":
                        _aoZAxisPlacePos_L.Value = lowbit;
                        _aoZAxisPlacePos_H.Value = highbit;
                        break;
                    case "XMoveDistance":
                        _aoXAxisMoveDistance_L.Value = lowbit;
                        _aoXAxisMoveDistance_H.Value = highbit;
                        break;
                    case "YMoveDistance":
                        _aoYAxisMoveDistance_L.Value = lowbit;
                        _aoYAxisMoveDistance_H.Value = highbit;
                        break;
                    case "ZMoveSpeed":
                        _aoZAxisMoveSpeed_L.Value = lowbit;
                        _aoZAxisMoveSpeed_H.Value = highbit;
                        break;
                    case "XMoveSpeed":
                        _aoXAxisMoveSpeed_L.Value = lowbit;
                        _aoXAxisMoveSpeed_H.Value = highbit;
                        break;
                    case "YMoveSpeed":
                        _aoYAxisMoveSpeed_L.Value = lowbit;
                        _aoYAxisMoveSpeed_H.Value = highbit;
                        break;
                    default:
                        return false;
                }
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }
            return true;
        }


        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            try
            {
                SetDoState((int)RobotStepEnum.ActionStep2, _doSaveCmd, true, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep3, RobotCommandTimeout, _diSave_Ack, true, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep4, _doSaveCmd, false, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep5, RobotCommandTimeout, _diSave_Ack, false, Notify, Stop);

            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm", "Pick wafer failed.");
                OnError("PickFailed");
            }

            return true;
        }

        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            return false;
        }

        protected override bool fStartGrip(object[] param)
        {
            return false;
        }

        protected override bool fStartGoTo(object[] param)
        {
            return false;
        }

        protected override bool fStartMapWafer(object[] param)
        {
            return false;
        }

        protected override bool fStartSwapWafer(object[] param)
        {
            return false;
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            if(!_diPLCReady.Value)
            {
                EV.PostAlarmLog("Robot", "PLC ready signal is OFF.");
                return false;
            }
            if(!_diMotionIdle.Value)
            {
                EV.PostAlarmLog("Robot", "Motion idle signal is OFF.");
                return false;
            }
            _dtActionStart = DateTime.Now;
            ResetRoutine();
            try
            {
                //RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                int slotindex = int.Parse(param[2].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    _aoTargetPos.Value = (short)(slotindex + 1);
                }
                else
                    _aoTargetPos.Value = 26;

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;


                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
            }
            catch (Exception ex)
            {                         
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fMonitorPlace(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            try
            {
                WaitAiValue((int)RobotStepEnum.ActionStep1, RobotCommandTimeout, _aiTargetPos, _aoTargetPos.Value, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep2, _doPlaceCmd, true, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep3, RobotCommandTimeout, _diPlace_Ack, true, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep4, _doPlaceCmd, false, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep5, RobotCommandTimeout, _diPlace_Ack, false, Notify, Stop);

            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm", "Pick wafer failed.");
                OnError("PickFailed");
                return true;
            }

            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            CmdTarget = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };


            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int SourceslotIndex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;

            WaferManager.Instance.WaferMoved(RobotModuleName, 0,sourcemodule, SourceslotIndex);


            return true;
        }

        protected override bool fMonitorPick(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            try
            {
                WaitAiValue((int)RobotStepEnum.ActionStep1, RobotCommandTimeout, _aiTargetPos, _aoTargetPos.Value, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep2, _doPickCmd, true, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep3, RobotCommandTimeout, _diPick_Ack, true, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep4, _doPickCmd, false, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep5, RobotCommandTimeout, _diPick_Ack, false, Notify, Stop);

            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm","Pick wafer failed.");
                OnError("PickFailed");
                return true;
            }

            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            CmdTarget = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };

            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int SourceslotIndex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;

            WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);


            return true;
        }

        protected override bool fStartPickWafer(object[] param)
        {
            if (!_diPLCReady.Value)
            {
                EV.PostAlarmLog("Robot", "PLC ready signal is OFF.");
                return false;
            }
            if (!_diMotionIdle.Value)
            {
                EV.PostAlarmLog("Robot", "Motion idle signal is OFF.");
                return false;
            }
            _dtActionStart = DateTime.Now;
            ResetRoutine();
            try
            {
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                int slotindex = int.Parse(param[2].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    _aoTargetPos.Value = (short)(slotindex + 1);
                }
                else
                    _aoTargetPos.Value = 26;


                Blade1Target = tempmodule;
                Blade2Target = tempmodule;


                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };



            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _dtActionStart = DateTime.Now;
            ResetRoutine();
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            try
            {
                SetDoState((int)RobotStepEnum.ActionStep1, _doPickCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep2, _doPlaceCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep3, _doHomeCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep4, _doSaveCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep5, _doX_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep6, _doX_RevCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep7, _doY_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep8, _doY_RevCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep9, _doZ_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep10, _doZ_RevCmd, false, Notify);

                SetDoState((int)RobotStepEnum.ActionStep11, _doResetCmd, true, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep12, RobotCommandTimeout, _diReset_Ack, true, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep13, _doResetCmd, false, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep14, RobotCommandTimeout, _diReset_Ack, false, Notify, Stop);

            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm", "Pick wafer failed.");
                OnError("PickFailed");
                return true;
            }
            return true;
        }



        protected override bool fStartInit(object[] param)
        {
            _dtActionStart = DateTime.Now;
            ResetRoutine();
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            try
            {
                SetDoState((int)RobotStepEnum.ActionStep1, _doPickCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep2, _doPlaceCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep3, _doResetCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep4, _doSaveCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep5, _doX_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep6, _doX_RevCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep7, _doY_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep8, _doY_RevCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep9, _doZ_FwdCmd, false, Notify);
                SetDoState((int)RobotStepEnum.ActionStep10, _doZ_RevCmd, false, Notify);



                SetDoState((int)RobotStepEnum.ActionStep11, _doHomeCmd, true, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep12, RobotCommandTimeout, _diHome_Ack, true, Notify, Stop);
                SetDoState((int)RobotStepEnum.ActionStep13, _doHomeCmd, false, Notify);
                WaitDiState((int)RobotStepEnum.ActionStep14, RobotCommandTimeout, _diHome_Ack, false, Notify, Stop);

            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm", "Pick wafer failed.");
                OnError("PickFailed");
                return true;
            }

            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            CmdTarget = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };



            return true;
        }

        protected override bool fStartExecuteCommand(object[] param)
        {
            CurrentExecuteCmd = param[0].ToString();
            if (string.IsNullOrEmpty(CurrentExecuteCmd))
                return false;

            _dtActionStart = DateTime.Now;
            ResetRoutine();


            return true;
            
        }

        public string CurrentExecuteCmd { get; private set; }

        protected override bool fMonitorExecuting(object[] param)
        {
            string command = CurrentParamter[0].ToString();
            Int32 pnum = Convert.ToInt32(CurrentParamter[1]);
            short high16bit = (short)(pnum >> 16);
            short low16bit = (short)(pnum & ushort.MaxValue);

            try
            {
                switch (command)
                {
                    case "XMoveFwd":
                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoXMoveFwdDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoXMoveFwdDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doX_FwdCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diX_Fwd_Ack, true, Notify,Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doX_FwdCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diX_Fwd_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoXMoveFwdDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoXMoveFwdDistance_L, 0, Notify);

                        break;
                    case "XMoveRev":
                        high16bit = (short)((-1*pnum) >> 16);
                        low16bit = (short)((-1 * pnum) & ushort.MaxValue);

                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoXMoveRevDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoXMoveRevDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doX_RevCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diX_Rev_Ack, true, Notify, Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doX_RevCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diX_Rev_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoXMoveRevDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoXMoveRevDistance_L, 0, Notify);
                        break;
                    case "YMoveFwd":
                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoYMoveFwdDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoYMoveFwdDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doY_FwdCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diY_Fwd_Ack, true, Notify, Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doY_FwdCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diY_Fwd_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoYMoveFwdDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoYMoveFwdDistance_L, 0, Notify);
                        break;
                    case "YMoveRev":
                        high16bit = (short)((-1 * pnum) >> 16);
                        low16bit = (short)((-1 * pnum) & ushort.MaxValue);
                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoYMoveRevDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoYMoveRevDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doY_RevCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diY_Rev_Ack, true, Notify, Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doY_RevCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diY_Rev_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoYMoveRevDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoYMoveRevDistance_L, 0, Notify);
                        break;
                    case "ZMoveFwd":
                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoZMoveFwdDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoZMoveFwdDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doZ_FwdCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diZ_Fwd_Ack, true, Notify, Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doZ_FwdCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diZ_Fwd_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoZMoveFwdDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoZMoveFwdDistance_L, 0, Notify);
                        break;
                    case "ZMoveRev":
                        high16bit = (short)((-1 * pnum) >> 16);
                        low16bit = (short)((-1 * pnum) & ushort.MaxValue);
                        SetAoValue((int)RobotStepEnum.ActionStep1, _aoZMoveRevDistance_H, high16bit, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep2, _aoZMoveRevDistance_L, low16bit, Notify);
                        SetDoState((int)RobotStepEnum.ActionStep3, _doZ_RevCmd, true, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep4, RobotCommandTimeout, _diZ_Rev_Ack, true, Notify, Stop);
                        SetDoState((int)RobotStepEnum.ActionStep5, _doZ_RevCmd, false, Notify);
                        WaitDiState((int)RobotStepEnum.ActionStep6, RobotCommandTimeout, _diZ_Rev_Ack, false, Notify, Stop);
                        SetAoValue((int)RobotStepEnum.ActionStep7, _aoZMoveRevDistance_H, 0, Notify);
                        SetAoValue((int)RobotStepEnum.ActionStep8, _aoZMoveRevDistance_L, 0, Notify);
                        break;
                    default:
                        break;


                }
            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.PostAlarmLog("Alarm", $"Execute command:{command} failed.");
                OnError($"Execute {command} failed");
                return true;
            }
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPick(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            return true;
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            return true;
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            return true;
        }

        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                return RobotArmWaferStateEnum.Present;
            return RobotArmWaferStateEnum.Absent;

        }

        private string BuildBladeTarget()
        {
            return (CmdRobotArm == RobotArmEnum.Upper ? "ArmB" : "ArmA") + "." + CmdTarget;
        }

        private static int TwoShortConvInt(short low16bit, short high16bit)
        {
            return (high16bit << 16) + low16bit + (low16bit<0 ? UInt16.MaxValue+1 :0);
        }

        private static void IntConvTwoShort(int num,ref short low16bit,ref short high16bit)
        {
            high16bit = (short)(num >> 16);
            low16bit = (short)(num & ushort.MaxValue);
        }

        #endregion















        public DOAccessor ParseDoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];

            return null;
        }

        public DIAccessor ParseDiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AOAccessor ParseAoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AIAccessor ParseAiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }
        public SCConfigItem ParseScNode(string name, XmlElement node, string ioModule = "", string defaultScPath = "")
        {
            SCConfigItem result = null;

            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                result = SC.GetConfigItem(node.GetAttribute(name));

            if (result == null && !string.IsNullOrEmpty(defaultScPath) && SC.ContainsItem(defaultScPath))
                result = SC.GetConfigItem(defaultScPath);

            return result;
        }

        public static T ParseDeviceNode<T>(string name, XmlElement node) where T : class, IDevice
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return DEVICE.GetDevice<T>(node.GetAttribute(name));
            LOG.Write(string.Format("{0}，未定义{1}", node.InnerXml, name));
            return null;
        }

        public static T ParseDeviceNode<T>(string module, string name, XmlElement node) where T : class, IDevice
        {
            string device_id = node.GetAttribute(name);
            if (!string.IsNullOrEmpty(device_id) && !string.IsNullOrEmpty(device_id.Trim()))
            {
                return DEVICE.GetDevice<T>($"{module}.{device_id}");
            }
            LOG.Write(string.Format("{0},undefined {1}", node.InnerXml, name));
            return null;
        }
        public enum CarrierMode
        {
            Foup,
            Fosb,
            OpenCassette,
        }

        private void SetAoValue(int id, AOAccessor ao, short value, Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                notify($"{RobotModuleName} set {ao.Name} to {value}.");
                ao.Value = value;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
            }
        }
        private void WaitAiValue(int id, int time, AIAccessor ai, short value, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait {RobotModuleName} {ai.Name} to be {value}");
                return true;
            }, () =>
            {
                if (ai.Value == value)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    //EV.Notify(RobotModuleName);
                    error($"Wait {RobotModuleName} {ai.Name} to be {value} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        private void WaitDiState(int id, int time, DIAccessor di, bool state, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait {RobotModuleName} {di.Name} to be {state}");
                return true;
            }, () =>
            {
                if (di.Value == state)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    //EV.Notify();
                    error($"Wait {RobotModuleName} {di.Name} to be {state} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        private void SetDoState(int id, DOAccessor _do, bool state, Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                notify($"{RobotModuleName} start set {_do.Name} to {state}.");
                //if (_do.Value == state)
                //{
                //    _do.Value = !state;
                //    Thread.Sleep(500);
                //}
                return _do.SetValue(state, out _);
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
            }
        }





        protected void Notify(string message)
        {
            EV.PostMessage(Name, EventEnum.GeneralInfo, string.Format("{0}:{1}", Name, message));
        }
        protected void Stop(string failReason)
        {
            OnError(string.Format("Failed {0}, {1} ", Name, failReason));
        }



        #region Routine Executor
        //timer, 计算routine时间
        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        private enum STATE
        {
            IDLE,
            WAIT,
        }

        public int TokenId
        {
            get { return _id; }
        }
        private int _id;         //step index

        /// <summary>
        /// already done steps
        /// </summary>
        private Stack<int> _steps = new Stack<int>();

        private STATE state;    //step state //idel,wait,

        //loop control
        private int loop = 0;
        private int loopCount = 0;
        private int loopID = 0;

        private DeviceTimer timer = new DeviceTimer();

        public int LoopCounter { get { return loop; } }
        public int LoopTotalTime { get { return loopCount; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
        }
        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!Acitve(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            loop = loopCount;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                loopID = idx;
                loopCount = count;

                next();
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (++loop >= loopCount)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    loop = 0;
                    loopCount = 0;  // Loop 结束时，当前loop和loop总数都清零

                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                next(loopID);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    else if (startRet == Result.DONE)
                    {
                        next();
                        return Tuple.Create(true, Result.DONE);
                    }
                    state = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    next();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }


        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    state = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    next();

                    if (bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute.Value)       //检查Success, next
                    {
                        next();
                        return Tuple.Create(true, Result.RUN);
                    }
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;
            double timeout = 0;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timeout = time();
                    if (!execute())
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }
                if (bExecute.Value)       //检查Success, next
                {
                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    rt.Start();
                    state = STATE.WAIT;
                }

                Result ret = rt.Monitor();

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
        #endregion


        private Tuple<bool, bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                if (bExecute)
                {
                    next();
                }
            }

            return Tuple.Create(bActive, bExecute);
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                next();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>

        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = Acitve(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool, Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    return Tuple.Create(true, Result.FAIL);
                }

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool Acitve(int id) //
        {
            if (_steps.Contains(id))
                return false;

            this._id = id;
            return true;
        }

        private void next()
        {
            _steps.Push(this._id);
            state = STATE.IDLE;
        }

        private void next(int step)   //loop
        {
            while (_steps.Pop() != step) ;

            state = STATE.IDLE;
        }


        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                return true;
            }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }

       
        #endregion

    }
}
