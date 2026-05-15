
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using TSC = Aitex.Sorter.Common;
using Aitex.Core.Common;
using MECF.Framework.Common.SubstrateTrackings;
using Aitex.Core.Util;
using System.IO.Ports;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.Hirata
{
    public enum HirataSystemStatus
    {
        Normal =0x30,
        RecoverableError = 0x41,
        UnrecoverableError = 0x45,
    }
    public enum HirataMode
    {
        Online=0x30,
        Teaching=0x31,
        Maintenance =0x32,
    }
    public enum HirataInitPosMovement
    {
        OperationStatus=0x30,
        HomePosStatus=0x31,
        LoadStatus=0x32,
    }
    public enum HirataOperationStatus
    {
        DuringStop=0x30,
        DuringOperation=0x31,
    }
    public enum HirataContainerStatus
    {
        Absence=0x30,
        NormalMount=0x31,
        MountError=0x32,
    }
    public enum HirataPosition
    {
        Open=0x30,
        Close=0x31,
        TBD =0x3F
    }
    public enum HirataVacummStatus
    {
        OFF=0x30,
        ON=0x31,
    }
    public enum HirataWaferProtrusion
    {
        ShadingStatus=0x30,
        LightIncidentStatus=0x31,
    }
    public enum HirataElevatorAxisPosition
    {
        UP=0x30,
        Down=0x31,
        MappingStartPos=0x32,
        MappingEndPos=0x33,
        TBD = 0x3F,
    }
    public enum HirataDockPosition
    {
        Undock=0x30,
        Dock=0x31,
        TBD = 0x3F,
    }
    public enum HirataMapPosition
    {
        MeasurementPos=0x30,
        WaitingPost=0x31,
        TBD = 0x3F,
    }
    public enum HirataMappingStatus
    {
        NotPerformed=0x30,
        NormalEnd=0x31,
        ErrorStop=0x32,
    }
    public enum HirataModel
    {
        Type1=0x30,
        Type2=0x31,
        Type3=0x32,
        Type4=0x33,
        Type5=0x34,
    }

    public class HirataLoadPort : LoadPort
    {
        private bool _isTcpConnection;
        public HirataLoadPort(string module, string name,bool IsTCPconnection = false) : base(module, name)
        {
            _isTcpConnection = IsTCPconnection;
        }
        private HirataLoadPortConnection _connection;
        public HirataLoadPortConnection Connection
        {
            get => _connection;
        }

        private HirataLoadPortTcpConnection _tcpconnection;
        public HirataLoadPortTcpConnection TCPconnection => _tcpconnection;


        public HirataSystemStatus SystemStatus { get; set; }
        public HirataMode Mode { get; set; }
        public HirataInitPosMovement InitPosMovement { get; set; }
        public HirataOperationStatus OperationStatus { get; set; }
        public byte HostErrorCode { get; set; }
        public byte LoadPortErrorCode { get; set; }

        public HirataContainerStatus ContainerStatus { get; set; }

        public HirataPosition ClampPosition { get; set; }
        public HirataPosition DoorLatchPosition { get; set; }
        public HirataVacummStatus VacuumStatus { get; set; }
        public HirataPosition DoorPosition { get; set;}
        public HirataWaferProtrusion WaferProtrusion { get; set; }
        public HirataElevatorAxisPosition ElevatorAxisPosition { get; set; }
        public HirataDockPosition DockPosition { get; set; }
        public HirataMapPosition MapperPostion { get; set; }
        public HirataMappingStatus MappingStatus { get; set; }

        public HirataModel Model { get; set; }



        private PeriodicJob _thread;
        private static Object _locker = new Object();
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private bool _enableLog = true;
        //private bool _commErr = false;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        public override bool IsBusy
        {

            get 
            {
                if (_isTcpConnection)
                    return (_lstHandler.Count > 0 || _tcpconnection.IsBusy);
                return _lstHandler.Count > 0 || _connection.IsBusy; 
            }
        }


        public override bool Initialize()
        {
            base.Initialize();
            IsMapWaferByLoadPort = true;

            if (_isTcpConnection)
            {
                string ipaddress = SC.GetStringValue($"LoadPort.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
                _tcpconnection = new HirataLoadPortTcpConnection(this, ipaddress);
                _tcpconnection.EnableLog(_enableLog);
            }
            else
            {

                string portName = SC.GetStringValue($"LoadPort.{Name}.PortName");
                int bautRate = SC.GetValue<int>($"LoadPort.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"LoadPort.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.StopBits"), out StopBits stopBits);
                //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
                _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");

                _connection = new HirataLoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
            }



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
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }
            } while (true);


            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            return true;
        }
        private bool OnTimer()
        {
            try
            {
                if (_isTcpConnection)
                {
                    _tcpconnection.MonitorTimeout();

                    if (!_tcpconnection.IsConnected || _tcpconnection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }

                        _trigRetryConnect.CLK = !_tcpconnection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {
                            
                            if (!_tcpconnection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_tcpconnection.Address}, {Module}.{Name}");
                            }

                        }
                        return true;
                    }

                    HandlerBase handler = null;
                    if (!_tcpconnection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                                //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null)); 
                                //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null)); 
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _tcpconnection.Execute(handler);
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

                    HandlerBase handler = null;
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                                //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null)); 
                                //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null)); 
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
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
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
                if (!_isPlaced)
                {
                    OnCarrierPlaced();
                }

                if (!_isPresent)
                {
                    OnCarrierPresent();
                }
            }
        }

        public override void Terminate()
        {
            if (_isTcpConnection)
                _tcpconnection.Disconnect();
            else
                _connection.Disconnect();
        }
        public override void Monitor()
        {
            base.Monitor();
            try
            {
                if (_isTcpConnection)
                {
                    _tcpconnection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _tcpconnection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_tcpconnection.LastCommunicationError}");
                    }
                }
                else
                {
                    _connection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _connection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
        public override void Reset()
        {
            base.Reset();
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            if (_isTcpConnection)
                _tcpconnection.SetCommunicationError(false, "");
            else
                _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            lock (_locker)
            {
                _lstHandler.Clear();                
            }
            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            _trigRetryConnect.RST = true;
        }



        public override bool IsEnableMapWafer()
        {
            if (IsIdle
                && _isPresent
                && _isPlaced
                && DoorState == FoupDoorState.Open
                && CassetteState == LoadportCassetteState.Normal
                )
                return true;

            return false;
        }

        public override bool IsEnableTransferWafer()
        {
            if (IsIdle
                && _isPresent
                && _isPlaced
                && DoorState == FoupDoorState.Open
                && CassetteState == LoadportCassetteState.Normal
                )
                return true;

            return false;
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            if (IsIdle
                && _isPresent
                && _isPlaced
                && DoorState == FoupDoorState.Open
                && CassetteState == LoadportCassetteState.Normal
                )
                return true;

            return false;
        }

        
        public override bool ClearError(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new SetHandler(this, "RESET", null));
            }

            return true;
        }

        public override bool Init(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new SetHandler(this, "INITL", null));
            }

            return true;
        }
        public override bool Home(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                
                _lstHandler.Clear();
                _lstHandler.AddLast(new MoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new GetHandler(this, "STATE", null));
                _lstHandler.AddLast(new ModHandler(this, "ONMGV", null));

            }
            return true;            
        }
        public override bool ForceHome(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {

                _lstHandler.Clear();
                _lstHandler.AddLast(new MoveHandler(this, "ABORG", null));
            }
            return true;
        }
        public override bool LoadWithoutMap(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {                
                _lstHandler.AddLast(new MoveHandler(this, "CLOAD", null));
            }
            return true;
        }
        public override bool Load(out string reason)  //map and loads
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CLDMP", null));
            }
            return true;
        }
        public override bool Unload(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CULOD", null));
            }
            return true;
        }
        public override bool Clamp(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "PODCL", null));
            }
            return true;
        }

        public override bool Unclamp(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "PODOP", null));
            }
            return true;
        }

        public override bool Dock(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CLDDK", null));
            }
            return true;
        }

        public override bool Undock(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CULFC", null));
            }
            return true;            
        }

        public override bool OpenDoor(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CLMPO", null));
            }
            return true;
           
        }

        public override bool OpenDoorNoMap(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CLDOP", null));
            }
            return true;
           
        }

        public override bool CloseDoor(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new MoveHandler(this, "CULDK", null));
            }
            return true;
            
        }
        public override bool SetIndicator(Indicator light, IndicatorState op, out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public bool SetOnlineMode(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new ModHandler(this, "ONMGV", null));
            }
            return true;
        }
        public override bool QueryState(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new GetHandler(this, "STATE", null));
            }
            return true;
        }

        public override bool QueryIndicator(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new GetHandler(this, "LEDST", null));
            }
            return true;
        }

        public override bool QueryWaferMap(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new GetHandler(this, "MAPDT", null));
            }
            return true;
        }



        public bool OnEvent(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new GetHandler(this, "STATE", null));
            }
            return true;



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
            QueryState(out _);
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
        public override bool ReadRfId(out string reason)
        {
            OP.DoOperation("ReadCarrierId", Name);
            reason = "";
            return true;

        }



    }
}
//        public string Address { get; set; }

