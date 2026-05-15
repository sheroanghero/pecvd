using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.BrooksVCE
{
    public class BrooksVCE : SerialPortDevice, IConnection
    {
        public string Address { get { return _address; } }
        public bool IsConnected { get; }
        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public string PortStatus { get; set; } = "Closed";

        private BrooksVCEConnection _connection;
        public BrooksVCEConnection Connection
        {
            get { return _connection; }
        }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;
        private string _scRoot;
        public BrooksVCE(string module, string name, string scRoot, string portName) : base(module, name)
        {
            _scRoot = scRoot;
            PortName = portName;
        }
        private void ResetPropertiesAndResponses()
        {

            DoorClosed = null;
            DoorOpened = null;
            IsHalted = null;


            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        private string _address;
        public override bool Initialize(string portName)
        {
            base.Initialize(portName);

            ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected && PortName == portName)
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            PortName = portName;
            _address = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new BrooksVCEConnection(PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                //return true;
                //_connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new BrooksVCEQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new BrooksVCESetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        internal void NoteCommonActionResult(string command)
        {
            if(command == "JD")
            {

            }
            else if (command == "JU")
            {

            }
            else if (command == "LOAD")
            {
                LoadCompleted = true;
            }
            else if (command == "LP")
            {
                LoadPosition = true;
            }
            else if (command == "PI")
            {
                RetractPosition = true;
            }
            else if (command == "PICK")
            {
                Picked = true;
            }
            else if (command == "PLACE")
            {
                Placed = true;
            }
            else if (command == "PO")
            {
                ExtendPosition = true;
            }
            else if (command == "UNLOAD")
            {
                Unloaded = true;
            }
        }

        internal void NoteZAxisPosition(string pos)
        {
            ZAxisPosition = pos;
        }

        internal void NoteHomed(bool homed)
        {
            Homed = homed;
        }

        internal void NoteMoveResult(string position)
        {
            MovedPosition = position;
        }

        public override void Monitor()
        {
            try
            {
                //_connection.EnableLog(_enableLog);


                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }



        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public override bool Home(out string reason)
        {
            return base.Home(out reason);
        }


        #region Command Functions

        public void PerformRawCommand(string commandType)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCERawCommandHandler(this, commandType, ""));
            }
        }
        public void PerformRawCommand(string commandType, string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCERawCommandHandler(this, commandType, command));
            }
        }

        internal void NoteCurrentCommunicationMode(string data)
        {
            CurrentCommunicationMode = data;
        }

        public void PerformRawCommand(string commandType, string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCERawCommandHandler(this, commandType,command, comandArgument));
            }
        }



        public void MonitorRawCommand(bool isSelected, string commandType, string command, string comandArgument)
        {
            lock (_locker)
            {
                string msg = comandArgument == null ? $"{command}\r" : $"{command} {comandArgument}\r";//??

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksVCERawCommandHandler) && ((BrooksVCERawCommandHandler)handler).IsSendText(commandType, command, comandArgument));

                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksVCERawCommandHandler(this, commandType,command, comandArgument));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        internal void NoteCurrentACEStatus(string data)
        {
            throw new NotImplementedException();
        }

        internal void NoteCurrentErrorStatus(string data)
        {
            CurrentErrorStatus = data;
        }

        internal void NoteIsLoadPositon(string data)
        {
            InLoadPositon = data.Last() == 'Y';
        }

        public void RequestArmRPositon(bool isSelected, string axisPosition)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestArmRPositonHandler) && handler.SendText.Contains(axisPosition));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestArmRPositonHandler(this, axisPosition));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        internal void NoteCurrentMappingInfo(string data)
        {
            CurrentMappingInfo = data;
        }

        internal void NoteIsPlatformPosition(string data)
        {
            InPlatformPosition = data.Last() == 'Y';
        }

        public void RequestArmZPositon(bool isSelected, string axisPosition)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestArmZPositonHandler) && handler.SendText.Contains(axisPosition));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestArmZPositonHandler(this, axisPosition));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        public void RequestCommunicationMode(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestCommModeHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestCommModeHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        public void RequestErrorStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestErrorStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestErrorStatusHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }
        public void RequestVCEStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestVCEStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestVCEStatusHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }

        public void RequestLoadPositon(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestLoadPositionHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestLoadPositionHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }


        public void RequestMappingInfo(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestMappingInfoHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestMappingInfoHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }


        public void RequestPlatformPosition(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksACERequestPlatformPositionHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksACERequestPlatformPositionHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }


        public void Abort()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCEAbortHandler(this));
            }
        }

        public void CloseDoor()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECloseDoorHandler(this));
            }
        }
        public void OpenDoor()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCEOpenDoorHandler(this));
            }
        }
        public void JogDown()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "JD"));
            }
        }
        public void JogUp()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "JU"));
            }
        }
        public void Load()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "LOAD"));
            }
        }

        public void MoveToLoadPositon()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "LP"));
            }
        }
        public void MapCassette()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "LP"));
            }
        }
        public void Move(string axis, string type, string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCEMoveHandler(this, axis, type, value));
            }
        }
        public void RoateRAxisToRetractPosition()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "PI"));
            }
        }
        public void RoateRAxisToExtendPosition()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "PO"));
            }
        }
        public void Pick()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "PICK"));
            }
        }
        public void Place()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "PLACE"));
            }
        }
        public void Unload()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCECommonActionHandler(this, "UNLOAD"));
            }
        }
        public void GotoZ(string direction)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCEGotoZHandler(this, direction));
            }
        }
        public void Home(string axis)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCEHomeHandler(this,axis));
            }
        }

        public void SetCommunicationMode(string commMode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksVCESetCommunicationModeHandler(this, commMode));
            }
        }


        #endregion

        #region Properties

        public string DoorClosed { get; private set; }
        public string DoorOpened { get; private set; }
        public string IsHalted { get; private set; }

        public string Error { get; private set; }
        public string ArmRPosition { get; private set; }
        public string CommunicationMode { get; private set; }
        public string ZAxisPosition { get; private set; }
        public bool Homed { get; private set; }
        public bool LoadCompleted { get; private set; }
        public bool LoadPosition { get; private set; }
        public bool RetractPosition { get; private set; }

        public bool Picked { get; private set; }
        public bool Placed { get; private set; }
        public bool ExtendPosition { get; private set; }
        public bool Unloaded { get; private set; }
        public string MovedPosition { get; private set; }
        public string ArmZPosition { get; private set; }
        public string CurrentCommunicationMode { get; private set; }
        public string CurrentErrorStatus { get; private set; }
        public bool InLoadPositon { get; private set; }
        public string CurrentMappingInfo { get; private set; }
        public bool InPlatformPosition { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
            }
            else
            {
                Error = null;
            }
        }

        internal void NoteDoorClosed(bool isClosed)
        {
            DoorClosed = isClosed ? "true" : "false";
            DoorOpened = isClosed ? "false" : "true";
        }

        internal void NoteIsHalted(bool isHalted)
        {
            IsHalted = isHalted ? "true" : "false";
        }


        internal void NoteRawCommandInfo(string commandType, string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommandType = commandType,SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        internal void NoteArmRPositon(string armRPosition)
        {
            ArmRPosition = armRPosition;
        }
        internal void NoteArmZPositon(string armPosition)
        {
            ArmZPosition = armPosition;
        }
        internal void NoteCommunicationMode(string mode)
        {
            CommunicationMode = mode;
        }

        #endregion
    }


}
