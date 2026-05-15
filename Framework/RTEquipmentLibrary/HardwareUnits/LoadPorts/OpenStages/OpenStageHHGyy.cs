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
    public class OpenStageHHGyy : LoadPortBaseDevice, IConnection
    {
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
        private IoSensor[] _diInfoPadA;
        private IoSensor[] _diInfoPadB;
        private IoSensor[] _diInfoPadC;
        private IoSensor _diPresence;
        private IoSensor _diWaferProtrude;

        private IoTrigger _doIndicatorCstPresent;
        private IoTrigger _doIndicatorCstPlacement;
        private IoTrigger _doIndicatorCstA;
        private IoTrigger _doIndicatorCstB;
        private IoTrigger _doIndicatorCstC;
        private IoTrigger _doIndicatorAlarm;




        private int m_InfoPadIndex;
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
                if (!_diPresence.Value)
                {
                    return LoadportCassetteState.Absent;
                }
                if (_diPresence.Value && (m_IndicatorPadIndex == 60 || m_IndicatorPadIndex == 48 || m_IndicatorPadIndex == 15))
                    return LoadportCassetteState.Normal;
                return LoadportCassetteState.Absent;

            }
        }

        public string Address { get => ""; }

        public bool IsConnected => true;

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        public OpenStageHHGyy(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            OP.Subscribe($"{Name}.SetAndLoad", (string cmd, object[] param) =>
            {
                if (!SetAndLoad(param,out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load");
                return true;
            });
            OP.Subscribe($"{Name}.SetAndMap", (string cmd, object[] param) =>
            {
                if (!SetAndMap(param,out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load");
                return true;
            });

            DATA.Subscribe($"{Name}.CarrierInformation", () => _carrierInformation);


            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diInfoPadA = new IoSensor[] { dis[0], dis[1] };
            _diInfoPadB = new IoSensor[] { dis[2], dis[3] };
            _diInfoPadC = new IoSensor[] { dis[4], dis[5] };

            _diPresence = dis[6];
            _diWaferProtrude = dis[7];


            _doIndicatorCstPresent = indicators[0];

            _doIndicatorCstPlacement = indicators[1];
            _doIndicatorCstA = indicators[2];
            _doIndicatorCstB = indicators[3];
            _doIndicatorCstC = indicators[4];
            _doIndicatorAlarm = indicators[5];



            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;

            m_InfoPadIndex = ((_diInfoPadA[0].Value && _diInfoPadA[1].Value) ? 1 : 0) +
                ((_diInfoPadB[0].Value && _diInfoPadB[1].Value) ? 2 : 0) +
                ((_diInfoPadC[0].Value && _diInfoPadC[1].Value) ? 4 : 0);

            InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{m_InfoPadIndex}") ?
                        SC.GetStringValue($"CarrierInfo.CarrierName{m_InfoPadIndex}") : "Unknown";
            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;

            InfoPadCarrierIndex = 3;
            LoadportReset(out _);

        }

        private bool SetAndMap(object[] param,out string reason)
        {
            _isLoaded = true;
            //InfoPadCarrierIndex = Convert.ToInt16(param[0]);
            return MapWafer(out reason);
        }

        private bool SetAndLoad(object[] param,out string reason)
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
            if (_isVirtualMode) return;
            if(SC.ContainsItem($"CarrierInfo.{LPModuleName}CarrierIndex"))
            {
                if ((SC.GetValue<int>($"CarrierInfo.{LPModuleName}CarrierIndex")) != m_InfoPadIndex)
                    SC.SetItemValue($"CarrierInfo.{LPModuleName}CarrierIndex", m_InfoPadIndex);
            }

            //m_InfoPadIndex = ((_diInfoPadA[0].Value && _diInfoPadA[1].Value) ? 1 : 0) +
            //    ((_diInfoPadB[0].Value && _diInfoPadB[1].Value) ? 2 : 0) +
            //    ((_diInfoPadC[0].Value && _diInfoPadC[1].Value) ? 4 : 0);

            m_IndicatorPadIndex = (_diInfoPadA[0].Value ? 32 : 0) + (_diInfoPadA[1].Value ? 16 : 0)
                + (_diInfoPadB[0].Value ? 8 : 0) + (_diInfoPadB[1].Value ? 4 : 0)
                + (_diInfoPadC[0].Value ? 2 : 0) + (_diInfoPadC[1].Value ? 1 : 0);

            switch(m_IndicatorPadIndex)
            {
                case 60:
                    m_InfoPadIndex = 6;
                    break;
                case 48:
                    m_InfoPadIndex = 5;
                    break;
                case 15:
                    m_InfoPadIndex = 3;
                    break;
                default:
                    m_InfoPadIndex = 0;
                    break;

            }


            InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{m_InfoPadIndex}") ?
                        SC.GetStringValue($"CarrierInfo.CarrierName{m_InfoPadIndex}") : "Unknown";
            if (_isPlaced)
            {
                SpecCarrierType = InfoPadCarrierType;
                string carrierinfor = SC.ContainsItem($"CarrierInfo.CarrierInformation{InfoPadCarrierIndex}") ?
                    SC.GetStringValue($"CarrierInfo.CarrierInformation{InfoPadCarrierIndex}") : "";
                string stationName = SC.GetStringValue($"CarrierInfo.{Name}Station{InfoPadCarrierIndex}");
                _carrierInformation = $"Thickness:{carrierinfor},Station Name:{stationName}";
            }
            else
            {
                SpecCarrierType = "";
                _carrierInformation = "";
            }

            _doIndicatorAlarm.SetTrigger(MapError, out _);

            _doIndicatorCstPresent.SetTrigger(_diPresence.Value, out _);


            if (CassetteState == LoadportCassetteState.Absent)
                _trigPresentAbsent.CLK = false;
            if (CassetteState == LoadportCassetteState.Normal)
            {
                if (m_InfoPadIndex == 6)
                {
                    _doIndicatorCstA.SetTrigger(true, out _);
                    _doIndicatorCstB.SetTrigger(false, out _);
                    _doIndicatorCstC.SetTrigger(false, out _);

                    _trigPresentAbsent.CLK = true;
                    _doIndicatorCstPlacement.SetTrigger(true, out _);
                }
                else if (m_InfoPadIndex == 5)
                {
                    _doIndicatorCstA.SetTrigger(false, out _);
                    _doIndicatorCstB.SetTrigger(true, out _);
                    _doIndicatorCstC.SetTrigger(false, out _);

                    _trigPresentAbsent.CLK = true;
                    _doIndicatorCstPlacement.SetTrigger(true, out _);
                }
                else if (m_InfoPadIndex == 3)
                {
                    _doIndicatorCstA.SetTrigger(false, out _);
                    _doIndicatorCstB.SetTrigger(false, out _);
                    _doIndicatorCstC.SetTrigger(true, out _);

                    _trigPresentAbsent.CLK = true;
                    _doIndicatorCstPlacement.SetTrigger(true, out _);
                }
                else
                {
                    _doIndicatorCstA.SetTrigger(false, out _);
                    _doIndicatorCstB.SetTrigger(false, out _);
                    _doIndicatorCstC.SetTrigger(false, out _);
                    _doIndicatorCstPlacement.SetTrigger(false, out _);
                }

               
                            
            }
            else
            {
                _isLoaded = false;
                _doIndicatorCstA.SetTrigger(false, out _);
                _doIndicatorCstB.SetTrigger(false, out _);
                _doIndicatorCstC.SetTrigger(false, out _);
                _doIndicatorCstPlacement.SetTrigger(false, out _);
               
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
            var wafers = WaferManager.Instance.GetWafers(LPModuleName);
            int OcrThreshold = SC.GetValue<int>("Process.OCRScoreThreshold");
            foreach(var wafer in wafers)
            {
                if (wafer.IsEmpty)
                    continue;
                if (wafer.SubstE90Status != EnumE90Status.Processed)
                    continue;
                if (String.IsNullOrEmpty(wafer.LaserMarkerScore))
                    continue;
                float waferscore = Convert.ToSingle(wafer.LaserMarkerScore);
                if(waferscore < (float)OcrThreshold)
                {
                    wafer.SubstE90Status = EnumE90Status.Aborted;
                    wafer.ProcessState = EnumWaferProcessStatus.Failed;
                }
            }


            base.Monitor();
        }

        public override string SlotMap => newSlotMap;
       
        private string newSlotMap;
        public override void OnSlotMapRead(string _slotMap)
        {
            CurrentSlotMapResult = _slotMap;
            if (!IsMapWaferByLoadPort)
                OnActionDone(new object[] { });
            newSlotMap = string.Empty;

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
                        newSlotMap += _slotMap[i];
                        break;
                    case '1':
                        Int32 thickness = Math.Abs(Convert.ToInt32(MapRobot.ReadMappingUpData[waferindex]) - Convert.ToInt32(MapRobot.ReadMappingDownData[waferindex]));
                        Int32 MinThickness = GetMinThickness(_slotMap);
                        if (thickness > (MinThickness * m_doubleWaferFactor))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                            WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                            CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                            EV.Notify(AlarmLoadPortMapDoubleWafer);
                            waferindex++;
                            newSlotMap += "2";
                            break;                            
                        }


                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        waferindex++;
                        newSlotMap += _slotMap[i];
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapCrossedWafer);
                        newSlotMap += _slotMap[i];
                        waferindex++;
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.Notify(AlarmLoadPortMapDoubleWafer);
                        newSlotMap += "3";
                        waferindex++;
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Unknown);
                        newSlotMap += "3";
                        waferindex++;
                        break;
                    default:
                        newSlotMap += _slotMap[i];
                        break;
                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

            dvid["SlotMap"] = newSlotMap;
            dvid["PortID"] = PortID;
            dvid["PORT_CTGRY"] = SpecPortName;
            dvid["CarrierType"] = SpecCarrierType;
            dvid["CarrierID"] = CarrierId;

            EV.Notify(EventSlotMapAvailable, dvid);
            EV.Notify(EventMapComplete, dvid);
            if (newSlotMap.Contains("2"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Crossed wafer." }
                });
            }
            if (newSlotMap.Contains("W"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Double wafer." }
                });
            }
            if (newSlotMap.Contains("?"))
            {
                MapError = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Unknown wafer." }
                });
            }

            if (LPCallBack != null)
                LPCallBack.MappingComplete(_carrierId, newSlotMap);

            _isMapped = true;
        }

        private Int32 GetMinThickness(string slotmap)
        {
            Int32 thickness =-1;
            int waferindex = 0;
            foreach(char slot in slotmap)
            {
                if(slot != '0')
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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion && CurrentState != LoadPortStateEnum.TransferBlock)
            {

                //_trigWaferProtrude.CLK = !_diWaferProtrude.Value;

                //if (_trigWaferProtrude.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                reason = "Found wafer protrude";
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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion && CurrentState != LoadPortStateEnum.TransferBlock)
            {
                //_trigWaferProtrude.CLK = !_diWaferProtrude.Value;

                //if (_trigWaferProtrude.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                reason = "Found wafer protrude";
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

            if (!_diWaferProtrude.Value && !IsBypassProtrusion)
            {
                OnError("Wafer protrude");
                reason = "Found wafer protrude";
                return false;
            }

            foreach (var wafer in WaferManager.Instance.GetWafers(LPModuleName))
            {
                if (wafer.IsEmpty) continue;
                if(wafer.Status == WaferStatus.Crossed)
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
        public bool IsBypassProtrusion
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.BypassProtrusionDetectCarrier{m_InfoPadIndex}") &&
                    SC.GetValue<bool>($"CarrierInfo.BypassProtrusionDetectCarrier{m_InfoPadIndex}"))
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
