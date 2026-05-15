using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;



namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.OpenStages
{
    public class OpenStageSanYouDemo : LoadPortBaseDevice, IConnection
    {
        public EnumLoadPortType PortType { get; set; }
        public bool Initalized { get; set; }
        public bool Error { get; set; }

        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }
        private IoSensor _diCstPlacement;
        private IoSensor _diWaferProtrude;
        private IoSensor _diDoorOpen;
        private IoSensor _diDoorClose;
        private IoSensor _diDoorUp;
        private IoSensor _diDoorDown;
        private IoSensor _diCstIncline1;
        private IoSensor _diCstIncline2;
        private IoSensor _diCstIncline3;






        private IoTrigger _doCloseDoor;
        private IoTrigger _doOpendDoor;
        private IoTrigger _doDoorDown;
        private IoTrigger _doDoorUp;
        private IoTrigger _doIndicatorAuto;
        private IoTrigger _doIndicatorPresence;
        private IoTrigger _doIndicatorPlacement;
        private IoTrigger _doIndicatorReserve;
        private IoTrigger _doIndicatorAlarm;




        private RD_TRIG _trigPresentAbsent = new RD_TRIG();

        private RD_TRIG _trigPlacement = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();
        private R_TRIG _trigCoverClosed = new R_TRIG();

        private R_TRIG _trigDoorOpen = new R_TRIG();
        private R_TRIG _trigDoorClose = new R_TRIG();




        private bool _isLoaded = false;
        private string _carrierInformation = "";



        public override LoadportCassetteState CassetteState
        {
            get
            {
                // 4"
                
                if (_diCstPlacement.Value)
                {
                    InfoPadCarrierIndex = 12;

                    _doIndicatorPlacement.SetTrigger(true, out _);
                    _doIndicatorPresence.SetTrigger(true, out _);

                    

                     return LoadportCassetteState.Normal;
                }
                _doIndicatorPlacement.SetTrigger(false, out _);
                _doIndicatorPresence.SetTrigger(false, out _);
                InfoPadCarrierIndex = 0;
                return LoadportCassetteState.Absent;
            }
        }
        public override string SpecCarrierType
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        public override WaferSize GetCurrentWaferSize()
        {
            switch (InfoPadCarrierIndex)
            {
                case 12:
                    return WaferSize.WS12;
                   
                default:
                    return WaferSize.WS0;
            }
        }
        public string Address { get => ""; }

        public bool IsConnected => true;


        public OpenStageSanYouDemo(string module, string name, IoSensor[] dis, IoTrigger[] dos, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "dis");

            if (dos == null)
                throw new ArgumentException("DO cannot be null", "dos");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diCstPlacement = dis[0];
            _diWaferProtrude = dis[1];
            _diDoorOpen=dis[2];
            _diDoorClose=dis[3];
            _diDoorUp=dis[4];
            _diDoorDown=dis[5];
            _diCstIncline1 = dis[6];
            _diCstIncline2 = dis[7];
            _diCstIncline3 = dis[8];

            _doCloseDoor = dos[0];
            _doOpendDoor = dos[1];
            _doDoorDown = dos[2];
            _doDoorUp = dos[3];

            _doIndicatorAuto = dos[4];
            _doIndicatorPresence = dos[5];
            _doIndicatorPlacement = dos[6];
            _doIndicatorReserve = dos[7];
            _doIndicatorAlarm = dos[8];


            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;


            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;

            LoadportReset(out _);

        }     

        public override bool IsLoaded => _isLoaded;



        protected override bool fStartExecute(object[] param)
        {
            try
            {
                switch (param[0].ToString())
                {
                    case "MapWafer":
                        if (!IsMapWaferByLoadPort)
                        {
                            string reason = "";
                            if (!IsEnableMapWafer(out reason))
                            {
                                EV.PostAlarmLog("LoadPort", $"{LPModuleName} is not ready to map wafer:{reason}");
                                return false;
                            }
                            if (MapRobot != null)
                                return MapRobot.WaferMapping(LPModuleName, out _);
                            return false;
                        }
                        break;
                }
                IsBusy = false;
                return false;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }
        public override void Monitor()
        {

            if (CassetteState == LoadportCassetteState.Absent)
            {
                _trigPresentAbsent.CLK = false;
                _isLoaded = false;
            }
            if (CassetteState == LoadportCassetteState.Normal)
            {
                _trigPresentAbsent.CLK = true;
                if (CurrentState == LoadPortStateEnum.Idle)
                {
                    
                }
                else
                {
                    

                }
            }

            if (_trigPresentAbsent.R)
            {
                SetPresent(true);
                SetPlaced(true);

            }

            if (_trigPresentAbsent.T)
            {
                SetPresent(false);
                SetPlaced(false);
            }






            base.Monitor();
        }

        






        public override void Reset()
        {
            base.Reset();

            //_trigWaferProtrude.RST = true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        public override bool IsEnableMapWafer(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }

            if(_diWaferProtrude.Value)
            {
                reason = "Wafer Protrude";
                return false;
            }
            if (!_isLoaded)
            {
                reason = "Cassette is not loaded";
                return false;
            }
            reason = "";
            return true;
        }
        public override bool MapWafer(out string reason)
        {
            _isLoaded = true;

            _trigWaferProtrude.RST = true;
            return base.MapWafer(out reason);
        }

        

        public override bool IsEnableLoad(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }
            reason = "";
            return true;
        }
        public override bool IsEnableTransferWafer(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_isLoaded)
            {
                reason = "Cassette is not loaded";
                return false;
            }

            if (!_isMapped)
            {
                reason = "FOUP not mapped";
                return false;
            }
            if (_diWaferProtrude.Value)
            {
                reason = "Wafer Protrude";
                return false;
            }
      

            foreach (var wafer in WaferManager.Instance.GetWafers(LPModuleName))
            {
                if (wafer.IsEmpty) continue;
                if (wafer.Status == WaferStatus.Crossed)
                {
                    reason = "Crossed wafer";
                    return false;
                }
                if (wafer.Status == WaferStatus.Double)
                {
                    reason = "Double wafer";
                    return false;
                }
            }


            reason = "";
            return true;
        }

        public override bool IsForbidAccessSlotAboveWafer()
        {
            return false;
        }

        protected override bool fStartUnload(object[] para)
        {
            ResetRoutine();
            


            return true;
        }


        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;

            try
            {

                SetDoState((int)LoadPortStepEnum.ActionStep5, _doDoorDown.DoTrigger, false, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep6, _doDoorUp.DoTrigger, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorUp.SensorDI, true, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorDown.SensorDI, false, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep1, _doOpendDoor.DoTrigger, false, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep2, _doCloseDoor.DoTrigger, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, TimelimitAction, _diDoorOpen.SensorDI, false, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep3, TimelimitAction, _diDoorClose.SensorDI, true, Notify, Stop);
                


            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortUnloadFailed);
                OnError("Unload failed");
                return true;
            }
            DockState = FoupDockState.Docked;
            OnUnloaded();
            _isLoaded = false;
            return true;


        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            switch (light)
            {
                case Indicator.LOAD:

                    break;
                case Indicator.UNLOAD:

                    break;
                case Indicator.ACCESSAUTO:

                    break;
                case Indicator.ALARM:
                    break;
                default:
                    reason = "Not support";
                    return false;
            }
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }


        protected override bool fStartLoad(object[] param)
        {
            string reason;
            ResetRoutine();
            if(!IsEnableLoad(out reason))
            {
                EV.PostAlarmLog("LoadPort", $"{Name} can't execute load:{reason}.");
                return false;
            }
            
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            
            IsBusy = false;
            try
            {

                SetDoState((int)LoadPortStepEnum.ActionStep1, _doOpendDoor.DoTrigger, true, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep2, _doCloseDoor.DoTrigger, false, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep3, TimelimitAction, _diDoorClose.SensorDI, false, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, TimelimitAction, _diDoorOpen.SensorDI, true, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep5, _doDoorDown.DoTrigger, true, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep6, _doDoorUp.DoTrigger, false, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorUp.SensorDI, false, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorDown.SensorDI, true, Notify, Stop);



            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortLoadFailed);
                OnError("Load failed");
                return true;
            }
            DockState = FoupDockState.Docked;
            OnLoaded();
            _isLoaded = true;
            return true;
        }


        protected override bool fStartInit(object[] param)
        {
            ResetRoutine();
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;

            try
            {

                SetDoState((int)LoadPortStepEnum.ActionStep5, _doDoorDown.DoTrigger, false, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep6, _doDoorUp.DoTrigger, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorUp.SensorDI, true, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep7, TimelimitAction, _diDoorDown.SensorDI, false, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep1, _doOpendDoor.DoTrigger, false, Notify);
                SetDoState((int)LoadPortStepEnum.ActionStep2, _doCloseDoor.DoTrigger, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, TimelimitAction, _diDoorOpen.SensorDI, false, Notify, Stop);
                WaitDiState((int)LoadPortStepEnum.ActionStep3, TimelimitAction, _diDoorClose.SensorDI, true, Notify, Stop);



            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortHomeFailed);
                OnError("Home failed");
                return true;
            }
            OnHomed();
            _isLoaded = false;
            return true;
        }
        protected override bool fStartReset(object[] param)
        {

            return true;
        }

        private void WaitDiState(int id, int time, DIAccessor di, bool state, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait {LPModuleName} {di.Name} to be {state}");
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
                    EV.Notify(AlarmLoadPortError);
                    error($"Wait {LPModuleName} {di.Name} to be {state} timeout after {time} seconds");
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
                notify($"{LPModuleName} start set {_do.Name} to {state}.");
                if (_do.Value == state)
                {
                    _do.Value = !state;
                    Thread.Sleep(500);
                }
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



        #region Sequence Interface
        protected void Notify(string message)
        {
            EV.PostMessage(Name, EventEnum.GeneralInfo, string.Format("{0}:{1}", Name, message));
        }
        protected void Stop(string failReason)
        {
            OnError(string.Format("Failed {0}, {1} ", Name, failReason));
        }





        private enum LoadPortStepEnum
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