//        public bool IsConnected
//        {
//            get { return _port.IsOpen(); }
//        }

//        public bool UnlockKey
//        {
//            set
//            {
//            }
//        }

//        public bool Enable
//        {
//            set
//            {
//                if (_enableTrigger != null)
//                {
//                    _enableTrigger.SetTrigger(value, out _);
//                }
//            }
//        }

//        public bool FFUIsOK { get; set; }


//        private LoadportCassetteState _cassetteState = LoadportCassetteState.None;
//        public override LoadportCassetteState CassetteState
//        {
//            get { return _cassetteState; }
//            set
//            {
//                _cassetteState = value;
//            }
//        }


//        public bool Processed { get; set; }



//        public bool Communication
//        {
//            get
//            {
//                return _port == null ? !_commErr : !_commErr && _port.IsOpen();
//            }
//        }

//        //
//        public override bool IsBusy
//        {
//            get { return _foregroundHandler != null || _handlers.Count > 0; }
//        }


//        public override bool IsMoving
//        {
//            get { return _foregroundHandler != null && _handlers.Count > 0; }
//        }

//        public const string delimiter = "\r";
//        public const int min_len = 3;       //S00

//        private static Object _locker = new Object();

//        private AsyncSerial _port;

//        private bool _commErr = false;

//        private IHandler _eventHandler = null;

