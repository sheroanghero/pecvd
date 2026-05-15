using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs
{
    public class IOWalkingAxisHH :WalkingAxisBaseDevice
    {
        private R_TRIG _trigMonitorError = new R_TRIG();
        public IOWalkingAxisHH(string module,string name,IoSensor[] dis,IoTrigger[] dos):base(module,name)
        {
            _diPosFeedback0 = dis[0];
            _diPosFeedback1 = dis[1];
            _diPosFeedback2 = dis[2];

            _diReady = dis[3];
            _diOnTarget = dis[4];
            _diOnError = dis[5];
            _diOnLeftLimit = dis[6];
            _diOnRightLimit = dis[7];
            _diHomeSensor = dis[8];

            _doStartMoving = dos[0];
            _doPosSet0 = dos[1];
            _doPosSet1 = dos[2];
            _doPosSet2 = dos[3];

            _doHome = dos[4];
            _doSetFree = dos[5];
            _doStop = dos[6];
            _doResetAlarm = dos[7];
            _doJogFwd = dos[8];
            _doJogRev = dos[9];

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Name}.PosFeedBack0", () => _diPosFeedback0.Value);
            DATA.Subscribe($"{Name}.PosFeedBack1", () => _diPosFeedback1.Value);
            DATA.Subscribe($"{Name}.PosFeedBack2", () => _diPosFeedback2.Value);
            DATA.Subscribe($"{Name}.Ready", () => _diReady.Value);
            DATA.Subscribe($"{Name}.OnTarget", () => _diOnTarget.Value);
            DATA.Subscribe($"{Name}.OnError", () => _diOnError.Value);
            DATA.Subscribe($"{Name}.OnLeftLimit", () => _diOnLeftLimit.Value);
            DATA.Subscribe($"{Name}.OnRightLimit", () => _diOnRightLimit.Value);
            DATA.Subscribe($"{Name}.HomeSensor", () => _diHomeSensor.Value);

            DATA.Subscribe($"{Name}.DoStartMoving", () => _doStartMoving.Value);
            DATA.Subscribe($"{Name}.DoPosSet0", () => _doPosSet0.Value);
            DATA.Subscribe($"{Name}.DoPosSet1", () => _doPosSet1.Value);
            DATA.Subscribe($"{Name}.DoPosSet2", () => _doPosSet2.Value);
            DATA.Subscribe($"{Name}.DoHome", () => _doHome.Value);
            DATA.Subscribe($"{Name}.DoSetFree", () => _doSetFree.Value);
            DATA.Subscribe($"{Name}.DoStop", () => _doStop.Value);
            DATA.Subscribe($"{Name}.DoResetAlarm", () => _doResetAlarm.Value);
            DATA.Subscribe($"{Name}.DoJogFwd", () => _doJogFwd.Value);
            DATA.Subscribe($"{Name}.DoJogRev", () => _doJogRev.Value);

            DATA.Subscribe($"{Name}.Status", () => DeviceState.ToString());
            OP.Subscribe($"{Name}.SetDo", (method, args) =>
            {
                SetDo(method, args);
                return true;
            });

        }

        private void SetDo(string method, object[] args)
        {
            bool value = args[1].ToString() == "ON";
            switch(args[0])
            {
                case "_doStartMoving":
                    _doStartMoving.SetTrigger(value, out _);
                    break;
                case "_doPosSet0":
                    _doPosSet0.SetTrigger(value, out _);
                    break;
                case "_doPosSet1":
                    _doPosSet1.SetTrigger(value, out _);
                    break;
                case "_doPosSet2":
                    _doPosSet2.SetTrigger(value, out _);
                    break;
                case "_doHome":
                    _doHome.SetTrigger(value, out _);
                    break;
                case "_doSetFree":
                    _doSetFree.SetTrigger(value, out _);
                    break;
                case "_doStop":
                    _doStop.SetTrigger(value, out _);
                    break;
                case "_doResetAlarm":
                    _doResetAlarm.SetTrigger(value, out _);
                    break;
                case "_doJogFwd":
                    _doJogFwd.SetTrigger(value, out _);
                    break;
                case "_doJogRev":
                    _doJogRev.SetTrigger(value, out _);
                    break;

            }

           
        }

        private readonly IoSensor _diPosFeedback0;
        private readonly IoSensor _diPosFeedback1;
        private readonly IoSensor _diPosFeedback2;
        private readonly IoSensor _diReady;
        private readonly IoSensor _diOnTarget;
        private readonly IoSensor _diOnError;
        private readonly IoSensor _diOnLeftLimit;
        private readonly IoSensor _diOnRightLimit;
        private readonly IoSensor _diHomeSensor;

        private readonly IoTrigger _doStartMoving;
        private readonly IoTrigger _doPosSet0;
        private readonly IoTrigger _doPosSet1;
        private readonly IoTrigger _doPosSet2;

        private readonly IoTrigger _doHome;
        private readonly IoTrigger _doSetFree;        
        private readonly IoTrigger _doStop;
        private readonly IoTrigger _doResetAlarm;
        private readonly IoTrigger _doJogFwd;
        private readonly IoTrigger _doJogRev;

        public override void Monitor()
        {
            _trigMonitorError.CLK = _diOnError.Value;
            if (_trigMonitorError.Q) 
                OnError("ErrorSignal");
        }

        protected override bool fStop(object[] param)
        {
            
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            _doHome.SetTrigger(false, out _);
            _doSetFree.SetTrigger(false, out _);
            _doStop.SetTrigger(true, out _);
            _doResetAlarm.SetTrigger(false, out _);
            _doJogFwd.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            return base.fStop(param);
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (_diHomeSensor.Value && _diOnTarget.Value && _diHomeSensor.Value && _diReady.Value)
            {
                _doHome.SetTrigger(false, out _);
                return true;
            }
                
            return false;
        }

        protected override bool fMonitorMove(object[] param)
        {
            IsBusy = false;
            if(_diOnTarget.Value && _diReady.Value && GetCurrentStation() == TargetStation)
            {
                _doStartMoving.SetTrigger(false, out _);
                //_doPosSet0.SetTrigger(false, out _);
                //_doPosSet1.SetTrigger(false, out _);
                //_doPosSet2.SetTrigger(false, out _);
                return true;
            }
            return false;
                
        }

        protected override bool fStartInit(object[] param)
        {
            _doStop.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            _doJogFwd.SetTrigger(false, out _);
            _doSetFree.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(false, out _);
            _doStartMoving.SetTrigger(false, out _);
            _doHome.SetTrigger(true,out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            return true;
        }
        protected override bool fInitComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartMove(object[] param)
        {
            int stationIndex = (int)param[0];
            if (stationIndex > 7 || stationIndex <0) return false;
            _doPosSet0.SetTrigger((stationIndex & 0x1) == 0x1, out _);
            _doPosSet1.SetTrigger((stationIndex & 0x2) == 0x2, out _);
            _doPosSet2.SetTrigger((stationIndex & 0x4) == 0x4, out _);
            Thread.Sleep(200);
            _doStartMoving.SetTrigger(true, out _);
            Thread.Sleep(500);
            return true;

        }
        protected override bool fMoveComplete(object[] param)
        {
            IsBusy = false;
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            IsBusy = false;
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            _doHome.SetTrigger(false, out _);
            _doSetFree.SetTrigger(false, out _);
            _doStop.SetTrigger(false, out _);
            //_doResetAlarm.SetTrigger(false, out _);
            _doJogFwd.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(true, out _);
            Thread.Sleep(1000);
            _doResetAlarm.SetTrigger(false, out _);
            return true;
        }

        public override bool IsReady()
        {
            if (_diOnError.Value) return false;
            if (_diOnLeftLimit.Value) return false;
            if (_diOnRightLimit.Value) return false;
            return base.IsReady();
        }
        public override int GetCurrentStation()
        {
            return (_diPosFeedback0.Value ? 1 : 0) + (_diPosFeedback1.Value ? 2 : 0) +
                (_diPosFeedback2.Value ? 4 : 0);


        }
        private AITServoMotorData DeviceData
        {
            get
            {
                return new AITServoMotorData()
                {
                    DiPosFeedBack1 = _diPosFeedback0.Value,
                    DiPosFeedBack2 = _diPosFeedback1.Value,
                    DiPosFeedBack3 = _diPosFeedback2.Value,
                    DiReady = _diReady.Value,
                    DiOnTarget = _diOnTarget.Value,
                    DiOnError = _diOnError.Value,
                    DiOnLeftLimit = _diOnLeftLimit.Value,
                    DiOnRightLimit = _diOnRightLimit.Value,
                    DiOnHomeSensor = _diHomeSensor.Value,

                    DoStart = _doStartMoving.Value,
                    DoPos1 = _doPosSet0.Value,
                    DoPos2 = _doPosSet1.Value,
                    DoPos3 = _doPosSet2.Value,
                    DoHomeOn = _doHome.Value,
                    DoFreeOn = _doSetFree.Value,
                    DoStop = _doStop.Value,
                    DoReset = _doResetAlarm.Value,
                    DoJogFwd = _doJogFwd.Value,
                    DoJogRev = _doJogRev.Value,

                    CurrentStatus = DeviceState.ToString(),





                    //State = _state,
                };
            }
        }


    }


    public class IOWalkingAxisHHTd02 : WalkingAxisBaseDevice
    {

        private R_TRIG _trigMonitorError = new R_TRIG();
        public IOWalkingAxisHHTd02(string module, string name, IoSensor[] dis, IoTrigger[] dos) : base(module, name)
        {
            _diPosFeedback0 = dis[0];
            _diPosFeedback1 = dis[1];
            _diPosFeedback2 = dis[2];

            _diReady = dis[3];
            _diOnTarget = dis[4];
            _diOnError = dis[5];
            _diHomeComplete = dis[6];
            _diMoveComplete = dis[7];

            _doStartMoving = dos[0];
            _doPosSet0 = dos[1];
            _doPosSet1 = dos[2];
            _doPosSet2 = dos[3];

            _doPause = dos[4];
            _doModeSelect = dos[5];
            _doResetAlarm = dos[6];
            _doJogRev = dos[7];

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Name}.PosFeedBack0", () => _diPosFeedback0.Value);
            DATA.Subscribe($"{Name}.PosFeedBack1", () => _diPosFeedback1.Value);
            DATA.Subscribe($"{Name}.PosFeedBack2", () => _diPosFeedback2.Value);
            DATA.Subscribe($"{Name}.Ready", () => _diReady.Value);
            DATA.Subscribe($"{Name}.OnTarget", () => _diOnTarget.Value);
            DATA.Subscribe($"{Name}.OnError", () => _diOnError.Value);
            DATA.Subscribe($"{Name}.HomeSensor", () => _diHomeComplete.Value);
            DATA.Subscribe($"{Name}.MoveComplete", () => _diMoveComplete.Value);

            DATA.Subscribe($"{Name}.DoStartMoving", () => _doStartMoving.Value);
            DATA.Subscribe($"{Name}.DoPosSet0", () => _doPosSet0.Value);
            DATA.Subscribe($"{Name}.DoPosSet1", () => _doPosSet1.Value);
            DATA.Subscribe($"{Name}.DoPosSet2", () => _doPosSet2.Value);
            DATA.Subscribe($"{Name}.DoPause", () => _doPause.Value);
            DATA.Subscribe($"{Name}.DoModeSelect", () => _doModeSelect.Value);
            DATA.Subscribe($"{Name}.DoResetAlarm", () => _doResetAlarm.Value);
            DATA.Subscribe($"{Name}.DoJogRev", () => _doJogRev.Value);

            DATA.Subscribe($"{Name}.Status", () => DeviceState.ToString());
            OP.Subscribe($"{Name}.SetDo", (method, args) =>
            {
                SetDo(method, args);
                return true;
            });

        }

        private void SetDo(string method, object[] args)
        {
            bool value = args[1].ToString() == "ON";
            switch (args[0])
            {
                case "_doStartMoving":
                    _doStartMoving.SetTrigger(value, out _);
                    break;
                case "_doPosSet0":
                    _doPosSet0.SetTrigger(value, out _);
                    break;
                case "_doPosSet1":
                    _doPosSet1.SetTrigger(value, out _);
                    break;
                case "_doPosSet2":
                    _doPosSet2.SetTrigger(value, out _);
                    break;
                case "_doPause":
                    _doPause.SetTrigger(value, out _);
                    break;
                case "_doModeSelect":
                    _doModeSelect.SetTrigger(value, out _);
                    break;
              
                case "_doResetAlarm":
                    _doResetAlarm.SetTrigger(value, out _);
                    break;
                case "_doJogRev":
                    _doJogRev.SetTrigger(value, out _);
                    break;

            }


        }

        private readonly IoSensor _diPosFeedback0;
        private readonly IoSensor _diPosFeedback1;
        private readonly IoSensor _diPosFeedback2;
        private readonly IoSensor _diReady;
        private readonly IoSensor _diOnTarget;
        private readonly IoSensor _diOnError;
        private readonly IoSensor _diHomeComplete;
        private readonly IoSensor _diMoveComplete;

        private readonly IoTrigger _doStartMoving;
        private readonly IoTrigger _doPosSet0;
        private readonly IoTrigger _doPosSet1;
        private readonly IoTrigger _doPosSet2;

        private readonly IoTrigger _doPause;
        private readonly IoTrigger _doModeSelect;
        private readonly IoTrigger _doResetAlarm;
        private readonly IoTrigger _doJogRev;

        public override void Monitor()
        {
            _trigMonitorError.CLK = !_diOnError.Value;
            if (_trigMonitorError.Q)
                OnError("ErrorSignal");
        }

        protected override bool fStop(object[] param)
        {

            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            return base.fStop(param);
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;

            if(DateTime.Now - _dtActionStart >TimeSpan.FromSeconds(TimeLimitHome))
            {
                OnError("InitTimeout");
                _doStartMoving.SetTrigger(false, out _);
                return true;
            }
            if (_diHomeComplete.Value)
            {
                _doStartMoving.SetTrigger(false, out _);
                return true;
            }

            return false;
        }

        protected override bool fMonitorMove(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitMove))
            {
                OnError("MoveTimeout");
                _doStartMoving.SetTrigger(false, out _);
                return true;
            }
            if (_diOnTarget.Value && _diReady.Value && GetCurrentStation() == TargetStation)
            {
                _doStartMoving.SetTrigger(false, out _);
                //_doPosSet0.SetTrigger(false, out _);
                //_doPosSet1.SetTrigger(false, out _);
                //_doPosSet2.SetTrigger(false, out _);
                return true;
            }
            return false;

        }

        protected override bool fStartInit(object[] param)
        {
            _doStartMoving.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(false, out _);
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);

            _doModeSelect.SetTrigger(true, out _);
            Thread.Sleep(200);
            _doStartMoving.SetTrigger(true, out _);
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fInitComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartMove(object[] param)
        {
            _dtActionStart = DateTime.Now;
            int stationIndex = (int)param[0];
            if (stationIndex > 7 || stationIndex < 0) return false;
            _doPosSet0.SetTrigger((stationIndex & 0x1) == 0x1, out _);
            _doPosSet1.SetTrigger((stationIndex & 0x2) == 0x2, out _);
            _doPosSet2.SetTrigger((stationIndex & 0x4) == 0x4, out _);
            _doModeSelect.SetTrigger(true, out _);

            Thread.Sleep(200);
            _doStartMoving.SetTrigger(true, out _);
            Thread.Sleep(500);
            return true;

        }
        protected override bool fMoveComplete(object[] param)
        {
            IsBusy = false;
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            IsBusy = false;
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            //_doResetAlarm.SetTrigger(false, out _);
            _doJogRev.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(true, out _);
            Thread.Sleep(1000);
            _doResetAlarm.SetTrigger(false, out _);
            return true;
        }

        public override bool IsReady()
        {
            if (!_diOnError.Value) return false;
            if (!_diReady.Value) return false;
            return base.IsReady();
        }
        public override int GetCurrentStation()
        {
            return (_diPosFeedback0.Value ? 1 : 0) + (_diPosFeedback1.Value ? 2 : 0) +
                (_diPosFeedback2.Value ? 4 : 0);


        }
        private AITServoMotorData DeviceData
        {
            get
            {
                return new AITServoMotorData()
                {
                    DiPosFeedBack1 = _diPosFeedback0.Value,
                    DiPosFeedBack2 = _diPosFeedback1.Value,
                    DiPosFeedBack3 = _diPosFeedback2.Value,
                    DiReady = _diReady.Value,
                    DiOnTarget = _diOnTarget.Value,
                    DiOnError = _diOnError.Value,
                    DiOnHomeSensor = _diHomeComplete.Value,

                    DoStart = _doStartMoving.Value,
                    DoPos1 = _doPosSet0.Value,
                    DoPos2 = _doPosSet1.Value,
                    DoPos3 = _doPosSet2.Value,
                    DoReset = _doResetAlarm.Value,
                    DoJogRev = _doJogRev.Value,

                    CurrentStatus = DeviceState.ToString(),





                    //State = _state,
                };
            }
        }


    }


}



