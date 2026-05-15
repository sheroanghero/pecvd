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
    public class OpenStageGonaZingSemi : LoadPortBaseDevice, IConnection
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
        private IoSensor _diCstPlaced;
        private IoSensor _diWaferProtrude;






        private IoTrigger _doIndicatorLoad;
        private IoTrigger _doIndicatorUnload;




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
                if (_diCstPlaced.Value)
                    return LoadportCassetteState.Normal;
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
            return WaferSize.WS12;
        }
        public string Address { get => ""; }

        public bool IsConnected => true;
      

        public OpenStageGonaZingSemi(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diCstPlaced = dis[0];
            _diWaferProtrude = dis[1];

            _doIndicatorLoad = indicators[0];
            _doIndicatorUnload = indicators[1];



            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;

            
            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;
            
            LoadportReset(out _);

            _threadMonitor = new PeriodicJob(10, OnTimerMonitor, Name, true);

        }

        private int _delayToCheckCarrier
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.DelayToDetectCarrier"))
                    return SC.GetValue<int>($"LoadPort.{Name}.DelayToDetectCarrier");
                return 3;
            }
        }

        private bool OnTimerMonitor()
        {

            if (CassetteState == LoadportCassetteState.Absent)
            {
                _trigPresentAbsent.CLK = false;
                
            }
            if (CassetteState == LoadportCassetteState.Normal)
            {
                _trigPresentAbsent.CLK = true;

            }

            if (_trigPresentAbsent.R)
            {
                Thread.Sleep(_delayToCheckCarrier * 1000);
                if (_diCstPlaced.Value)
                {
                    SetPresent(true);
                    SetPlaced(true);
                }

            }

            if (_trigPresentAbsent.T)
            {
                Thread.Sleep(_delayToCheckCarrier * 1000);
                if (!_diCstPlaced.Value)
                {
                    _isLoaded = false;
                    SetPresent(false);
                    SetPlaced(false);
                }
            }


            return true;
        }

        private PeriodicJob _threadMonitor;

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

        public override bool Load(out string reason)
        {
            _isLoaded = true;
            reason = "";
            _trigWaferProtrude.RST = true;
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
            if (!_isPlaced) return false;
            return true;
        }


        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
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
            
            _isLoaded = true;
            IsBusy = false;
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
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


        public override bool SetIndicator(IndicatorType light, IndicatorState state)
        {
            switch(light)
            {
                case IndicatorType.Load:
                    _doIndicatorLoad.SetTrigger(state == IndicatorState.ON, out _);
                    break;

                case IndicatorType.Unload:
                    _doIndicatorUnload.SetTrigger(state == IndicatorState.ON, out _);
                    break;
            }


            return true;
        }


    }
}
