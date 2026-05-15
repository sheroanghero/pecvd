using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using System;
using System.Collections.Generic;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using static Aitex.Core.RT.Device.Unit.IOE84Passive;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public enum IndicatorType
    {
        Load,
        Unload,

        Presence,
        Placement,
        Alarm,
        AccessManual,
        AccessAuto,
        Status1,
        Status2,
        Manual,
        Auto,
        Reserve,
        Error,
        Access,
        Busy,
        Complete,
    }


    public enum E84PioPosition
    {
        Floor = 0,
        Middle = 1,
        Overhead = 2
    }
    public enum E84PioSignalAtoP
    {
        VALID = 1,
        CS_0 = 2,
        CS_1 = 3,
        TR_REQ = 4,
        BUSY = 5,
        COMPT = 6,
        CONT = 7,
        AM_AVBL = 8
    }
    public enum E84PioSignalPtoA
    {
        L_REQ = 11,
        U_REQ = 12,
        READY = 13,
        HO_AVBL = 14,
        ES = 15,
        VA = 16,
        VS_0 = 17,
        VS_1 = 18
    }

    public interface IE87CallBack
    {
        void CarrierArrive();
        void CarrerRemove(string carrierID);
        void CarrierIDReadSuccess(string carrierID);
        void CarrierIDReadFail();
        void MappingComplete(string carrierID, string slotmap);
        void LoadportError(string errorcode);
        void LoadComplete();
        void UnloadComplete();
        void OnLPHomed();
        void OnE84HandoffStart(bool isload);
        void OnE84HandoffComplete(bool isload);
    }
    public interface IE84CallBack
    {
        void SetHoAutoControl(bool value);
        void SetHoAvailable(bool value);

        void SetE84SignalState(E84PassiveSignal signal, bool value);
        bool GetE84SignalState(E84SignalID signal);
        void SetFoupStatus(bool foupon);
        void SetReadyTransferStatus(bool ready);

        void SetLightCurtainHandle(LightCurtainHandleEnum handle);

        E84State GetCurrentE84State();
        void Stop();
        void Reset();
        void ResetE84();
        void Complete();
        event Action<string> OnE84HandOffStart;
        event Action<string> OnE84HandOffComplete;
        event Action<E84Timeout, string> OnE84HandOffTimeout;
        event Action<E84SignalID, bool> OnE84ActiveSignalChange;
        event Action<E84SignalID, bool> OnE84PassiveSignalChange;
    }

    public abstract class LoadPort : BaseDevice, ILoadPort
    {
        public event Action<bool> ActionDone;
        public IndicatorState IndicatiorLoad { get; set; }
        public IndicatorState IndicatiorUnload { get; set; }
        public IndicatorState IndicatiorPresence { get; set; }
        public IndicatorState IndicatorAlarm { get; set; }
        public IndicatorState IndicatiorPlacement { get; set; }
        public IndicatorState IndicatiorOpAccess { get; set; }
        public IndicatorState IndicatiorAccessAuto { get; set; }
        public IndicatorState IndicatiorAccessManual { get; set; }
        public IndicatorState IndicatiorStatus1 { get; set; }
        public IndicatorState IndicatiorStatus2 { get; set; }
        public IndicatorState IndicatiorManualMode { get; set; }
        public IndicatorState IndicatiorClampUnclamp { get; set; }
        public IndicatorState IndicatiorDockUndock { get; set; }

        public static Dictionary<IndicatorType, Indicator> LoadPortIndicatorLightMap =
            new Dictionary<IndicatorType, Indicator>()
            {
                {IndicatorType.Load, Indicator.LOAD},
                {IndicatorType.Unload, Indicator.UNLOAD},
                {IndicatorType.AccessAuto, Indicator.ACCESSAUTO },
                {IndicatorType.AccessManual, Indicator.ACCESSMANUL},
                {IndicatorType.Alarm, Indicator.ALARM},
                {IndicatorType.Presence, Indicator.PRESENCE},
                {IndicatorType.Placement, Indicator.PLACEMENT},
                {IndicatorType.Status1, Indicator.RESERVE1},
                {IndicatorType.Status2, Indicator.RESERVE2},
            };

        public IndicatorState[] IndicatorStateFeedback { get; set; }

        public virtual FoupClampState ClampState { get; set; }

        public virtual FoupDockState DockState { get; set; }

        public virtual FoupDoorState DoorState { get; set; }

        public virtual CasstleType CasstleType { get; set; }

        public string SlotMap
        {
            get { return GetSlotMap(); }
        }
        //Hirata
        public int WaferCount { get; set; }

        public virtual bool IsBusy { get; set; }
        public virtual bool IsIdle { get; set; }
        public virtual bool IsMoving { get; set; }

        public virtual bool IsInfoPadAOn { get; set; }
        public virtual bool IsInfoPadBOn { get; set; }
        public virtual bool IsInfoPadCOn { get; set; }
        public virtual bool IsInfoPadDOn { get; set; }

        public virtual bool IsWaferProtrude { get; set; }
        public virtual bool IsMonitorProtrude { get; set; } = false;
        public virtual string InfoPadCarrierType { get; set; } = "";

        public virtual string InfoPadCarrierTypeInformation
        {
            get
            {
                if (_isPresent)
                    return ("Index:" + InfoPadCarrierIndex.ToString() + "\r\n"
                    + "Type:" + InfoPadCarrierType + "\r\n"
                    + "Size:" + GetCurrentWaferSize().ToString());
                return "";
            }
        }


        public virtual int InfoPadCarrierIndex { get; set; } = -1;

        public virtual LoadportCassetteState CassetteState
        {
            get;
            set;
        }


        public WaferSize Size
        {
            get
            {
                IoWaferSizeDetector detector = DEVICE.GetDevice<IoWaferSizeDetector>($"{Name}WaferDetector");
                if (detector != null)
                    return detector.Value;
                int carrierIndex = SC.ContainsItem($"CarrierInfo.{Name}CarrierIndex") ?
                SC.GetValue<int>($"CarrierInfo.{Name}CarrierIndex") : 0;
                WaferSize wsize = SC.ContainsItem($"CarrierInfo.CarrierWaferSize{carrierIndex}") ?
                    (WaferSize)SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{carrierIndex}") : WaferSize.WS12;
                return wsize;
            }
        }

        public virtual WaferSize GetCurrentWaferSize()
        {
            return Size;
        }

        public bool IsMapWaferByLoadPort { get; set; }

        public EnumLoadPortType PortType { get; set; }

        public bool Initalized { get; set; }

        public bool IsPresent
        {
            get { return _isPresent; }
        }

        public bool IsPlacement
        {
            get { return _isPlaced; }
        }

        public bool IsMapped
        {
            get { return _isMapped; }
        }

        public bool IsComplete { get; set; }

        public bool IsAccessSwPressed
        {
            get { return _isAccessSwPressed; }
        }

        public bool Error { get; set; }
        public string ErrorCode { get; set; }

        public bool MapError { get; set; }

        public bool ReadCarrierIDError { get; set; }
        public bool ExecuteError { get; set; } = false;

        /// <summary>
        /// 是否处于FOSB模式
        /// </summary>
        public bool IsFOSBMode { get; set; }

        public TDKY_AxisPos DockPOS { get; set; } = TDKY_AxisPos.Unknown;

        public TDKZ_AxisPos DoorPOS { get; set; } = TDKZ_AxisPos.Unknown;


        public DeviceState State
        {
            get
            {
                if (!Initalized)
                {
                    return DeviceState.Unknown;
                }
                if (Error || ExecuteError || MapError || ReadCarrierIDError)
                {
                    return DeviceState.Error;
                }

                if (IsBusy)
                    return DeviceState.Busy;

                return DeviceState.Idle;
            }
        }

        public string CarrierId
        {
            get { return _carrierId; }
        }
        public string LPLotID
        {
            get { return _lplotID; }
        }

        public string RfId
        {
            get { return _rfid; }
        }

        public RD_TRIG PresentTrig
        {
            get { return _presentTrig; }
        }

        public RD_TRIG PlacedtTrig
        {
            get { return _placetTrig; }
        }

        public RD_TRIG AccessSwPressedTrig
        {
            get { return _accessSwPressedTrig; }
        }

        public RD_TRIG ClampedTrig
        {
            get { return _clampTrig; }
        }

        public RD_TRIG DockTrig
        {
            get { return _dockTrig; }
        }

        public RD_TRIG DoorTrig
        {
            get { return _doorTrig; }
        }

        protected bool _isPresent;
        protected bool _isPlaced;
        protected bool _isDocked;
        protected bool _isAccessSwPressed;
        protected string _carrierId = "";
        protected string _rfid;
        protected string _lplotID;

        protected bool _isMapped;


        private ModuleName _module;
        public ModuleName LPModuleName => _module;

        //private ModuleStateEnum _state;

        private readonly RD_TRIG _presentTrig = new RD_TRIG();
        private readonly RD_TRIG _placetTrig = new RD_TRIG();
        private readonly RD_TRIG _accessSwPressedTrig = new RD_TRIG();
        private readonly RD_TRIG _clampTrig = new RD_TRIG();
        private readonly RD_TRIG _dockTrig = new RD_TRIG();
        private readonly RD_TRIG _doorTrig = new RD_TRIG();

        public R_TRIG TrigWaferProtrude = new R_TRIG();

        private List<List<string>> _waferId = new List<List<string>>();
        private int _slotNumber;


        public string PortId
        {
            get; private set;
        }

        public string PortCategory
        {
            get
            {
                return (SC.ContainsItem($"LoadPort.LoadPort{PortId}CarrierLabel") ?
                     SC.GetStringValue($"LoadPort.LoadPort{PortId}CarrierLabel") : "");
            }
        }


        ModuleName[] PortModuleNames = new[]
        {
            ModuleName.LP1,ModuleName.LP2,ModuleName.LP3,ModuleName.LP4,ModuleName.LP5,
            ModuleName.LP6,ModuleName.LP7,ModuleName.LP8,ModuleName.LP9,ModuleName.LP10,
            ModuleName.Cassette,
            ModuleName.CassAL, ModuleName.CassAR, ModuleName.CassBL, ModuleName.CassBR,
        };




        private string EventCarrierArrived = "CARRIER_ARRIVED";
        private string EventCarrierIdRead = "CARRIER_ID_READ";
        private string EventCarrierIdReadFailed = "CARRIER_ID_READ_FAILED";
        private string EventCarrierIdWrite = "CARRIER_ID_WRITE";
        private string EventCarrierIdWriteFailed = "CARRIER_ID_WRITE_FAILED";
        protected string EventSlotMapAvailable = "SLOT_MAP_AVAILABLE";
        private string EventCarrierRemoved = "CARRIER_REMOVED";
        private string EventCarrierUnloaded = "CARRIER_UNLOADED";
        public string EventCarrierLoaded = "CARRIER_LOADED";

        public string EventCarrierClamped = "CCLMP";
        public string EventCarrierUnClamped = "CUCLMP";

        private string EventLPHomed = "LP_HOMED";

        protected string PORT_ID = "PORT_ID";
        protected string CAR_ID = "CAR_ID";
        protected string SLOT_MAP = "SLOT_MAP";
        protected string SLOT_MAP_List = "SlotMapList";
        protected string PORT_CTGRY = "PORT_CTGRY";
        protected string RF_ID = "RF_ID";
        protected string PORT_CARRIER_TYPE = "PORT_CARRIER_TYPE";

        private string EventRfIdRead = "RF_ID_READ";
        private string EventRfIdReadFailed = "RF_ID_READ_FAILED";
        private string EventRfIdWrite = "RF_ID_WRITE";
        private string EventRfIdWriteFailed = "RF_ID_WRITE_FAILED";

        public string AlarmLoadPortError { get => _module + "Error"; }
        public string AlarmLoadPortMappingError { get => _module + "MappingError"; }
        public string AlarmCarrierIDReadError { get => _module + "CarrierIDReadFail"; }


        private IE87CallBack _lpcallback = null;
        public IE87CallBack LPCallBack
        {
            get { return _lpcallback; }
            set { _lpcallback = value; }
        }
        private IE84CallBack _lpE84Callback = null;
        public IE84CallBack LPE84Callback
        {
            get { return _lpE84Callback; }
            set { _lpE84Callback = value; }
        }
        private ICarrierIDReader _carrierIDReadercallback = null;
        public ICarrierIDReader CarrierIDReaderCallBack
        {
            get { return _carrierIDReadercallback; }
            set { _carrierIDReadercallback = value; }
        }
        public CIDReaderBaseDevice CurrentReader { get; }


        public LoadPort(int slotNumber = 25)
        {
            _slotNumber = slotNumber;
        }

        public LoadPort(string module, string name, int slotNumber = 25) : base(module, name, name, "")
        {
            _slotNumber = slotNumber;
        }
        public virtual bool IsAutoClampOnFoupOn
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.LP{PortId}.AutoClampOnFoupOn"))
                    return SC.GetValue<bool>($"LoadPort.LP{PortId}.AutoClampOnFoupOn");
                return false;
            }
        }

        public virtual LightCurtainHandleEnum CurrentLightCurtainHandle
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.LightCurtainHandle"))
                    return (LightCurtainHandleEnum)SC.GetValue<int>($"LoadPort.{LPModuleName}.LightCurtainHandle");
                return LightCurtainHandleEnum.Bypass;
            }
        }


        public virtual bool Initialize()
        {
            for (int i = 0; i < 25; i++)
            {
                _waferId.Add(new List<string>()
                {
                    i.ToString("D2"),"","",""
                });
            }


            if (!Enum.TryParse(Name, out ModuleName m))
                Enum.TryParse(Module, out m);

            _module = m;

            if ((int)_module >= 97)
                PortId = ((int)_module - 96).ToString();
            else
                PortId = ((int)_module).ToString();


            DoorState = FoupDoorState.Unknown;



            WaferManager.Instance.SubscribeLocation(_module, _slotNumber);

            CarrierManager.Instance.SubscribeLocation(_module.ToString());

            DATA.Subscribe(Name, "IsPresent", () => _isPresent);
            DATA.Subscribe(Name, "IsPlaced", () => _isPlaced);
            DATA.Subscribe(Name, "IsClamped", () => ClampState == FoupClampState.Close);
            DATA.Subscribe(Name, "IsDocked", () => DockState == FoupDockState.Docked);
            DATA.Subscribe(Name, "IsDoorOpen", () => DoorState == FoupDoorState.Open);
            DATA.Subscribe(Name, "ModuleState", () => "Idle");
            DATA.Subscribe(Name, "CarrierId", () => _carrierId);
            DATA.Subscribe(Name, "LPLotID", () => _lplotID);


            DATA.Subscribe(Name, "IsMapped", () => _isMapped);

            DATA.Subscribe($"{Name}.LoadportState", () => State);
            DATA.Subscribe($"{Name}.LoadportBusy", () => IsBusy);
            DATA.Subscribe($"{Name}.LoadportError", () => ErrorCode);
            DATA.Subscribe($"{Name}.CassetteState", () => CassetteState);
            DATA.Subscribe($"{Name}.FoupClampState", () => ClampState);
            DATA.Subscribe($"{Name}.FoupDockState", () => DockState);
            DATA.Subscribe($"{Name}.FoupDoorState", () => DoorState);
            DATA.Subscribe($"{Name}.SlotMap", () => SlotMap);
            DATA.Subscribe($"{Name}.IndicatiorLoad", () => IndicatiorLoad);
            DATA.Subscribe($"{Name}.IndicatiorUnload", () => IndicatiorUnload);
            DATA.Subscribe($"{Name}.IndicatiorPresence", () => IndicatiorPresence);
            DATA.Subscribe($"{Name}.IndicatiorPlacement", () => IndicatiorPlacement);
            DATA.Subscribe($"{Name}.IndicatiorAlarm", () => IndicatorAlarm);
            DATA.Subscribe($"{Name}.IndicatiorAccessAuto", () => IndicatiorAccessAuto);
            DATA.Subscribe($"{Name}.IndicatiorAccessManual", () => IndicatiorAccessManual);
            DATA.Subscribe($"{Name}.IndicatiorOpAccess", () => IndicatiorOpAccess);
            DATA.Subscribe($"{Name}.IndicatiorStatus1", () => IndicatiorStatus1);
            DATA.Subscribe($"{Name}.IndicatiorStatus2", () => IndicatiorStatus2);

            DATA.Subscribe($"{Name}.CasstleType", () => (int)CasstleType);
            DATA.Subscribe($"{Name}.InfoPadCarrierType", () => InfoPadCarrierType);
            DATA.Subscribe($"{Name}.InfoPadCarrierTypeInformation", () => InfoPadCarrierTypeInformation);
            DATA.Subscribe($"{Name}.InfoPadCarrierIndex", () => InfoPadCarrierIndex);
            DATA.Subscribe($"{Name}.IsError", () => State == DeviceState.Error);


            //if (PortStateVariableNames.Length > _lpIndex)
            //    DATA.Subscribe(PortStateVariableNames[_lpIndex], () => (_isPlaced && _isPresent) ? "1" : "0");
            //if (PortSlotMapVariableNames.Length > _lpIndex)
            //    DATA.Subscribe(PortSlotMapVariableNames[_lpIndex], () => SlotMap);
            //if (PortWaferIdVariableNames.Length > _lpIndex)
            //    DATA.Subscribe(PortWaferIdVariableNames[_lpIndex], UpdatedWaferIdList);


            EV.Subscribe(new EventItem("Event", EventCarrierArrived, "Carrier arrived"));
            EV.Subscribe(new EventItem("Event", EventCarrierRemoved, "Carrier removed"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdRead, "Carrier ID read"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdReadFailed, "Carrier ID read failed"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWrite, "Carrier ID write"));
            EV.Subscribe(new EventItem("Event", EventCarrierIdWriteFailed, "Carrier ID write failed"));
            EV.Subscribe(new EventItem("Event", EventSlotMapAvailable, "Slot map available"));
            EV.Subscribe(new EventItem("Event", EventCarrierUnloaded, "Carrier unloaded"));
            EV.Subscribe(new EventItem("Event", EventCarrierLoaded, "Carrier loaded"));
            EV.Subscribe(new EventItem("Event", EventRfIdRead, "Carrier RFID read"));
            EV.Subscribe(new EventItem("Event", EventRfIdReadFailed, "Carrier RFID read failed"));
            EV.Subscribe(new EventItem("Event", EventRfIdWrite, "Carrier RFID write"));
            EV.Subscribe(new EventItem("Event", EventRfIdWriteFailed, "Carrier RFID write failed"));

            EV.Subscribe(new EventItem("Event", EventCarrierClamped, "Carrier was clamped."));
            EV.Subscribe(new EventItem("Event", EventCarrierUnClamped, "Carrier was unclamped."));


            EV.Subscribe(new EventItem("Event", AlarmLoadPortError, $"Load Port {Name}error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Event", AlarmLoadPortMappingError, $"Load Port {Name} mapping error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Event", AlarmCarrierIDReadError, $"Load Port {Name} read carrierID error", EventLevel.Alarm, EventType.EventUI_Notify));

            //}
            IsIdle = true;
            //_state = ModuleStateEnum.Idle;
            IndicatorStateFeedback = new IndicatorState[20];


            if (_lpE84Callback != null)
            {
                _lpE84Callback.OnE84ActiveSignalChange += _lpE84Callback_OnE84ActiveSignalChange;
                _lpE84Callback.OnE84PassiveSignalChange += _lpE84Callback_OnE84PassiveSignalChange;

                _lpE84Callback.OnE84HandOffComplete += _lpE84Callback_OnE84HandOffComplete;
                _lpE84Callback.OnE84HandOffStart += _lpE84Callback_OnE84HandOffStart;
                _lpE84Callback.OnE84HandOffTimeout += _lpE84Callback_OnE84HandOffTimeout;
            }



            RegisterOperation();

            return true;
        }

        private void _lpE84Callback_OnE84PassiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        private void _lpE84Callback_OnE84HandOffTimeout(E84Timeout arg2, string arg3)
        {
            ;
        }

        private void _lpE84Callback_OnE84HandOffStart(string arg2)
        {
            OnE84HandOffStart(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84HandOffComplete(string arg2)
        {
            OnE84HandOffComplete(arg2 == "Load");
        }

        private void _lpE84Callback_OnE84ActiveSignalChange(E84SignalID arg2, bool arg3)
        {
            ;
        }

        public virtual bool Connect()
        {
            return true;
        }
        public virtual bool IsAutoReadCarrierID
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{_module}.EnableAutoCarrierIdRead") ?
                        SC.GetValue<bool>($"LoadPort.{_module}.EnableAutoCarrierIdRead") : false;

            }
        }

        public virtual bool IsBypassCarrierIDReader
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{_module}.BypassCarrierIDReader") ?
                        SC.GetValue<bool>($"LoadPort.{_module}.BypassCarrierIDReader") : false;

            }
        }



        private void RegisterOperation()
        {
            OP.Subscribe($"{Name}.LoadportHome", (string cmd, object[] param) =>
            {
                if (!Home(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start home");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportReset", (string cmd, object[] param) =>
            {
                if (!ClearError(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not reset, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start reset");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportStop", (string cmd, object[] param) =>
            {
                if (!Stop(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not stop, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} stop");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoad", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithoutMap", (string cmd, object[] param) =>
            {
                if (!LoadWithoutMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load without map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load without map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportLoadWithMap", (string cmd, object[] param) =>
            {
                if (!Load(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not load with map, {reason}");
                    return false;
                }

                if (!QueryWaferMap(out reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start load with map");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnload", (string cmd, object[] param) =>
            {
                if (!Unload(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unload, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unload");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportClamp", (string cmd, object[] param) =>
            {
                if (!Clamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not clamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start clamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUnclamp", (string cmd, object[] param) =>
            {
                if (!Unclamp(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not unclamp, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start unclamp");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoor", (string cmd, object[] param) =>
            {
                if (!OpenDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportOpenDoorNoMap", (string cmd, object[] param) =>
            {
                if (!OpenDoorNoMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not open door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start open door");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportCloseDoor", (string cmd, object[] param) =>
            {
                if (!CloseDoor(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not close door, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start close door");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportDock", (string cmd, object[] param) =>
            {
                if (!Dock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not dock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start dock");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportUndock", (string cmd, object[] param) =>
            {
                if (!Undock(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not undock, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start undock");
                return true;
            });


            OP.Subscribe($"{Name}.LoadportQueryState", (string cmd, object[] param) =>
            {
                if (!QueryState(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportQueryLED", (string cmd, object[] param) =>
            {
                if (!QueryIndicator(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not query led state, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start query led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportSetLED", (string cmd, object[] param) =>
            {
                int light = (int)param[0];
                int state = (int)param[1];
                if (!SetIndicator((Indicator)light, (IndicatorState)state, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set led state, {reason}");
                    return true;
                }

                EV.PostInfoLog(Module, $"{Name} start set led state");
                return true;
            });

            OP.Subscribe($"{Name}.LoadportMap", (string cmd, object[] param) =>
            {
                if (!QueryWaferMap(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not map, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start map");
                return true;
            });

            OP.Subscribe($"{Name}.SetCassetteType", (string cmd, object[] param) =>
            {
                if (!SetCassetteType(param, out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not set type, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} cassette type have set to {CasstleType}");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportForceHome", (string cmd, object[] param) =>
            {
                if (!ForceHome(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not start force home, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start force home");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOSBMode", (string cmd, object[] param) =>
            {
                if (!FOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }

                IsFOSBMode = true;
                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportFOUPMode", (string cmd, object[] param) =>
            {
                if (!FOUPMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOSB mode, {reason}");
                    return false;
                }

                IsFOSBMode = true;
                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.LoadportQueryFOSBMode", (string cmd, object[] param) =>
            {
                if (!QueryFOSBMode(out string reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not change to FOUP mode, {reason}");
                    return false;
                }

                IsFOSBMode = false;
                EV.PostInfoLog(Module, $"{Name} changed to FOSB mode");
                return true;
            });
            OP.Subscribe($"{Name}.SetLoadportLotID", (string cmd, object[] param) =>
            {
                if (!SetLotID(param, out string reason))
                {

                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} set lotID for loadport.");
                return true;
            });


        }

        private bool SetLotID(object[] param, out string reason)
        {
            reason = "";
            if (param == null || param.Length == 0) return false;
            _lplotID = param[0].ToString();
            return true;
        }

        private bool SetCassetteType(object[] param, out string reason)
        {
            reason = "";
            if (param.Length != 1)
            {
                reason = "Invalid setting parameter.";
                return false;
            }
            CasstleType = (CasstleType)int.Parse(param[0].ToString());
            return true;
        }
        public virtual bool FALoad(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool Load(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool LoadWithoutMap(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool QueryWaferMap(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool GetMapInfo(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool SetWaferMap(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool QueryFOSBMode(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的Dock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDock(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的FOSBUnDock指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBUnDock(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的开门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorOpen(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的关门指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorClose(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的门下移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorDown(out string reason)
        {
            reason = "";
            return true;
        }

        /// <summary>
        /// FOSB模式下的门上移指令
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public virtual bool FOSBDoorUp(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool SetIndicator(IndicatorType light, IndicatorState state)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                SetIndicator(LoadPortIndicatorLightMap[light], state, out string _);
                return true;
            }
            EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return false;
        }

        public IndicatorState GetIndicator(IndicatorType light)
        {
            if (LoadPortIndicatorLightMap.ContainsKey(light))
            {
                return IndicatorStateFeedback[(int)LoadPortIndicatorLightMap[light]];
            }
            EV.PostWarningLog(Module, $"Not supported indicator {light}");
            return IndicatorState.OFF;
        }


        public virtual bool SetE84Available(out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(true);
            }
            reason = "";
            return true;
        }
        public virtual bool SetE84Unavailable(out string reason)
        {
            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetHoAutoControl(false);
                _lpE84Callback.SetHoAvailable(false);
            }
            reason = "";
            return true;
        }

        public virtual bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool QueryIndicator(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool QueryState(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Undock(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Dock(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool CloseDoor(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool OpenDoor(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool OpenDoorNoMap(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool OpenDoorAndMap(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Unclamp(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Clamp(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Unload(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Stop(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ClearError(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool Init(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool Home(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ForceHome(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool FOSBMode(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool FOUPMode(out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ReadRfId(out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool ReadRfid(int startpage, int length, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool WriteRfid(string cid, int startpage, int length, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual bool ChangeAccessMode(bool auto, out string reason)
        {
            reason = "";
            return true;
        }

        //public virtual bool ChangeTransferState(LoadPortTransferState newState, out string reason)
        //{
        //    reason = "";
        //    return true;
        //}

        public virtual bool SetServiceCommand(bool inService, out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool GetE84HandoffActiveSignalState(E84PioPosition piopostion, E84PioSignalAtoP AtoPsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (AtoPsignal)
            {
                case E84PioSignalAtoP.AM_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.AM_AVBL);
                case E84PioSignalAtoP.BUSY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.BUSY);
                case E84PioSignalAtoP.COMPT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.COMPT);
                case E84PioSignalAtoP.CONT:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CONT);
                case E84PioSignalAtoP.CS_0:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_0);
                case E84PioSignalAtoP.CS_1:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.CS_1);
                case E84PioSignalAtoP.TR_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.TR_REQ);
                case E84PioSignalAtoP.VALID:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.VALID);
                default:
                    return false;
            }
        }
        public virtual bool GetE84HandoffPassiveSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal)
        {
            if (_lpE84Callback == null) return false;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.ES);
                case E84PioSignalPtoA.HO_AVBL:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.HO_AVBL);
                case E84PioSignalPtoA.L_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.L_REQ);
                case E84PioSignalPtoA.READY:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.READY);
                case E84PioSignalPtoA.U_REQ:
                    return _lpE84Callback.GetE84SignalState(E84SignalID.U_REQ);
                default:
                    return false;
            }



        }
        public virtual void SetE84HandoffSignalState(E84PioPosition piopostion, E84PioSignalPtoA PtoAsignal, bool state)
        {
            if (_lpE84Callback == null) return;
            switch (PtoAsignal)
            {
                case E84PioSignalPtoA.ES:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.ES, state);
                    break;
                case E84PioSignalPtoA.HO_AVBL:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.HOAvbl, state);
                    break;
                case E84PioSignalPtoA.L_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.LoadReq, state);
                    break;
                case E84PioSignalPtoA.READY:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.Ready, state);
                    break;
                case E84PioSignalPtoA.U_REQ:
                    _lpE84Callback.SetE84SignalState(E84PassiveSignal.UnloadReq, state);
                    break;
                default:
                    break;

            }
        }

        public virtual void Monitor()
        {
            _presentTrig.CLK = _isPresent;
            _placetTrig.CLK = _isPlaced;
            _dockTrig.CLK = _isDocked;
            _clampTrig.CLK = ClampState == FoupClampState.Close;
            _doorTrig.CLK = DoorState == FoupDoorState.Close;
            _accessSwPressedTrig.CLK = _isAccessSwPressed;

            if (_lpE84Callback != null)
            {
                _lpE84Callback.SetFoupStatus(_isPlaced);
                _lpE84Callback.SetReadyTransferStatus((ClampState == FoupClampState.Open) &&
                    (DockState == FoupDockState.Undocked) && Initalized);

                _lpE84Callback.SetLightCurtainHandle(CurrentLightCurtainHandle);

            }
        }

        public virtual void Reset()
        {
            Error = false;
            ExecuteError = false;
            MapError = false;
            ReadCarrierIDError = false;
            if (_carrierIDReadercallback != null)
                _carrierIDReadercallback.Reset();

        }

        public virtual void Terminate()
        {

        }

        private List<List<string>> UpdatedWaferIdList()
        {
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(_module);
            for (int i = 0; i < wafers.Length; i++)
            {
                _waferId[i][1] = wafers[i].LaserMarker;
                _waferId[i][2] = wafers[i].T7Code;
                _waferId[i][3] = wafers[i].WaferID;
            }
            return _waferId;
        }

        public abstract bool IsEnableMapWafer();
        public abstract bool IsEnableTransferWafer();
        public abstract bool IsEnableTransferWafer(out string reason);

        public virtual bool IsEnableLoad()
        {
            return _isPresent && _isPlaced;
        }

        private CarrierOnLPState _CarrierOnState { get; set; } = CarrierOnLPState.Unknow;


        protected void ConfirmAddCarrier()
        {
            if (_isPresent && _isPlaced)
            {
                if (_CarrierOnState != CarrierOnLPState.On)
                {
                    CarrierManager.Instance.CreateCarrier(Name);
                    _CarrierOnState = CarrierOnLPState.On;
                    SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();
                    dvid[PORT_ID] = PortId;
                    dvid["PortID"] = PortId;
                    dvid[PORT_CTGRY] = PortCategory;
                    dvid[PORT_CARRIER_TYPE] = InfoPadCarrierType;
                    EV.Notify(EventCarrierArrived, dvid);
                    if (_lpcallback != null) _lpcallback.CarrierArrive();

                    if (IsAutoClampOnFoupOn)
                        Clamp(out _);
                    if (IsAutoReadCarrierID)
                        ReadRfId(out _);

                }
                IsComplete = false;
            }
        }

        protected void ConfirmRemoveCarrier()
        {
            if (!_isPlaced)
            {
                WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, _slotNumber);

                CarrierManager.Instance.DeleteCarrier(Name);
                if (_CarrierOnState != CarrierOnLPState.Off)
                {
                    _CarrierOnState = CarrierOnLPState.Off;
                    SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();
                    dvid[PORT_ID] = PortId;
                    dvid["PortID"] = PortId;
                    dvid[PORT_CTGRY] = PortCategory;
                    dvid[CAR_ID] = _carrierId ?? "";
                    dvid["CarrierID"] = _carrierId ?? "";
                    EV.Notify(EventCarrierRemoved, dvid);
                    if (_lpcallback != null) _lpcallback.CarrerRemove(_carrierId);
                }



                IsComplete = false;

                _isMapped = false;
                _carrierId = "";
            }
        }

        public void OnSlotMapRead(string _slotMap)
        {
            for (int i = 0; i < _slotNumber; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                WaferInfo wafer = null;
                switch (_slotMap[i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(_module, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        wafer = WaferManager.Instance.CreateWafer(_module, i, WaferStatus.Normal);
                        WaferManager.Instance.UpdateWaferSize(_module, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '2':
                        wafer = WaferManager.Instance.CreateWafer(_module, i, WaferStatus.Crossed);
                        WaferManager.Instance.UpdateWaferSize(_module, i, GetCurrentWaferSize());

                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Crossed);
                        break;
                    case 'W':
                        wafer = WaferManager.Instance.CreateWafer(_module, i, WaferStatus.Double);
                        WaferManager.Instance.UpdateWaferSize(_module, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Double);
                        break;
                    case '?':
                        wafer = WaferManager.Instance.CreateWafer(_module, i, WaferStatus.Unknown);
                        WaferManager.Instance.UpdateWaferSize(_module, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //NotifyWaferError(Name, i, WaferStatus.Unknown);
                        break;
                }
            }
            SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
            _slotMap= _slotMap.Replace("1","3");
            _slotMap = _slotMap.Replace("0", "1");
            List<int> data = new List<int>();
            foreach (char c in _slotMap)
            {
                data.Add(Convert.ToInt32(c.ToString()));
            }
            dvid[SLOT_MAP_List] = data;
            dvid[PORT_ID] = PortId;
            dvid[PORT_CTGRY] = PortCategory;
            dvid[CAR_ID] = CarrierId == null ? "" : CarrierId;
            //dvid["CarrierID"] = CarrierId ?? "";

            EV.Notify(EventSlotMapAvailable, dvid);
            if (_slotMap.Contains("2"))
            {
                MapError = true;
                Error = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Crossed wafer." }
                });
            }
            if (_slotMap.Contains("W"))
            {
                MapError = true;
                Error = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Double wafer." }
                });
            }
            if (_slotMap.Contains("?"))
            {
                MapError = true;
                Error = true;
                EV.Notify(AlarmLoadPortMappingError, new SerializableDictionary<string, object> {
                    {"AlarmText","Mapped Unknown wafer." }
                });
            }



            if (_lpcallback != null) _lpcallback.MappingComplete(_carrierId, _slotMap);


            _isMapped = true;
        }

        private string GetSlotMap()
        {
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
            string slot = "";
            for (int i = 0; i < _slotNumber && i < wafers.Length; i++)
            {
                slot += wafers[i].IsEmpty ? "1" : "3";
            }

            return slot;
        }

        public bool IsWaferEnableTransfer()
        {
            WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
            foreach (WaferInfo wafer in wafers)
            {
                if (wafer.Status == WaferStatus.Crossed || wafer.Status == WaferStatus.Double)
                {
                    EV.PostWarningLog(Name, $"At least one wafer is {wafer.Status.ToString()}.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取LP中空缺Slot
        /// </summary>
        /// <returns>返回一个list, 顺序为从下到上.(0-25)</returns>
        public List<int> GetEmptySlot()
        {
            List<int> slot = new List<int>();
            if (IsMapped)
            {
                WaferInfo[] wafers = WaferManager.Instance.GetWafers(ModuleHelper.Converter(Name));
                for (int i = 0; i < _slotNumber; i++)
                {
                    if (wafers[i].IsEmpty)
                        slot.Add(i);
                }
                return slot;
            }
            else
            {
                return null;
            }

        }

        public virtual bool ReadCarrierID(int offset = 0, int length = 16)
        {
            if (_carrierIDReadercallback != null)
                return _carrierIDReadercallback.ReadCarrierID(offset, length);
            return false;
        }
        public virtual bool WriteCarrierID(string carrierID, int offset = 0, int length = 16)
        {
            if (_carrierIDReadercallback != null)
                return _carrierIDReadercallback.WriteCarrierID(offset, length, carrierID);
            return false;
        }
        public void OnCarrierIdRead(string carrierId)
        {
            if (_isPlaced && _isPresent)
            {
                _carrierId = carrierId;

                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();
                dvid["CarrierID"] = carrierId ?? "";
                dvid[CAR_ID] = carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid["PortID"] = PortId;
                dvid[PORT_CTGRY] = PortCategory;
                dvid[PORT_CARRIER_TYPE] = InfoPadCarrierType;
                EV.Notify(EventCarrierIdRead, dvid);

                CarrierManager.Instance.UpdateCarrierId(Name, carrierId);
                if (_lpcallback != null) _lpcallback.CarrierIDReadSuccess(_carrierId);
                ReadCarrierIDError = false;

            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }

        public void OnCarrierIdRead(ModuleName module, string name, string code)
        {
            OnCarrierIdRead(code);
        }
        public void ProceedSetCarrierID(string cid)
        {
            _carrierId = cid;
            CarrierManager.Instance.UpdateCarrierId(Name, cid);
        }
        public void OnCarrierIdWrite(ModuleName module, string name, string id)
        {
            OnCarrierIdWrite(id);
        }
        public void OnCarrierIdWrite(string carrierId)
        {
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();
                dvid[CAR_ID] = carrierId ?? "";
                dvid["CarrierID"] = carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;
                EV.Notify(EventCarrierIdWrite, dvid);

            }
            else
            {
                EV.PostWarningLog(Module, $"No FOUP found, carrier id {carrierId} not saved");
            }
        }
        public void OnCarrierIdReadFailed(ModuleName module, string name)
        {
            OnCarrierIdReadFailed();
        }
        public void OnCarrierIdReadFailed()
        {
            if (_isPlaced && _isPresent)
            {
                _carrierId = "";

                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[PORT_ID] = PortId;
                dvid["PortID"] = PortId;

                dvid[PORT_CTGRY] = PortCategory;

                EV.Notify(EventCarrierIdReadFailed, dvid);
                //if (_lpcallback != null) _lpcallback.CarrierIDReadFail();

                EV.Notify(AlarmCarrierIDReadError, new SerializableDictionary<string, object> {
                    {"AlarmText","CarrierID read fail." }
                });
                //ReadCarrierIDError = true;


            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id read is not valid");
            }
        }
        public void OnCarrierIdWriteFailed(ModuleName module, string name)
        {
            OnCarrierIdWriteFailed();
        }
        public void OnCarrierIdWriteFailed()
        {
            if (_isPlaced && _isPresent)
            {
                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;

                EV.Notify(EventCarrierIdWriteFailed, dvid);

            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, carrier id not valid");
            }
        }


        public void OnCarrierIdRead(ModuleName module, string carrierId)
        {
            OnCarrierIdRead(carrierId);
        }
        public void OnCarrierIdReadFailed(ModuleName module)
        {
            OnCarrierIdReadFailed();
        }
        public void OnCarrierIdWrite(ModuleName module, string carrierId)
        {
            OnCarrierIdWrite(carrierId);
        }

        public void OnCarrierIdWriteFailed(ModuleName module)
        {
            OnCarrierIdWriteFailed();
        }
        public void OnRfIdRead(ModuleName module, string rfid)
        {
            if (_isPlaced && _isPresent)
            {
                _rfid = rfid;


                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[CAR_ID] = _carrierId ?? "";
                dvid["CarrierID"] = _carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;
                dvid[RF_ID] = rfid ?? "";

                EV.Notify(EventRfIdRead, dvid);


                CarrierManager.Instance.UpdateRfId(Name, rfid);
                ReadCarrierIDError = false;

            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, rf id read not valid");
            }
        }
        public void OnRfIdReadFailed(ModuleName module)
        {
            if (_isPlaced && _isPresent)
            {
                _rfid = "";


                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[CAR_ID] = _carrierId ?? "";
                dvid["CarrierID"] = _carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;

                EV.Notify(EventRfIdReadFailed, dvid);

            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, rf id read is not valid");
            }
        }

        public void OnRfIdWrite(ModuleName module, string rfid)
        {
            if (_isPlaced && _isPresent)
            {
                _rfid = rfid;


                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[CAR_ID] = _carrierId ?? "";
                dvid["CarrierID"] = _carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;
                dvid[RF_ID] = rfid ?? "";

                EV.Notify(EventRfIdWrite, dvid);

                CarrierManager.Instance.UpdateRfId(Name, rfid);
            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, rf id write not valid");
            }
        }
        public void OnRfIdWriteFailed(ModuleName module)
        {
            if (_isPlaced && _isPresent)
            {
                _rfid = "";


                SerializableDictionary<string, string> dvid = new SerializableDictionary<string, string>();

                dvid[CAR_ID] = _carrierId ?? "";
                dvid["CarrierID"] = _carrierId ?? "";
                dvid[PORT_ID] = PortId;
                dvid[PORT_CTGRY] = PortCategory;
                dvid[RF_ID] = "";

                //EV.PostWarningLog(Module, "Write RFID failed.");
                EV.Notify(EventRfIdWriteFailed, dvid);


            }
            else
            {
                EV.PostWarningLog(Module, "No FOUP found, rf id write not valid");
            }
        }
        public void OnLoaded()
        {
            var dvid = new SerializableDictionary<string, string>
            {
                [CAR_ID] = _carrierId ?? "",
                ["CarrierID"] = _carrierId ?? "",
                [PORT_ID] = PortId
            };
            EV.Notify(EventCarrierLoaded, dvid);
            if (_lpcallback != null) _lpcallback.LoadComplete();

            //}
        }
        public void OnUnloaded()
        {

            var dvid = new SerializableDictionary<string, string>();
            dvid[PORT_CTGRY] = PortCategory;
            dvid[PORT_ID] = PortId;
            dvid[CAR_ID] = _carrierId ?? "";
            dvid["CarrierID"] = _carrierId ?? "";
            EV.Notify(EventCarrierUnloaded, dvid);
            //}
            DockState = FoupDockState.Undocked;
            if (_lpcallback != null) _lpcallback.UnloadComplete();

            _isMapped = false;
        }

        public void OnE84HandOffStart(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffStart(isload);
        }
        public void OnE84HandOffComplete(bool isload)
        {
            if (_lpcallback != null) _lpcallback.OnE84HandoffComplete(isload);
        }
        public void OnFosbUndock()
        {
            var dvid = new SerializableDictionary<string, string>();
            dvid[PORT_CTGRY] = PortCategory;
            dvid[PORT_ID] = PortId;
            dvid[CAR_ID] = _carrierId ?? "";
            dvid["CarrierID"] = _carrierId ?? "";
            EV.Notify(EventCarrierUnloaded, dvid);
            //}
            if (_lpcallback != null) _lpcallback.UnloadComplete();
        }


        public void OnHomed()
        {
            //for (int i = 0; i < _slotNumber; i++)
            //{
            WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, _slotNumber);
            //}
            _isMapped = false;
            var dvid = new SerializableDictionary<string, object>();
            dvid[PORT_CTGRY] = PortCategory;
            dvid[PORT_ID] = PortId;
            dvid[CAR_ID] = _carrierId ?? "";
            dvid["CarrierID"] = _carrierId ?? "";
            EV.Notify(EventLPHomed);

            if (_lpcallback != null) _lpcallback.OnLPHomed();


        }

        public void OnCloseDoor()
        {
            WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Name), 0, _slotNumber);

            _isMapped = false;
        }

        public void OnError(string error = "")
        {

            EV.Notify($"{_module}{AlarmLoadPortError}", new SerializableDictionary<string, object> {
                {"AlarmText",error }
            });

            if (ActionDone != null)
                ActionDone(false);
        }

        protected void SetPresent(bool isPresent)
        {
            _isPresent = isPresent;
            if (_isPresent)
            {
                //ConfirmAddCarrier();
            }
            else
            {
                //ConfirmRemoveCarrier();
            }
        }

        protected void SetPlaced(bool isPlaced)
        {
            _isPlaced = isPlaced;
            if (_isPlaced)
            {
                ConfirmAddCarrier();
            }
            else
            {
                ConfirmRemoveCarrier();
            }
        }


        public void OnActionDone(bool result)
        {
            if (ActionDone != null)
                ActionDone(result);
        }

        public virtual bool FAUnload(out string reason)
        {
            reason = "";
            return true;
        }
    }

}
