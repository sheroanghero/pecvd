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
    public class OpenStageBoffottoMetro : LoadPortBaseDevice, IConnection
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
        private IoSensor _di4InchCassettePlacement;
        private IoSensor _di4InchWaferProtrude;
        private IoSensor _di6InchCassettePlacement;
        private IoSensor _di6InchWaferProtrude;








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
                if (_di4InchCassettePlacement.Value && !_di6InchCassettePlacement.Value)
                {                    
                    InfoPadCarrierIndex = 4; 
                    return LoadportCassetteState.Normal;
                }
                if (!_di4InchCassettePlacement.Value && _di6InchCassettePlacement.Value)
                {
                    InfoPadCarrierIndex = 6;
                    return LoadportCassetteState.Normal;
                }
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
                case 1:
                case 2:
                case 3:                    
                case 4:
                    return WaferSize.WS4;
                case 5:
                case 6:
                    return WaferSize.WS6;
                default:
                    return WaferSize.WS0;
            }
        }
        public string Address { get => ""; }

        public bool IsConnected => true;


        public OpenStageBoffottoMetro(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");            

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _di4InchCassettePlacement = dis[0];
            _di4InchWaferProtrude = dis[1];
            _di6InchCassettePlacement = dis[2];
            _di6InchWaferProtrude = dis[3];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;


            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;

            if (CassetteState == LoadportCassetteState.Absent)
            {
                SetPresent(false);
                SetPlaced(false);
            }

            LoadportReset(out _);

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

            switch (InfoPadCarrierIndex)
            {
                case 1:
                case 2:
                case 3:
                    
                    break;
                case 4:
                    if (_di4InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                case 5:
                case 6:
                    if (_di6InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                default:
                    break;
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
            switch (InfoPadCarrierIndex)
            {
                case 1:
                case 2:
                case 3:

                    break;
                case 4:
                    if (_di4InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                case 5:
                case 6:
                    if (_di6InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                default:
                    break;
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