//        private IHandler _foregroundHandler = null;  //moving
//        private Queue<IHandler> _handlers = new Queue<IHandler>();


//        private IoSensor _sensorEnable = null;
//        private IoTrigger _enableTrigger = null;


//        public HirataLoadPort(string module, string name, string port, IoSensor sensorEnable, IoTrigger triggerEnable,
//            EnumLoadPortType portType = EnumLoadPortType.LoadPort)
//            : base(module, name)
//        {
//            _port = new AsyncSerial(port, 9600, 8);
//            _port.OnDataChanged += (OnDataChanged);
//            _port.OnErrorHappened += (OnErrorHandler);

//            _eventHandler = new handler<OnEventHandler>(Name);

//            _sensorEnable = sensorEnable;
//            _enableTrigger = triggerEnable;

//            Initalized = false;

//            IsMapWaferByLoadPort = portType == EnumLoadPortType.LoadPort;
//            PortType = portType;

//            IndicatiorLoad = IndicatorState.UNKNOW;
//            IndicatiorUnload = IndicatorState.UNKNOW;
//            IndicatiorPresence = IndicatorState.UNKNOW;
//            IndicatorAlarm = IndicatorState.UNKNOW;
//            IndicatiorPlacement = IndicatorState.UNKNOW;
//            IndicatiorOpAccess = IndicatorState.UNKNOW;
//            IndicatiorStatus1 = IndicatorState.UNKNOW;
//            IndicatiorStatus2 = IndicatorState.UNKNOW;

//            DoorState = FoupDoorState.Unknown;
//            ClampState = FoupClampState.Unknown;
//            CassetteState = LoadportCassetteState.Unknown;
//            //if (!this.QueryState(out string reason))
//            //{
//            //    EV.PostAlarmLog(module,$"Query state error.{reason}");
//            //}

//            Enable = true;

//            Address = port;
//        }

//        public void SetCassetteState(LoadportCassetteState state)
//        {
//            _cassetteState = state;

//            if (state == LoadportCassetteState.Normal)
//            {
//                if (!_isPlaced)
//                {
//                    OnCarrierPlaced();
//                }

//                if (!_isPresent)
//                {
//                    OnCarrierPresent();
//                }
//            }
//        }

//        public override bool Initialize()
//        {
//            base.Initialize();

//            _commErr = true;

//            ConnectionManager.Instance.Subscribe($"{Name}", this);

//            Connect();

//            //if (!RetryInstance.Instance().Execute<bool>(
//            //    ()=> _port.Open(),
//            //    SC.GetValue<int>("System.ComPortRetryDelayTime"),
//            //    SC.GetValue<int>("System.ComPortRetryCount"),
//            //    true
//            //))
//            //{
//            //    return false;
//            //}

