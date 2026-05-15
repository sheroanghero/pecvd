using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace Aitex.Core.RT.Device.Unit
{
    public enum LPReservedState
    {
        NO_STATE = 0,
        NOT_RESERVED,
        RESERVED,
    }
    public enum LPTransferState
    {
        No_state = 10,
        OUT_OF_SERVICE = 0,
        IN_SERVICE,
        TRANSFER_READY,
        READY_TO_LOAD,
        READY_TO_UNLOAD,
        TRANSFER_BLOCKED,
        //PLACED_NORMAL_NOT_RUNNING,
    }
    public enum LPAccessMode
    {
        NO_STATE = -1,
        MANUAL,
        AUTO,
    }
    public enum SignalType
    {
        Acitvie,
        Passive,
    }
    public enum  SignalID
    {
        LightCurtain,
        CS_0,       
        CS_1,
        AM_AVBL,
        VALID,
        TR_REQ,
        BUSY,
        COMPT,
        CONT,

        L_REQ,
        U_REQ,
        HO_AVBL,
        READY,
        ES
    }


    public class Signal
    {
        public bool Value
        {
            get
            {
                if (_di != null)
                    return _di.Value;
                if (_do != null)
                    return _do.Value;

                return false;
            }
        }

        private SignalID _id;
        private RD_TRIG _trig = new RD_TRIG();
        private DIAccessor _di = null;
        private DOAccessor _do = null;

        public event Action<SignalID, bool> OnChanged;
        public Signal(SignalID id, DIAccessor diAccessor, DOAccessor doAccessor)
        {
            _id = id;
            _di = diAccessor;
            _do = doAccessor;          
        }
        
        public void Monitor()
        {
            if (_di != null)
                _trig.CLK = _di.Value;

            if (_do != null)
                _trig.CLK = _do.Value;

            if (_trig.R)
            {
                if (OnChanged != null)
                    OnChanged(_id, true);
            }

            if (_trig.M)
            {
                if (OnChanged != null)
                    OnChanged(_id, false);
            }
        }

        public void Reset()
        {
            _trig.RST = true;
        }
        
    }

    

    public interface IE84Provider
    {
        bool IsCarrierPlacement();
        LPTransferState GetTransferState();
        LPAccessMode GetAccessMode();
        LPReservedState GetReservedState();
        bool SetAccessMode(bool value);
        void E84LoadStart();
        void E84LoadComplete();
        void E84UnloadStart();
        void E84UnloadComplte();
        void E84LoadError(string errorcode);
        void E84UnloadError(string errorcode);
        void E84Retry();

    }

    public class E84Passiver : BaseDevice, IDevice
    {
        public enum E84State
        {
            Normal,
            LD_TP1_Timeout,
            LD_TP2_Timeout,
            LD_TP3_Timeout,
            LD_TP4_Timeout,
            LD_TP5_Timeout,
            LD_TP6_Timeout,
            ULD_TP1_Timeout,
            ULD_TP2_Timeout,
            ULD_TP3_Timeout,
            ULD_TP4_Timeout,
            ULD_TP5_Timeout,
            ULD_TP6_Timeout,
            Error,
           
        }

        public enum Timeout
        {
            TP1,
            TP2,
            TP3,
            TP4,
            TP5,
        }
        public bool LightCurtainBroken { get { return !DiLightCurtain; } }   //interlock
        public LPAccessMode  Mode{ get; set; }
        public LPTransferState TransferState { get; set; }
        public LPReservedState LPReserved { get; set; }

        public IE84Provider Provider { get; set; }


        public bool DiLightCurtain { get; set; }
        public bool DiValid { get { return _diValid != null ? _diValid.Value : false; } }
        public bool DiCS0 { get { return _diCS0 != null ? _diCS0.Value : false; } }
        public bool DiCS1 { get { return _diCS1 != null ? _diCS1.Value : false; } }
        public bool DiTrReq { get { return _diTrReq != null ? _diTrReq.Value : false; } }
        public bool DiBusy { get { return _diBusy != null ? _diBusy.Value : false; } }
        public bool DiCompt { get { return _diCompt != null ? _diCompt.Value : false; } }
        public bool DiCont { get { return _diCont != null ? _diCont.Value : false; } }
        public bool DiAmAvbl { get { return _diAmAvbl != null ? _diAmAvbl.Value : false; } }

        public bool DoLoadReq { get { return _doLoadReq != null ? _doLoadReq.Value : false; } }
        public bool DoUnloadReq { get { return _doUnloadReq != null ? _doUnloadReq.Value : false; } }
        public bool DoReady { get { return _doReady != null ? _doReady.Value : false; } }
        public bool DoHOAvbl { get { return _doHOAvbl != null ? _doHOAvbl.Value : false; } }
        public bool DoES { get { return _doES != null ? _doES.Value : false; } }

        public static string EventE84LoadTransactionStart = "E84LoadTransactionStart";
        public static string EventE84UnloadTransactionStart = "E84UnloadTransactionStart";
        public static string EventE84LoadTransactionComplete = "E84LoadTransactionComplete";
        public static string EventE84UnloadTransactionComplete = "E84UnloadTransactionComplete";
        public static string EventE84LoadTransactionRestart = "E84LoadTransactionRestart";
        public static string EventE84UnloadTransactionRestart = "E84UnloadTransactionRestart";
        public static string EventE84ChangeAccessModeToAuto = "EventE84ChangeAccessModeToAuto";
        public static string EventE84ChangeAccessModeToManual = "EventE84ChangeAccessModeToManual";



        public static string EventE84TP1Timeout = "E84TP1Timeout";
        public static string EventE84TP2Timeout = "E84TP2Timeout";
        public static string EventE84TP3Timeout = "E84TP3Timeout";
        public static string EventE84TP4Timeout = "E84TP4Timeout";
        public static string EventE84TP5Timeout = "E84TP5Timeout";
        public static string EventE84TP6Timeout = "E84TP6Timeout";


        //Active equipment signal
        private DIAccessor _diLightCurtain;
        private DIAccessor _diValid;            
        private DIAccessor _diCS0;
        private DIAccessor _diCS1;
        private DIAccessor _diAmAvbl;
        private DIAccessor _diTrReq;
        private DIAccessor _diBusy;
        private DIAccessor _diCompt;
        private DIAccessor _diCont;

        //Passive 
        private DOAccessor _doLoadReq;
        private DOAccessor _doUnloadReq;
        private DOAccessor _doReady;         
        private DOAccessor _doHOAvbl;
        private DOAccessor _doES;

               
        private DeviceTimer _timer = new DeviceTimer();
        private DeviceTimer _timer_TP1 = new DeviceTimer();
        private DeviceTimer _timer_TP2 = new DeviceTimer();
        private DeviceTimer _timer_TP3 = new DeviceTimer();
        private DeviceTimer _timer_TP4 = new DeviceTimer();
        private DeviceTimer _timer_TP5 = new DeviceTimer();
        private DeviceTimer _timer_TP6 = new DeviceTimer();
        private R_TRIG _trigReset = new R_TRIG();

        private List<Signal> _signals = new List<Signal>();

        //timeout
        private SCConfigItem _scTimeoutTP1;
        private SCConfigItem _scTimeoutTP2;
        private SCConfigItem _scTimeoutTP3;
        private SCConfigItem _scTimeoutTP4;
        private SCConfigItem _scTimeoutTP5;
        private SCConfigItem _scTimeoutTP6;
        private SCConfigItem _scTimeoutTP7;
        private SCConfigItem _scTimeoutTP8;

        //private int reqOnTimeout    = 2;        //L_REQ|U_REQ_ON ON ---> TR REQ ON
        //private int readyOnTimeout  = 2;        //READY ON ---> BUSY ON  
        //private int busyOnTimeout   = 2;        //BUSYON -- CARRIAGE DETECT|CARRIAGE REMOVE
        //private int reqOffTimeout   = 2;        //L_REQ|U_REQ off --->BUSY off
        //private int readyOffTimeout = 2;        //Ready off --->Valid off

        private E84State _state;
        //private Timeout _tp;
        /// <summary>
        /// This constructor uses DeviceModel's node to define IOs
        /// </summary>
        /// <param name="module"></param>
        /// <param name="node"></param>
        /// <param name="ioModule"></param>
        public E84Passiver(string module, XmlElement node, string ioModule = "")
        {
            base.Module = node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _diLightCurtain = ParseDiNode("LightCurtain", node, ioModule);
            _signals.Add(new Signal(SignalID.LightCurtain, _diLightCurtain, null));
            //Indicates that the signal transition is active and selected
            //ON: valid; OFF: not valid
            _diValid = ParseDiNode("VALID", node, ioModule);
            _signals.Add(new Signal(SignalID.VALID, _diValid, null));

            //Carrier stage 0
            //ON: Use the load port for carrier handoff; vice versa
            _diCS0 = ParseDiNode("CS_0", node, ioModule);
            _signals.Add(new Signal(SignalID.CS_0, _diCS0, null));

            //Carrier stage 1
            //ON: Use the load port for carrier handoff; vice versa
            _diCS1 = ParseDiNode("CS_1", node, ioModule);
            _signals.Add(new Signal(SignalID.CS_1, _diCS1, null));

            //Transfer Arm Available
            //ON: Handoff is available; OFF: Handoff is unavailable with any error
            _diAmAvbl = ParseDiNode("AM_AVBL", node, ioModule);
            _signals.Add(new Signal(SignalID.AM_AVBL, _diAmAvbl, null));

            //Transfer Request
            //ON: Request Handoff; vice versa
            _diTrReq = ParseDiNode("TR_REQ", node, ioModule);
            _signals.Add(new Signal(SignalID.TR_REQ, _diTrReq, null));

            //BUSY for transfer
            //ON: Handoff is in progress; vice versa
            _diBusy = ParseDiNode("BUSY", node, ioModule);
            _signals.Add(new Signal(SignalID.BUSY, _diBusy, null));
            
            //Complete Transfer
            //ON:The handoff is completed; vice versa
            _diCompt = ParseDiNode("COMPT", node, ioModule);
            _signals.Add(new Signal(SignalID.COMPT, _diCompt, null));
            
            //Continuous Handoff
            //ON: Continuous Handoff; vice versa
            _diCont = ParseDiNode("CONT", node, ioModule);
            _signals.Add(new Signal(SignalID.CONT, _diCont, null));

            //Load Request
            //ON: The load port is assigned to load a carrier; vice versa
            //CS_0 && VALID && !CarrierLoaded
            _doLoadReq = ParseDoNode("L_REQ", node, ioModule);
            _signals.Add(new Signal(SignalID.L_REQ, null, _doLoadReq));

            //Unload Request
            //ON: The load port is assigned to unload a carrier; vice versa
            //CS_0 && VALID && !CarrierUnloaded
            _doUnloadReq = ParseDoNode("U_REQ", node, ioModule);
            _signals.Add(new Signal(SignalID.U_REQ, null, _doUnloadReq));
            
            //READY for transfer(after accepted the transfer request set ON, turned OFF when COMPT ON)
            //ON: Ready for handoff; vice versa
            _doReady = ParseDoNode("READY", node, ioModule);
            _signals.Add(new Signal(SignalID.READY, null, _doReady));
            
            //Indicates the passive equipment is not available for the handoff.
            //ON: Handoff is available; OFF: vice versa but with error
            //ON when normal; OFF when : Maintenance mode / Error State 
            _doHOAvbl = ParseDoNode("HO_AVBL", node, ioModule);
            _signals.Add(new Signal(SignalID.HO_AVBL, null, _doHOAvbl));
            
            //Emergency stop
            _doES = ParseDoNode("ES", node, ioModule);
            _signals.Add(new Signal(SignalID.ES, null, _doES));

            foreach (var signal in _signals)
            {
                signal.OnChanged += OnSignalChange;
            }

            _scTimeoutTP1 = SC.GetConfigItem("FA.E84.TP1");
            _scTimeoutTP2 = SC.GetConfigItem("FA.E84.TP2");
            _scTimeoutTP3 = SC.GetConfigItem("FA.E84.TP3");
            _scTimeoutTP4 = SC.GetConfigItem("FA.E84.TP4");
            _scTimeoutTP5 = SC.GetConfigItem("FA.E84.TP5");
            _scTimeoutTP6 = SC.GetConfigItem("FA.E84.TP6");
            _scTimeoutTP7 = SC.GetConfigItem("FA.E84.TP7");
            _scTimeoutTP8 = SC.GetConfigItem("FA.E84.TP8");

            OP.Subscribe($"{Module}.E84Place", (cmd, param) =>
            {
                if (!AutoLoad(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not Place, {reason}");
                    return false;
                }

                return true;
            });
            
            OP.Subscribe($"{Module}.E84Pick", (cmd, param) =>
            {
                if (!AutoUnLoad(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not Pick, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.E84Retry", (cmd, param) =>
            {
                if (!E84Retry(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not retry E84 transaction, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.E84Complete", (cmd, param) =>
            {
                if (!E84Complete(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not complete E84 transaction, {reason}");
                    return false;
                }

                return true;
            });

            DATA.Subscribe($"{Module}.E84State", () => _state.ToString());
            //DATA.Subscribe($"{Module}.TransferState", () => TransferState.ToString());
            //DATA.Subscribe($"{Module}.AccessMode", () => Mode.ToString());

            DATA.Subscribe($"{Module}_E84STATE", () => (ushort)_state);
            //DATA.Subscribe($"{Module}_TRANSFER_STATE", () => (ushort)TransferState);
            //DATA.Subscribe($"{Module}_ACCESS_MODE", () => (ushort)Mode);
            DATA.Subscribe($"{Module}.LightCurtain", () => (_diLightCurtain != null? _diLightCurtain.Value:true));
            DATA.Subscribe($"{Module}.Valid", () => _diValid.Value);
            DATA.Subscribe($"{Module}.TransferRequest", () => _diTrReq.Value);
            DATA.Subscribe($"{Module}.Busy", () => _diBusy.Value);
            DATA.Subscribe($"{Module}.TransferComplete", () => _diCompt.Value);
            DATA.Subscribe($"{Module}.CS0", () => _diCS0.Value);
            DATA.Subscribe($"{Module}.CS1", () => _diCS1.Value);
            DATA.Subscribe($"{Module}.CONT", () => _diCont.Value);
            
            DATA.Subscribe($"{Module}.LoadRequest", () => _doLoadReq.Value);
            DATA.Subscribe($"{Module}.UnloadRequest", () => _doUnloadReq.Value);
            DATA.Subscribe($"{Module}.ReadyToTransfer", () => _doReady.Value);
            DATA.Subscribe($"{Module}.HandoffAvailable", () => _doHOAvbl.Value);
            DATA.Subscribe($"{Module}.ES", () => _doES.Value);

            int index = (int)(ModuleName)(Enum.Parse(typeof(ModuleName), Module))-1;

            EV.Subscribe(new EventItem(60061 + 10*index, "Event", AlarmTP1timeout, $"{Module} Occurred TP1 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60062 + 10 * index, "Event", AlarmTP2timeout, $"{Module} Occurred TP2 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60063 + 10 * index, "Event", AlarmTP3timeout, $"{Module} Occurred TP3 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60064 + 10 * index, "Event", AlarmTP4timeout, $"{Module} Occurred TP4 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60065 + 10 * index, "Event", AlarmTP5timeout, $"{Module} Occurred TP5 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            _thread = new PeriodicJob(10, OnTimer, $"{Module}.{Name} MonitorE84Handler", true);
        }
        private string AlarmTP1timeout { get => $"{Module}TP1Timeout"; }
        private string AlarmTP2timeout { get => $"{Module}TP2Timeout"; }
        private string AlarmTP3timeout { get => $"{Module}TP3Timeout"; }
        private string AlarmTP4timeout { get => $"{Module}TP4Timeout"; }
        private string AlarmTP5timeout { get => $"{Module}TP5Timeout"; }

        /// <summary>
        /// This constructor gets IO signals using GetIO
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="display"></param>
        /// <param name="deviceId"></param>
        public E84Passiver(string module, string name, string display, string deviceId)
        {
            Module = module;
            Name = name;
            Display = display;
            DeviceID = deviceId;


            _diLightCurtain = IO.DI[$"DI_{module}_LightCurtain"];
            _signals.Add(new Signal(SignalID.LightCurtain, _diLightCurtain, null));
            //Indicates that the signal transition is active and selected
            //ON: valid; OFF: not valid
            _diValid = IO.DI[$"DI_{module}_VALID"];
            _signals.Add(new Signal(SignalID.VALID, _diValid, null));

            //Carrier stage 0
            //ON: Use the load port for carrier handoff; vice versa
            _diCS0 = IO.DI[$"DI_{module}_CS_0"];
            _signals.Add(new Signal(SignalID.CS_0, _diCS0, null));

            //Carrier stage 1
            //ON: Use the load port for carrier handoff; vice versa
            _diCS1 = IO.DI[$"DI_{module}_CS_1"] ?? IO.DI[$"DI_{module}_CS_0"];
            _signals.Add(new Signal(SignalID.CS_1, _diCS1, null));

            //Transfer Arm Available
            //ON: Handoff is available; OFF: Handoff is unavailable with any error
            _diAmAvbl = IO.DI[$"DI_{module}_AM_AVBL"];
            _signals.Add(new Signal(SignalID.AM_AVBL, _diAmAvbl, null));

            //Transfer Request
            //ON: Request Handoff; vice versa
            _diTrReq = IO.DI[$"DI_{module}_TR_REQ"];
            _signals.Add(new Signal(SignalID.TR_REQ, _diTrReq, null));

            //BUSY for transfer
            //ON: Handoff is in progress; vice versa
            _diBusy = IO.DI[$"DI_{module}_BUSY"];
            _signals.Add(new Signal(SignalID.BUSY, _diBusy, null));
            
            //Complete Transfer
            //ON:The handoff is completed; vice versa
            _diCompt = IO.DI[$"DI_{module}_COMPT"];
            _signals.Add(new Signal(SignalID.COMPT, _diCompt, null));
            
            //Continuous Handoff
            //ON: Continuous Handoff; vice versa
            _diCont = IO.DI[$"DI_{module}_CONT"];
            _signals.Add(new Signal(SignalID.CONT, _diCont, null));

            //Load Request
            //ON: The load port is assigned to load a carrier; vice versa
            //CS_0 && VALID && !CarrierLoaded
            _doLoadReq = IO.DO[$"DI_{module}_L_REQ"];
            _signals.Add(new Signal(SignalID.L_REQ, null, _doLoadReq));

            //Unload Request
            //ON: The load port is assigned to unload a carrier; vice versa
            //CS_0 && VALID && !CarrierUnloaded
            _doUnloadReq = IO.DO[$"DI_{module}_U_REQ"];
            _signals.Add(new Signal(SignalID.U_REQ, null, _doUnloadReq));
            
            //READY for transfer(after accepted the transfer request set ON, turned OFF when COMPT ON)
            //ON: Ready for handoff; vice versa
            _doReady = IO.DO[$"DI_{module}_READY"];
            _signals.Add(new Signal(SignalID.READY, null, _doReady));
            
            //Indicates the passive equipment is not available for the handoff.
            //ON: Handoff is available; OFF: vice versa but with error
            //ON when normal; OFF when : Maintenance mode / Error State 
            _doHOAvbl = IO.DO[$"DI_{module}_HO_AVBL"];
            _signals.Add(new Signal(SignalID.HO_AVBL, null, _doHOAvbl));
            
            //Emergency stop
            _doES = IO.DO[$"DI_{module}_ES"];
            _signals.Add(new Signal(SignalID.ES, null, _doES));

            foreach (var signal in _signals)
            {
                signal.OnChanged += OnSignalChange;
            }

            _scTimeoutTP1 = SC.GetConfigItem("FA.E84.TP1");
            _scTimeoutTP2 = SC.GetConfigItem("FA.E84.TP2");
            _scTimeoutTP3 = SC.GetConfigItem("FA.E84.TP3");
            _scTimeoutTP4 = SC.GetConfigItem("FA.E84.TP4");
            _scTimeoutTP5 = SC.GetConfigItem("FA.E84.TP5");
            _scTimeoutTP6 = SC.GetConfigItem("FA.E84.TP6");
            _scTimeoutTP7 = SC.GetConfigItem("FA.E84.TP7");
            _scTimeoutTP8 = SC.GetConfigItem("FA.E84.TP8");


            OP.Subscribe($"{Module}.E84Place", (cmd, param) =>
            {
                if (!AutoLoad(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not Place, {reason}");
                    return false;
                }

                return true;
            });
            
            OP.Subscribe($"{Module}.E84Pick", (cmd, param) =>
            {
                if (!AutoUnLoad(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not Pick, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.E84Retry", (cmd, param) =>
            {
                if (!E84Retry(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not retry E84 transaction, {reason}");
                    return false;
                }

                return true;
            });
            OP.Subscribe($"{Module}.E84Complete", (cmd, param) =>
            {
                if (!E84Complete(out var reason))
                {
                    EV.PostWarningLog(Module, $"{Name} can not complete E84 transaction, {reason}");
                    return false;
                }

                return true;
            });

            DATA.Subscribe($"{Module}.E84State", () => _state.ToString());
            //DATA.Subscribe($"{Module}.TransferState", () => TransferState.ToString());
            // DATA.Subscribe($"{Module}.AccessMode", () => Mode.ToString());
            DATA.Subscribe($"{Module}.LightCurtain", () => _diLightCurtain.Value);
            DATA.Subscribe($"{Module}.Valid", () => _diValid.Value);
            DATA.Subscribe($"{Module}.TransferRequest", () => _diTrReq.Value);
            DATA.Subscribe($"{Module}.Busy", () => _diBusy.Value);
            DATA.Subscribe($"{Module}.TransferComplete", () => _diCompt.Value);
            DATA.Subscribe($"{Module}.CS0", () => _diCS0.Value);
            DATA.Subscribe($"{Module}.CS1", () => _diCS1.Value);
            DATA.Subscribe($"{Module}.CONT", () => _diCont.Value);
            
            DATA.Subscribe($"{Module}.LoadRequest", () => _doLoadReq.Value);
            DATA.Subscribe($"{Module}.UnloadRequest", () => _doUnloadReq.Value);
            DATA.Subscribe($"{Module}.ReadyToTransfer", () => _doReady.Value);
            DATA.Subscribe($"{Module}.HandoffAvailable", () => _doHOAvbl.Value);
            DATA.Subscribe($"{Module}.ES", () => _doES.Value);

            int index = (int)(ModuleName)(Enum.Parse(typeof(ModuleName), Module)) - 1;
            EV.Subscribe(new EventItem(60061 + 10 * index, "Event", AlarmTP1timeout, $"{Module} Occurred TP1 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60062 + 10 * index, "Event", AlarmTP2timeout, $"{Module} Occurred TP2 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60063 + 10 * index, "Event", AlarmTP3timeout, $"{Module} Occurred TP3 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60064 + 10 * index, "Event", AlarmTP4timeout, $"{Module} Occurred TP4 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            EV.Subscribe(new EventItem(60065 + 10 * index, "Event", AlarmTP5timeout, $"{Module} Occurred TP5 Timeout", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            _thread = new PeriodicJob(10, OnTimer, $"{Module}.{Name} MonitorE84Handler", true);
        }

        

        private PeriodicJob _thread;
        public bool Stop(out string reason)
        {
            reason = String.Empty;
            ResetSignal();
            return false;
        }

        public bool AutoLoad(out string reason)
        {
            //reason = String.Empty;
            //if (Mode != LPAccessMode.AUTO)
            //{
            //    if (!SetAMHS(out reason))
            //    {
            //        return false;
            //    }
            //}
            //if (Provider.ReadyForLoad())
            //{
            //    TransferState = LPTransferState.READY_TO_LOAD;
            //    return true;
            //}
            reason = "Not Ready For Auto Load";
            return false;
        }

        public bool AutoUnLoad(out string reason)
        {
            //reason = String.Empty;
            //if (Mode != LPAccessMode.AUTO)
            //{
            //    if (!SetAMHS(out reason))
            //    {
            //        return false;
            //    }
            //}
            //if (Provider.ReadyForUnload())
            //{
            //    TransferState = LPTransferState.READY_TO_UNLOAD;
            //    return true;
            //}
            reason = "Not Ready For Auto Load";
            return false;
        }

        public bool SetAMHS(out string reason)
        {
            reason = "";
            Provider.SetAccessMode(true);
            return true;
        }

        public bool SetManual(out string reason)
        {            
            reason = "";
            Provider.SetAccessMode(false);
            return true;           
        }

        public bool E84Retry(out string reason)
        {
            reason = "";
            var dvid = new SerializableDictionary<string, object>();
            int portID = 0;
            dvid["PORT_ID"] = int.TryParse(Module.Replace("LP", ""), out portID) ? portID : portID;
            ResetSignal();
            Provider.E84Retry();

           
            return true;
        }
        public bool E84Complete(out string reason)
        {
            reason = "";
            var dvid = new SerializableDictionary<string, object>();
            int portID = 0;
            dvid["PORT_ID"] = int.TryParse(Module.Replace("LP", ""), out portID) ? portID : portID;

            switch (_state)
            {
                case E84State.Error:
                    ResetSignal();
                    break;
                case E84State.LD_TP1_Timeout:
                    if (Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84LoadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup not detected";
                        return false;
                    }
                    
                    break;
                case E84State.LD_TP2_Timeout:
                    if (Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84LoadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup not detected";
                        return false;
                    }
                    break;
                case E84State.LD_TP3_Timeout:
                    if (Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84LoadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup not detected";
                        return false;
                    }
                    break;
                case E84State.LD_TP4_Timeout:
                    if (Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84LoadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup not detected";
                        return false;
                    }
                    break;
                case E84State.LD_TP5_Timeout:
                    ResetSignal();
                    break;
                case E84State.ULD_TP1_Timeout:
                    if(!Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84UnloadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup detected";
                        return false;
                    }

                    break;
                case E84State.ULD_TP2_Timeout:
                    if (!Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84UnloadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup detected";
                        return false;
                    }
                    break;
                case E84State.ULD_TP3_Timeout:
                    if (!Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84UnloadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup detected";
                        return false;
                    }

                    break;
                case E84State.ULD_TP4_Timeout:
                    if (!Provider.IsCarrierPlacement())
                    {
                        EV.Notify(EventE84UnloadTransactionComplete, dvid);
                        ResetSignal();
                    }
                    else
                    {
                        reason = "Foup detected";
                        return false;
                    }

                    break;
                case E84State.ULD_TP5_Timeout:
                    ResetSignal();
                    break;
                default:
                    ResetSignal();
                    
                    break;
            }
            return true;
        }

        public bool Initialize()
        {
            ResetSignal();



            return true;
        }

        private bool InvokeReset(string arg1, object[] arg2)
        {
            //string reason;
            EV.PostInfoLog(Module, $"E84 reset {Module}.{Name}");

            ResetSignal();

            return true;
        }

        public void Terminate()
        {
            ResetSignal();
        }

        private void ResetSignal()
        {
            _doLoadReq.Value = false;
            _doUnloadReq.Value = false;
            _doReady.Value = false;
            _doHOAvbl.Value = false;
            _doES.Value = false;
            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop();
            _state = E84State.Normal;
            foreach (var signal in _signals)
            {
                signal.Reset();
            }
        }

        private LPTransferState preTranState;

        private bool OnTimer()
        {          
            try
            {
                foreach (var signal in _signals)
                {
                    signal.Monitor();
                }
                if (Provider == null) 
                    return true;

                TransferState = Provider.GetTransferState();
                Mode = Provider.GetAccessMode();
                LPReserved = Provider.GetReservedState();

                if (_diLightCurtain != null)
                {
                    DiLightCurtain = _diLightCurtain.Value;
                    if (SC.ContainsItem("Fa.E84.BypassLightCurtain") && SC.GetValue<bool>("Fa.E84.BypassLightCurtain"))
                        DiLightCurtain = true;
                }
                else DiLightCurtain = true;

                if (LightCurtainBroken || TransferState == LPTransferState.OUT_OF_SERVICE || Mode != LPAccessMode.AUTO)
                {
                    _doHOAvbl.Value = false;
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doES.Value = false;
                    _timer_TP1.Stop();
                    _timer_TP2.Stop();
                    _timer_TP3.Stop();
                    _timer_TP4.Stop();
                    _timer_TP5.Stop();
                    _timer_TP6.Stop();
                    return true;
                }

                if (_state != E84State.Normal)
                    return true; 

                _doHOAvbl.Value = true;
                _doES.Value = true;
                var dvid = new SerializableDictionary<string, object>();
                int portID = 0;
                dvid["PORT_ID"] = int.TryParse(Module.Replace("LP", ""), out portID) ? portID : portID;


                if (TransferState == LPTransferState.READY_TO_LOAD)
                {
                    preTranState = TransferState;
                    if (DiCS0 && DiValid && !_doLoadReq.Value)
                    {
                        _doLoadReq.Value = true;                        

                    }
                    if (DiCS0 && DiValid && _doLoadReq.Value && !DiTrReq)
                    {
                        if (_timer_TP1.IsIdle()) _timer_TP1.Start(_scTimeoutTP1.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid && _doLoadReq.Value && DiTrReq && !_doReady.Value)
                    {
                        _timer_TP1.Stop();
                        _doReady.Value = true;

                        if (Provider != null) Provider.E84LoadStart();
                        EV.Notify(EventE84LoadTransactionStart, dvid);
                    }

                }

                if (TransferState == LPTransferState.TRANSFER_BLOCKED && preTranState == LPTransferState.READY_TO_LOAD)  //Load Sequence
                {
                    if (DiCS0 && DiValid && _doLoadReq.Value && DiTrReq && _doReady.Value && !DiBusy)
                    {
                        if (_timer_TP2.IsIdle()) _timer_TP2.Start(_scTimeoutTP2.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid && _doLoadReq.Value && DiTrReq && _doReady.Value && DiBusy)
                    {
                        _timer_TP2.Stop();
                        if (!Provider.IsCarrierPlacement())
                        {
                            if (_timer_TP3.IsIdle()) _timer_TP3.Start(_scTimeoutTP3.IntValue * 1000);

                        }
                        else
                        {
                            _timer_TP3.Stop();
                            _doLoadReq.Value = false;
                        }
                    }
                    if (DiCS0 && DiValid && !_doLoadReq.Value && DiTrReq && _doReady.Value && DiBusy)
                    {
                        if (_timer_TP4.IsIdle()) _timer_TP4.Start(_scTimeoutTP4.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid && !_doLoadReq.Value && _doReady.Value && !DiBusy)
                    {
                        _timer_TP4.Stop();

                    }
                    if (DiCS0 && DiValid && !_doLoadReq.Value && _doReady.Value  && DiCompt)
                    {
                        _doReady.Value = false;
                        _timer_TP4.Stop();

                        if (_timer_TP5.IsIdle()) _timer_TP5.Start(_scTimeoutTP5.IntValue * 1000);

                    }
                    if (!DiCS0 && !_doLoadReq.Value && !_doUnloadReq.Value && !DiTrReq && !_doReady.Value && !DiBusy && !DiCompt)

                    {
                        _timer_TP5.Stop();
                        EV.Notify(EventE84LoadTransactionComplete, dvid);
                        preTranState = LPTransferState.TRANSFER_BLOCKED;
                        if (Provider != null) Provider.E84LoadComplete();
                    }



                }



                if (TransferState == LPTransferState.READY_TO_UNLOAD)
                {
                    preTranState = TransferState;
                    if (DiCS0 && DiValid && !_doUnloadReq.Value)
                    {
                        _doUnloadReq.Value = true;

                        //Provider.LPTSTrans();
                        //EV.Notify(EventE84UnloadTransactionStart, dvid);
                    }
                    if (DiCS0 && DiValid && _doUnloadReq.Value && !DiTrReq)
                    {
                        if (_timer_TP1.IsIdle()) _timer_TP1.Start(_scTimeoutTP1.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid && _doUnloadReq.Value && DiTrReq && !_doReady.Value)
                    {
                        _timer_TP1.Stop();
                        _doReady.Value = true;
                        EV.Notify(EventE84UnloadTransactionStart, dvid);
                        if (Provider != null) Provider.E84UnloadStart();
                    }
                }

                if (TransferState == LPTransferState.TRANSFER_BLOCKED && preTranState == LPTransferState.READY_TO_UNLOAD)  //Unload Sequence
                {

                    if (DiCS0 && DiValid && _doUnloadReq.Value && DiTrReq && _doReady.Value && !DiBusy)
                    {
                        if (_timer_TP2.IsIdle()) _timer_TP2.Start(_scTimeoutTP1.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid && _doUnloadReq.Value && DiTrReq && _doReady.Value && DiBusy)
                    {
                        _timer_TP2.Stop();
                        if (Provider.IsCarrierPlacement())
                        {
                            if (_timer_TP3.IsIdle()) _timer_TP3.Start(_scTimeoutTP3.IntValue * 1000);
                        }
                        else
                        {
                            _timer_TP3.Stop();
                            _doUnloadReq.Value = false;

                        }
                    }
                    if (DiCS0 && DiValid && !_doUnloadReq.Value && DiTrReq && _doReady.Value && DiBusy)
                    {
                        if (_timer_TP4.IsIdle()) _timer_TP4.Start(_scTimeoutTP4.IntValue * 1000);

                    }
                    if (DiCS0 && DiValid &&  DiTrReq && _doReady.Value && !DiBusy)
                    {
                        _timer_TP4.Stop();

                    }
                    if (DiCS0 && DiValid && !_doUnloadReq.Value && _doReady.Value && DiCompt)
                    {
                        _doReady.Value = false;
                        _timer_TP4.Stop();

                        if (_timer_TP5.IsIdle()) _timer_TP5.Start(_scTimeoutTP5.IntValue * 1000);

                    }
                    if (!DiValid && !_doUnloadReq.Value && !DiTrReq && !_doReady.Value && !DiBusy && !DiCompt)
                    {
                        _timer_TP5.Stop();
                        EV.Notify(EventE84UnloadTransactionComplete, dvid);
                        preTranState = LPTransferState.TRANSFER_BLOCKED;
                        if (Provider != null) Provider.E84UnloadComplte();
                    }
                }

                if (_timer_TP1.IsTimeout())
                {
                    if (preTranState == LPTransferState.READY_TO_LOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP1Timeout, dvid);
                        _state = E84State.LD_TP1_Timeout;
                        _timer_TP1.Stop();
                        if (Provider != null) Provider.E84LoadError(_state.ToString());
                        
                    }
                    if (preTranState == LPTransferState.READY_TO_UNLOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;

                        EV.Notify(EventE84TP1Timeout, dvid);
                        _state = E84State.ULD_TP1_Timeout;
                        if (Provider != null) Provider.E84UnloadError(_state.ToString());
                       
                    }
                   
                    _timer_TP1.Stop();
                    EV.Notify(AlarmTP1timeout);



                }

                if (_timer_TP2.IsTimeout())
                {
                    if (preTranState == LPTransferState.READY_TO_LOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP2Timeout, dvid);
                        _state = E84State.LD_TP2_Timeout;
                        if (Provider != null) Provider.E84LoadError(_state.ToString());
                    }
                    if (preTranState == LPTransferState.READY_TO_UNLOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP2Timeout, dvid);
                        _state = E84State.ULD_TP2_Timeout;
                        if (Provider != null) Provider.E84UnloadError(_state.ToString());

                    }
                    _timer_TP2.Stop();
                    EV.Notify(AlarmTP2timeout);
                }

                if (_timer_TP3.IsTimeout())
                {
                    if (preTranState == LPTransferState.READY_TO_LOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP3Timeout, dvid);
                        _state = E84State.LD_TP3_Timeout;
                        if (Provider != null) Provider.E84LoadError(_state.ToString());
                    }
                    if (preTranState == LPTransferState.READY_TO_UNLOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP3Timeout, dvid);
                        _state = E84State.ULD_TP3_Timeout;
                        if (Provider != null) Provider.E84UnloadError(_state.ToString());
                    }
                    _timer_TP3.Stop();
                    EV.Notify(AlarmTP3timeout);
                }

                if (_timer_TP4.IsTimeout())
                {
                    if (preTranState == LPTransferState.READY_TO_LOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP4Timeout, dvid);
                        _state = E84State.LD_TP4_Timeout;
                        if (Provider != null) Provider.E84LoadError(_state.ToString());
                    }
                    if (preTranState == LPTransferState.READY_TO_UNLOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP4Timeout, dvid);
                        _state = E84State.ULD_TP4_Timeout;
                        if (Provider != null) Provider.E84UnloadError(_state.ToString());
                    }
                    _timer_TP4.Stop();
                    EV.Notify(AlarmTP4timeout);

                }
                if (_timer_TP5.IsTimeout())
                {
                    if (preTranState == LPTransferState.READY_TO_LOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP5Timeout, dvid);
                        _state = E84State.LD_TP5_Timeout;
                        if (Provider != null) Provider.E84LoadError(_state.ToString());

                    }
                    if (preTranState == LPTransferState.READY_TO_UNLOAD)
                    {
                        _doLoadReq.Value = false;
                        _doUnloadReq.Value = false;
                        _doReady.Value = false;
                        _doHOAvbl.Value = false;
                        EV.Notify(EventE84TP5Timeout, dvid);
                        _state = E84State.ULD_TP5_Timeout;
                        if (Provider != null) Provider.E84UnloadError(_state.ToString());
                    }
                    _timer_TP5.Stop();
                    EV.Notify(AlarmTP5timeout);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return true;

        }

        public void Reset()
        {
            //_doLoadReq.Value = false;
            //_doUnloadReq.Value = false;
            //_doReady.Value = false;
            //_doHOAvbl.Value = false;
            //_doES.Value = false;
            //_state = E84State.Normal;
            //foreach (var signal in _signals)
            //{
            //    signal.Reset();
            //}                

        }



        private void autoRecovery()
        {
            var dvid = new SerializableDictionary<string, object>();
            int portID = 0;
            dvid["PORT_ID"] = int.TryParse(Module.Replace("LP", ""), out portID) ? portID : portID;
            
            switch (_state)
            {
                case E84State.Error:
                    break;
                case E84State.LD_TP1_Timeout:
                    if(Provider.IsCarrierPlacement()) EV.Notify(EventE84LoadTransactionComplete, dvid);
                    else EV.Notify(EventE84LoadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.LD_TP2_Timeout:
                    if (Provider.IsCarrierPlacement()) EV.Notify(EventE84LoadTransactionComplete, dvid);
                    else EV.Notify(EventE84LoadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.LD_TP3_Timeout:
                    if (Provider.IsCarrierPlacement()) EV.Notify(EventE84LoadTransactionComplete, dvid);
                    else EV.Notify(EventE84LoadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.LD_TP4_Timeout:
                    if (Provider.IsCarrierPlacement()) EV.Notify(EventE84LoadTransactionComplete, dvid);
                    else EV.Notify(EventE84LoadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.LD_TP5_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84LoadTransactionComplete, dvid);
                    else EV.Notify(EventE84LoadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.ULD_TP1_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84UnloadTransactionComplete, dvid);
                    else EV.Notify(EventE84UnloadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.ULD_TP2_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84UnloadTransactionComplete, dvid);
                    else EV.Notify(EventE84UnloadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.ULD_TP3_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84UnloadTransactionComplete, dvid);
                    else EV.Notify(EventE84UnloadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.ULD_TP4_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84UnloadTransactionComplete, dvid);
                    else EV.Notify(EventE84UnloadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                case E84State.ULD_TP5_Timeout:
                    if (!Provider.IsCarrierPlacement()) EV.Notify(EventE84UnloadTransactionComplete, dvid);
                    else EV.Notify(EventE84UnloadTransactionRestart, dvid);
                    ResetSignal();
                    break;
                default:break;
            }
            ResetSignal();

        }

        private void OnSignalChange(SignalID signal, bool value)
        {

        }

        public void Monitor()
        {
            
        }
    }
}
