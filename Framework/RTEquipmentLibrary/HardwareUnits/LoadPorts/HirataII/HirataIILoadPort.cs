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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.HirataII
{
    public class HirataIILoadPort:LoadPortBaseDevice,IConnection
    {
        public HirataIILoadPort(string module,string name,string scRoot, IoTrigger[] dos,bool IsTCPconnection = false,RobotBaseDevice robot =null) :base(module,name,robot)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "TDKLoadPort";
            if (dos != null && dos.Length >= 1)
            {
                _doLoadPortOK = dos[0];

            }
            InitializeLP();
            SubscribeLPData();
        }
        public HirataIILoadPort(string module, string name, string scRoot, IoSensor[] dis = null, IoTrigger[] dos = null, bool IsTCPconnection = false, RobotBaseDevice robot = null) : base(module, name, robot)
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
            }
            if (dis != null && dis.Length >= 2)
            {
                _diInfoPadB = dis[1];
            }
            if (dis != null && dis.Length >= 3)
            {
                _diInfoPadC = dis[2];
            }
            if (dis != null && dis.Length >= 4)
            {
                _diInfoPadD = dis[3];
            }
            if (dis != null && dis.Length >= 5)
            {
                _diWaferProtrude = dis[4];
            }



            InitializeLP();
            SubscribeLPData();
        }

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
        public override string SpecCarrierType
        {
            get
            {
                if (_isPlaced)
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        private bool _needMoveBaseToReadCarrierID
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{Name}.NeedMoveToReadCarrierID"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.NeedMoveToReadCarrierID");
                return false;
            }
        }

        public override bool ReadCarrierID(int offset = 0, int length = 16)
        {
            if (_needMoveBaseToReadCarrierID)
            {
                return QueryCarrierID(out _);

                //lock (_locker)
                //{
                //    _lstHandler.AddLast(new HirataIIMoveToReadCarrierIDHandler(this));
                //    _lstHandler.AddLast(new HirataIIMoveHandler(this, "FCOTB", null));
                //}
                //return true;
            }
            else
                return base.ReadCarrierID(offset, length);

        }
        public override bool IsEnableLoad(out string reason)
        {
            //if(IsWaferProtrude)
            //{
            //    reason = "Wafer Protrusion";
            //    return false;
            //}
            if (_isNeedCheckIronStickDoor)
            {
                if (DiIronCassetteDoorClose != null && !DiIronCassetteDoorClose.Value)
                {
                    reason = "Iron Door Closed";
                    return false;
                }
            }

            return base.IsEnableLoad(out reason);
        }
        public override bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            if (SC.ContainsItem($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}") &&
                SC.GetValue<bool>($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}"))
            {

                return true;
            }
            return base.IsEnableDualTransfer(out reason);
        }
        public override bool IsEnableTransferWafer(out string reason)
        {
            if (LPDoorState != TDKPosition.Open)
            {
                reason = "Door is not open";
                return false;
            }
            if (DockPosition != TDKDockPosition.Dock)
            {
                reason = "Foup is not dock";
                return false;
            }
            if(WaferProtrusion == TDKWaferProtrusion.ShadingStatus)
            {
                reason = "Wafer Protrude";
                return false;
            }
            if(_diWaferProtrude!=null && !_diWaferProtrude.Value)
            {
                reason = "Wafer Protrude signal on";
                return false;
            }
            if (_isNeedCheckIronStickDoor)
            {
                if (DiIronCassetteDoorClose != null && !DiIronCassetteDoorClose.Value)
                {
                    reason = "Iron Door Closed";
                    return false;
                }
            }

            return base.IsEnableTransferWafer(out reason);
        }

        private bool _isNeedCheckIronStickDoor
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.NeedCheckIronDoorCarrier{InfoPadCarrierIndex}"))
                    return SC.GetValue<bool>($"CarrierInfo.NeedCheckIronDoorCarrier{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier");
                return false;
            }
        }
        private void InitializeLP()
        {            
            IsMapWaferByLoadPort = true;
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);

            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new HirataIILoadPortTCPConnection(this, Address);
                
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

                _connection = new HirataIILoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
                int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 10;
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

            SetCarrierType();


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
                            _tcpConnection = new HirataIILoadPortTCPConnection(this, Address);
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
                    //_trigActionDone.CLK = (_lstHandler.Count == 0 && !_connection.IsBusy);
                    //if (_trigActionDone.Q)
                    //    OnActionDone(null);

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


                if (_infoPadType == 1)   //Extenal
                {
                    InfoPadSensorIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                    (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                    (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                    (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
                }
                if (_infoPadType == 2)  //Fixed by SC
                {
                    if (SC.ContainsItem($"LoadPort.{Name}.CarrierIndex"))
                        InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                    else
                        InfoPadCarrierIndex = 0;
                }
                if (_infoPadType == 0)
                {
                    if (InfoPadCarrierIndex != InfoPadSensorIndex)
                    {
                        InfoPadCarrierIndex = InfoPadSensorIndex;
                        SetCarrierType();
                    }
                    InfoPadCarrierIndex = InfoPadSensorIndex;
                }
                


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
       
        }

        private int recipeNo
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierRecipeNumber{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierRecipeNumber{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.LPRecipeNumber"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.LPRecipeNumber");
                return 0;
            }
        }

        private void SetCarrierType()
        {
            if (_isNeedSetType)
            {              
                if (recipeNo > 5 || recipeNo < 1)
                {
                    EV.PostAlarmLog(Name, $"Carrier is configured on invalid recipe no:{recipeNo},can't load.");
                    return;
                }
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataIISetHandler(this, "TYPE", recipeNo.ToString()));
                    _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                    _lstHandler.AddLast(new HirataIIGetHandler(this, "LPIOI", null));
                }
            }
        }
        public override void Monitor()
        {
            base.Monitor();

        }
        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private HirataIILoadPortConnection _connection;
        private HirataIILoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA = null;
        private IoSensor _diInfoPadB = null;
        private IoSensor _diInfoPadC = null;
        private IoSensor _diInfoPadD = null;
        private IoSensor _diWaferProtrude = null;

        //InfoPadType,0=OnLP,1=Ext,2=FixedbySC
        private IoSensor _diIronCassetteDoorClose;
        public IoSensor DiIronCassetteDoorClose
        {
            get => _diIronCassetteDoorClose;
            set
            {
                _diIronCassetteDoorClose = value;
                _diIronCassetteDoorClose.OnSignalChanged += _diIronCassetteDoorClose_OnSignalChanged;
            }
        }

        private void _diIronCassetteDoorClose_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (!IsPlacement || !IsMapped) return;
            if (arg2) return;
            if (_diIronCassetteDoorClose.Value) return;
            if (!IsReady()) return;
            Unclamp(out _);
        }




        private int _infoPadType
        {
            get
            {
                return SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;
                
            }
        }
        

        public override int ValidSlotsNumber
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}");
                return base.ValidSlotsNumber;
            }
        }
        public HirataIILoadPortConnection Connection
        {
            get => _connection;
        }
        public HirataIILoadPortTCPConnection TCPConnection => _tcpConnection;

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
       
        public bool IsConnected => this.Connect();// _connection.IsConnected;

        private DateTime _dtStartAction;
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
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LPIOI", null));
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



        protected override bool fStartWrite(object[] param)
        {
            return false;
        }

        protected override bool fStartRead(object[] param)
        {
            switch(param[0].ToString())
            {
                case "CarrierID":
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataIIMoveToReadCarrierIDHandler(this));
                        _lstHandler.AddLast(new HirataIIMoveHandler(this, "FCOTB", null));
                    }
                    return true;
            }

            _dtStartAction = DateTime.Now;
            return false;
        }

        protected override bool fMonitorReadingData(object[] param)
        {
            IsBusy = false;
            if(DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("ReadTimeout");
                return true;
            }

            switch (CurrentParamter[0].ToString())
            {
                case "CarrierID":
                    if(_isTcpConnection)
                    {
                        if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                        {
                            if(CurrentReader.IsReady)
                            {
                                base.OnCarrierIdRead(CurrentReader.CarrierIDBeRead);
                                return true;
                            }
                            if(CurrentReader.DeviceState == CIDReaderStateEnum.Error)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (_lstHandler.Count == 0 && !_connection.IsBusy)
                        {
                            if (CurrentReader.IsReady)
                            {
                                base.OnCarrierIdRead(CurrentReader.CarrierIDBeRead);
                                return true;
                            }
                            if (CurrentReader.DeviceState == CIDReaderStateEnum.Error)
                            {
                                return true;
                            }
                        }
                    }                    
                    return false;
            }


            return base.fMonitorReadingData(param);
        }

        public override void OnCarrierIdRead(string carrierId, int readerIndex = 1, int startPage = 1, int length = 16, bool updateCarrierID = true)
        {
            if (_needMoveBaseToReadCarrierID) return;
            base.OnCarrierIdRead(carrierId, readerIndex, startPage, length, updateCarrierID);
        }

        protected override bool fStartExecute(object[] param)
        {
            _dtStartAction = DateTime.Now;
            try
            {
                switch(param[0].ToString())
                {
                    case "Move":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Set":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIISetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Get":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIGetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] {"","LP","BL","LO" }; //{ "","LON", "LBL", "LOF" };
                        string strLight = "";
                        if (light == Indicator.LOAD)
                            strLight = "LOD";
                        if (light == Indicator.UNLOAD)
                            strLight = "ULD";
                        if (light == Indicator.ACCESSMANUL)
                            strLight = "ST2";
                        if (light == Indicator.ACCESSAUTO)
                            strLight = "ST3";
                        if (string.IsNullOrEmpty(strLight))
                            return false;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIISetHandler(this, statestr[(int)state], strLight));
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));

                        }
                        break;
                    case "QueryIndicator":
                        lock (_locker)
                        {                            
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "YWAIT", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORFW", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORBK", null));
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODOP", null));
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODCL", null));
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRUP", null));
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "ZDRDW", null));
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
                            if(DockPosition != TDKDockPosition.Dock)
                                _lstHandler.AddLast(new HirataIIMoveHandler(this, "YDOOR", null));
                            if(DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new HirataIIMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new HirataIIMoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                        }                        
                        break;

                    case "ReadCarrierID":
                        lock(_locker)
                        {

                            _lstHandler.AddLast(new HirataIIMoveToReadCarrierIDHandler(this));
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

        protected override bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                string command = CurrentParamter[0].ToString();
                OnError(command);
                return true;
            }
            if (_isTcpConnection)
            {
                if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                {
                    return true;
                }
            }
            else
            {
                if (_lstHandler.Count == 0 && !_connection.IsBusy)
                {
                    return true;
                }
            }


            return base.fMonitorExecuting(param);
        }

        protected override bool fStartUnload(object[] param)
        {
            _dtStartAction = DateTime.Now;
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
            if (param.Length >= 1 && param[0].ToString() == "UnloadWithMap")
            {
                _lstHandler.AddLast(new HirataIIMoveHandler(this, "CUDMP", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
            }
            else
                _lstHandler.AddLast(new HirataIIMoveHandler(this, "CULOD", null));

            if(IsKeepClampAfterUnload)
                _lstHandler.AddLast(new HirataIIMoveHandler(this, "PODCL", null));

            return true;

        }

        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {                
                OnError("UnloadTimeout");
                return true;
            }
            if (_isTcpConnection)
            {
                if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                {
                    OnUnloaded();
                    return true;
                }
            }
            else
            {
                if (_lstHandler.Count == 0 && !_connection.IsBusy)
                {
                    OnUnloaded();
                    return true;
                }
            }

            return base.fMonitorUnload(param);
        }




        private bool _isNeedSetType
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.NeedSetType"))
                    return SC.GetValue<bool>($"LoadPort.{Name}.NeedSetType");
                return false;
            }
        }
        protected override bool fStartLoad(object[] param)
        {
            _dtStartAction = DateTime.Now;
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

            if (_isNeedSetType)
            {                
                if(recipeNo >5 || recipeNo < 1 )
                {
                    EV.PostAlarmLog(Name, $"Carrier is configured on invalid recipe no:{recipeNo},can't load.");
                    return false;
                }
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataIISetHandler(this, "TYPE", recipeNo.ToString()));
                }
            }

            lock (_locker)
            {

                if (param.Length >= 1 && param[0].ToString() == "LoadWithoutMap")
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "CLOAD", null));
                else
                {
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "CLDMP", null));
                    _lstHandler.AddLast(new HirataIIGetHandler(this, "MAPRD", null));
                }
            }
            return true;
        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("LoadTimeout");
                return true;
            }
            if (_isTcpConnection)
            {
                if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                {
                    OnLoaded();
                    return true;
                }
            }
            else
            {
                if (_lstHandler.Count == 0 && !_connection.IsBusy)
                {
                    OnLoaded();
                    return true;
                }
            }
            return base.fMonitorLoad(param);
        }



        protected override bool fStartInit(object[] param)
        {
            _dtStartAction = DateTime.Now;
            lock (_locker)
            {
                if(param.Length >=1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new HirataIIMoveHandler(this, "ORGSH", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LPIOI", null));
            }
            return true;
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("InitTimeout");
                return true;
            }
            if (_isTcpConnection)
            {
                if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                {
                    OnHomed();
                    return true;
                }
            }
            else
            {
                if (_lstHandler.Count == 0 && !_connection.IsBusy)
                {
                    OnHomed();
                    return true;
                }
            }
            return base.fMonitorInit(param);
        }


        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        protected override bool fStartReset(object[] param)
        {
            _dtStartAction = DateTime.Now;
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
                _lstHandler.AddLast(new HirataIISetHandler(this, "RESET", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
                //_lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LPIOI", null));
            }
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtStartAction > TimeSpan.FromSeconds(TimelimitAction))
            {
                OnError("Reset");
                return true;
            }
            if (_isTcpConnection)
            {
                if (_lstHandler.Count == 0 && !_tcpConnection.IsBusy)
                {
                    return true;
                }
            }
            else
            {
                if (_lstHandler.Count == 0 && !_connection.IsBusy)
                {
                    return true;
                }
            }


            return base.fMonitorReset(param);
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
                _lstHandler.AddLast(new HirataIIGetHandler(this, "STATE", null));
               // _lstHandler.AddLast(new HirataIIGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LEDST", null));
                _lstHandler.AddLast(new HirataIIGetHandler(this, "LPIOI", null));
            }

            base.OnError(error);
        }

        public override bool IsReadyForE84Transfer
        {
            get
            {
                if (_isTcpConnection)
                {
                    if (_lstHandler.Count != 0 || _tcpConnection.IsBusy)
                    {
                        return false;
                    }
                }
                else
                {
                    if (_lstHandler.Count != 0 || _connection.IsBusy)
                    {
                        return false;
                    }
                }
                return base.IsReadyForE84Transfer;
            }
        }

        public override WaferSize GetCurrentWaferSize()
        {

            int intwz=12;
            if(SC.ContainsItem($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}"))
                intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            if(SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize"))
                intwz = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize");

            switch (intwz)
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


        public void ParseLPIO(string strData)
        {
            string data = strData.Replace("LPIOI/", "");

            string infoStr = data.Substring(1, 1);

            InfoPadSensorIndex = Convert.ToInt32(infoStr, 16);
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