//            return true;
//        }

//        public override bool Connect()
//        {
//            _commErr = false;
//            Task.Factory.StartNew(() =>
//            {
//                int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
//                int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
//                if (sleep <= 0 || sleep > 10)
//                    sleep = 2;

//                int retry = 0;
//                do
//                {
//                    _port.Close();
//                    Thread.Sleep(sleep * 1000);
//                    if (_port.Open())
//                    {
//                        //LOG.Write($"Connected with {Module}.{Name} .");
//                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
//                        break;
//                    }

//                    if (count > 0 && retry++ > count)
//                    {
//                        LOG.Write($"Retry connect {Module}.{Name} stop retry.");
//                        EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
//                        break;
//                    }

//                    Thread.Sleep(sleep * 1000);
//                    LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

//                } while (true);

//            });

//            return true;
//        }

//        public bool Disconnect()
//        {
//            return true;
//        }

//        public override bool IsEnableMapWafer()
//        {
//            if (IsIdle
//                && _isPresent
//                && _isPlaced
//                && DoorState == FoupDoorState.Open
//                && CassetteState == LoadportCassetteState.Normal
//                )
//                return true;

//            return false;
//        }

//        public override bool IsEnableTransferWafer()
//        {
//            return IsEnableTransferWafer(out _);
//        }

//        public override bool IsEnableTransferWafer(out string reason)
//        {
//            reason = "";

//            if (_isPlaced
//                && _isPresent
//                && _isMapped
//                && IsIdle
//                && DoorState == FoupDoorState.Open
//                && CassetteState == LoadportCassetteState.Normal
//                && IsWaferEnableTransfer()
//                ) return true;

//            return false;
//        }

//        /// <summary>
//        ///The Indicator is for EAP/Operator
//        /// LOAD READY: LoadPort Ready to receive FOUP
//        /// UNLOAD READY: FOUP ready to remove
//        /// 
//        /// </summary>
//        public void OnStateUpdated()
//        {
//            if (!SC.ContainsItem("Process.OptionLoadportMonitor") || !SC.GetValue<bool>("Process.OptionLoadportMonitor"))
//                return;

//            if (_isPresent && _isPlaced && this.ClampState == FoupClampState.Open && this.DockPOS == TDKY_AxisPos.Undock)
//            {
//                if (IndicatiorUnload != IndicatorState.ON)
//                    SetIndicator(Indicator.UNLOAD, IndicatorState.ON);
//                if (IndicatiorLoad != IndicatorState.OFF)
//                    SetIndicator(Indicator.LOAD, IndicatorState.OFF);
//                return;
//            }

//            if (!_isPresent || !_isPlaced)
//            {
//                if (IndicatiorUnload != IndicatorState.OFF)
//                    SetIndicator(Indicator.UNLOAD, IndicatorState.OFF);
//                if (IndicatiorLoad != IndicatorState.ON)
//                    SetIndicator(Indicator.LOAD, IndicatorState.ON);
//                return;
//            }

//            if (IndicatiorUnload != IndicatorState.OFF)
//                SetIndicator(Indicator.UNLOAD, IndicatorState.OFF);
//            if (IndicatiorLoad != IndicatorState.OFF)
//                SetIndicator(Indicator.LOAD, IndicatorState.OFF);
//        }

//        public void OnAccessButtonPushed()
//        {
//            if (_isPresent && _isPlaced)
//            {

//            }
//        }

//        /// <summary>
//        ///reset load port error
//        /// 
//        /// </summary>
//        public override void Reset()
//        {
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }
//            FFUIsOK = true;
//            _commErr = false;
//            Enable = true;
//            Error = false;

//            base.Reset();
//        }

//        public override bool Home(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<HomeHandler>(Name), out reason);
//        }

//        public override bool ForceHome(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<ForceHomeHandler>(Name), out reason);
//        }

//        public override bool FOSBMode(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<FOSBModeHandler>(Name), out reason);
//        }

//        public override bool FOUPMode(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<FOUPModeHandler>(Name), out reason);
//        }

//        public override bool FOSBDock(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<DockPosHandler>(Name), out reason);
//        }

//        public override bool FOSBUnDock(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<UnDockPosHandler>(Name), out reason);
//        }

//        public override bool FOSBDoorOpen(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<DoorOpenHandler>(Name), out reason);
//        }

//        public override bool FOSBDoorClose(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<DoorCloseHandler>(Name), out reason);
//        }

