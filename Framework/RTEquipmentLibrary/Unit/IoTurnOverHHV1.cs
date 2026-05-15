using System;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Flipper.FlipperBase;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 
    /// </summary>
    public class IoTurnOverHHV1 : FlipperBaseDevice
    {
        private readonly DIAccessor _di0Degree;
        private readonly DIAccessor _di180Degree;

        private readonly DIAccessor _diAlarm;
        private readonly DIAccessor _diBusy;
        private readonly DIAccessor _diGrip;
        private readonly DIAccessor _diOrigin;

        private readonly DIAccessor _diPlacement;
        private readonly DIAccessor _diPressureError;
        private readonly DIAccessor _diUngrip;

        private readonly DOAccessor _doGrip;
        private readonly DOAccessor _doM0;
        private readonly DOAccessor _doM1;

        private readonly DOAccessor _doOrigin;
        private readonly DOAccessor _doResetError;
        private readonly DOAccessor _doStop;
        private readonly DOAccessor _doUngrip;

        private DateTime _dtActionStart;

        
        //private DeviceTimer _loopTimeout = new DeviceTimer();
        //private readonly DeviceTimer _loopTimer = new DeviceTimer();

        //private readonly SCConfigItem _scLoopInterval;
        

        //private TurnOverState _state = TurnOverState.Idle;
        //public TurnOverState State => _state;
        

        //private R_TRIG _trigCloseError = new R_TRIG();
        //private R_TRIG _trigOpenError = new R_TRIG();
        //private R_TRIG _trigReset = new R_TRIG();
        //private R_TRIG _trigSetPointDone = new R_TRIG();
        
        

        // DI_TurnOverPlacement
        // DI_TurnOverGrip
        // DI_TurnOverUngrip
        // DI_TurnOverBusy
        // DI_TurnOverPressureError
        //
        // DI_TurnOverAlarm
        // DI_TurnOverOrigin
        // DI_TurnOver0Degree
        // DI_TurnOver180Degree
        //
        // DO_TurnOverGrip
        // DO_TurnOverUngrip
        // DO_TurnOverM0
        // DO_TurnOverM1
        // DO_TurnOverStop
        //
        // DO_TurnOverOrigin
        // DO_TurnOverResetError
        public string Display, DeviceID;
        private PeriodicJob _thread;
        public IoTurnOverHHV1(string module, XmlElement node, string ioModule = ""):base(node.GetAttribute("module"), node.GetAttribute("id"))
        {
            Module = node.GetAttribute("module");
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");

            _diPlacement = ParseDiNode("DI_TurnOverPlacement", node, ioModule);
            _diGrip = ParseDiNode("DI_TurnOverGrip", node, ioModule);
            _diUngrip = ParseDiNode("DI_TurnOverUngrip", node, ioModule);
            _diBusy = ParseDiNode("DI_TurnOverBusy", node, ioModule);
            _diPressureError = ParseDiNode("DI_TurnOverPressureError", node, ioModule);

            _diAlarm = ParseDiNode("DI_TurnOverAlarm", node, ioModule);
            _diOrigin = ParseDiNode("DI_TurnOverOrigin", node, ioModule);
            _di0Degree = ParseDiNode("DI_TurnOver0Degree", node, ioModule);
            _di180Degree = ParseDiNode("DI_TurnOver180Degree", node, ioModule);

            _doGrip = ParseDoNode("DO_TurnOverGrip", node, ioModule);
            _doUngrip = ParseDoNode("DO_TurnOverUngrip", node, ioModule);
            _doM0 = ParseDoNode("DO_TurnOverM0", node, ioModule);
            _doM1 = ParseDoNode("DO_TurnOverM1", node, ioModule);
            _doStop = ParseDoNode("DO_TurnOverStop", node, ioModule);

            _doOrigin = ParseDoNode("DO_TurnOverOrigin", node, ioModule);
            _doResetError = ParseDoNode("DO_TurnOverResetError", node, ioModule);

            //_scLoopInterval = SC.GetConfigItem("Turnover.IntervalTimeLimit");
            
            InitializeIoTurnover();

        }

       

        
        public override bool IsPlacement
        {
            get
            {
                if(_diPlacement !=null) return !_diPlacement.Value;
                return WaferManager.Instance.CheckHasWafer(ModuleName.TurnOverStation, 0);
            }
        }
        
        public bool IsAlarm => !_diAlarm.Value;
        public bool IsPressureError => _diPressureError ==null? false: !_diPressureError.Value;

        public override FlipperPosEnum CurrentFlipperPosition 
        {
            get
            {
                if (_di0Degree.Value && !_di180Degree.Value) return FlipperPosEnum.FrontSide;
                if (!_di0Degree.Value && _di180Degree.Value) return FlipperPosEnum.BackSide;
                return FlipperPosEnum.Unknow;

            }
              
        }
        public override GripPosEnum CurrentGripperPositon 
        { 
            get 
            {
                if (_diGrip.Value && !_diUngrip.Value) return GripPosEnum.Close;
                if (!_diGrip.Value && _diUngrip.Value) return GripPosEnum.Open;
                return GripPosEnum.Unknow;
            }
        }

        //public bool IsBusy => _diBusy.Value || _state != TurnOverState.Idle;

        //public bool IsIdle => !_diBusy.Value && _state == TurnOverState.Idle;
        //public bool IsGrip => _diGrip.Value && !_diUngrip.Value;
        //public bool IsUnGrip => _diUngrip.Value && !_diGrip.Value;
        //public bool Is0Degree => _di0Degree.Value &&!_di180Degree.Value;
        //public bool Is180Degree => _di180Degree.Value&&!_di0Degree.Value;

        //public bool IsEnableWaferTransfer => Is0Degree && !IsBusy && IsUnGrip && !IsAlarm &&
        //                                     IsPlacement;

        public bool InitializeIoTurnover()
        {
            //DATA.Subscribe($"{Module}.{Name}.State", () => _state.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsHomed", () => _diOrigin.Value);
            DATA.Subscribe($"{Module}.{Name}.IsBusy", () => _diBusy.Value);
            DATA.Subscribe($"{Module}.{Name}.IsGrip", () => _diGrip.Value);
            DATA.Subscribe($"{Module}.{Name}.IsUnGrip", () => _diUngrip.Value);
            DATA.Subscribe($"{Module}.{Name}.Is0Degree", () => _di0Degree.Value);
            DATA.Subscribe($"{Module}.{Name}.Is180Degree", () => _di180Degree.Value);
            DATA.Subscribe($"{Module}.{Name}.IsPlacement", () => IsPlacement);
            DATA.Subscribe($"{Module}.{Name}.IsAlarm", () => _diAlarm.Value);
            DATA.Subscribe($"{Module}.{Name}.IsPressureError", () => _diPressureError==null?false: _diPressureError.Value);

            DATA.Subscribe($"{Module}.{Name}.GripCmd", () => _doGrip.Value);
            DATA.Subscribe($"{Module}.{Name}.TurnTo0Cmd", () => _doM0.Value);
            DATA.Subscribe($"{Module}.{Name}.TurnTo180Cmd", () => _doM1.Value);
            DATA.Subscribe($"{Module}.{Name}.HomeCmd", () => _doOrigin.Value);
            DATA.Subscribe($"{Module}.{Name}.ResetCmd", () => _doResetError.Value);
            DATA.Subscribe($"{Module}.{Name}.StopCmd", () => _doStop==null?false: _doStop.Value);
            DATA.Subscribe($"{Module}.{Name}.UnGripCmd", () => _doUngrip.Value);           

            _trigError = new R_TRIG();

            _thread = new PeriodicJob(50, OnTimerMonitor, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        private R_TRIG _trigError;

        private bool OnTimerMonitor()
        {
            _trigError.CLK = (_diAlarm != null && !_diAlarm.Value) || (_diPressureError != null && !_diPressureError.Value);

            if(_trigError.Q)
            {
                OnError($"{FlipperModuleName}Error");
            }
            return true;

        }

        public override void Terminate()
        {
        }








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

        protected override bool fStartGrip(object[] param)
        {
            _doGrip.SetValue(true, out _);
            _doUngrip.SetValue(false, out _);
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Grip timeout");
                return false;
            }

            if (_diGrip.Value && !_diUngrip.Value &&  !_diBusy.Value)
            {
                EV.PostInfoLog("Flipper", $"{FlipperModuleName} grip completed");
                return true;
            }
            return false;
        }

        protected override bool fStartUnGrip(object[] param)
        {

            _doGrip.SetValue(false, out _);
            _doUngrip.SetValue(true, out _);
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("UnGrip timeout");
                return false;
            }

            if (!_diGrip.Value && _diUngrip.Value && !_diBusy.Value)
            {
                EV.PostInfoLog("Flipper", $"{FlipperModuleName} ungrip completed");
                return true;
            }
            return false;
        }


        protected override bool fStartHome(object[] param)
        {
            _doM0.SetValue(false, out _);
            _doM1.SetValue(false, out _);
            _doOrigin.SetValue(true, out _);
            _dtActionStart = DateTime.Now;
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorHome(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Home timeout"); 
                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);

                return false;
            }

            if (_diOrigin.Value && !_diBusy.Value)
            {
                _doOrigin.SetValue(false, out _);
                EV.PostInfoLog("Flipper", $"{FlipperModuleName} home completed");

                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);
                return true;
            }
            return false;
        }



        protected override bool fStartTurnTo0(object[] param)
        {
            _doM0.SetValue(true, out _);
            _doM1.SetValue(false, out _);
            _dtActionStart = DateTime.Now;
            var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation, 0);
            if (!wafer.IsEmpty)
            {
                var dvid = new SerializableDictionary<string, string>()
                    {
                        {"LOT_ID", wafer.LotId},
                        {"WAFER_ID", wafer.WaferID},
                        {"ARRIVE_POS_NAME", FlipperModuleName.ToString()}
                    };

                EV.Notify(EventWaferTurnOverStart, dvid);
            }

            return true;
        }
        protected override bool fMonitorTurnTo0(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Turn to 0 timeout");
                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);
                return false;
            }

            if (_di0Degree.Value && !_di180Degree.Value && !_diBusy.Value)
            {
                EV.PostInfoLog("Flipper", $"{FlipperModuleName} turn to 0 degree completed");
                var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation, 0);
                if (!wafer.IsEmpty)
                {
                    var dvid = new SerializableDictionary<string, string>()
                    {
                        {"LOT_ID", wafer.LotId},
                        {"WAFER_ID", wafer.WaferID},
                        {"ARRIVE_POS_NAME", FlipperModuleName.ToString()}
                    };

                    EV.Notify(EventWaferTurnOverStart, dvid);
                }
                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);

                return true;
            }
            return false;
        }


        protected override bool fStartTurnTo180(object[] param)
        {
            _doM0.SetValue(false, out _);
            _doM1.SetValue(true, out _);
            _dtActionStart = DateTime.Now;
            var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation, 0);
            if (!wafer.IsEmpty)
            {
                var dvid = new SerializableDictionary<string, string>()
                    {
                        {"LOT_ID", wafer.LotId},
                        {"WAFER_ID", wafer.WaferID},
                        {"ARRIVE_POS_NAME", "TRN1"}
                    };

                EV.Notify(EventWaferTurnOverStart, dvid);
            }
            return true;
        }

       

        protected override bool fMonitorTurnTo180(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Turn to 180 timeout");
                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);
                return false;
            }

            if (!_di0Degree.Value && _di180Degree.Value && !_diBusy.Value)
            {
                EV.PostInfoLog("Flipper", $"{FlipperModuleName} turn to 180 degree completed");
                var wafer = WaferManager.Instance.GetWafer(ModuleName.TurnOverStation, 0);
                if (!wafer.IsEmpty)
                {
                    var dvid = new SerializableDictionary<string, string>()
                    {
                        {"LOT_ID", wafer.LotId},
                        {"WAFER_ID", wafer.WaferID},
                        {"ARRIVE_POS_NAME", FlipperModuleName.ToString()}
                    };

                    EV.Notify(EventWaferTurnOverStart, dvid);
                }
                _doM1.SetValue(false, out _);
                _doM0.SetValue(false, out _);
                _doOrigin.SetValue(false, out _);
                return true;
            }
            return false;
        }

        protected override bool fStartAbort(object[] param)
        {
            _doStop.SetValue(true, out _);
            _doResetError.SetValue(false, out _);
            _doM0.SetValue(false, out _);
            _doM1.SetValue(false, out _);
            IsBusy = false;
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            _doM0.SetValue(false, out _);
            _doM1.SetValue(false, out _);
            _doOrigin.SetValue(false, out _);
            _doStop.SetValue(false, out _);
            _doResetError.SetValue(true, out _);

            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Reset timeout");
                _doResetError.SetValue(false, out _);
                return false;
            }

            if (_diAlarm != null && !_diAlarm.Value) 
                return false;
            
            
            EV.PostInfoLog("Flipper", $"{FlipperModuleName} reset completed");
            _doResetError.SetValue(false, out _);
            return true;
           
        }
    }
}