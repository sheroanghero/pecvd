using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class OpenStageLoadPortZJPolish : LoadPortBaseDevice, IConnection
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
        private IoSensor[] _diPlacements;
        private IoSensor _diOcTypeDetect;
        private IoSensor _diOcPresent;
        private IoSensor _diWaferProtrude;
        private IoSensor _diStageDoorOpened;
        private IoSensor _diDoorClose;
        private IoSensor _diLatched;
        private IoSensor _diUnlatched;
        private IoSensor _diCoverClosed;
        private IoSensor _diUnlockRequest;




        private IoTrigger _doStageCloseDoor;
        private IoTrigger _doStageOpenDoor;
        private IoTrigger _doStageLockCassette;
        private IoTrigger _doStageUnlockCassette;
        private IoTrigger _doStagCoverLock;
        private IoTrigger _doStageUnlockCoverLamp;

        private IoTrigger _doIndicatorRTLoad;
        private IoTrigger _doIndicatorRTUnload;
        private IoTrigger _doIndicatorAccessMode;
        private IoTrigger _doIndicatorPresence;
        private IoTrigger _doIndicatorPlacement;
        private IoTrigger _doIndicatorAlarm;



        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();

        private RD_TRIG _trigPlacement = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();
        private R_TRIG _trigCoverClosed = new R_TRIG();

        private R_TRIG _trigDoorOpen = new R_TRIG();
        private R_TRIG _trigDoorClose = new R_TRIG();

        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        //private bool _isStillThere = false;

        private bool _isVirtualMode = false;

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (_diPlacements[0].Value && _diPlacements[1].Value && _diPlacements[2].Value)
                {
                    return LoadportCassetteState.Absent;
                }
                if (!_diPlacements[0].Value && !_diPlacements[1].Value && !_diPlacements[2].Value)
                    return LoadportCassetteState.Normal;
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
        public string Address { get => ""; }

        public bool IsConnected => true;

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }


        public OpenStageLoadPortZJPolish(string module, string name, IoSensor[] dis, IoTrigger[] stageControls, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (stageControls == null)
                throw new ArgumentException("DI Stagecontrol cannot be null", "diNoWaferProtrude");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPlacements = new IoSensor[] { dis[0], dis[1], dis[2] };
            _diOcTypeDetect = dis[3];
            _diOcPresent = dis[4];
            _diWaferProtrude = dis[5];
            _diStageDoorOpened = dis[6];
            _diDoorClose = dis[7];
            _diLatched = dis[8];
            _diUnlatched = dis[9];
            _diCoverClosed = dis[10];
            _diUnlockRequest = dis[11];


            _doStageOpenDoor = stageControls[0];
            _doStageCloseDoor = stageControls[1];
            _doStageLockCassette = stageControls[2];
            _doStageUnlockCassette = stageControls[3];
            _doStageUnlockCoverLamp = stageControls[4];
            _doStagCoverLock = stageControls[5];


            _doIndicatorRTLoad = indicators[0];
            _doIndicatorRTUnload = indicators[1];
            _doIndicatorAccessMode = indicators[2];
            _doIndicatorPresence = indicators[3];
            _doIndicatorPlacement = indicators[4];
            _doIndicatorAlarm = indicators[5];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;

            Initalized = true;
            IsBusy = false;
            Error = false;
            LoadPortType = EnumLoadPortType.OpenStage.ToString();
            InfoPadCarrierIndex = _diOcTypeDetect.Value ? 1 : 2;


            _diUnlockRequest.OnSignalChanged += _diUnlockRequest_OnSignalChanged;
        }

        private void _diUnlockRequest_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (!IsReady()) return;
            if (_diStageDoorOpened.Value) return;
            if (!arg2) return;
            _doStagCoverLock.SetTrigger(true, out _);
            _doStageLockCassette.SetTrigger(false, out _);
            _doStageUnlockCassette.SetTrigger(true, out _);
        }

        public OpenStageLoadPortZJPolish(string module, string name) : base(module, name)
        {
            //_isVirtualMode = true;





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

        public override bool IsLoaded
        {
            get
            {
                if (_isPlaced && _diStageDoorOpened.Value)
                    return true;
                return false;
            }
        }
        public override void Monitor()
        {
            if (_isVirtualMode) return;

            _doStageUnlockCoverLamp.SetTrigger(_diUnlatched.Value && !_diLatched.Value && _doStagCoverLock.Value, out _);

            
            InfoPadCarrierIndex = _diOcTypeDetect.Value ? 1 : 2;
            

            if (_diPlacements[0].Value && _diPlacements[1].Value && _diPlacements[2].Value)
                _trigPresentAbsent.CLK = false;
            if (!_diPlacements[0].Value && !_diPlacements[1].Value && !_diPlacements[2].Value)
            {
                _trigPresentAbsent.CLK = true;
                DockState = FoupDockState.Docked;
            }


            InfoPadCarrierIndex = _diOcTypeDetect.Value ? 1 : 2;

            if (_trigPresentAbsent.R)
            {
                SetPresent(true);
                SetPlaced(true);

            }

            //remove foup
            if (_trigPresentAbsent.T)
            {
                SetPresent(false);
                SetPlaced(false);

            }
            if(CassetteState == LoadportCassetteState.Normal)
            {
                _doIndicatorPlacement.SetTrigger(true, out _);
                _doIndicatorPresence.SetTrigger(true, out _);
            }
            else
            {
                
                    _doIndicatorPlacement.SetTrigger(false, out _);
                    _doIndicatorPresence.SetTrigger(false, out _);
                
            }

            base.Monitor();

            //place foup
        }



        public override void Reset()
        {
            base.Reset();
            IsBusy = false;
            _trigWaferProtrude.RST = true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        public override bool IsEnableMapWafer(out string reason)
        {
            if (_diPlacements[0].Value || _diPlacements[1].Value || _diPlacements[2].Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_diWaferProtrude.Value)
            {
                reason = "Found wafer protrude";
                return false;
            }
            if (!_diStageDoorOpened.Value)
            {
                reason = "Door is not open";
                return false;
            }
            //if (!IsReady())
            //{
            //    reason = "Not Ready";
            //    return false;
            //}
            reason = "";
            return true;
        }

        public override bool IsEnableLoad(out string reason)
        {
            if (_diPlacements[0].Value || _diPlacements[1].Value || _diPlacements[2].Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_diWaferProtrude.Value)
            {
                reason = "Found wafer protrude";
                return false;
            }
            reason = "";
            return true;
        }
        public override bool IsEnableTransferWafer(out string reason)
        {
            if (_diPlacements[0].Value || _diPlacements[1].Value || _diPlacements[2].Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_diWaferProtrude.Value)
            {
                reason = "Found wafer protrude";
                return false;
            }
            if (!_diStageDoorOpened.Value)
            {
                reason = "Door is not open";
                return false;
            }

            if (!_isMapped)
            {
                reason = "FOUP not mapped";
                return false;
            }


            reason = "";
            return true;
        }


        protected override bool fStartUnload(object[] para)
        {
            if (!_isPlaced) return false;

            _doStageOpenDoor.SetTrigger(false, out _);
            _doStageCloseDoor.SetTrigger(true, out _);
            _doStageUnlockCassette.SetTrigger(true, out _);
            _doStageLockCassette.SetTrigger(false, out _);
            _doStagCoverLock.SetTrigger(true, out _);



            return true;
        }


        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            if (!_diLatched.Value && _diUnlatched.Value && _diDoorClose.Value && !_diStageDoorOpened.Value)
            {
                base.OnUnloaded();
                return true;
            }
            return base.fMonitorUnload(param);
        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            switch (light)
            {
                case Indicator.LOAD:
                    if (state == IndicatorState.ON) _doIndicatorRTLoad.SetTrigger(true, out _);
                    else _doIndicatorRTLoad.SetTrigger(false, out _);
                    break;
                case Indicator.UNLOAD:
                    if (state == IndicatorState.ON) _doIndicatorRTUnload.SetTrigger(true, out _);
                    else _doIndicatorRTUnload.SetTrigger(false, out _);
                    break;
                case Indicator.ACCESSAUTO:
                    if (state == IndicatorState.ON) _doIndicatorAccessMode.SetTrigger(true, out _);
                    else _doIndicatorAccessMode.SetTrigger(false, out _);
                    break;
                case Indicator.ALARM:
                    if (state == IndicatorState.ON) _doIndicatorAlarm.SetTrigger(true, out _);
                    else _doIndicatorAlarm.SetTrigger(false, out _);
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
            if (!IsPlacement)
            {
                return false;
            }
            _doStagCoverLock.SetTrigger(false, out _);
            _doStageLockCassette.SetTrigger(true, out _);
            _doStageUnlockCassette.SetTrigger(false, out _);

            _doStageOpenDoor.SetTrigger(true, out _);
            _doStageCloseDoor.SetTrigger(false, out _);
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            if (_diLatched.Value && !_diUnlatched.Value && !_diDoorClose.Value && _diStageDoorOpened.Value)
                return true;

            return base.fMonitorLoad(param);
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

        protected override bool fCompleteHome(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            return true;
        }

        public override bool QueryState(out string reason)
        {
            reason = "";
            IsBusy = false;
            return true;
        }

        public override int ValidEndSlotIndex
        {
            get
            {
                return ValidSlotsNumber - 1;
            }
        }
        public override int ValidStartSlotIndex
        {
            get
            {
                return 0;
            }
        }

        public override int ValidSlotsNumber
        {
            get
            {                
                if (SC.ContainsItem($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}");
                return 25;
            }
        }
    }
}
