using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB
{
    public class TDKB200LoadPort : LoadPortBaseDevice, IConnection
    {
        public TDKB200LoadPort(string module, string name, string scRoot, IoTrigger[] dos = null, IoSensor[] dis = null, RobotBaseDevice robot = null, bool IsTCPconnection = false, IE84CallBack e84 =null) : base(module, name, robot,e84)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "TDK200LoadPort";
            if (dos != null && dos.Length >= 1)
            {
                _doLoadPortOK = dos[0];

            }
            if (dis != null && dis.Length >= 1)
            {
                _diInfoPadA = dis[0];
                _diInfoPadA.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 2)
            {
                _diInfoPadB = dis[1];
                _diInfoPadB.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 3)
            {
                _diInfoPadC = dis[2];
                _diInfoPadC.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 4)
            {
                _diInfoPadD = dis[3];
                _diInfoPadB.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            InitializeLP();
            SubscribeLPData();
            SubscribeLPAlarm();
        }

        
        private void SubscribeLPData()
        {
            DATA.Subscribe($"{Module}.{Name}.SystemStatus", () => SystemStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.Mode", () => Mode.ToString());
            DATA.Subscribe($"{Module}.{Name}.InitPosMovement", () => InitPosMovement.ToString());
            DATA.Subscribe($"{Module}.{Name}.OperationStatus", () => OperationStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode.ToString());
            DATA.Subscribe($"{Module}.{Name}.ContainerStatus", () => ContainerStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ClampPosition", () => ClampPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.VacuumStatus", () => VacuumStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorState", () => LPDoorState.ToString());
            DATA.Subscribe($"{Module}.{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.MapperPostion", () => MapperPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingStatus", () => MappingStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.Model", () => Model.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());
            DATA.Subscribe($"{Module}.{Name}.DockPosition", () => DockPosition.ToString());
        }

        private void SubscribeLPAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", AlarmTdkZLMIT, $"Load Port {Name} Z-axis position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkYLMIT, $"Load Port {Name} Y-axis position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkPROTS, $"Load Port {Name} Wafer protrusion", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDLMIT, $"Load Port {Name} Door forward/backward position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPBAR, $"Load Port {Name} Mapper arm position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            
            
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPSTP, $"Load Port {Name} Mapper stopper position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPEDL, $"Load Port {Name} Mapping end position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkCLOPS, $"Load Port {Name} FOUP clamp open error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkCLCLS, $"Load Port {Name} FOUP clamp close error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDROPS, $"Load Port {Name} Latch key open error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRCLS, $"Load Port {Name} Latch key close error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkVACCS, $"Load Port {Name} Vacuum on error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkVACOS, $"Load Port {Name} Vacuum off error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkAIRSN, $"Load Port {Name} Main air error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTOP, $"Load Port {Name} Normal position error at FOUP open", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTCL, $"Load Port {Name} Normal position error at FOUP close", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTMP, $"Load Port {Name} Mapper storage error when Z-axis lowered", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTPI, $"Load Port {Name} Parallel signal error from upper machine", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkSAFTY, $"Load Port {Name} Interlock relay failure", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkFANST, $"Load Port {Name} Fan operation error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPDOG, $"Load Port {Name} Mapping mechanical(Adjustment) error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRDKE, $"Load Port {Name} Door detection error during dock.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRSWE, $"Load Port {Name} Door detection error except dock", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkNoOperation, $"Load Port {Name} No action when foup is present", EventLevel.Alarm, EventType.EventUI_Notify));


        }

        private string AlarmTdkZLMIT { get => LPModuleName.ToString() + "ZLMIT"; }
        private string AlarmTdkYLMIT { get => LPModuleName.ToString() + "YLMIT"; }
        private string AlarmTdkPROTS { get => LPModuleName.ToString() + "PROTS"; }
        private string AlarmTdkDLMIT { get => LPModuleName.ToString() + "DLMIT"; }
        private string AlarmTdkMPBAR { get => LPModuleName.ToString() + "MPBAR"; }
        private string AlarmTdkMPSTP { get => LPModuleName.ToString() + "MPSTP"; }
        private string AlarmTdkMPEDL { get => LPModuleName.ToString() + "MPEDL"; }
        private string AlarmTdkCLOPS { get => LPModuleName.ToString() + "CLOPS"; }
        private string AlarmTdkCLCLS { get => LPModuleName.ToString() + "CLCLS"; }
        private string AlarmTdkDROPS { get => LPModuleName.ToString() + "DROPS"; }
        private string AlarmTdkDRCLS { get => LPModuleName.ToString() + "DRCLS"; }
        private string AlarmTdkVACCS { get => LPModuleName.ToString() + "VACCS"; }
        private string AlarmTdkVACOS { get => LPModuleName.ToString() + "VACOS"; }
        private string AlarmTdkAIRSN { get => LPModuleName.ToString() + "AIRSN"; }
        private string AlarmTdkINTOP { get => LPModuleName.ToString() + "INTOP"; }
        private string AlarmTdkINTCL { get => LPModuleName.ToString() + "INTCL"; }
        private string AlarmTdkINTMP { get => LPModuleName.ToString() + "INTMP"; }
        private string AlarmTdkINTPI { get => LPModuleName.ToString() + "INTPI"; }
        private string AlarmTdkSAFTY { get => LPModuleName.ToString() + "SAFTY"; }
        private string AlarmTdkFANST { get => LPModuleName.ToString() + "FANST"; }
        private string AlarmTdkMPDOG { get => LPModuleName.ToString() + "MPDOG"; }
        private string AlarmTdkDRDKE { get => LPModuleName.ToString() + "DRDKE"; }
        private string AlarmTdkDRSWE { get => LPModuleName.ToString() + "DRSWE"; }
        private string AlarmTdkNoOperation { get => LPModuleName.ToString() + "NoOperation"; }






        private void _diInfoPad_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            //if (_infoPadType == 1)
            //    InfoPadCarrierIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
            //        (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
            //        (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
            //        (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
        }

        public override int InfoPadCarrierIndex 
        {
            get { return base.InfoPadCarrierIndex; }
            set 
            {
                if (base.InfoPadCarrierIndex != value)
                {
                    base.InfoPadCarrierIndex = value;
                    EV.PostInfoLog("LoadPort", $"{LPModuleName} infopad index change to {value}");
                    //if (CIDReaders != null && CIDReaders.Length > 1)
                    //{
                    //    int cidindex = SC.GetValue<int>($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}");
                    //    if (CIDReaders.Length <= cidindex)
                    //    {
                    //        EV.PostAlarmLog("System", $"The carrier info configuration for CIDReaderIndex{cidindex} is invalid.");
                    //    }
                    //    else
                    //    {
                    //        CarrierIDReaderCallBack = CIDReaders[cidindex];
                    //    }
                    //}
                }
            }
        }

        public override bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            if(SC.ContainsItem($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}") && 
                SC.GetValue<bool>($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}"))
            {
                
                return true;
            }
            return base.IsEnableDualTransfer(out reason);
        }

        public override CIDReaderBaseDevice[] CIDReaders
        {
            get { return base.CIDReaders; }
            set
            {
                base.CIDReaders = value;
                //if (CIDReaders != null && CIDReaders.Length > 1)
                //{
                //    int cidindex = SC.GetValue<int>($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}");
                //    if (CIDReaders.Length <= cidindex)
                //    {
                //        EV.PostAlarmLog("System", $"The carrier info configuration for CIDReaderIndex{cidindex} is invalid.");
                //    }
                //    else
                //    {
                //        CarrierIDReaderCallBack = CIDReaders[cidindex];
                //    }

                //}
            }
        }

        private void InitializeLP()
        {
            IsMapWaferByLoadPort = false;
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);


            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
             //InfoPadType,0=TDK,1=Ext,2=FixedbySC
            if (IsAutoDetectCarrierType)
            {
                _infoPadType = SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;

                if (_infoPadType == 1)
                {
                    InfoPadCarrierIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                        (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                        (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                        (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
                }
                if (_infoPadType == 2)
                {
                    InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                }

            }



            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new TDKB200LoadPortTCPConnection(this, Address);

                _tcpConnection.EnableLog(_enableLog);
                if (_tcpConnection.Connect())
                {
                    //LOG.Write($"Connected with {Module}.{Name} .");
                    EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                }
                else
                {
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                }
            }
            else
            {
                string portName = SC.GetStringValue($"LoadPort.{Name}.PortName");
                int bautRate = SC.GetValue<int>($"LoadPort.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"LoadPort.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.StopBits"), out StopBits stopBits);
                Address = portName;
                _connection = new TDKB200LoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
                int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
                int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
                if (sleep <= 0 || sleep > 10)
                    sleep = 2;

                int retry = 0;
                do
                {
                    _connection.Disconnect();
                    Thread.Sleep(sleep * 1000);
                    if (_connection.Connect())
                    {
                        //LOG.Write($"Connected with {Module}.{Name} .");
                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                        break;
                    }
                    if (count > 0 && retry++ > count)
                    {
                        EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                        break;
                    }
                } while (true);
            }
            
            ConnectionManager.Instance.Subscribe($"{Name}", this);

            _thread = new PeriodicJob(50, OnTimer, $"{Module}.{Name} MonitorHandler", true);




        }

        private bool OnTimer()
        {
            try
            {
                MonitorFoupState();


                if (_isTcpConnection)
                {
                    _tcpConnection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _tcpConnection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_tcpConnection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }
                else
                {
                    _connection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _connection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }





                if (_isTcpConnection)
                {
                    _tcpConnection.MonitorTimeout();
                    if (!_tcpConnection.IsConnected || _tcpConnection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_tcpConnection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {

                            Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                            _tcpConnection = new TDKB200LoadPortTCPConnection(this, Address);
                            _tcpConnection.EnableLog(_enableLog);
                            if (!_tcpConnection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_tcpConnection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }
                    _trigActionDone.CLK = (_lstHandler.Count == 0 && !_tcpConnection.IsBusy);
                    if (_trigActionDone.Q)
                        OnActionDone(null);

                    HandlerBase handler = null;
                    if (!_tcpConnection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _tcpConnection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }


                    }

                }
                else
                {

                    _connection.MonitorTimeout();

                    if (!_connection.IsConnected || _connection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_connection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                            if (!_connection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }
                    _trigActionDone.CLK = (_lstHandler.Count == 0 && !_connection.IsBusy);
                    if (_trigActionDone.Q)
                        OnActionDone(null);

                    HandlerBase handler = null;
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _connection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }
                    }
                }
                if (IsAutoDetectCarrierType)
                {
                    _infoPadType = SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;

                    if (_infoPadType == 2)
                    {
                        InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                    }
                    if (_infoPadType == 1)
                    {
                        InfoPadCarrierIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                        (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                        (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                        (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
                    }
                }
                else
                {
                    InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                }                
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;

        }
        public override void Monitor()
        {
            base.Monitor();
            
            try
            {
               

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        private void MonitorFoupState()
        {
            if (IsPlacement)
            {
                int currentfoupstatecode = (ClampPosition == TDKPosition.Close ? 0 : 1) +
                    (DockPosition == TDKDockPosition.Dock ? 0 : 2) +
                    (DoorState == FoupDoorState.Close ? 0 : 4) +
                    (DoorPosition == FoupDoorPostionEnum.Down ? 0 : 8);

                if(currentfoupstatecode!= _foupstatecode)
                {
                    _dtFoupStateStart = DateTime.Now;
                    _foupstatecode = currentfoupstatecode;
                }

                int timeoutNoOperation = SC.ContainsItem($"{_scRoot}.{Name}.TimeLimitNoOperation") ?
                    SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitNoOperation") : 3600000;

                if(DateTime.Now - _dtFoupStateStart > TimeSpan.FromSeconds(timeoutNoOperation))
                {
                    EV.Notify(AlarmTdkNoOperation);
                    _dtFoupStateStart = DateTime.Now;
                }
            }
        }

        private DateTime _dtFoupStateStart = DateTime.Now;
        private int _foupstatecode = -1;

        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private TDKB200LoadPortConnection _connection;
        private TDKB200LoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA;
        private IoSensor _diInfoPadB;
        private IoSensor _diInfoPadC;
        private IoSensor _diInfoPadD;

        private int _infoPadType=0;
        public int InfoPadType => _infoPadType;

        public TDKB200LoadPortConnection Connection
        {
            get => _connection;
        }
        public TDKB200LoadPortTCPConnection TCPConnection => _tcpConnection;

        private PeriodicJob _thread;
        private static Object _locker = new Object();
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private bool _enableLog = true;
        //private bool _commErr = false;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        public TDKSystemStatus SystemStatus { get; set; }
        public TDKMode Mode { get; set; }
        public TDKInitPosMovement InitPosMovement { get; set; }
        public TDKOperationStatus OperationStatus { get; set; }
        public TDKContainerStatus ContainerStatus { get; set; }
        public TDKPosition ClampPosition { get; set; }
        public TDKPosition LPDoorLatchPosition { get; set; }
        public TDKVacummStatus VacuumStatus { get; set; }
        public TDKPosition LPDoorState { get; set; }
        public TDKWaferProtrusion WaferProtrusion { get; set; }
        public TDKElevatorAxisPosition ElevatorAxisPosition { get; set; }
        public TDKDockPosition DockPosition { get; set; }
        public TDKMapPosition MapperPostion { get; set; }
        public TDKMappingStatus MappingStatus { get; set; }

        public TDKModel Model { get; set; }
        public string Address { get; set; }

        public bool IsConnected => _connection.IsConnected;
        public override bool IsEnableTransferWafer(out string reason)
        {
            if(LPDoorState != TDKPosition.Open)
            {
                reason = "Door is not open";
                return false;
            }
            if(DockPosition != TDKDockPosition.Dock)
            {
                reason = "Foup is not dock";
                return false;
            }
            return base.IsEnableTransferWafer(out reason);
        }
        public bool Disconnect()
        {
            if (_isTcpConnection)
                return _tcpConnection.Disconnect();
            return _connection.Disconnect();
        }
        public void OnCarrierNotPlaced()
        {
            _isPlaced = false;
            ConfirmRemoveCarrier();
        }


        public void OnCarrierNotPresent()
        {
            _isPresent = false;
            //ConfirmRemoveCarrier();
        }

        public void OnCarrierPlaced()
        {
            _isPlaced = true;
            ConfirmAddCarrier();

        }


        public void OnCarrierPresent()
        {
            _isPresent = true;
            //ConfirmAddCarrier();
        }

        public void OnSwitchKey1()
        {
            _isAccessSwPressed = true;
        }

        public void OnSwitchKey2()
        {

        }

        public void OffSwitchKey1()
        {
            _isAccessSwPressed = false;
        }

        public void OffSwitchKey2()
        {

        }
        public bool OnEvent(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new TDKB200GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
            }
            return true;
        }
        private LoadportCassetteState _cassetteState = LoadportCassetteState.None;
        public override LoadportCassetteState CassetteState
        {
            get { return _cassetteState; }
            set
            {
                _cassetteState = value;
            }
        }
        public void SetCassetteState(LoadportCassetteState state)
        {
            _cassetteState = state;

            if (state == LoadportCassetteState.Normal)
            {
                OnCarrierPresent();
                OnCarrierPlaced();

            }
            if (state == LoadportCassetteState.Absent)
            {
                OnCarrierNotPlaced();
                OnCarrierNotPresent();
            }
        }
        public override WaferSize GetCurrentWaferSize()
        {
            int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            switch(intwz)
            {
                case 0:
                    return WaferSize.WS0;
                case 1:
                    return WaferSize.WS0;
                case 2:
                    return WaferSize.WS2;
                case 3:
                    return WaferSize.WS3;
                case 4:
                    return WaferSize.WS4;
                case 5:
                    return WaferSize.WS5;
                case 6:
                    return WaferSize.WS6;
                case 7:
                case 8:
                    return WaferSize.WS8;
                case 12:
                    return WaferSize.WS12;
                default:
                    return WaferSize.WS0;
            }
        }

        public override string SpecCarrierType
        {
            get
            {
                if(SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }

        protected override bool fStartExecute(object[] param)
        {
            try
            {

                switch (param[0].ToString())
                {
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        if (light != Indicator.LOAD && light != Indicator.UNLOAD)
                        {
                            IsBusy = false;
                            return true;
                        }
                            

                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] { "", "LON", "LBL", "LOF" };
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200SetHandler(this, statestr[(int)state]+ $"{(int)light:D2}",null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                            IsBusy = false;
                        }
                        break;
                        
                    case "QueryIndicator":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "YWAIT", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "YDOOR", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "DORFW", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "PODOP", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "PODCL", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "ZDRUP", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                            
                        }
                        break;


                    case "OpenDoorNoMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "OpenDoorAndMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "MapWafer":
                        lock (_locker)
                        {
                            if (!IsMapWaferByLoadPort)
                            {
                                if (MapRobot != null)
                                    return MapRobot.WaferMapping(LPModuleName, out _);
                                return false;
                            }
                            if (DockPosition != TDKDockPosition.Dock)
                                _lstHandler.AddLast(new TDKB200MoveHandler(this, "YDOOR", null));
                            if (DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new TDKB200MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));                            
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "MAPRD", null));
                        }
                        break;
                    case "DoorUpAndClose":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "ZDRUP", null));
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "DORFW", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));                            
                        }
                        
                        break;
                    case "OpenDoorAndDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));                            
                        }
                        break;
                    case "Move":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200MoveHandler(this, param[1].ToString(), null));
                            _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Set":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200SetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Get":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKB200GetHandler(this, param[1].ToString(), null));
                        }
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }

        protected override bool fStartUnload(object[] param)
        {
            if (!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't unload.");
                return false;
            }
            if (!_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is not docked on {Name},can't unload.");
                return false;
            }
            
                lock (_locker)
                {

                    _lstHandler.AddLast(new TDKB200MoveHandler(this, "CULOD", null));
                    _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                }                
            
            return true;

        }

        protected override bool fStartLoad(object[] param)
        {
            if (!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't load.");
                return false;
            }
            if (_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is docked on {Name},can't load.");
                return false;
            }
           
            lock (_locker)
            {
                _lstHandler.AddLast(new TDKB200MoveHandler(this, "CLOAD", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
            }
                   
            
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                if (param.Length >= 1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new TDKB200MoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new TDKB200MoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
            }
            return true;
        }
        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        protected override bool fStartReset(object[] param)
        {
            _lstHandler.Clear();
            if (_isTcpConnection)
            {
                if (!_tcpConnection.IsConnected)
                    _tcpConnection.Connect();
                _tcpConnection.ForceClear();
            }
            else
            {
                if (!_connection.IsConnected)
                    _connection.Connect();
                _connection.ForceClear();
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new TDKB200SetHandler(this, "RESET", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
            }
            return true;
        }
        public override void OnError(string error = "")
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_isTcpConnection)
                    _tcpConnection.ForceClear();
                else
                    _connection.ForceClear();
                _lstHandler.AddLast(new TDKB200GetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKB200GetHandler(this, "LEDST", null));
            }

            base.OnError(error);
        }

        public void OnAbs(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r","");
                EV.Notify($"{Name}{absContext}");

            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }
            OnError("Recieve ABS Message:" + absMsg);



        }
        public override void Terminate()
        {
            _thread.Stop();
            Thread.Sleep(100);
            if (!SC.ContainsItem($"{_scRoot}.{Name}.CloseConnectionOnShutDown") || SC.GetValue<bool>($"{_scRoot}.{Name}.CloseConnectionOnShutDown"))
            {
                LOG.Write($"Close {Address} for connection of {LPModuleName}");
                _connection.Disconnect();
                _connection.TerminateCom();
                
            }
            base.Terminate();
        }
        public override bool IsForbidAccessSlotAboveWafer()
        {
            if (SC.ContainsItem($"CarrierInfo.ForbidAccessAboveWaferCarrier{InfoPadCarrierIndex}"))
                return SC.GetValue<bool>($"CarrierInfo.ForbidAccessAboveWaferCarrier{InfoPadCarrierIndex}");
            return false;
        }



    }
   
}
