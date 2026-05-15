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
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;

namespace Aitex.Core.RT.Device.Unit
{
    public enum E84SignalType
    {
        Acitvie,
        Passive,
    }
    public enum E84SignalID
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
        VA,        
        READY,
        VS_0,
        VS_1,
        HO_AVBL,
        ES,




    }


    public class E84Signal
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

        private E84SignalID _id;
        private RD_TRIG _trig = new RD_TRIG();
        private DIAccessor _di = null;
        private DOAccessor _do = null;

        public event Action<E84SignalID, bool> OnChanged;
        public E84Signal(E84SignalID id, DIAccessor diAccessor, DOAccessor doAccessor)
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

            if (_trig.T)
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
    public enum LightCurtainHandleEnum
    {
        Bypass=0,
        Normal,
        Reverse,
    }
    public class IOE84Passive : BaseDevice, IDevice, IE84CallBack
    {
        public event Action<string> OnE84HandOffStart;
        public event Action<string> OnE84HandOffComplete;
        public event Action<E84Timeout, string> OnE84HandOffTimeout;
        public event Action<E84SignalID, bool> OnE84ActiveSignalChange;
        public event Action<E84SignalID, bool> OnE84PassiveSignalChange;


        private List<E84Signal> _signals = new List<E84Signal>();

     
        public LightCurtainHandleEnum _lightCurtainHandler { get; set; }

        public void SetLightCurtainHandle(LightCurtainHandleEnum handle)
        {
            _lightCurtainHandler = handle;
        }

        public IOE84Passive(string module, string name, string display, string deviceId, bool IsfoupPresent,bool IsTransferReady,
            DIAccessor[] activesignals, DOAccessor[] passivesignals)
        {
            Module = module;
            Name = name;
            //_moduleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
            Display = display;
            DeviceID = deviceId;

            _diValid = activesignals[0];
            _diCS0 = activesignals[1];            
            _diTrReq = activesignals[2];
            _diBusy = activesignals[3];
            _diCompt = activesignals[4];
            _diCS1 = activesignals.Length >5? activesignals[5]:null;
            _diCont = activesignals.Length > 6 ? activesignals[6] : null;
            _diAmAvbl = activesignals.Length > 7 ? activesignals[7] : null;

            _doLoadReq = passivesignals[0];
            _doUnloadReq = passivesignals[1];
            _doReady = passivesignals[2];
            _doHOAvbl = passivesignals[3];
            _doES = passivesignals[4];

            _isFoupON = IsfoupPresent;
            _isTransferReady = IsTransferReady;

            SubscribeData();

            
        }

        private void SubscribeData()
        {
            DATA.Subscribe($"{Module}.{Name}.E84State", () => _state.ToString());
            DATA.Subscribe($"{Module}.{Name}.AlwaysOn", () => _diAlwaysOn==null? true:_diAlwaysOn.Value);
            DATA.Subscribe($"{Module}.{Name}.Valid", () => _diValid.Value);
            DATA.Subscribe($"{Module}.{Name}.TransferRequest", () => _diTrReq.Value);
            DATA.Subscribe($"{Module}.{Name}.Busy", () => _diBusy.Value);
            DATA.Subscribe($"{Module}.{Name}.TransferComplete", () => _diCompt.Value);
            DATA.Subscribe($"{Module}.{Name}.CS0", () => _diCS0.Value);
            DATA.Subscribe($"{Module}.{Name}.CS1", () => _diCS1 == null ? false : _diCS1.Value);
            DATA.Subscribe($"{Module}.{Name}.CONT", () => _diCont == null ? false : _diCont.Value);

            DATA.Subscribe($"{Module}.{Name}.LoadRequest", () => _doLoadReq.Value);
            DATA.Subscribe($"{Module}.{Name}.UnloadRequest", () => _doUnloadReq.Value);
            DATA.Subscribe($"{Module}.{Name}.ReadyToTransfer", () => _doReady.Value);
            DATA.Subscribe($"{Module}.{Name}.HandoffAvailable", () => _doHOAvbl.Value);
            DATA.Subscribe($"{Module}.{Name}.ES", () => _doES.Value);

            DATA.Subscribe($"{Module}.E84State", () => _state.ToString());

            DATA.Subscribe($"{Module}.Valid", () => _diValid.Value);
            DATA.Subscribe($"{Module}.TransferRequest", () => _diTrReq.Value);
            DATA.Subscribe($"{Module}.Busy", () => _diBusy.Value);
            DATA.Subscribe($"{Module}.TransferComplete", () => _diCompt.Value);
            DATA.Subscribe($"{Module}.CS0", () => _diCS0.Value);
            DATA.Subscribe($"{Module}.CS1", () => _diCS1 == null ? false : _diCS1.Value);
            DATA.Subscribe($"{Module}.CONT", () => _diCont == null ? false : _diCont.Value);

            DATA.Subscribe($"{Module}.LoadRequest", () => _doLoadReq.Value);
            DATA.Subscribe($"{Module}.UnloadRequest", () => _doUnloadReq.Value);
            DATA.Subscribe($"{Module}.ReadyToTransfer", () => _doReady.Value);
            DATA.Subscribe($"{Module}.HandoffAvailable", () => _doHOAvbl.Value);
            DATA.Subscribe($"{Module}.ES", () => _doES.Value);
            DATA.Subscribe($"{Module}.{Name}.E84Mode", () => IsAutoMode ? "Auto" : "Manual");

            OP.Subscribe(String.Format("{0}.{1}", Module, "SetE84Auto"), (out string reason, int time, object[] param) =>
            {

                SetHoAutoControl(true);
                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Module, "SetE84Manual"), (out string reason, int time, object[] param) =>
            {

                SetHoAutoControl(false);
                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Module, "TransReq"), (out string reason, int time, object[] param) =>
            {
                if (param[0].ToString() == "Load" && _isFoupON)
                {
                    reason = "Can't load, foup is on";
                    EV.PostAlarmLog(Module, "Can start E84 load, foup is On");
                    return false;
                }
                if (param[0].ToString() == "Unload" && !_isFoupON)
                {
                    reason = "Can't unload, foup is off";
                    EV.PostAlarmLog(Module, "Can start E84 unload, foup is Off");
                    return false;
                }
                SetHoAvailable(true);
                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Module, "TransStop"), (out string reason, int time, object[] param) =>
            {

                Stop();
                reason = "";
                return true;
            });




            _signals.Add(new E84Signal(E84SignalID.LightCurtain, _diLightCurtain, null));
            _signals.Add(new E84Signal(E84SignalID.VALID, _diValid, null));
            _signals.Add(new E84Signal(E84SignalID.CS_0, _diCS0, null));
            _signals.Add(new E84Signal(E84SignalID.CS_1, _diCS1, null));
            _signals.Add(new E84Signal(E84SignalID.AM_AVBL, _diAmAvbl, null));
            _signals.Add(new E84Signal(E84SignalID.TR_REQ, _diTrReq, null));
            _signals.Add(new E84Signal(E84SignalID.BUSY, _diBusy, null));
            _signals.Add(new E84Signal(E84SignalID.CONT, _diCont, null));
            _signals.Add(new E84Signal(E84SignalID.L_REQ, null, _doLoadReq));
            _signals.Add(new E84Signal(E84SignalID.U_REQ, null, _doUnloadReq));
            _signals.Add(new E84Signal(E84SignalID.READY, null, _doReady));
            _signals.Add(new E84Signal(E84SignalID.HO_AVBL, null, _doHOAvbl));
            _signals.Add(new E84Signal(E84SignalID.ES, null, _doES));




            foreach (var signal in _signals)
            {
                signal.OnChanged += OnSignalChange;
            }

            
        }

        public IOE84Passive(string module, XmlElement node, string ioModule = "")
        {
            base.Module = node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");
            ioModule = node.GetAttribute("ioModule");

            _diAlwaysOn = ParseDiNode("ALWAYS_ON", node, ioModule);
            _diLightCurtain = ParseDiNode("LightCurtain", node, ioModule);
            
            //Indicates that the signal transition is active and selected
            //ON: valid; OFF: not valid
            _diValid = ParseDiNode("VALID", node, ioModule);
            
            //Carrier stage 0
            //ON: Use the load port for carrier handoff; vice versa
            _diCS0 = ParseDiNode("CS_0", node, ioModule);
            
            //Carrier stage 1
            //ON: Use the load port for carrier handoff; vice versa
            _diCS1 = ParseDiNode("CS_1", node, ioModule);
            
            //Transfer Arm Available
            //ON: Handoff is available; OFF: Handoff is unavailable with any error
            _diAmAvbl = ParseDiNode("AM_AVBL", node, ioModule);
            
            //Transfer Request
            //ON: Request Handoff; vice versa
            _diTrReq = ParseDiNode("TR_REQ", node, ioModule);
            

            //BUSY for transfer
            //ON: Handoff is in progress; vice versa
            _diBusy = ParseDiNode("BUSY", node, ioModule);
            

            //Complete Transfer
            //ON:The handoff is completed; vice versa
            _diCompt = ParseDiNode("COMPT", node, ioModule);
            _signals.Add(new E84Signal(E84SignalID.COMPT, _diCompt, null));

            //Continuous Handoff
            //ON: Continuous Handoff; vice versa
            _diCont = ParseDiNode("CONT", node, ioModule);
            

            //Load Request
            //ON: The load port is assigned to load a carrier; vice versa
            //CS_0 && VALID && !CarrierLoaded
            _doLoadReq = ParseDoNode("L_REQ", node, ioModule);
            
            //Unload Request
            //ON: The load port is assigned to unload a carrier; vice versa
            //CS_0 && VALID && !CarrierUnloaded
            _doUnloadReq = ParseDoNode("U_REQ", node, ioModule);
            
            //READY for transfer(after accepted the transfer request set ON, turned OFF when COMPT ON)
            //ON: Ready for handoff; vice versa
            _doReady = ParseDoNode("READY", node, ioModule);
            
            //Indicates the passive equipment is not available for the handoff.
            //ON: Handoff is available; OFF: vice versa but with error
            //ON when normal; OFF when : Maintenance mode / Error State 
            _doHOAvbl = ParseDoNode("HO_AVBL", node, ioModule);
            
            //Emergency stop
            _doES = ParseDoNode("ES", node, ioModule);

            SubscribeData();
        }

        private void OnSignalChange(E84SignalID arg1, bool arg2)
        {
            if((int)arg1 >=0 && (int)arg1<=8 && OnE84ActiveSignalChange!=null)
            {
                OnE84ActiveSignalChange(arg1, arg2);
            }
            if((int)arg1 > 8 && OnE84PassiveSignalChange!=null)
                OnE84PassiveSignalChange(arg1, arg2);

        }

        public enum E84State
        {
            Complete,
            Idle,
            Busy,            
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
            Stop,
        }

        public enum E84Timeout
        {
            TP1,
            TP2,
            TP3,
            TP4,
            TP5,
        }
        public enum E84ActiveSignal
        {
            Valid,
            CS0,
            CS1,
            TrReq,
            Busy,
            Compt,
            Cont,
            AmAvble,
        }
        public enum E84PassiveSignal
        {
            LoadReq,
            UnloadReq,
            Ready,
            HOAvbl,
            ES,
        }
        private DIAccessor _diAlwaysOn;
        public DIAccessor DiAlwaysOn => _diAlwaysOn;

        private DIAccessor _diValid;
        private DIAccessor _diCS0;
        private DIAccessor _diCS1;
        private DIAccessor _diTrReq;
        private DIAccessor _diBusy;
        private DIAccessor _diCompt;
        private DIAccessor _diCont;
        private DIAccessor _diAmAvbl;

        private DIAccessor _diLightCurtain;

        private DOAccessor _doLoadReq;
        private DOAccessor _doUnloadReq;
        private DOAccessor _doReady;
        private DOAccessor _doHOAvbl;
        private DOAccessor _doES;

        private bool _isFoupON;
        private bool _isTransferReady;
        //Active equipment signal

        private DeviceTimer _timer = new DeviceTimer();
        private DeviceTimer _timer_TP1 = new DeviceTimer();
        private DeviceTimer _timer_TP2 = new DeviceTimer();
        private DeviceTimer _timer_TP3 = new DeviceTimer();
        private DeviceTimer _timer_TP4 = new DeviceTimer();
        private DeviceTimer _timer_TP5 = new DeviceTimer();
        private DeviceTimer _timer_TP6 = new DeviceTimer();


        //private ModuleName _moduleName;
        //timeout
        private int _tp1
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP1"))
                    return SC.GetValue<int>("FA.E84.TP1");
                return 2;
            }
        }
      
        private int _tp2
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP2"))
                    return SC.GetValue<int>("FA.E84.TP2");
                return 2;
            }
        }
    
        private int _tp3
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP3"))
                    return SC.GetValue<int>("FA.E84.TP3");
                return 60;
            }
        }
      
        private int _tp4
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP4"))
                    return SC.GetValue<int>("FA.E84.TP4");
                return 60;
            }
        }
      
        private int _tp5
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP5"))
                    return SC.GetValue<int>("FA.E84.TP5");
                return 2;
            }
        }
      
        private int _tp6
        {
            get
            {
                if (SC.ContainsItem("FA.E84.TP6"))
                    return SC.GetValue<int>("FA.E84.TP6");
                return 2;
            }
        }


        private bool _isAutoMode = false;
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set { _isAutoMode = value; }
        }

        private E84State _state;
        public E84State State { get => _state; }

        private LPTransferState _currentTState = LPTransferState.No_state;

        #region E84Interface
        public void SetHoAutoControl(bool value)
        {
             IsAutoMode = value; 
        }

        public void SetHoAvailable(bool value)
        {
            if (value)
            {
                _state = E84State.Idle;
            }
            else
                _state = E84State.Complete;
        }

        public bool GetE84SignalState(E84SignalID signal)
        {
            switch(signal)
            {
                case E84SignalID.AM_AVBL:
                    return _diAmAvbl.Value;
                case E84SignalID.BUSY:
                    return _diBusy.Value;
                case E84SignalID.COMPT:
                    return _diCompt.Value;
                case E84SignalID.CONT:
                    return _diCont.Value;
                case E84SignalID.CS_0:
                    return _diCS0.Value;
                case E84SignalID.CS_1:
                    return _diCS1==null? false:_diCS1.Value;
                case E84SignalID.ES:
                    return _doES.Value;
                case E84SignalID.HO_AVBL:
                    return _doHOAvbl.Value;
                case E84SignalID.LightCurtain:
                    return (_diLightCurtain == null) ? false: _diLightCurtain.Value;
                case E84SignalID.L_REQ:
                    return _doLoadReq.Value;
                case E84SignalID.READY:
                    return _doReady.Value;
                case E84SignalID.TR_REQ:
                    return _diTrReq.Value;
                case E84SignalID.U_REQ:
                    return _doUnloadReq.Value;
                case E84SignalID.VALID:
                    return _diValid.Value;
                default:
                    return false;
            }
            
        }
        public void SetFoupStatus(bool isfoupon)
        {
            _isFoupON = isfoupon; ;
        }

        public void SetReadyTransferStatus(bool ready)
        {
            _isTransferReady = ready;
        }
        public void SetE84SignalState(E84PassiveSignal signal, bool value)
        {
            switch(signal)
            {
                case E84PassiveSignal.ES:
                    _doES.SetValue(value,out _);
                    break;
                case E84PassiveSignal.HOAvbl:
                    _doHOAvbl.SetValue(value, out _);
                    break;
                case E84PassiveSignal.LoadReq:
                    _doLoadReq.SetValue(value, out _);
                    break;
                case E84PassiveSignal.Ready:
                    _doReady.SetValue(value, out _);
                    break;
                case E84PassiveSignal.UnloadReq:
                    _doUnloadReq.SetValue(value, out _);
                    break;
                default:
                    break;
            }
        }
        #endregion
        public void Stop()
        {
            _doLoadReq.SetValue(false, out _);
            _doUnloadReq.SetValue(false, out _);
            _doReady.SetValue(false, out _);
            _doHOAvbl.SetValue(false, out _);
            _doES.SetValue(false, out _);
            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop();
            _state = E84State.Stop;
        }
        public void Complete()
        {
            _doLoadReq.SetValue(false, out _);
            _doUnloadReq.SetValue(false, out _);
            _doReady.SetValue(false, out _);
            _doHOAvbl.SetValue(true, out _);
            _doES.SetValue(true, out _);
            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop();
            _state = E84State.Complete;
        }


        public bool Initialize()
        {
            //reset();            
            return true;
        }

        public void Terminate()
        {
            _doLoadReq.SetValue(false, out _);
            _doUnloadReq.SetValue(false, out _);
            _doReady.SetValue(false, out _);
            _doHOAvbl.SetValue(true, out _);
            _doES.SetValue(true, out _);
            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop(); 
        }

        private void reset()
        {

            _doLoadReq.SetValue(false, out _);
            _doUnloadReq.SetValue(false, out _);
            _doReady.SetValue(false, out _);
            _doHOAvbl.SetValue(false, out _);
            _doES.SetValue(false, out _);
            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop();
            _state = E84State.Idle;

        }
        

        public void Monitor()
        {
            try
            {
                foreach (var signal in _signals)
                {
                    signal.Monitor();
                }
                if (IsAutoMode)
                {
                    if (State == E84State.Complete)
                        _state = E84State.Idle;
                }

                if(State == E84State.LD_TP1_Timeout || State == E84State.LD_TP2_Timeout || State == E84State.LD_TP5_Timeout
                    ||State == E84State.ULD_TP1_Timeout || State == E84State.ULD_TP2_Timeout || State == E84State.ULD_TP5_Timeout)
                {
                    if (!_diCS0.Value && !_diValid.Value)
                        _state = E84State.Idle;
                }
                switch(_lightCurtainHandler)
                {
                    case LightCurtainHandleEnum.Normal:
                        if (_diLightCurtain == null) break;
                        if (_diLightCurtain.Value) break;
                        _doES.SetValue(false, out _);
                        _doHOAvbl.SetValue(false, out _);

                        return;

                    case LightCurtainHandleEnum.Reverse:
                        if (_diLightCurtain == null) break;
                        if (!_diLightCurtain.Value) break;
                        _doES.SetValue(false, out _);
                        _doHOAvbl.SetValue(false, out _);
                        return;


                }




                if (State == E84State.Idle)
                {
                    if (_isFoupON && _isTransferReady) _currentTState = LPTransferState.READY_TO_UNLOAD;
                    if (!_isFoupON && _isTransferReady) _currentTState = LPTransferState.READY_TO_LOAD;
                }
                if(State == E84State.Stop)
                {
                    _doES.SetValue(false, out _);
                    return;
                }
                if (State == E84State.Complete || !_isTransferReady)
                {
                    _doES.SetValue(true, out _);
                    _doHOAvbl.SetValue(true, out _);
                    return;
                }

                if (_currentTState != LPTransferState.READY_TO_LOAD &&
                    _currentTState != LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.SetValue(false, out _);
                    _doUnloadReq.SetValue(false, out _);
                    _doReady.SetValue(false, out _);
                    _doHOAvbl.SetValue(true, out _);
                    _doES.SetValue(true, out _);

                    _timer_TP1.Stop();
                    _timer_TP2.Stop();
                    _timer_TP3.Stop();
                    _timer_TP4.Stop();
                    _timer_TP5.Stop();
                    _timer_TP6.Stop();
                    
                    return;
                }

                bool divalid = _diValid.Value;
                bool dics0 = _diCS0.Value;
                bool diTrReq = _diTrReq.Value;
                bool diBusy = _diBusy.Value;
                bool dicompt = _diCompt.Value;

                bool doloadreq = _doLoadReq.Value;
                bool dounloadreq = _doUnloadReq.Value;
                bool doready = _doReady.Value;
                bool doHOavbl = _doHOAvbl.Value;
                bool doES = _doES.Value;

                if (_state != E84State.Idle && _state != E84State.Busy) return;
                _doES.SetValue(true,out _);

                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    if (!_isFoupON)
                    {
                        _doHOAvbl.SetValue(true, out _);
                        if (_state == E84State.Idle)
                        {
                            _doLoadReq.SetValue(false, out _);
                            _doUnloadReq.SetValue(false, out _);
                            _doReady.SetValue(false, out _);
                            _doHOAvbl.SetValue(true, out _);
                            _doES.SetValue(true, out _);
                            if (dics0 && divalid && !doloadreq)
                            {
                                EV.PostInfoLog("E84", $"{Name} E84 set load request.");
                                _doLoadReq.SetValue(true, out _);
                                _state = E84State.Busy;
                                if (OnE84HandOffStart != null)
                                    OnE84HandOffStart("Load");
                            }
                        }
                        if (_state == E84State.Busy)
                        {
                            if (dics0 && divalid && doloadreq && !diTrReq)
                            {
                                if (_timer_TP1.IsIdle())
                                    _timer_TP1.Start(_tp1 * 1000);

                            }
                            if (dics0 && divalid && doloadreq && diTrReq && !doready)
                            {
                                EV.PostInfoLog("E84", $"{Name} E84 set ready.");
                                _timer_TP1.Stop();
                                _doReady.SetValue(true, out _);
                                if (_timer_TP2.IsIdle())
                                    _timer_TP2.Start(_tp2 * 1000);
                            }
                            if (dics0 && divalid && doloadreq && diTrReq && doready && diBusy)
                            {
                                _timer_TP2.Stop();
                                if (_timer_TP3.IsIdle())
                                    _timer_TP3.Start(_tp3 * 1000);
                            }
                        }
                    }

                    if (_isFoupON && _state == E84State.Busy)
                    {
                        _doLoadReq.SetValue(false,out _);
                        _timer_TP2.Stop();
                        _timer_TP3.Stop();

                        if (dics0 && divalid && !doloadreq && diTrReq && doready && diBusy)
                        {
                            if (_timer_TP4.IsIdle()) 
                                _timer_TP4.Start(_tp4 * 1000);

                        }
                        if (dics0 && divalid && !doloadreq && doready && !diBusy)
                        {
                            _timer_TP4.Stop();
                        }
                        if (dics0 && divalid && !doloadreq && doready && dicompt)
                        {
                            EV.PostInfoLog("E84", $"{Name} E84 set ready off.");
                            _doReady.SetValue( false,out _);
                            _timer_TP4.Stop();
                            if (_timer_TP5.IsIdle()) 
                                _timer_TP5.Start(_tp5 * 1000);

                        }
                        if (!dics0 && !doloadreq && !dounloadreq && !diTrReq && !doready && !diBusy && !dicompt)
                        {
                            EV.PostInfoLog("E84", $"{Name} E84 load transaction completed.");
                            _timer_TP5.Stop();
                            _doHOAvbl.SetValue(true,out _);
                            _state = E84State.Complete;
                            _currentTState = LPTransferState.No_state;
                            OnE84HandOffComplete?.Invoke("Load");

                        }
                    }
                }

                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    if (_isFoupON)
                    {
                        if (_state == E84State.Idle)
                        {
                            _doLoadReq.SetValue(false, out _);
                            _doUnloadReq.SetValue(false, out _);
                            _doReady.SetValue(false, out _);
                            _doHOAvbl.SetValue(true, out _);
                            _doES.SetValue(true, out _);
                        }
                        _doHOAvbl.SetValue(true,out _);
                        if (dics0 && divalid && !dounloadreq)
                        {
                            EV.PostInfoLog("E84", $"{Name} E84 set unload request.");
                            _doUnloadReq.SetValue(true,out _);
                            _state = E84State.Busy;
                            if (OnE84HandOffStart != null)
                                OnE84HandOffStart("Unload");
                        }
                        if (dics0 && divalid && dounloadreq && !diTrReq)
                        {
                            if (_timer_TP1.IsIdle()) 
                                _timer_TP1.Start(_tp1 * 1000);

                        }
                        if (dics0 && divalid && dounloadreq && diTrReq && !doready)
                        {
                            _timer_TP1.Stop();
                            EV.PostInfoLog("E84", $"{Name} E84 set ready.");
                            _doReady.SetValue(true,out _);
                            if (_timer_TP2.IsIdle())
                                _timer_TP2.Start(_tp2 * 1000);
                        }
                        if (dics0 && divalid && dounloadreq && diTrReq && doready && diBusy)
                        {
                            _timer_TP2.Stop();
                            if (_timer_TP3.IsIdle()) 
                                _timer_TP3.Start(_tp3 * 1000);
                        }
                    }
                    if ((!_isFoupON) && _state == E84State.Busy)
                    {
                        _doUnloadReq.SetValue(false,out _);

                        if (dics0 && divalid && diTrReq && doready && diBusy)
                        {
                            EV.PostInfoLog("E84", $"{Name} E84 set unload request off.");

                            _timer_TP2.Stop();
                            _timer_TP3.Stop();
                            if (_timer_TP4.IsIdle()) 
                                _timer_TP4.Start(_tp4 * 1000);
                        }

                        if (dics0 && divalid && !dounloadreq && doready && !diBusy)
                        {
                            _timer_TP4.Stop();
                        }
                        if (dics0 && divalid && !dounloadreq && doready && dicompt)
                        {
                            EV.PostInfoLog("E84", $"{Name} E84 set ready off.");
                            _doReady.SetValue(false,out _);
                            _timer_TP4.Stop();
                            if (_timer_TP5.IsIdle()) 
                                _timer_TP5.Start(_tp5 * 1000);
                        }
                        if (!divalid && !dounloadreq && !diTrReq && !doready && !diBusy && !dicompt)
                        {
                            _timer_TP5.Stop();
                            EV.PostInfoLog("E84", $"{Name} E84 unload transaction completed.");
                            _state = E84State.Complete;
                            _currentTState = LPTransferState.No_state;
                            OnE84HandOffComplete?.Invoke("Unload");
                        }
                    }

                }

                MonitorTimeout();



            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
        private void MonitorTimeout()
        {
            if (_timer_TP1.IsTimeout())
            {
                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;                    
                    _state = E84State.LD_TP1_Timeout;
                    
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout(E84Timeout.TP1, "Load");


                }
                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;                   
                    _state = E84State.ULD_TP1_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout(E84Timeout.TP1, "Unload");
                }
                _timer_TP1.Stop();

            }

            if (_timer_TP2.IsTimeout())
            {
                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.LD_TP2_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout(E84Timeout.TP2, "Load");
                }
                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.ULD_TP2_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP2, "Unload");
                }
                _timer_TP2.Stop();
               
            }

            if (_timer_TP3.IsTimeout())
            {
                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.LD_TP3_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP3, "Load");
                }
                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.ULD_TP3_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP3, "Unload");
                }
                _timer_TP3.Stop();
               
            }

            if (_timer_TP4.IsTimeout())
            {
                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                   
                    _state = E84State.LD_TP4_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP4, "Load");
                }
                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.ULD_TP4_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP4, "UUnload");
                }
                _timer_TP4.Stop();

            }
            if (_timer_TP5.IsTimeout())
            {
                if (_currentTState == LPTransferState.READY_TO_LOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.LD_TP5_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP5, "Load");

                }
                if (_currentTState == LPTransferState.READY_TO_UNLOAD)
                {
                    _doLoadReq.Value = false;
                    _doUnloadReq.Value = false;
                    _doReady.Value = false;
                    _doHOAvbl.Value = false;
                    
                    _state = E84State.ULD_TP5_Timeout;
                    if (OnE84HandOffTimeout != null)
                        OnE84HandOffTimeout( E84Timeout.TP5, "Unload");
                }
                _timer_TP5.Stop();
            }
        }

        public void Reset()
        {
            //if (_state == E84State.Busy) return;
            //if (_state == E84State.Idle) return;
            
            //_timer_TP1.Stop();
            //_timer_TP2.Stop();
            //_timer_TP3.Stop();
            //_timer_TP4.Stop();
            //_timer_TP5.Stop();
            //_doLoadReq.SetValue(false,out _);
            //_doUnloadReq.SetValue(false, out _);
            //_doReady.SetValue(false, out _);
            //_doHOAvbl.SetValue(true, out _);
            //_doES.SetValue(true, out _);
            
            //_state = E84State.Idle;
        }

        public void ResetE84()
        {
            if (_state == E84State.Busy) return;
            if (_state == E84State.Idle) return;

            _timer_TP1.Stop();
            _timer_TP2.Stop();
            _timer_TP3.Stop();
            _timer_TP4.Stop();
            _timer_TP5.Stop();
            _doLoadReq.SetValue(false, out _);
            _doUnloadReq.SetValue(false, out _);
            _doReady.SetValue(false, out _);
            _doHOAvbl.SetValue(true, out _);
            _doES.SetValue(true, out _);

            _state = E84State.Idle;
        }

        public E84State GetCurrentE84State()
        {
            return _state;
        }
    }
}
