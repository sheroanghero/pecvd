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
    public class OpenStageHHGcl200 : LoadPortBaseDevice, IConnection
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
        private IoSensor _diCstPresent;
        private IoSensor _diInfoPad1;
        private IoSensor _diInfoPad2;
        private IoSensor _diWaferProtrude;




        private IoTrigger _doIndicatorCstReady;
        private IoTrigger _doIndicatorCstPresent;
        private IoTrigger _doIndicatorCstBusy;
        private IoTrigger _doIndicatorCstComplete;
        private IoTrigger _doIndicatorCstAlarm;




        private RD_TRIG _trigPresentAbsent = new RD_TRIG();

        private RD_TRIG _trigPlacement = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();

        

        
        private bool _isLoaded = false;
        private string _carrierInformation = "";



        public override LoadportCassetteState CassetteState
        {
            get
            {
                if(_diCstPresent.Value && _diInfoPad1.Value && _diInfoPad2.Value)
                    return LoadportCassetteState.Normal;
                if(!_diCstPresent.Value && !_diInfoPad1.Value && !_diInfoPad2.Value)
                    return LoadportCassetteState.Absent;
                return LoadportCassetteState.Unknown;
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
             return WaferSize.WS8;
             
        }
        public string Address { get => ""; }

        public bool IsConnected => true;

        private PeriodicJob _thread;
        public OpenStageHHGcl200(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diCstPresent = dis[0];
            _diInfoPad1 = dis[1];
            _diInfoPad2 = dis[2];
            _diWaferProtrude = dis[3];
            


            _doIndicatorCstReady = indicators[0]; 
            _doIndicatorCstPresent = indicators[1]; 
            _doIndicatorCstBusy = indicators[2]; 
            _doIndicatorCstComplete = indicators[3]; 
            _doIndicatorCstAlarm = indicators[4]; 

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;

            
            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;
            


            LoadportReset(out _);
            _thread = new PeriodicJob(50, OnTimerMonitor, $"{LPModuleName}OnTimerMonitor", true);

        }

        private bool OnTimerMonitor()
        {

            if (CassetteState == LoadportCassetteState.Absent)
            {
                _doIndicatorCstAlarm.SetTrigger(false, out _);
                _doIndicatorCstBusy.SetTrigger(false, out _);
                _doIndicatorCstComplete.SetTrigger(false, out _);
                _doIndicatorCstPresent.SetTrigger(false, out _);
                _doIndicatorCstReady.SetTrigger(true, out _);

                _trigPresentAbsent.CLK = false;
                _isLoaded = false;


            }
            if (CassetteState == LoadportCassetteState.Normal)
            {
                _doIndicatorCstPresent.SetTrigger(true, out _);
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


            if (CurrentState == LoadPortStateEnum.Error)
            {
                _doIndicatorCstAlarm.SetTrigger(true, out _);
            }
            else
                _doIndicatorCstAlarm.SetTrigger(false, out _);
            return true;
        }

        public override void InformProcessComplete()
        {
            _doIndicatorCstComplete.SetTrigger(true, out _);
            _doIndicatorCstBusy.SetTrigger(false, out _);

        }
        public override void InformProcessStart()
        {
            _doIndicatorCstComplete.SetTrigger(false, out _);
            _doIndicatorCstBusy.SetTrigger(true, out _);
        }
        private bool SetAndMap(object[] param, out string reason)
        {
            _isLoaded = true;
            //InfoPadCarrierIndex = Convert.ToInt16(param[0]);
            return MapWafer(out reason);
        }

        private bool SetAndLoad(object[] param, out string reason)
        {
            InfoPadCarrierIndex = Convert.ToInt16(param[0]);
            _isLoaded = true;
            reason = "";
            return true;
            //return Load(out reason);
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


        }

      
        
        


        public override void Reset()
        {
            base.Reset();

            //_trigWaferProtrude.RST = true;
        }
        protected override bool fMonitorReset(object[] param)
        {



            IsBusy = false;

            SetPlaced(CassetteState == LoadportCassetteState.Normal);


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
            if (_diWaferProtrude.Value)
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


    }
}
