using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB
{
    public class TDKBLoadPortHhYx : TDKBLoadPort
    { 
        public TDKBLoadPortHhYx(string module, string name, string scRoot, IoTrigger[] dos = null, IoSensor[] dis = null, RobotBaseDevice robot = null, bool IsTCPconnection = false, IE84CallBack e84 = null) : 
        base(module, name, scRoot, dos, dis,robot, false, e84)
        {
           
        }


        private bool _isDisableEvenSlot
        {
            get
            {
                if (!SC.ContainsItem($"CarrierInfo.DisableEvenSlot{InfoPadCarrierIndex}")) return false;
                return SC.GetValue<bool>($"CarrierInfo.DisableEvenSlot{InfoPadCarrierIndex}");
            }
        }

        public override int ValidSlotsNumber
        {
            get
            {
                if (_isDisableEvenSlot)
                    return 13;
                return 25;
            }
        }

        public override void OnSlotMapRead(string _slotMap)
        {
            CurrentSlotMapResult = _slotMap;
            if (!_isDisableEvenSlot)
            {
                base.OnSlotMapRead(_slotMap);
                return;
            }
            if (!IsMapWaferByLoadPort)
                OnActionDone(new object[] { });
            string _newSlotMap = "";

            for (int i = 0; i < ValidSlotsNumber; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                _newSlotMap += _slotMap[2 * i];
                WaferInfo wafer = null;
                switch (_slotMap[2*i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapCrossedWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map crossed wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapDoubleWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map double wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map unknown wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = _newSlotMap;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierIndex"] = InfoPadCarrierIndex;
            dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            EV.Notify(EventMapComplete, dvid);
            if (_slotMap.Contains("2"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError);
            }
            if (_slotMap.Contains("W"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError);
            }
            if (_slotMap.Contains("?"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError);
            }

            if (LPCallBack != null)
                LPCallBack.MappingComplete(_carrierId, _slotMap);

            _isMapped = true;
        }

        public void WaitLoadPortReady(int id, int time,LoadPortBaseDevice lp, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog("System", $"Wait {lp.LPModuleName} to be ready");
                return true;
            }, () =>
            {
                if (lp.IsReady())
                    return Result.DONE;
                if (lp.CurrentState == LoadPortStateEnum.Error)
                    return Result.FAIL;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error("Wait lp ready failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error("Wait lp ready timeout");

                }
            }
        }
        public void WaitRobotReady(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog("System", $"Wait {MapRobot.RobotModuleName} to be ready");             
                return true;
            }, () =>
            {
                if (MapRobot.IsReady())
                    return Result.DONE;
                if (MapRobot.RobotState == Robots.RobotStateEnum.Error)
                    return Result.FAIL;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error("Wait robot ready failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error("Wait robot ready timeout");

                }
            }
        }

        public void RobotHome(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog("System",$"{MapRobot.RobotModuleName} start homing");
                var reason = string.Empty;
                return MapRobot.HomeModule(null);
            }, () =>
            {
                if (MapRobot.IsReady())
                    return Result.DONE;
                if (MapRobot.RobotState == Robots.RobotStateEnum.Error)
                    return Result.FAIL;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error("Map robot home failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error("Map robot home timeout");

                }
            }
        }


        public void RobotMap(int id, int time,Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"System",$"{MapRobot.RobotModuleName} start to map LP:{LPModuleName}");
                var reason = string.Empty;
                return MapRobot.WaferMapping(LPModuleName,out _);
            }, () =>
            {
                if (MapRobot.IsReady())
                    return Result.DONE;
                if (MapRobot.RobotState == Robots.RobotStateEnum.Error)
                    return Result.FAIL;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error($"Robot map {LPModuleName} failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"Robot map {LPModuleName} timeout");

                }
            }
        }


        public void XssLoad(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"System", $"Start to load LP:{LPModuleName}");
                var reason = string.Empty;
                return base.fStartLoad(null);
            }, () =>
            {
                if(!IsHandlerBusy)
                    return Result.DONE;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error($"{LPModuleName} load failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"{LPModuleName} load timeout");

                }
            }
        }

        public void XssUnload(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"System", $"Start to unload LP:{LPModuleName}");
                var reason = string.Empty;
                return base.fStartUnload(null);
            }, () =>
            {
                if (!IsHandlerBusy)
                    return Result.DONE;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error($"{LPModuleName} unload failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"{LPModuleName} unload timeout");

                }
            }
        }

        public void XssHome(int id, int time, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                EV.PostInfoLog($"System", $"Start to home LP:{LPModuleName}");
                var reason = string.Empty;
                return base.fStartInit(CurrentParamter);
            }, () =>
            {
                if (!IsHandlerBusy)
                    return Result.DONE;
                return Result.RUN;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                    error($"{LPModuleName} load failed");

                if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"{LPModuleName} load timeout");

                }
            }
        }
        public enum LpStepEnum
        {
            Step1,
            Step2,
            Step3,
            Step4,
            Step5,
            Step6,
            Step7,
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



    }

}