//        public override bool FOSBDoorUp(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<DoorUpHandler>(Name), out reason);
//        }

//        public override bool FOSBDoorDown(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<DoorDownHandler>(Name), out reason);
//        }

//        /// <summary>
//        /// Clear LP error
//        /// </summary>
//        public override bool ClearError(out string reason)
//        {

//            reason = string.Empty;

//            reason = string.Empty;
//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }

//            return execute(new handler<ResetHandler>(Name), out reason);
//        }

//        public override bool Stop(out string reason)
//        {
//            reason = string.Empty;

//            lock (_locker)
//            {
//                _foregroundHandler = null;
//                _handlers.Clear();
//            }
//            return execute(new handler<MovHandler>(Name, MovType.STOP_), out reason);
//        }

//        public override bool Load(out string reason)  //map and loads
//        {
//            return Move(MovType.CLDMP, out reason);
//        }

//        public override bool LoadWithoutMap(out string reason)
//        {
//            return Move(MovType.CLOAD, out reason);
//        }

//        public override bool Unload(out string reason)
//        {
//            return Move(MovType.CULOD, out reason);
//        }

//        public override bool Clamp(out string reason)
//        {
//            return Move(MovType.PODCL, out reason);
//        }

//        public override bool Unclamp(out string reason)
//        {
//            return Move(MovType.PODOP, out reason);
//        }

//        public override bool Dock(out string reason)
//        {
//            return Move(MovType.CLDDK, out reason);
//        }

//        public override bool Undock(out string reason)
//        {
//            return Move(MovType.CULFC, out reason);  //？？？？
//        }

//        public override bool OpenDoor(out string reason)
//        {
//            return Move(MovType.CLMPO, out reason);
//        }

//        public override bool OpenDoorNoMap(out string reason)
//        {
//            return Move(MovType.CLDOP, out reason);
//        }

//        public override bool CloseDoor(out string reason)
//        {
//            return Move(MovType.CULDK, out reason);
//        }

//        public override bool SetIndicator(Indicator light, IndicatorState op, out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<IndicatorHandler>(Name, light, op), out reason);
//        }

//        public bool SetOnlineMode(out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<ModHandler>(Name, Mode.ONMGV), out reason);
//        }

//        public override bool QueryState(out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<QueryStateHandler>(Name), out reason);
//        }

//        public override bool QueryIndicator(out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<QueryIndicatorHandler>(Name), out reason);
//        }

//        public override bool QueryWaferMap(out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<QueryWaferMappingHandler>(DeviceID), out reason);
//        }

//        public override bool QueryFOSBMode(out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<QueryFOSBModeHandler>(DeviceID), out reason);
//        }

//        public bool Move(MovType fun, out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<MovHandler>(Name, fun), out reason);
//        }

//        //public bool Query(QueryType type, out string reason)
//        //{
//        //    reason = string.Empty;
//        //    return execute(new handler<QueryWaferMappingHandler>(Name), out reason);
//        //}


//        public bool SetEvent(EvtType fun, out string reason)
//        {
//            reason = string.Empty;
//            return execute(new handler<SetEventHandler>(Name, fun), out reason);
//        }



//        public bool OnEvent(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                if (!execute(new handler<QueryStateHandler>(Name), out reason))
//                    return false;

//                //execute(new handler<QueryIndicatorHandler>(Name), out reason);

//                //LoadLight();
//                //UnloadLight();

//                return execute(new handler<QueryIndicatorHandler>(Name), out reason);
//            }



//        }

//        private bool execute(IHandler handler1, out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                if (_foregroundHandler == null)
//                {
//                    if (!handler1.Execute(ref _port))
//                    {
//                        reason = "Communication failed, please recovery it.";
//                        return false;
//                    }

//                    _foregroundHandler = handler1;
//                }
//                else
//                {
//                    var type = handler1.GetType();
//                    string name = type.Name;

//                    if (type.GenericTypeArguments.Length > 0)
//                        name = type.GenericTypeArguments[0].Name;

//                    LOG.Info(string.Format("Add command {0}", name));

//                    _handlers.Enqueue(handler1);
//                }

//            }
//            return true;
//        }

//        private bool execute(out string reason)
//        {
//            reason = string.Empty;
//            lock (_locker)
//            {
//                if (_handlers.Count > 0)
//                {
//                    IHandler handler = _handlers.Dequeue();

