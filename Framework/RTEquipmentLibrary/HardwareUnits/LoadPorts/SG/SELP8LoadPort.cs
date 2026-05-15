using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.SG
{
    public class SELP8LoadPort : LoadPortBaseDevice, IConnection
    {
        public SELP8LoadPort(string module, string name, string scRoot, IoTrigger[] dos = null, IoSensor[] dis = null, RobotBaseDevice robot = null, bool IsTCPconnection = false, IE84CallBack e84 =null) : base(module, name, robot,e84)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "SELP8LoadPort";
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
            //SubscribeLPAlarm();
        }

        protected Stopwatch _timerActionMonitor = new Stopwatch();
        private void SubscribeLPData()
        {
            if (Module != "")
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
            else
            {
                DATA.Subscribe($"{Name}.SystemStatus", () => SystemStatus.ToString());
                DATA.Subscribe($"{Name}.Mode", () => Mode.ToString());
                DATA.Subscribe($"{Name}.InitPosMovement", () => InitPosMovement.ToString());
                DATA.Subscribe($"{Name}.OperationStatus", () => OperationStatus.ToString());
                DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode.ToString());
                DATA.Subscribe($"{Name}.ContainerStatus", () => ContainerStatus.ToString());
                DATA.Subscribe($"{Name}.ClampPosition", () => ClampPosition.ToString());
                DATA.Subscribe($"{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
                DATA.Subscribe($"{Name}.VacuumStatus", () => VacuumStatus.ToString());
                DATA.Subscribe($"{Name}.LPDoorState", () => LPDoorState.ToString());
                DATA.Subscribe($"{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
                DATA.Subscribe($"{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
                DATA.Subscribe($"{Name}.MapperPostion", () => MapperPostion.ToString());
                DATA.Subscribe($"{Name}.MappingStatus", () => MappingStatus.ToString());
                DATA.Subscribe($"{Name}.Model", () => Model.ToString());
                DATA.Subscribe($"{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());
                DATA.Subscribe($"{Name}.DockPosition", () => DockPosition.ToString());
            }
        }

        public void OnInputSignaChange(string evtcontent)
        {
            string data = evtcontent.Split('/').LastOrDefault().Replace("\r", "").Replace(";", "");

            int value11 = Convert.ToInt32(data.Substring(0, 1), 16);
            int value7 = Convert.ToInt32(data.Substring(4, 1), 16);

            InfoPadSensorIndex = ((value11 & 0x1) == 0x1 ? 8 : 0) + ((value11 & 0x2) == 0x2 ? 4 : 0) +
                ((value7 & 0x2) == 0x2 ? 2 : 0) + ((value7 & 0x4) == 0x4 ? 1 : 0);
           
            
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
                   
                }
            }
        }

        public override bool IsEnableDualTransfer(out string reason)
        {
            return base.IsEnableDualTransfer(out reason);
        }

        public override CIDReaderBaseDevice[] CIDReaders
        {
            get { return base.CIDReaders; }
            set
            {
                base.CIDReaders = value;

                //EV.PostInfoLog("LoadPort", $"{LPModuleName} infopad index change to {value}");


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

        public bool IsHandlerBusy
        {
            get
            {
                return _lstHandler.Count != 0 || _connection.IsBusy;
            }
        }

        public LinkedList<Tuple<int, int>> WaferPositionData { get; set; } = new LinkedList<Tuple<int, int>>();
        private void InitializeLP()
        {
            IsMapWaferByLoadPort = true;
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);


            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
             //InfoPadType,0=TDK,1=Ext,2=FixedbySC
           


            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new SELP8LoadPortTCPConnection(this, Address);

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
                _connection = new SELP8LoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
                _connection.IsEnableHandlerRetry = true;
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
        private HandlerBase _currentHandler;
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
                            _tcpConnection = new SELP8LoadPortTCPConnection(this, Address);
                            _tcpConnection.EnableLog(_enableLog);
                            if (!_tcpConnection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_tcpConnection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }
                    //_trigActionDone.CLK = (_lstHandler.Count == 0 && !_tcpConnection.IsBusy);
                    //if (_trigActionDone.Q)
                    //    OnActionDone(null);

                    //HandlerBase handler = null;
                    if (!_tcpConnection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                _currentHandler = _lstHandler.First.Value;
                                if (_currentHandler != null) _tcpConnection.Execute(_currentHandler);
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
                    //_trigActionDone.CLK = (_lstHandler.Count == 0 && !_connection.IsBusy);
                    //if (_trigActionDone.Q)
                    //    OnActionDone(null);

                    //HandlerBase handler = null;
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                _currentHandler = _lstHandler.First.Value;
                                if (_currentHandler != null) _connection.Execute(_currentHandler);
                                _lstHandler.RemoveFirst();                                
                            }
                        }
                    }
                }
                if (_infoPadType == 1) //External
                {
                    InfoPadSensorIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                    (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                    (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                    (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
                }
                if (IsAutoDetectCarrierType)
                {
                    if (_infoPadType == 2)  //Fixed by SC
                    {
                        InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                    }
                    if (_infoPadType == 0 || _infoPadType == 1)
                    {
                        InfoPadCarrierIndex = InfoPadSensorIndex;
                    }
                }
                else //Fixed by SC
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

        public void LPSetInfoPadSensorIndex(int index)
        {
            if (_infoPadType == 0)
            {
                InfoPadSensorIndex = index;
            }
        }
        protected int _retryTimes;
        public bool RetryHandler()
        {
            if (_currentHandler == null) return false;
            if (_retryTimes > 6) return false;
            Thread.Sleep(500);
            lock(_locker)
            {
                _lstHandler.AddFirst(_currentHandler);
                //if(_isTcpConnection)
                //{
                //    _tcpConnection.Execute(_currentHandler);
                //}
                //else
                //{
                //    _connection.Execute(_currentHandler);
                //}
            }
            _retryTimes++;
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

              

                if(DateTime.Now - _dtFoupStateStart > TimeSpan.FromSeconds(TimeLimitNoOperation) && TimeLimitNoOperation>0)
                {
                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"carrier:{_carrierId} on {LPModuleName}  no operation time over limit";
                    EV.Notify(AlarmTdkNoOperation,dvid);
                    _dtFoupStateStart = DateTime.Now;
                }
            }
            else
            {
                _dtFoupStateStart = DateTime.Now;
            }
        }

        private DateTime _dtFoupStateStart = DateTime.Now;
        private int _foupstatecode = -1;

        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private SELP8LoadPortConnection _connection;
        private SELP8LoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA;
        private IoSensor _diInfoPadB;
        private IoSensor _diInfoPadC;
        private IoSensor _diInfoPadD;

        private int _infoPadType
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ?
                    SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;
            }
        }
        public int InfoPadType => _infoPadType;

        public SELP8LoadPortConnection Connection
        {
            get => _connection;
        }
        public SELP8LoadPortTCPConnection TCPConnection => _tcpConnection;

        private PeriodicJob _thread;
        protected static Object _locker = new Object();
        protected LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
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

        public bool IsConnected
        {
            get
            {
                if (_isTcpConnection)
                    return TCPConnection.IsConnected;

                return _connection.IsConnected;
            }
        }
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
               
                //_lstHandler.AddLast(new SELP8GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
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
            if(state == LoadportCassetteState.Absent)
            {
                OnCarrierNotPlaced();
                OnCarrierNotPresent();
            }



        }
        public override bool IsRequestFOSBMode
        {
            get
            {
                //if (SC.ContainsItem($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}"))
                //    return SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}") == 1;

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode");

                return false;
            }

            set => base.IsRequestFOSBMode = value;

        }

        public override bool IsMapWaferByLoadPort
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.MappedByRobot"))
                    return !SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.MappedByRobot");

                return !IsRequestFOSBMode;
            }
        }


        public override WaferSize GetCurrentWaferSize()
        {
            return base.GetCurrentWaferSize();
            //int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            //switch(intwz)
            //{
            //    case 0:
            //        return WaferSize.WS0;
            //    case 1:
            //        return WaferSize.WS0;
            //    case 2:
            //        return WaferSize.WS2;
            //    case 3:
            //        return WaferSize.WS3;
            //    case 4:
            //        return WaferSize.WS4;
            //    case 5:
            //        return WaferSize.WS5;
            //    case 6:
            //        return WaferSize.WS6;
            //    case 7:
            //    case 8:
            //        return WaferSize.WS8;
            //    case 12:
            //        return WaferSize.WS12;
            //    default:
            //        return WaferSize.WS0;
            //}
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
            _retryTimes = 0;
            try
            {
                EV.PostInfoLog("LoadPort", $"{LPModuleName} start to execute command {param[0]}");
                switch (param[0].ToString())
                {
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] { "", "LON", "LBL", "LOF" };
                        int lightNo = (int)light;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8SetHandler(this, statestr[(int)state]+ $"{lightNo:D2}",null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                            IsBusy = false;
                        }
                        break;
                        
                    case "QueryIndicator":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "YWAIT", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "YDOOR", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "DORFW", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "PODOP", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "PODCL", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRUP", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                            
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
                                _lstHandler.AddLast(new SELP8MoveHandler(this, "YDOOR", null));
                            if (DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new SELP8MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));                            
                            _lstHandler.AddLast(new SELP8GetHandler(this, "MAPRD", null));
                        }
                        break;
                    case "DoorUpAndClose":
                        lock (_locker)
                        {
                            WaferPositionData.Clear();
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "CUMDK", null));

                            //_lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRUP", null));
                            //_lstHandler.AddLast(new SELP8MoveHandler(this, "DORFW", null));

                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));                            
                        }
                        
                        break;
                    case "OpenDoorAndDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));                            
                        }
                        break;
                    case "Move":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8MoveHandler(this, param[1].ToString(), null));
                            _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        }
                        break;
                    case "Set":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8SetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Get":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new SELP8GetHandler(this, param[1].ToString(), null));
                        }
                        break;
                }
                _timerActionMonitor.Restart();
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
            WaferPositionData.Clear();
            _retryTimes = 0;
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
                if (DoorPosition == FoupDoorPostionEnum.Up && DoorState == FoupDoorState.Close)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "CUDNC", null));
                    }
                }
                else
                {     
                    if(IsNeedMapOnUnload)
                    {
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "CUDMP", null));

                    }                        
                    else
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "CULOD", null));

                }
                if (IsKeepClampAfterUnload)
                {
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "PODCL", null));
                }



                if (IsNeedMapOnUnload)
                {
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+01"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+02"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+03"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+04"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+05"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+06"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+07"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+08"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+09"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+10"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+11"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+12"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+13"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+14"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+15"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+16"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+17"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+18"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+19"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+20"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+21"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+22"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+23"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+24"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+25"));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "MAPRD", null));
                }               

            }

            _timerActionMonitor.Restart();
            return true;



        }

        protected override bool fStartLoad(object[] param)
        {
            WaferPositionData.Clear();
            _retryTimes = 0;
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
            if (IsRequestFOSBMode)
            {
                lock (_locker)
                {
                    if (ClampPosition == TDKPosition.Open)
                    {
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "PODCL", null));
                    }
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "DORBK", null));
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "YDOOR", null));
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRDW", null));
                    _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                }
            }
            else
            {
                
                lock (_locker)
                {
                    if (IsMapWaferByLoadPort)
                    {
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "CLDMP", null));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+01"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+02"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+03"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+04"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+05"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+06"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+07"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+08"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+09"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+10"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+11"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+12"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+13"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+14"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+15"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+16"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+17"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+18"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+19"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+20"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+21"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+22"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+23"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+24"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MPENC", "/+25"));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "MAPRD", null));
                    }
                    else
                    {
                        _lstHandler.AddLast(new SELP8MoveHandler(this, "CLOAD", null));
                        _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                    }
                   
                }
            }

            _timerActionMonitor.Restart();

            return true;
        }


        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError("LoadTimeout");
            }

            if (!IsHandlerBusy)
            {
                OnLoaded();
                _timerActionMonitor.Stop();
                return true;
            }
            return false;
        }

        protected override bool fStartInit(object[] param)
        {
            _retryTimes = 0;
            lock (_locker)
            {
                _lstHandler.AddLast(new SELP8EvtHandler(this, "EVTON", null));
                if (param.Length >= 1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new SELP8MoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new SELP8GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
                

            }
            _timerActionMonitor.Restart();
            return true;
        }

        protected override bool fMonitorInit(object[] param)
        {
            
            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitHome))
            {
                _timerActionMonitor.Stop();
                OnError("HomeTimeout");
            }



            if (!IsHandlerBusy)
            {
                OnHomed();
                return true;
            }
            return false;

        }
        protected override bool fMonitorHome(object[] param)
        {

            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitHome))
            {
                _timerActionMonitor.Stop();
                OnError("HomeTimeout");
            }

            if (!IsHandlerBusy)
            {
                OnHomed();
                return true;
            }
            return false;

        }
        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError("UnloadTimeout");
            }
            if (!IsHandlerBusy)
            {
                OnUnloaded();
                return true;
            }
            return false;

        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (!IsHandlerBusy)
            {
               
                return true;
            }
            return false;
        }

        protected override bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            if (_timerActionMonitor.IsRunning && _timerActionMonitor.Elapsed > TimeSpan.FromSeconds(TimelimitAction))
            {
                _timerActionMonitor.Stop();
                OnError($"Execute{CurrentParamter[0]}Timeout");
            }
            if (!IsHandlerBusy)
            {
                EV.PostInfoLog("LoadPort", $"{LPModuleName} success to execute command {CurrentParamter[0]}");
                return true;
            }
            return false;
        }
        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        protected override bool fStartReset(object[] param)
        {
            _retryTimes = 0;
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
                _lstHandler.AddLast(new SELP8SetHandler(this, "RESET", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new SELP8GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
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
                _lstHandler.AddLast(new SELP8GetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new SELP8GetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new SELP8GetHandler(this, "LEDST", null));
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



        public void OnNak(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r", "");
                EV.Notify($"{Name}{absContext}");

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            OnError("Recieve Nak Message:" + absMsg);



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

        public override void OnSlotMapRead(string _slotMap)
        {
            CurrentSlotMapResult = _slotMap;
            //if (!IsMapWaferByLoadPort)
            //    OnActionDone(new object[] { });

            if (CurrentSlotMapResult.Length != ValidSlotsNumber)
            {
                EV.PostAlarmLog("LoadPort", $"{LPModuleName} Mapping Data Error," +
                    $"slotmap length is {CurrentSlotMapResult.Length} not match the valid slots number:{ValidSlotsNumber}.");
            }

            for (int i = 0; i < CurrentSlotMapResult.Length; i++)
            {
                // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W"
                WaferInfo wafer = null;
                switch (CurrentSlotMapResult[i])
                {
                    case '0':
                        WaferManager.Instance.DeleteWafer(LPModuleName, i);
                        CarrierManager.Instance.UnregisterCarrierWafer(Name, i);
                        break;
                    case '1':
                        if (WaferManager.Instance.CheckNoWafer(LPModuleName, i))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Normal);
                        }
                        else
                            WaferManager.Instance.GetWafer(LPModuleName, i).Status = WaferStatus.Normal;
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        break;
                    case '2':
                        if (WaferManager.Instance.CheckNoWafer(LPModuleName, i))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Crossed);
                        }
                        else
                            WaferManager.Instance.GetWafer(LPModuleName, i).Status = WaferStatus.Crossed;
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //EV.Notify(AlarmLoadPortMapCrossedWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map crossed wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    case 'W':
                        if (WaferManager.Instance.CheckNoWafer(LPModuleName, i))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Double);
                        }
                        else
                            WaferManager.Instance.GetWafer(LPModuleName, i).Status = WaferStatus.Double;
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        //EV.Notify(AlarmLoadPortMapDoubleWafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map double wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    case '?':
                        if (WaferManager.Instance.CheckNoWafer(LPModuleName, i))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        }
                        else
                            WaferManager.Instance.GetWafer(LPModuleName, i).Status = WaferStatus.Unknown;
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map unknown wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;
                    default:
                        CurrentSlotMapResult = CurrentSlotMapResult.Substring(0, i) + "?" + CurrentSlotMapResult.Substring(i + 1);
                        if (WaferManager.Instance.CheckNoWafer(LPModuleName, i))
                        {
                            wafer = WaferManager.Instance.CreateWafer(LPModuleName, i, WaferStatus.Unknown);
                        }
                        else
                            WaferManager.Instance.GetWafer(LPModuleName, i).Status = WaferStatus.Unknown;
                        WaferManager.Instance.UpdateWaferSize(LPModuleName, i, GetCurrentWaferSize());
                        CarrierManager.Instance.RegisterCarrierWafer(Name, i, wafer);
                        EV.PostInfoLog("LoadPort", $"{LPModuleName} map unknown wafer on carrier:{_carrierId},slot:{i + 1}.");
                        break;



                }
            }
            if (CurrentSlotMapResult.Replace("1", "").Replace("0", "") == "")
            {
                SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();

                dvid["SlotMap"] = CurrentSlotMapResult;
                dvid["PortID"] = PortID;
                dvid["PORT_CTGRY"] = SpecPortName;
                dvid["CarrierType"] = SpecCarrierType;
                dvid["CarrierIndex"] = InfoPadCarrierIndex;
                dvid["InfoPadSensorIndex"] = InfoPadSensorIndex;
                dvid["CarrierID"] = CarrierId;

                EV.Notify(EventSlotMapAvailable, dvid);
                EV.Notify(EventMapComplete, dvid);
            }

            if (CurrentState == LoadPortStateEnum.Unloading)
            {
                if (CurrentSlotMapResult.Contains("2"))
                {
                    MapError = true;
                    string slotSpec = "";
                    for(int i=0;i<CurrentSlotMapResult.Length;i++)
                    {
                        if (CurrentSlotMapResult[i] == '2')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }

                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} unload map crossed wafer on slot:{slotSpec} of carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortUnloadMapCrossedWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:crossed wafer when unload carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortUnloadMappingError,dvid1);
                    //OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("3"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == '3')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }


                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} unload map double wafer on slot:{slotSpec} of carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortUnloadMapDoubleWafer,dvid);

                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when unload carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortUnloadMappingError,dvid1);
                    //OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("W"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == 'W')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }


                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} unload map double wafer on slot:{slotSpec} of carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortUnloadMapDoubleWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when unload carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortUnloadMappingError, dvid1);
                    //OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("?"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == '?')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }


                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} unload map unknow wafer on slot:{slotSpec} of carrier:{_carrierId}";


                    EV.Notify(AlarmLoadPortUnloadMapUnknownWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:unknow wafer when unload carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortUnloadMappingError,dvid1);
                    //OnError("MappingError");
                }
            }
            else
            {
                if (CurrentSlotMapResult.Contains("2"))
                {
                    MapError = true;
                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == '2')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }

                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} load map crossed wafer on slot:{slotSpec} of carrier:{_carrierId}";


                    EV.Notify(AlarmLoadPortMapCrossedWafer, dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:crossed wafer when load carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortMappingError, dvid1);
                    OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("3"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == '3')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                    }

                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} load map double wafer on slot:{slotSpec} of carrier:{_carrierId}";




                    EV.Notify(AlarmLoadPortMapDoubleWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when load carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortMappingError, dvid1);
                    OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("W"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == 'W')
                        {
                            if (slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                            
                    }

                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} load map double wafer on slot:{slotSpec} of carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortMapDoubleWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:double wafer when load carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortMappingError,dvid1);
                    OnError("MappingError");
                }
                if (CurrentSlotMapResult.Contains("?"))
                {
                    MapError = true;

                    string slotSpec = "";
                    for (int i = 0; i < CurrentSlotMapResult.Length; i++)
                    {
                        if (CurrentSlotMapResult[i] == '?')
                        {
                            if(slotSpec == "")
                                slotSpec += $"{i + 1}";
                            else
                                slotSpec += $",{i + 1}";
                        }
                            
                    }

                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"{LPModuleName} load unknow wafer on slot:{slotSpec} of carrier:{_carrierId}";

                    EV.Notify(AlarmLoadPortMapUnknownWafer,dvid);
                    SerializableDictionary<string, object> dvid1 = new SerializableDictionary<string, object>();

                    dvid1["AlarmDescription"] = $"{LPModuleName} map error:unknow wafer when load carrier:{_carrierId}";
                    EV.Notify(AlarmLoadPortMappingError,dvid1);
                    OnError("MappingError");
                }
                if (CurrentSlotMapResult.Replace("1", "").Replace("0", "") == "")
                {
                    if (LPCallBack != null && IsPlacement)
                        LPCallBack.MappingComplete(_carrierId, CurrentSlotMapResult);
                }
            }

            _isMapped = true;







            WaferMapThickness = new Int32[_slotMap.Length];
            int wIndex = 0;
            string thicknessMsg = "";
            for (int i = 0; i < _slotMap.Length; i++)
            {
                if (_slotMap[i] != '0')
                {
                    WaferMapThickness[i] = Math.Abs(WaferPositionData.ToArray()[wIndex].Item2 - WaferPositionData.ToArray()[wIndex].Item1);
                    
                    if (WaferMapThickness[i] > WaferThicknessTolerance.Item2)
                    {
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} Wafer on slot {i + 1} " +
                            $"thickness is {WaferMapThickness[i]} um more than highlimit {WaferThicknessTolerance.Item2}.");
                        OnError("Mapping Data Error");
                    }
                    if (WaferMapThickness[i] < WaferThicknessTolerance.Item1)
                    {
                        EV.PostAlarmLog("LoadPort", $"{LPModuleName} Wafer on slot {i + 1} " +
                            $"thickness is {WaferMapThickness[i]} um less than lowlimit {WaferThicknessTolerance.Item1}.");
                        OnError("Mapping Data Error");
                    }


                    int slotBasePos = SlotPositionBaseLine + (SlotPitch * i);

                    int wafercenterpos = (WaferPositionData.ToArray()[wIndex].Item2 + WaferPositionData.ToArray()[wIndex].Item1) / 2;

                    EV.PostInfoLog("LoadPort",$"{LPModuleName} Wafer on slot{i + 1} wafer horizontal center is {wafercenterpos}um,thickness is {WaferMapThickness[i]}um.");


                    //if (Math.Abs(slotBasePos - wafercenterpos) >=WaferCenterDeviationLimit)
                    //{
                    //    EV.PostAlarmLog("LoadPort", $"{LPModuleName} Wafer on slot {i + 1} " +
                    //        $"wafer center is {wafercenterpos}um,deviation from baseline:{slotBasePos}um is more than lowlimit {WaferCenterDeviationLimit}um.");
                    //    OnError("Mapping Data Error");
                    //}
                    wIndex++;

                }
                else
                    WaferMapThickness[i] = 0;


                thicknessMsg += $"Slot:{i + 1},Thickness:{WaferMapThickness[i] *0.001} mm;";

            }

            LOG.Write($"{Name} map carrier:{CarrierId},slotmap:{_slotMap},thickness data:{thicknessMsg}.");



        }


        

    }
   
}
