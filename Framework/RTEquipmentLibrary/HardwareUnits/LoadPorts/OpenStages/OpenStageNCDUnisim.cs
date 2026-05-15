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
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;



namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.OpenStages
{
    public class OpenStageNCDUnisim : LoadPortBaseDevice, IConnection
    {
        public event Action<ModuleName, string> OnSlotMapReadEvent;
        public EnumLoadPortType PortType { get; set; }
        public bool Initalized { get; set; }
        public bool Error { get; set; }
        public new virtual string InfoPadCarrierType { get; set; } = "";
        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }
        private IoSensor _diPlaced;
        private IoSensor _diWaferProtrude;
        private IoSensor _diLock;


        private IoTrigger _doLocker;


        
        private int m_IndicatorPadIndex;
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
        private bool _isLoaded = false;
        private string _carrierInformation = "";

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (!_diPlaced.Value)
                {
                    return LoadportCassetteState.Absent;
                }
                return LoadportCassetteState.Normal;

            }
        }
        public override bool IsVerifyPreDefineWaferCount
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount"))
                    return SC.GetValue<bool>($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount");
                return false;
            }
        }
        public string Address { get => ""; }

        public bool IsConnected => true;

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        public OpenStageNCDUnisim(string module, string name, IoSensor[] dis, IoTrigger dolocker, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            

            DATA.Subscribe($"{Name}.CarrierInformation", () => _carrierInformation);


            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");


            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPlaced = dis[0];
            _diWaferProtrude = dis[1];
            _diLock = dis[2];

            _doLocker = dolocker;

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;
            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;
        }

       

        public override bool IsLoaded => _isLoaded;



        protected override bool fStartExecute(object[] param)
        {
            try
            {
                _doLocker.SetTrigger(false, out _);
                Thread.Sleep(200);

                switch (param[0].ToString())
                {
                    case "MapWafer":
                        if (!IsMapWaferByLoadPort)
                        {
                            string reason = "";
                            if (!_isPlaced)
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


        public override int InfoPadCarrierIndex => 0;
        public override void Monitor()
        {
            if (CassetteState != LoadportCassetteState.Normal)
            {             
                
                _isLoaded = false;
                _isPresent = false;
                _isPlaced = false;
                _isMapped = false;

            }
            _trigPresentAbsent.CLK = _diPlaced.Value;
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
        public override WaferSize GetCurrentWaferSize()
        {
         
            int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            return WaferSize.WS8;
        }
  
        public override void OnSlotMapRead(string _slotMap)
        {
            CurrentSlotMapResult = _slotMap;
            if (!IsMapWaferByLoadPort)
                OnActionDone(new object[] { });

            if (_slotMap.Length != ValidSlotsNumber)
            {
                EV.PostAlarmLog("LoadPort", "Mapping Data Error.");
            }
            int waferindex = 0;

            for (int i = 0; i < _slotMap.Length; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                WaferInfo wafer = null;
                switch (_slotMap[i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);

                        waferindex++;
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapCrossedWafer);

                        waferindex++;
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapDoubleWafer);

                        waferindex++;
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapUnknownWafer);

                        waferindex++;
                        break;
                    default:
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapUnknownWafer);
                        waferindex++;
                        break;
                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = _slotMap;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            EV.Notify(EventMapComplete, dvid);
            if (_slotMap.Contains("2"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Crossed wafer." }
                });
                OnError("Mapped crossed Wafer.");
            }
            if (_slotMap.Contains("W"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Double wafer." }
                });
                OnError("Mapped double Wafer.");
            }
            if (_slotMap.Contains("?"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Unknown wafer." }
                });
                OnError("Mapped unknow Wafer.");
            }
            if (_slotMap.Contains("E"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Unknown wafer." }
                });
                OnError("Mapped unknow Wafer.");
            }

            if (IsVerifyPreDefineWaferCount && waferindex != PreDefineWaferCount)
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped wafer count not match." }
                });
                OnError("Mapped wafer count is not matched.");
            }

            if (LPCallBack != null)
                LPCallBack.MappingComplete(_carrierId, _slotMap);
            if (OnSlotMapReadEvent != null)
                OnSlotMapReadEvent(LPModuleName, _slotMap);
            _isMapped = true;



        }

        private Int32 GetMinThickness(string slotmap)
        {
            Int32 thickness = -1;
            int waferindex = 0;
            foreach (char slot in slotmap)
            {
                if (slot != '0')
                {
                    int currentThickness = Math.Abs(Convert.ToInt32(MapRobot.ReadMappingUpData[waferindex]) - Convert.ToInt32(MapRobot.ReadMappingDownData[waferindex]));
                    if (thickness > currentThickness || thickness == -1)
                        thickness = currentThickness;
                    waferindex++;
                }
            }
            return thickness;
        }

        private int m_doubleWaferFactor
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.DoubleWaferFactor"))
                    return SC.GetValue<int>($"LoadPort.{LPModuleName}.DoubleWaferFactor");
                return 2;
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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion && IsReady())
            {

                //_trigWaferProtrude.CLK = !_diWaferProtrude.Value;

                //if (_trigWaferProtrude.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                reason = "Found wafer protrude";
                OnError($"{LPModuleName} Found wafer protrude.");

                return false;
            }

            if(!_diLock.Value)
            {
                reason = "Door is not locked.";
                return false;
            }

            if (!_isLoaded)
            {
                reason = "Cassette is not loaded";
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
        public override bool MapWafer(out string reason)
        {
            _isLoaded = true;
            _doLocker.SetTrigger(false, out _);
            Thread.Sleep(200);

            _trigWaferProtrude.RST = true;
            return base.MapWafer(out reason);
        }

        public override bool Load(out string reason)
        {
            _doLocker.SetTrigger(false, out _);
            Thread.Sleep(200);
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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion && IsReady())
            {

                //_trigWaferProtrude.CLK = !_diWaferProtrude.Value;

                //if (_trigWaferProtrude.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                reason = "Found wafer protrude";
                OnError($"{LPModuleName} Found wafer protrude.");

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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion && IsReady())
            {

                //_trigWaferProtrude.CLK = !_diWaferProtrude.Value;

                //if (_trigWaferProtrude.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                reason = "Found wafer protrude";
                OnError($"{LPModuleName} Found wafer protrude.");

                return false;
            }

            //if (!_isLoaded)
            //{
            //    reason = "Cassette is not loaded";
            //    return false;
            //}
            if(!_diLock.Value)
            {
                reason = "Door is not locked.";
                return false;
            }

            if (!_isMapped)
            {
                reason = "FOUP not mapped";
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
                if (wafer.Status == WaferStatus.Unknown)
                {
                    reason = "Unknown wafer";
                    return false;
                }
            }


            reason = "";
            return true;
        }
        public bool IsBypassProtrusion
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.BypassProtrusionDetectCarrier{InfoPadCarrierIndex}") &&
                    SC.GetValue<bool>($"CarrierInfo.BypassProtrusionDetectCarrier{InfoPadCarrierIndex}"))
                {
                    return true;
                }
                return false;
            }

        }
        public override bool IsForbidAccessSlotAboveWafer()
        {
            return false;
        }

        protected override bool fStartUnload(object[] para)
        {
            if (!_isPlaced) return false;



            //_isMapped = false;


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
            //if (!IsEnableLoad(out reason))
            //{
            //    EV.PostAlarmLog("System", $"{LPModuleName} load fail:{reason}");
            //    IsBusy = false;
            //    return false;
            //}
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