//                    if (!handler.Execute(ref _port))
//                    {
//                        reason = " communication failed, please recovery it.";
//                        LOG.Error(reason);
//                        EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】" + reason);

//                        return false;
//                    }

//                    _foregroundHandler = handler;
//                }
//            }
//            return true;
//        }

//        private void OnDataChanged(string package)
//        {
//            try
//            {
//                package = package.ToUpper();
//                string[] msgs = Regex.Split(package, delimiter);

//                foreach (string msg in msgs)
//                {
//                    if (msg.Length > min_len)
//                    {
//                        bool completed = false;
//                        string resp = msg.Substring(3, msg.Length - min_len);

//                        lock (_locker)
//                        {
//                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _port, resp, out completed))
//                            {
//                                string reason = string.Empty;
//                                if (!_foregroundHandler.IsBackground)
//                                {
//                                    _foregroundHandler = null;
//                                    execute(out reason);


//                                    OnActionDone(true);

//                                }
//                                else
//                                {
//                                    if (completed)
//                                    {
//                                        _foregroundHandler = null;
//                                        QueryState(out reason);
//                                        if (_foregroundHandler is handler<HomeHandler>)
//                                        {
//                                            QueryIndicator(out reason);
//                                        }
//                                    }
//                                }

//                            }
//                            else
//                            {
//                                _eventHandler.OnMessage(ref _port, resp, out completed);  //process event
//                            }
//                        }
//                    }
//                }
//            }
//            catch (ExcuteFailedException ex)
//            {
//                EV.PostWarningLog(Module, $"{Name} action failed, " + ex.Message);
//                _foregroundHandler = null;
//                OnError($"{ Name} action failed");
//            }
//            catch (InvalidPackageException ex)
//            {
//                EV.PostWarningLog(Module, $"{Name} received unprocessed message, {ex.Message}");
//                OnError($"{Name} received unprocessed message");
//            }
//            catch (Exception ex)
//            {
//                LOG.Write($" {Name} has exception：" + ex.Message);
//            }

//        }

//        private void OnErrorHandler(ErrorEventArgs args)
//        {
//            _commErr = true;
//            Initalized = false;

//            EV.PostAlarmLog(Module, $"{Display} Communication error, {args.Reason}");

//            OnError($"{Display} Communication error");
//        }

//        public void OnCarrierNotPlaced()
//        {
//            _isPlaced = false;
//            ConfirmRemoveCarrier();
//        }


//        public void OnCarrierNotPresent()
//        {
//            _isPresent = false;
//            //ConfirmRemoveCarrier();
//        }

//        public void OnCarrierPlaced()
//        {
//            _isPlaced = true;
//            ConfirmAddCarrier();
//            QueryState(out _);
//        }


//        public void OnCarrierPresent()
//        {
//            _isPresent = true;
//            //ConfirmAddCarrier();
//        }

//        public void OnSwitchKey1()
//        {
//            _isAccessSwPressed = true;
//        }

//        public void OnSwitchKey2()
//        {

//        }

//        public void OffSwitchKey1()
//        {
//            _isAccessSwPressed = false;
//        }

//        public void OffSwitchKey2()
//        {

//        }


//        public void SetIndicator(Indicator led, IndicatorState state)
//        {
//            SetIndicator(led, state, out string reason);
//        }


//        //impment E84 interface

//        public bool ReadyForLoad()
//        {
//            return CassetteState == LoadportCassetteState.Absent;
//        }
//        public bool ReadyForUnload()
//        {
//            return CassetteState == LoadportCassetteState.Normal;
//        }

//        public bool FoupArrirved()
//        {
//            return CassetteState == LoadportCassetteState.Normal;
//        }
//        public bool FoupRemoved()
//        {
//            return CassetteState == LoadportCassetteState.Absent;
//        }
//        public LPTransferState GetTransferState()
//        {
//            return LPTransferState.IN_SERVICE;
//        }
//        public LPAccessMode GetAccessMode()
//        {
//            return LPAccessMode.AUTO;
//        }
//        public LPReservedState GetReservedState()
//        {
//            return LPReservedState.NOT_RESERVED;
//        }

//        public void LPTSTrans(LPTransferState lpts)
//        {
//            return;
//        }

//        public bool IsSystemAutoMode()
//        {
//            return false;
//        }

//        public void SetAccessMode(bool isauto)
//        {
//            return;
//        }

//        public void E84Retry()
//        {
//            throw new NotImplementedException();
//        }

//        public void SetCompleted()
//        {
//            return;
//        }
//    }
//}
