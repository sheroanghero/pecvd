using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
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
    public class OpenStageHHZsw : LoadPortBaseDevice, IConnection
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
        private IoSensor _diCstPlaced1;
        private IoSensor _diCstPlaced2;
        private IoSensor _diCstPlaced3;
        private IoSensor _diCstPresent;
        private IoSensor _diInfoPadA;
        private IoSensor _diInfoPadB;
        private IoSensor _diInfoPadC;
        private IoSensor _diInfoPadD;
        private IoSensor _diWaferProtrude;
        private IoSensor _diDoorOpen;







        private IoTrigger _doOpenDoor;
        private IoTrigger _doIndicatorCstPresent;
        private IoTrigger _doIndicatorCstPlaced;
        private IoTrigger _doIndicatorLoadReady;
        private IoTrigger _doIndicatorUnloadReady;
        private IoTrigger _doIndicatorAuto;
        private IoTrigger _doIndicatorManual;
        private IoTrigger _doIndicatoralarm;
        private IoTrigger _doIndicatorReserve;


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
                
                if (_diCstPresent.Value && _diCstPlaced1.Value && _diCstPlaced2.Value && _diCstPlaced3.Value)
                {
                    return LoadportCassetteState.Absent;

                }
                if (!_diCstPresent.Value && !_diCstPlaced1.Value && !_diCstPlaced2.Value && !_diCstPlaced3.Value)
                {
                    return LoadportCassetteState.Normal;

                    
                }
                return LoadportCassetteState.Unknown;
            }
        }

        public string Address { get => ""; }

        public bool IsConnected => true;

        protected Stopwatch _timerActionMonitor = new Stopwatch();

        public OpenStageHHZsw(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diCstPlaced1 = dis[0];
            _diCstPlaced2 = dis[1];
            _diCstPlaced3 = dis[2];
            _diCstPresent = dis[3];
            _diInfoPadA = dis[4];
            _diInfoPadB = dis[5];
            _diInfoPadC = dis[6];
            _diInfoPadD = dis[7];
            _diWaferProtrude = dis[8];
            _diDoorOpen = dis[9];

            _doOpenDoor = indicators[0];
            _doIndicatorCstPresent = indicators[1];
            _doIndicatorCstPlaced = indicators[2];
            _doIndicatorLoadReady = indicators[3];
            _doIndicatorUnloadReady = indicators[4];
            _doIndicatorAuto = indicators[5];
            _doIndicatorManual = indicators[6];
            _doIndicatoralarm = indicators[7];
            _doIndicatorReserve = indicators[8];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;


            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;

            LoadportReset(out _);

            if (CassetteState == LoadportCassetteState.Absent)
            {
                SetPresent(false);
                SetPlaced(false);
            }

        }     

        public override bool IsLoaded => _isLoaded;

        public override bool IsEnableUnload(out string reason)
        {
            if (!IsReady())
            {
                reason = "Not Ready";
                return false;
            }
            reason = "";
            return true;
        }

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
            InfoPadSensorIndex = (_diInfoPadA.Value ? 0 : 8) + (_diInfoPadB.Value ? 0 : 4) +
                (_diInfoPadC.Value ? 0 : 2) + (_diInfoPadD.Value ? 0 : 1);
            if(IsAutoDetectCarrierType)
                InfoPadCarrierIndex = InfoPadSensorIndex;

            _doIndicatorCstPresent.SetTrigger(!_diCstPresent.Value, out _);

            if (CassetteState == LoadportCassetteState.Absent)
            {
                _trigPresentAbsent.CLK = false;
                _isLoaded = false;
            }


            if (CassetteState == LoadportCassetteState.Normal)
            {
                _trigPresentAbsent.CLK = true;
                _doIndicatorCstPlaced.SetTrigger(true, out _);
            }
            else
            {
                _doIndicatorCstPlaced.SetTrigger(false, out _);
                _doOpenDoor.SetTrigger(true, out _);
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

            if(CurrentState == LoadPortStateEnum.Error)
            {
                _doIndicatoralarm.SetTrigger(true, out _);
            }
            else
                _doIndicatoralarm.SetTrigger(false, out _);

            base.Monitor();
        }


        public override void InformProcessComplete()
        {
            
            base.InformProcessComplete();
        }

        public override void InformProcessStart()
        {
            base.InformProcessComplete();
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
            if(!_diWaferProtrude.Value)
            {
                reason = "WaferProtrude";
                return false;
            }
            if(_diDoorOpen.Value || _doOpenDoor.Value)
            {
                reason = "DoorNotClose";
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

            if (!_diWaferProtrude.Value)
            {
                reason = "WaferProtrude";
                return false;
            }
            if (_diDoorOpen.Value || _doOpenDoor.Value)
            {
                reason = "DoorNotClose";
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


        protected override bool fStartUnload(object[] para)
        {
            if (!_isPlaced) return false;
            _doOpenDoor.SetTrigger(true, out _);
            _timerActionMonitor.Restart();
            return true;
        }


        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            


            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError("UnloadTimeout");
            }

            OnUnloaded();
            _isLoaded = false;
            return true;

            //if (!_diDoorOpen.Value)
            //{
            //    _timerActionMonitor.Stop();
            //    OnUnloaded();
            //    _isLoaded = true;
            //    return true;
            //}


            //return false;
        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            switch (light)
            {
                case Indicator.LOAD:
                    _doIndicatorLoadReady.SetTrigger(state == IndicatorState.ON, out _);
                    break;
                case Indicator.UNLOAD:
                    _doIndicatorUnloadReady.SetTrigger(state == IndicatorState.ON, out _);
                    break;
                case Indicator.ACCESSAUTO:
                    _doIndicatorAuto.SetTrigger(state == IndicatorState.ON, out _);
                    break;
                case Indicator.ACCESSMANUL:
                    _doIndicatorManual.SetTrigger(state == IndicatorState.ON, out _);
                    break;
                case Indicator.ALARM:
                    _doIndicatoralarm.SetTrigger(state == IndicatorState.ON, out _);
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

            
            IsBusy = false;

            _doOpenDoor.SetTrigger(false, out _);
            _timerActionMonitor.Restart();
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;

            if(_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError("LoadTimeout");
            }

            if (!_diDoorOpen.Value)
            {
                _timerActionMonitor.Stop();
                OnLoaded();
                _isLoaded = true;
                return true;
            }
            return false;
        }


        protected override bool fStartInit(object[] param)
        {
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected override bool fStartReset(object[] param)
        {

            return true;
        }


    }
}
