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
using System.Text.RegularExpressions;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.Common.CommonData;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs;
using Aitex.Core.RT.Routine;
using static MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs.WalkingAxisBaseDevice;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public class JelRobotWithX : JelRobot, IConnection
    {
        protected WalkingAxisBaseDevice m_WalkingAxis { get; set; }

        protected bool m_IsNeedMoveWalkingAxis { get; set; }

        public JelRobotWithX(string module, string name, string scRoot,WalkingAxisBaseDevice xaxis=null) : 
            base(module, name, scRoot)
        {
            m_WalkingAxis = xaxis;
        }

      

        public override bool OnTimer()
        {
            return base.OnTimer();
        }

        protected override bool fReset(object[] param)
        {
            m_WalkingAxis.Reset(null);
            return base.fReset(param);
        }



        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("ResetTimeout");
                return true;
            }
            if (!m_WalkingAxis.IsReady()) return false;

            return base.fMonitorReset(param);
        }
        protected override bool fStartGoTo(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)param[0];
            ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

            BladeTarget = module;
            Blade1Target = module;
            Blade2Target = module;
            int slot = (int)param[2] + 1;
            RobotPostionEnum postype = (RobotPostionEnum)param[3];

            if (!m_WalkingAxis.IsReady()) 
                return false;

            m_WalkingAxis.MoveTo(module);

            int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}{postype}CmdNO");

            var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, (int)arm);
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                if (lp != null) lp.NoteTransferStart();
            }

            int bankno;
            int cassetteNO;

            if(!GetBankAndCassetteNumber(module, wz, out bankno, out cassetteNO))
                return false;


            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", slot.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
            }
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorGoTo(object[] param)
        {
            IsBusy = false;
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError($"Goto{BladeTarget}Timeout");
                return true;
            }
            if(m_WalkingAxis.DeviceState == WalkingAxisState.ERROR)
            {
                HandlerError($"WalkingAxisError");
                return true;
            }

            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {


                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
                    BladeTarget = ModuleName.System;
                    Blade1Target = ModuleName.System;
                    Blade2Target = ModuleName.System;

                    CmdTarget = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Picking,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };

                    if (ModuleHelper.IsLoadPort(tempmodule))
                    {
                        var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                        if (lp != null) lp.NoteTransferStop();

                    }

                    return true;
                }
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    HandlerError("GotoFailed");
                }
            }
            return false;
        }
        protected override bool fStartInit(object[] param)
        {
            _dtActionStart = DateTime.Now;
            int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.InitCmdNO");
            int Robotspeed = SC.GetValue<int>($"Robot.{Name}.RobotSpeed");
            if (Robotspeed < 0)
                Robotspeed = 0;
            if (Robotspeed > 9)
                Robotspeed = 9;

            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, ""));
                
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "CS1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));     // Read bank number
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));    // Read Cassette Number
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "2"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "3"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "4"));
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "SP", Robotspeed.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
            }

            ResetRoutine();
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("InitTimeout");
                return true;
            }

            WaitRobotCompleteMove((int)JelStep.Step1, "Wait robot home complete", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            HomeWalkingAxis((int)JelStep.Step2, "Home walking axis", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;

            CmdTarget = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };   
       
            return true;            
        }
    
        

        public override bool IsReady()
        {
            return RobotState == RobotStateEnum.Idle && !IsBusy;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }


        protected override bool fStop(object[] param)
        {
            m_WalkingAxis.Stop(null);
            _lstMonitorHandler.Clear();            
            _connection.ForceClear();
            _connection.Execute(new JelRobotSetHandler(this, "S"));
            return ReadStatus();
            
        }

        

        protected override bool fStartMapWafer(object[] param)
        {
            ResetRoutine();

            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());
            _dtActionStart = DateTime.Now;
            return true;

        }

        protected override bool fMonitorMap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("MapTimeout");
                return true;
            }

            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[0].ToString());          


            WalkingAxisMoveToPos((int)JelStep.Step1, "Wait Axis move complete", tempmodule, RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }

            RobotExecuteMap((int)JelStep.Step2);
            if (ExecuteResult.Item1)
            {
                return false;
            }

            WaitRobotCompleteMove((int)JelStep.Step3, "Wait mappingg complete", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }


            EV.PostInfoLog("Robot", $"{RobotModuleName} Map {tempmodule} complete. ");
            return true;
        }

        protected override bool fStartPickWafer(object[] param)
        {
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            m_IsNeedMoveWalkingAxis = m_WalkingAxis.GetCurrentStation() != m_WalkingAxis.GetStationIndex(tempmodule);
            ResetRoutine();            
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorPick(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("PickTimeout");
                return true;
            }
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            int slotindex = int.Parse(CurrentParamter[2].ToString());


            if(m_IsNeedMoveWalkingAxis)
            {
                RobotExecutePickReady((int)JelStep.Step1);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WalkingAxisMoveToPos((int)JelStep.Step2, "Wait Axis move complete", tempmodule, RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WaitRobotCompleteMove((int)JelStep.Step3, "Wait picking complete", RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
            }            

            RobotExecutePick((int)JelStep.Step4);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            WaitRobotCompleteMove((int)JelStep.Step5, "Wait picking complete", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            RobotCompletePick((int)JelStep.Step7);

            EV.PostInfoLog("Robot", $"{RobotModuleName} pick wafer from {tempmodule}:{slotindex+1} complete. ");
            return true;
        }





        protected override bool fStartSwapWafer(object[] param)
        {
            ResetRoutine();
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            m_IsNeedMoveWalkingAxis = m_WalkingAxis.GetCurrentStation() != m_WalkingAxis.GetStationIndex(tempmodule);

            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorSwap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("SwapTimeout");
                return true;
            }
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            int slotindex = int.Parse(CurrentParamter[2].ToString());

            if (m_IsNeedMoveWalkingAxis)
            {
                RobotExecutePickReady((int)JelStep.Step1);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WalkingAxisMoveToPos((int)JelStep.Step2, "Wait Axis move complete", tempmodule, RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WaitRobotCompleteMove((int)JelStep.Step3, "Wait picking complete", RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
            }

            RobotExecuteSwap((int)JelStep.Step4);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            WaitRobotCompleteMove((int)JelStep.Step5, "Wait swapping complete", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            RobotCompleteSwap((int)JelStep.Step6);

            EV.PostInfoLog("Robot", $"{RobotModuleName} Swap wafer from {tempmodule}:{slotindex + 1} complete. ");
            return true;
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            ResetRoutine();
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            m_IsNeedMoveWalkingAxis = m_WalkingAxis.GetCurrentStation() != m_WalkingAxis.GetStationIndex(tempmodule);
            _dtActionStart = DateTime.Now;
            return true;
        }

        
        protected override bool fMonitorPlace(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("PlaceTimeout");
                return true;
            }
            ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            int slotindex = int.Parse(CurrentParamter[2].ToString());

            if (m_IsNeedMoveWalkingAxis)
            {
                RobotExecutePickReady((int)JelStep.Step1);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WalkingAxisMoveToPos((int)JelStep.Step2, "Wait Axis move complete", tempmodule, RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
                WaitRobotCompleteMove((int)JelStep.Step3, "Wait picking complete", RobotCommandTimeout);
                if (ExecuteResult.Item1)
                {
                    return false;
                }
            }

            RobotExecutePlace((int)JelStep.Step4);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            WaitRobotCompleteMove((int)JelStep.Step5, "Wait placing complete", RobotCommandTimeout);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            RobotCompletePlace((int)JelStep.Step6);
            if (ExecuteResult.Item1)
            {
                return false;
            }
            EV.PostInfoLog("Robot", $"{RobotModuleName} place wafer to {tempmodule}:{slotindex + 1} complete. ");
            return true;

        }
        



        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower)
                return IsLeftArmPressureSensorON? RobotArmWaferStateEnum.Present: RobotArmWaferStateEnum.Absent;
            if (arm == RobotArmEnum.Upper)
                return IsRightArmPressureSensorON ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent; ;
            return RobotArmWaferStateEnum.ArmInvalid;
        }
        protected void RobotExecutePickReady(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                int slotindex = int.Parse(CurrentParamter[2].ToString());

                var wz = WaferManager.Instance.GetWaferSize(tempmodule, slotindex);               

                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                    return false;

                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickReadyCmdNO");
                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;

                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}GoPickReadyError.");
                }
            }
        }

        protected void RobotExecutePlaceReady(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                int slotindex = int.Parse(CurrentParamter[2].ToString());

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);               

                int bankno;
                int cassetteNO;

                if(!GetBankAndCassetteNumber(tempmodule,wz, out bankno, out cassetteNO))
                    return false;
                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PlaceReadyCmdNO");

                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}GoPlaceReadyError.");
                }
            }
        }
        protected void RobotExecuteMap(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[0].ToString());
                EV.PostInfoLog($"{RobotModuleName}", $"Start mapping wafer on {tempmodule}");
               
                WaferSize wz = WaferManager.Instance.GetWaferSize(tempmodule, 0);              
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                    return false;

                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.MapCmdNO");
                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));

                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));                    
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFW"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFK"));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                return true; 
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}MappingError.");
                }
            }
        }

        protected void RobotExecutePick(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {

                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                int slotindex = int.Parse(CurrentParamter[2].ToString());               

                var wz = WaferManager.Instance.GetWaferSize(tempmodule, slotindex);
            

                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                    return false;


                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickCmdNO");
                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}PickingError.");
                }
            }
        }

        protected void RobotCompletePick(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {

                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName sourcemodule;
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                int SourceslotIndex;
                if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
                if (arm == RobotArmEnum.Lower)
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                if (arm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                if (arm == RobotArmEnum.Both)
                {
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                }

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;



                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}CompletePickingError.");
                }
            }
        }

        protected void RobotExecutePlace(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                int slotindex = int.Parse(CurrentParamter[2].ToString());               

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);             
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                    return false;
                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PlaceCmdNO");

                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}PlacingError.");
                }
            }
        }

        protected void RobotCompletePlace(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName sourcemodule;
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                int Sourceslotindex;
                if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
                if (arm == RobotArmEnum.Lower)
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                if (arm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                if (arm == RobotArmEnum.Both)
                {
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                }

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;



                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}CompletePickingError.");
                }
            }
        }
        protected void RobotExecuteSwap(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                int slotindex = int.Parse(CurrentParamter[2].ToString());
                
                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);
            
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                    return false;
                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickSwapCmdNO");

                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}SwappingError.");
                }
            }
        }
        protected void RobotCompleteSwap(int id)
        {
            Tuple<bool, Result> ret = Execute(id, () =>
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName sourcemodule;
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                int Sourceslotindex;
                if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
                if (arm == RobotArmEnum.Lower)
                {
                    WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                }
                if (arm == RobotArmEnum.Upper)
                {
                    WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                }
                if (arm == RobotArmEnum.Both)
                {

                }
                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;



                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError($"{RobotModuleName}CompletePickingError.");
                }
            }
        }



        public void WaitRobotCompleteMove(int id, string name, int time)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"{RobotModuleName}",name);  
                return true;
            }, () =>
            {
                if (_lstMonitorHandler.Count >0 || _connection.IsBusy)
                {                   
                    return Result.RUN;
                }
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                    return Result.FAIL;
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
                    Blade1Target = ModuleName.System;
                    Blade2Target = ModuleName.System;
                    CmdTarget = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Moving,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };
                    return Result.DONE;
                }
                    

                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError(string.Format("{0} occurred error", name));
                }

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError(string.Format("{0} timeout, than {1} seconds", name, time));

                }
            }

        }
        public void HomeWalkingAxis(int id, string name, int time)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"{RobotModuleName}", name);

                m_WalkingAxis.Home(null);
                return true;
            }, () =>
            {
                if (m_WalkingAxis.IsReady())
                    return Result.DONE;
                if (m_WalkingAxis.DeviceState == WalkingAxisState.ERROR)
                    return Result.FAIL;

                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError(string.Format("{0} occurred error", name));
                }

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError(string.Format("{0} timeout, than {1} seconds", name, time));

                }
            }

        }
        public void WalkingAxisMoveToPos(int id, string name, ModuleName module, int time)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"{m_WalkingAxis.Name}", name);
                return m_WalkingAxis.MoveTo(module);
            }, () =>
            {
                if (m_WalkingAxis.IsReady() && m_WalkingAxis.IsArrivedTarget)
                    return Result.DONE;
                if (m_WalkingAxis.DeviceState == WalkingAxisState.ERROR)
                    return Result.FAIL;

                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    OnError(string.Format("{0} occurred error", name));
                }

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    OnError(string.Format("{0} timeout, than {1} seconds", name, time));

                }
            }

        }

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

        private int _currentTokenId = -1;
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

        protected Tuple<bool, Result> ExecuteResult;

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
            _currentTokenId = -1;
            ExecuteResult = Tuple.Create(false, Result.DONE);
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

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<Result?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            Result? bExecute = Result.RUN;
            if (bActive)
            {
                //if (idx != _currentTokenId && ExecuteResult.Item1) return ExecuteResult;

                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        ExecuteResult = Tuple.Create(bActive, Result.FAIL);
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                    _currentTokenId = idx;
                }
                bExecute = check();

                if (bExecute == null)
                {
                    ExecuteResult = Tuple.Create(bActive, Result.FAIL);
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute == Result.DONE)       //检查Success, next
                    {
                        next();
                        ExecuteResult = Tuple.Create(true, Result.RUN);
                        return Tuple.Create(true, Result.RUN);
                    }
                    if (bExecute == Result.Succeed)       //检查Success, next
                    {
                        next();
                        ExecuteResult = Tuple.Create(true, Result.RUN);
                        return Tuple.Create(true, Result.RUN);
                    }
                    if (bExecute == Result.FAIL)       //检查 Fail 直接返回Fail
                    {
                        ExecuteResult = Tuple.Create(true, Result.FAIL);
                        return Tuple.Create(true, Result.FAIL);
                    }
                }

                if (timer.IsTimeout())
                {
                    ExecuteResult = Tuple.Create(true, Result.TIMEOUT);
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
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
                //if (_currentTokenId != idx && !ExecuteResult.Item1) return ExecuteResult;
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        ExecuteResult = Tuple.Create(true, Result.FAIL);
                        return Tuple.Create(true, Result.FAIL);
                    }
                    _currentTokenId = idx;
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
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
                //if (ExecuteResult.Item1) return Tuple.Create(true, true);
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
                if (ExecuteResult.Item1) return Tuple.Create(true, true);
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
                    ExecuteResult = Tuple.Create(true, Result.FAIL);
                    return Tuple.Create(true, Result.FAIL);
                }
                ExecuteResult = Tuple.Create(true, Result.RUN);
                return Tuple.Create(true, Result.RUN);
            }
            ExecuteResult = Tuple.Create(false, Result.RUN);
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

                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }

        enum JelStep
        {
            Step1,
            Step2,
            Step3,
            Step4,
            Step5,
            Step6,
            Step7,
            Step8,
            Step9,
            Step10,
            Step11,
            Step12,
        }



    }
   

}
