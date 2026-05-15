using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKII
{
    public class TDKIILoadPort:LoadPortBaseDevice,IConnection
    {
        public TDKIILoadPort(string module, string name, string scRoot, IoTrigger[] dos = null,IoSensor[] dis=null,RobotBaseDevice robot=null, bool IsTCPconnection = false) :base(module,name,robot)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "TDKLoadPort";
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
        }

      

        private void _diInfoPad_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if(_infoPadType == 1)
                InfoPadCarrierIndex = (_diInfoPadA== null || !_diInfoPadA.Value ? 0 :1) +
                    (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                    (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                    (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
        }

        private void InitializeLP()
        {
            IsMapWaferByLoadPort = true;
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);


            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _infoPadType = SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;
            //InfoPadType,0=TDK,1=Ext,2=FixedbySC
            
            if(_infoPadType == 1)
            {
                InfoPadCarrierIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                    (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                    (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                    (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
            }
            if(_infoPadType == 2 || !IsAutoDetectCarrierType)
            {
                InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
            }
            
            
            
            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new TDKLoadPortTCPConnection(this, Address);
                
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

                _connection = new TDKLoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
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


            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            



        }

        private bool OnTimer()
        {
            try
            {
                if (_isTcpConnection)
                {
                    _tcpConnection.MonitorTimeout();
                    if(!_tcpConnection.IsConnected || _tcpConnection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_tcpConnection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {

                            Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                            _tcpConnection = new TDKLoadPortTCPConnection(this, Address);
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
                if (_infoPadType == 2)
                {
                    InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                }

                IsRequestFOSBMode = SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}") == 1;
                IsMapWaferByLoadPort = !IsRequestFOSBMode;
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
            
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private TDKLoadPortConnection _connection;
        private TDKLoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA;
        private IoSensor _diInfoPadB;
        private IoSensor _diInfoPadC;
        private IoSensor _diInfoPadD;

        private int _infoPadType;
        public int InfoPadType => _infoPadType;
        
        public TDKLoadPortConnection Connection
        {
            get => _connection;
        }
        public TDKLoadPortTCPConnection TCPConnection => _tcpConnection;

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
                _lstHandler.AddLast(new TDKGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));
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
        public override WaferSize GetCurrentWaferSize()
        {
            int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            return (WaferSize)intwz;
        }

        public override string SpecCarrierType 
        { 
            get
            {
                if(_isPlaced)
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value; }

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
                
                switch(param[0].ToString())
                {
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] { "","LON", "LBL", "LOF" };
                        lock(_locker)
                        {
                            _lstHandler.AddLast(new TDKSetHandler(this, statestr[(int)state], $"{(int)light:D2}"));
                            _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));

                        }
                        break;
                    case "QueryIndicator":
                        lock (_locker)
                        {                            
                            _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "YWAIT", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "DORFW", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "DORBK", null));
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "PODOP", null));
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "PODCL", null));
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "ZDRUP", null));
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKMoveHandler(this, "ZDRDW", null));
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
                        lock(_locker)
                        {
                            if (!IsMapWaferByLoadPort)
                            {
                                if(MapRobot!=null)
                                    return MapRobot.WaferMapping(LPModuleName, out _);                                
                                return false;
                            }
                            if (DockPosition != TDKDockPosition.Dock)
                                _lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                            if(DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new TDKMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKMoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new TDKGetHandler(this, "MAPRD", null));
                        }                        
                        break;
                }
                return true;
            }
            catch(Exception ex)
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
            if (IsRequestFOSBMode)
            {
                lock (_locker)
                {
                    if (!IsFosbModeActual)
                        _lstHandler.AddLast(new TDKSetHandler(this, "FSB", "ON"));

                    _lstHandler.AddLast(new TDKMoveHandler(this, "ZDRUP", null));
                    _lstHandler.AddLast(new TDKMoveHandler(this, "DORFW", null));
                    _lstHandler.AddLast(new TDKMoveHandler(this, "YWAIT", null));
                }
            }
            else
            {
                if (IsFosbModeActual)
                    _lstHandler.AddLast(new TDKSetHandler(this, "FSB", "OF"));
                if (param.Length >= 1 && param[0].ToString() == "UnloadWithMap")
                {
                    _lstHandler.AddLast(new TDKMoveHandler(this, "CUDMP", null));
                    _lstHandler.AddLast(new TDKGetHandler(this, "MAPRD", null));
                }
                if (param.Length >= 1 && param[0].ToString() == "UnloadWithoutMap")
                    _lstHandler.AddLast(new TDKMoveHandler(this, "CULOD", null));
                if (param == null || param.Length == 0)
                {
                    _lstHandler.AddLast(new TDKMoveHandler(this, "CUDMP", null));
                    _lstHandler.AddLast(new TDKGetHandler(this, "MAPRD", null));
                }
            }
            return true;



        }

        protected override bool fStartLoad(object[] param)
        {
            if(!_isPlaced)
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
                    if(!IsFosbModeActual)
                        _lstHandler.AddLast(new TDKSetHandler(this, "FSB", "ON"));
                    if(ClampPosition == TDKPosition.Open)
                        _lstHandler.AddLast(new TDKMoveHandler(this, "PODCL", null));
                    _lstHandler.AddLast(new TDKMoveHandler(this, "DORBK", null));
                    _lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                    _lstHandler.AddLast(new TDKMoveHandler(this, "ZDRDW", null));
                }               
            }
            else
            {
                lock (_locker)
                {

                    if (IsFosbModeActual)
                        _lstHandler.AddLast(new TDKSetHandler(this, "FSB", "OF"));
                    if (param.Length >= 1 && param[0].ToString() == "LoadWithMap")
                    {
                        _lstHandler.AddLast(new TDKMoveHandler(this, "CLDMP", null));
                        _lstHandler.AddLast(new TDKGetHandler(this, "MAPRD", null));
                    }
                    if (param.Length >= 1 && param[0].ToString() == "LoadWithoutMap")
                        _lstHandler.AddLast(new TDKMoveHandler(this, "CLOAD", null));
                    if (param == null || param.Length == 0)
                    {
                        _lstHandler.AddLast(new TDKMoveHandler(this, "CLDMP", null));
                        _lstHandler.AddLast(new TDKGetHandler(this, "MAPRD", null));
                    }
                }
            }
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                if(param.Length >=1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new TDKMoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new TDKMoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));
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
                _lstHandler.AddLast(new TDKSetHandler(this, "RESET", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));
            }
            return true;
        }
        public override void OnError(string error = "")
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if(_isTcpConnection)
                    _tcpConnection.ForceClear();
                else
                    _connection.ForceClear();
                _lstHandler.AddLast(new TDKGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKGetHandler(this, "LEDST", null));
            }

            base.OnError(error);
        }
    }
    public enum TDKSystemStatus
    {
        Normal = 0x30,
        RecoverableError = 0x41,
        UnrecoverableError = 0x45,
    }
    public enum TDKMode
    {
        Online = 0x30,
        Teaching = 0x31,
        Maintenance = 0x32,
    }
    public enum TDKInitPosMovement
    {
        OperationStatus = 0x30,
        HomePosStatus = 0x31,
        LoadStatus = 0x32,
    }
    public enum TDKOperationStatus
    {
        DuringStop = 0x30,
        DuringOperation = 0x31,
    }
    public enum TDKContainerStatus
    {
        Absence = 0x30,
        NormalMount = 0x31,
        MountError = 0x32,
    }
    public enum TDKPosition
    {
        Open = 0x30,
        Close = 0x31,
        TBD = 0x3F
    }
    public enum TDKVacummStatus
    {
        OFF = 0x30,
        ON = 0x31,
    }
    public enum TDKWaferProtrusion
    {
        ShadingStatus = 0x30,
        LightIncidentStatus = 0x31,
    }
    public enum TDKElevatorAxisPosition
    {
        UP = 0x30,
        Down = 0x31,
        MappingStartPos = 0x32,
        MappingEndPos = 0x33,
        TBD = 0x3F,
    }
    public enum TDKDockPosition
    {
        Undock = 0x30,
        Dock = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMapPosition
    {
        MeasurementPos = 0x30,
        WaitingPost = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMappingStatus
    {
        NotPerformed = 0x30,
        NormalEnd = 0x31,
        ErrorStop = 0x32,
    }
    public enum TDKModel
    {
        Type1 = 0x30,
        Type2 = 0x31,
        Type3 = 0x32,
        Type4 = 0x33,
        Type5 = 0x34,
    }
}
