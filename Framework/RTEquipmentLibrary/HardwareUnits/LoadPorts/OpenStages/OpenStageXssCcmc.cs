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
    public class OpenStageXssCcmc : LoadPortBaseDevice, IConnection
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
        private IoSensor _diCst1Present;
        private IoSensor _diCst2Present;
        private IoSensor _diCst3Present;
        private IoSensor _diCst4Present;
        private IoSensor _diCst5Present;
        private IoSensor _diCst6Present;
        private IoSensor _diCst7Present;
        private IoSensor _diCst8Present;
        private IoSensor _diCst9Present;
        private IoSensor _diCst10Present;
        private IoSensor _diCst11Present;
        private IoSensor _diCst12Present;
        private IoSensor _diCst13Present;
        private IoSensor _diCst14Present;
        private IoSensor _diCst15Present;
        private IoSensor _diCst16Present;

        private IoSensor _diWSAProtrude;
        private IoSensor _diWSBProtrude;
        private IoSensor _diWSCProtrude;
        private IoSensor _diWSDProtrude;

        private IoSensor _diWSAPresent;
        private IoSensor _diWSBPresent;
        private IoSensor _diWSCPresent;
        private IoSensor _diWSDPresent;

        private IoSensor _diDoorOpen;
        private IoSensor _diOperationState;
        //private IoSensor _diCstPlacement;
        private IoSensor _diInterlock;



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

                int Index = (_diCst1Present.Value ? 1 : 0) + (_diCst2Present.Value ? 2 : 0) +
                    (_diCst3Present.Value ? 4 : 0) + (_diCst4Present.Value ? 8 : 0) +
                    (_diCst5Present.Value ? 16 : 0);

                if (Index == 1 || Index ==2 || Index == 4 || Index ==8 ||Index ==16)
                {
                    return LoadportCassetteState.Normal;

                }
                return LoadportCassetteState.Absent;
            }
        }

        public string Address { get => ""; }

        public bool IsConnected => true;

        protected Stopwatch _timerActionMonitor = new Stopwatch();
        private PeriodicJob _thread;
        public OpenStageXssCcmc(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            //if (indicators == null)
            //    throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");


            _diWSAProtrude = dis[0];
            _diCst1Present = dis[1];
            _diCst2Present = dis[2];
            _diCst3Present = dis[3];
            _diCst4Present = dis[4];
            _diCst5Present = dis[5];

            //_diCstPlacement = dis[2];

            

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
            _thread = new PeriodicJob(10, OnTimerMonitor, Module + Name + "Monitor", true);
        }

        private bool OnTimerMonitor()
        {
            
            InfoPadCarrierIndex = InfoPadSensorIndex;


            if (CassetteState == LoadportCassetteState.Absent)
            {
                _trigPresentAbsent.CLK = false;
                _isLoaded = false;
            }


            if (CassetteState == LoadportCassetteState.Normal)
            {
                if(_diCst1Present.Value)
                {
                    InfoPadSensorIndex = 1;
                }
                if (_diCst2Present.Value)
                {
                    InfoPadSensorIndex = 2;
                }
                if (_diCst3Present.Value)
                {
                    InfoPadSensorIndex = 3;
                }
                if (_diCst4Present.Value)
                {
                    InfoPadSensorIndex = 4;
                }
                if (_diCst5Present.Value)
                {
                    InfoPadSensorIndex = 5;
                }

                _trigPresentAbsent.CLK = true;
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
            return true;

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
            if (_diWSAProtrude.Value)
            {
                reason = "WaferProtrude";
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

            if (_diWSAProtrude.Value)
            {
                reason = "WaferProtrude";
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

        }

        public override bool MapWafer(out string reason)
        {
            _isLoaded = true;
            return base.MapWafer(out reason);
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
          
            _timerActionMonitor.Restart();
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;

            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError("LoadTimeout");
            }

      
            _timerActionMonitor.Stop();
            OnLoaded();
            _isLoaded = true;
            return true;
           
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
