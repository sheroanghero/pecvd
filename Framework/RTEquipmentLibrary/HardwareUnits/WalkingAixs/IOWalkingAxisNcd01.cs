using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs
{
    public class IOWalkingAxisNcd01:WalkingAxisBaseDevice
    {
        private R_TRIG _trigMonitorError = new R_TRIG();
        public IOWalkingAxisNcd01(string module,string name,IoSensor[] dis,IoTrigger[] dos):base(module,name)
        { 
            _diBusy = dis[0];            
            _diOnTarget = dis[1];
            _diReady = dis[2];
            _diOnError = dis[3];
            _diHomeSensor = dis[4];

            
            _diPosFeedback1 = dis[5];
            _diPosFeedback2 = dis[6];
            _diPosFeedback3 = dis[7];
            _diPosFeedback4 = dis[8];
            _diPosFeedback5 = dis[9];

            _doServoOn = dos[0];
            _doStartMoving = dos[1];
            _doPosSet0 = dos[2];
            _doPosSet1 = dos[3];
            _doPosSet2 = dos[4];          
            _doResetAlarm = dos[5];

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            
            DATA.Subscribe($"{Name}.PosFeedBack1", () => _diPosFeedback1.Value);
            DATA.Subscribe($"{Name}.PosFeedBack2", () => _diPosFeedback2.Value);
            DATA.Subscribe($"{Name}.PosFeedBack3", () => _diPosFeedback3.Value);
            DATA.Subscribe($"{Name}.PosFeedBack4", () => _diPosFeedback4.Value);
            DATA.Subscribe($"{Name}.PosFeedBack5", () => _diPosFeedback5.Value);

            DATA.Subscribe($"{Name}.Ready", () => _diReady.Value);
            DATA.Subscribe($"{Name}.OnTarget", () => _diOnTarget.Value);
            DATA.Subscribe($"{Name}.OnError", () => _diOnError.Value);
            DATA.Subscribe($"{Name}.HomeSensor", () => _diHomeSensor.Value);

            DATA.Subscribe($"{Name}.DoStartMoving", () => _doStartMoving.Value);
            DATA.Subscribe($"{Name}.DoPosSet0", () => _doPosSet0.Value);
            DATA.Subscribe($"{Name}.DoPosSet1", () => _doPosSet1.Value);
            DATA.Subscribe($"{Name}.DoPosSet2", () => _doPosSet2.Value);
            DATA.Subscribe($"{Name}.DoResetAlarm", () => _doResetAlarm.Value);

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
               
                case "_doResetAlarm":
                    _doResetAlarm.SetTrigger(value, out _);
                    break;
             

            }

           
        }

        
        private readonly IoSensor _diPosFeedback1;
        private readonly IoSensor _diPosFeedback2;
        private readonly IoSensor _diPosFeedback3;
        private readonly IoSensor _diPosFeedback4;
        private readonly IoSensor _diPosFeedback5;

        private readonly IoSensor _diBusy;
        private readonly IoSensor _diReady;
        private readonly IoSensor _diOnTarget;
        private readonly IoSensor _diOnError;       
        private readonly IoSensor _diHomeSensor;

        private readonly IoTrigger _doServoOn;
        private readonly IoTrigger _doStartMoving;
        private readonly IoTrigger _doPosSet0;
        private readonly IoTrigger _doPosSet1;
        private readonly IoTrigger _doPosSet2;
        private readonly IoTrigger _doResetAlarm;


        public override void Monitor()
        {
            _trigMonitorError.CLK = _diOnError.Value;
            if (_trigMonitorError.Q) 
                OnError("ErrorSignal");
        }

        protected override bool fStop(object[] param)
        {
            _doServoOn.SetTrigger(false, out _);

            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);
            _doResetAlarm.SetTrigger(false, out _);
            return base.fStop(param);
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitHome))
            {
                _doStartMoving.SetTrigger(false, out _);
                OnError("InitTimeout");
                return true;
            }            
            if (_diHomeSensor.Value && _diOnTarget.Value && _diReady.Value)
            {
                _doStartMoving.SetTrigger(false, out _);
                EV.PostInfoLog("WalkingAxis", $"{Name} init complete.");
                return true;
            }                
            return false;
        }

        protected override bool fMonitorMove(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitMove))
            {
                _doStartMoving.SetTrigger(false, out _);
                OnError("MoveTimeout");
                return true;
            }


            if (_diOnTarget.Value && _diReady.Value && GetCurrentStation() == TargetStation)
            {
                _doStartMoving.SetTrigger(false, out _);
                return true;
            }
            return false;
                
        }

        protected override bool fStartInit(object[] param)
        {
            _doServoOn.SetTrigger(true, out _);
            _doResetAlarm.SetTrigger(false, out _);
            _doStartMoving.SetTrigger(false, out _);            
            _doPosSet0.SetTrigger(true, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);

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
            int stationIndex = (int)param[0];
            if (stationIndex > 7 || stationIndex <0) return false;
            _doPosSet0.SetTrigger((stationIndex & 0x1) == 0x1, out _);
            _doPosSet1.SetTrigger((stationIndex & 0x2) == 0x2, out _);
            _doPosSet2.SetTrigger((stationIndex & 0x4) == 0x4, out _);
            Thread.Sleep(200);
            _doStartMoving.SetTrigger(true, out _);
            Thread.Sleep(500);
            _dtActionStart = DateTime.Now;
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
            _doServoOn.SetTrigger(true, out _);
            _doStartMoving.SetTrigger(false, out _);
            _doPosSet0.SetTrigger(false, out _);
            _doPosSet1.SetTrigger(false, out _);
            _doPosSet2.SetTrigger(false, out _);     
            _doResetAlarm.SetTrigger(true, out _);
            
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorResetting(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitMove))
            {
                _doResetAlarm.SetTrigger(false, out _);
                OnError("ResetTimeout");
                return true;
            }
            if (_diOnError.Value) return false;
            if (!_diReady.Value) return false;

            _doResetAlarm.SetTrigger(false, out _);
            return true;
        }

        public override bool IsReady()
        {
            if (_diOnError.Value) return false;
            if (!_diReady.Value) return false;
            return base.IsReady();
        }
        public override int GetCurrentStation()
        {            
            if (_diPosFeedback1.Value && !_diPosFeedback2.Value && !_diPosFeedback3.Value && !_diPosFeedback4.Value && !_diPosFeedback5.Value)
                return 1;
            if (!_diPosFeedback1.Value && _diPosFeedback2.Value && !_diPosFeedback3.Value && !_diPosFeedback4.Value && !_diPosFeedback5.Value)
                return 2;
            if (!_diPosFeedback1.Value && !_diPosFeedback2.Value && _diPosFeedback3.Value && !_diPosFeedback4.Value && !_diPosFeedback5.Value)
                return 3;
            if (!_diPosFeedback1.Value && !_diPosFeedback2.Value && !_diPosFeedback3.Value && _diPosFeedback4.Value && !_diPosFeedback5.Value)
                return 4;
            if (!_diPosFeedback1.Value && !_diPosFeedback2.Value && !_diPosFeedback3.Value && !_diPosFeedback4.Value && _diPosFeedback5.Value)
                return 5;
            return 0;

        }
        private AITServoMotorData DeviceData
        {
            get
            {
                return new AITServoMotorData()
                {
                    DiPosFeedBack1 = _diPosFeedback1.Value,
                    DiPosFeedBack2 = _diPosFeedback2.Value,
                    DiPosFeedBack3 = _diPosFeedback3.Value,
                    DiReady = _diReady.Value,
                    DiOnTarget = _diOnTarget.Value,
                    DiOnError = _diOnError.Value,
                    DiOnHomeSensor = _diHomeSensor.Value,

                    DoStart = _doStartMoving.Value,
                    DoPos1 = _doPosSet0.Value,
                    DoPos2 = _doPosSet1.Value,
                    DoPos3 = _doPosSet2.Value,                
                    DoReset = _doResetAlarm.Value,

                    CurrentStatus = DeviceState.ToString(),
                    //State = _state,
                };
            }
        }


    }
}
