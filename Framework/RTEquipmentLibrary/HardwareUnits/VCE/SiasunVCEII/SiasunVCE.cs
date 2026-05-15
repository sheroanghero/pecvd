using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.FAServices;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.SiasunVCEII
{
    //SiasunVCE II只有Z轴，没有R轴X轴
    public class SiasunVCE : VCEBase, IConnection
    {

        private SiasunVCEConnection _connection;
        private bool _isIdle;
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        public Dictionary<string, string> ErrorCodeReference;
        private object _locker = new object();
        private SCConfigItem _scHomeTimeout;
        private SCConfigItem _scMappingTimeout;
        private SCConfigItem _scMotionTimeout;
        private bool _enableLog;
        private string _scRoot;
        private string _portName;
        private DeviceTimer _QueryTimer = new DeviceTimer();
        private readonly int _QueryInterval = 2000;
        private RD_TRIG _trigDoorOpen = new RD_TRIG();
        private R_TRIG _trigCassetteArrive = new R_TRIG();
        private R_TRIG _trigCassetteRemove = new R_TRIG();

        protected string _platformPresent = PlatformState.In.ToString();

        private int _vceSlotCount;

        public enum PlatformState
        {
            Unknown,
            Out,
            In,

        }

        public string Address => _connection.Address;
        public override bool IsConnected => _connection.IsConnected && !_connection.IsCommunicationError;
        public string PortStatus { get; set; } = "Closed";

        public override bool IsIdle
        {
            get => _isIdle && _lstHandler.Count == 0 && !IsAlarm && !_connection.IsCommunicationError;
            set => _isIdle = value;
        }

        public bool IsCassettePresentChecked { get; set; }

        public string Error { get; private set; }
        public string ArmRPosition { get; private set; }
        public string CommunicationMode { get; private set; }
        public string ZAxisPosition { get; private set; }
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

        private DeviceTimer _timer;
        private DeviceTimer _DelayTimer = new DeviceTimer();

        public SiasunVCE(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        //private string _address = "00";
        public override bool Initialize()
        {
            base.Initialize();

            if (_connection != null && _connection.IsConnected)
                return true;

            if (string.IsNullOrEmpty(_scRoot))
            {
                _portName = SC.GetStringValue($"VCE.{Module}.Address");
                _enableLog = SC.GetValue<bool>($"VCE.{Module}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"VCE.HomeTimeout");
                _scMappingTimeout = SC.GetConfigItem($"VCE.{Module}.MappingTimeout");
                _scMotionTimeout = SC.GetConfigItem($"VCE.MotionTimeout");
            }
            else
            {
                _portName = SC.GetStringValue($"{_scRoot}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"{_scRoot}.HomeTimeout");
                _scMappingTimeout = SC.GetConfigItem($"{_scRoot}.MappingTimeout");
                _scMotionTimeout = SC.GetConfigItem($"{_scRoot}.MotionTimeout");
            }

            _vceSlotCount = SC.GetValue<int>($"VCE.{Module}CassetteSlotCount");
            _timer = new DeviceTimer();

            _timer.Start(2000);
            _DelayTimer.Start(10 * 1000);

            _connection = new SiasunVCEConnection(_portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            //_lstMonitorHandler.AddLast(new SiasunVCERequestWaferSlideOutHandler(this));   // R,SO
            //_lstMonitorHandler.AddLast(new SiasunVCERequestCassettePresentHandler(this)); // R,W2
            //_lstMonitorHandler.AddLast(new SiasunVCERequestDoorStatusHandler(this));      // R,STAT,DOOR
            //_lstMonitorHandler.AddLast(new SiasunVCERequestArmPositionHandler(this));     // R,STAT,ATM
            _QueryTimer.Start(_QueryInterval);

            DATA.Subscribe($"{Module}.ProcessWaferStatus", () => new string(WaferManager.Instance.GetWafers(ModuleHelper.Converter(Module)).Select(x => char.Parse(((int)x.ProcessState).ToString())).ToArray()));

            DATA.Subscribe($"{Module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.IsMapped", () => IsMapped);
            DATA.Subscribe($"{Module}.Address", () => Address);
            DATA.Subscribe($"{Module}.IsDoorOpened", () => IsDoorOpened);
            DATA.Subscribe($"{Module}.IsDoorClosed", () => IsDoorClosed);
            DATA.Subscribe($"{Name}.DoorState", () =>
            {
                if (IsDoorOpened)
                    return 1;
                else if (IsDoorClosed)
                    return 16;
                else
                    return 0;
            });

            DATA.Subscribe($"{Module}.IsPresent", () => IsCassettePresent);
            DATA.Subscribe($"{Module}.PlatformPresent", () => _platformPresent);

            OP.Subscribe($"{Module}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            ErrorCodeReference = new Dictionary<string, string>()
            {
                { "40","E2PROM initialize hardware fail"},
                { "E2","E2PROM initialize hardware fail"},
                { "41","VCE is busy"},
                { "C10","VCE is busy"},
                { "42","Illegal command type"},
                { "A37","Illegal command type"},
                { "43","Illegal request command"},
                { "A38","Illegal request command"},
                { "44","Illegal set parameter command"},
                { "A39","Illegal set parameter command"},
                { "45","Illegal save parameter command"},
                { "A40","Illegal save parameter command"},
                { "46","Illegal action command"},
                { "A41","Illegal action command"},
                { "47","Command string error"},
                { "A42","Command string error"},
                { "48","Bad command parameter"},
                { "50","Z axis move position is exceeded"},
                { "Z02","Z axis move position is exceeded"},
                { "51","Z axis current position is exceeded"},
                { "Z03","Z axis current position is exceeded"},
                { "52","Z axis limit"},
                { "Z04","Z axis limit"},
                { "54","Z axis is not homed"},
                { "Z06","Z axis is not homed"},
                { "55","Door is opened"},
                { "Z07","Door is opened"},
                { "56","Cassette is not present"},
                { "Z08","Cassette is not present"},
                { "63","Door motion is obstructed"},
                { "D01","Door motion is obstructed"},
                { "64","Door motion timeout"},
                { "D02","Door motion timeout"},
                { "66","Emergency stop"},
                { "M10","Emergency stop"},
                { "67","Motor motion error"},
                { "M26","Motor motion error"},
                { "68","Motion is collided"},
                { "M27","Motion is collided"},
                { "69","Mapping failed, for the result is non-integral multiple"},
                { "M28","Mapping failed, for the result is non-integral multiple"},
                { "71","Z axis motion timeout"},
                { "T02","Z axis motion timeout"},
                { "72","CAN open failed for illegal frame"},
                { "CTX","CAN open failed for illegal frame"},
                { "73","CAN open failed for Emcy error"},
                { "CEM","CAN open failed for Emcy error"},
                { "74","CAN open failed for sdo error"},
                { "CSD","CAN open failed for sdo error"},
                { "WOT","Wafer slide out"},
                { "75","Wafer slide out"},
            };

            if (SC.ContainsItem($"{Module}.StatusPersistence"))
            {
                var sPersistentStatus = SC.GetStringValue($"{Module}.StatusPersistence");
                if (sPersistentStatus.ToString().ToUpper() == "LOADED")
                {
                    IsMapped = true;
                }
            }

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                //return true;
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
                        _connection.SetPortAddress(_portName);
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
                        if (_lstHandler.Count == 0 && _QueryTimer.IsTimeout())
                        {
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }
                            _QueryTimer.Start(_QueryInterval);
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

        internal void NoteZAxisPosition(string pos)
        {
            ZAxisPosition = pos;
        }

        internal void NoteHomed(bool homed)
        {
            IsHomed = homed;
        }

        internal void NoteMoveResult(string position)
        {
            MovedPosition = position;
        }

        public override void Monitor()
        {
            try
            {
                if (IsPlatformIn && !IsPlatformOut)
                {
                    _platformPresent = PlatformState.In.ToString();
                }
                else if (!IsPlatformIn && IsPlatformOut)
                {
                    _platformPresent = PlatformState.Out.ToString();
                }
                //else
                //{
                //    _platformPresent = PlatformState.Unknown.ToString();
                //}

                //_connection.EnableLog(_enableLog);
                //_trigDoorOpen.CLK = this.IsDoorOpened;
                //if (_trigDoorOpen.R)
                //{
                //    _lstMonitorHandler.AddLast(new SiasunVCERequestCassettePresentHandler(this));
                //}
                //if (_trigDoorOpen.T)
                //{
                //    _lstMonitorHandler.RemoveLast();
                //}

                if (_DelayTimer.IsTimeout())
                {
                    if (!SC.GetValue<bool>("System.IsSimulatorMode"))
                    {
                        if (_timer.IsTimeout() && this.IsDoorOpened && _lstHandler.Count == 0)
                        {
                            _lstHandler.AddLast(new SiasunVCERequestCassettePresentHandler(this));

                            _timer.Stop();
                            _timer.Start(2000);
                        }
                    }
                }

                if (this.IsCassettePresentChecked)
                {
                    _trigCassetteArrive.CLK = this.IsCassettePresent;
                    if (_trigCassetteArrive.Q)
                    {
                        EV.Notify(UniversalEvents.CARRIER_ARRIVED, new SerializableDictionary<string, object>()
                        {
                            {DataVariables.PortID, portidswicth(Name)},
                        });

                        CarrierManager.Instance.CreateCarrier(Name);

                        EV.PostInfoLog(Module, $"Confirm add carrier.");
                    }

                    _trigCassetteRemove.CLK = !this.IsCassettePresent;
                    if (_trigCassetteRemove.Q)
                    {
                        EV.Notify(UniversalEvents.CARRIER_REMOVED, new SerializableDictionary<string, object>()
                        {
                            {DataVariables.PortID, portidswicth(Name)},
                        });

                        for (var i = 0; i < _vceSlotCount; i++)
                        {
                            if (WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(Module), i))
                            {
                                WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Module), i);
                            }
                        }

                        CarrierManager.Instance.DeleteCarrier(Name);

                        EV.PostInfoLog(Module, $"Confirm delete carrier.");
                        EV.PostInfoLog(Module, $"Clear all {Module} wafers information.");
                    }
                }

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

        public int portidswicth(string modulename)
        {
            if (modulename == "VCEA")
            {
                return 1;
            }
            else if (modulename == "VCEB")
            {
                return 2;
            }
            else return 0;
        }

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;
            if (IsAlarm)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new SiasunVCEResetErrorHandler(this));
                }
            }

            IsAlarm = false;

            base.Reset();
        }

        public override void Terminate()
        {
            SC.SetItemValue($"{Module}.StatusPersistence", $"{(IsMapped ? "Loaded" : "Unloaded")}");
        }

        public void PerformRawCommand(string commandType)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERawCommandHandler(this, commandType, ""));
            }
        }

        public void PerformRawCommand(string commandType, string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERawCommandHandler(this, commandType, command));
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
                _lstHandler.AddLast(new SiasunVCERawCommandHandler(this, commandType, command, comandArgument));
            }
        }

        public void MonitorRawCommand(bool isSelected, string commandType, string command, string comandArgument)
        {
            lock (_locker)
            {
                string msg = comandArgument == null ? $"{command}\r" : $"{command} {comandArgument}\r";//??

                var existHandlers = _lstMonitorHandler
                    .Where(handler => handler.GetType() == typeof(SiasunVCERawCommandHandler) && ((SiasunVCERawCommandHandler)handler)
                    .IsSendText(commandType, command, comandArgument));

                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunVCERawCommandHandler(this, commandType, command, comandArgument));
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

        internal void NoteCurrentErrorStatus(string data)
        {
            CurrentErrorStatus = data;
        }

        internal void NoteCurrentMappingInfo(string data)
        {
            SlotMap = data;
        }

        public void RequestErrorStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SiasunVCERequestErrorInfoHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunVCERequestErrorInfoHandler(this));
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
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SiasunVCERequestMappingInfoHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunVCERequestMappingInfoHandler(this));
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

        public override bool QuerySlotMap(out string reason)
        {
            reason = string.Empty;
            SlotMap = "";
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERequestMappingInfoHandler(this));
            }

            return true;
        }

        public override bool CheckCassettePresent(bool isCheckPresent, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERequestCassettePresentHandler(this));
            }
            IsIdle = false;
            return true;
        }

        public override bool CheckWaferSlideOutStatus(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERequestWaferSlideOutHandler(this));
            }
            return true;
        }



        public override void Abort()
        {
            lock (_locker)
            {
                _lstHandler.Clear();
            }
        }

        public override bool MoveToSlot(int slot, out string reason)
        {
            reason = string.Empty;
            IsIdle = false;
            ZAxisAtSlot = -1;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEZAxisGotoSlotHandler(this, slot, _scMotionTimeout.IntValue));
                _lstHandler.AddLast(new SiasunVCERequestArmPositionHandler(this));
            }

            return true;
        }

        public override bool MoveToLoadPositon(out string reason)
        {
            reason = string.Empty;
            IsZAxisAtLoadPostion = false;
            IsIdle = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEZAxisMoveToLoadPositionHandler(this, _scMotionTimeout.IntValue));
                _lstHandler.AddLast(new SiasunVCERequestArmPositionHandler(this));
            }

            return true;
        }

        public override bool PlatformIn(out string reason)
        {
            reason = string.Empty;
            IsIdle = false;
            IsPlatformIn = false;
            IsPlatformOut = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEPlatformInHandler(this, _scMotionTimeout.IntValue));
            }

            return true;
        }

        public override bool PlatformOut(out string reason)
        {
            reason = string.Empty;
            IsIdle = false;
            IsPlatformIn = false;
            IsPlatformOut = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEPlatformOutHandler(this, _scMotionTimeout.IntValue));
            }

            return true;
        }

        public override bool CheckOutsideDoorStatus()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCERequestDoorStatusHandler(this));
            }
            IsIdle = false;
            return true;
        }

        public override bool CloseDoor(out string reason)
        {
            reason = string.Empty;
            IsIdle = false;
            IsDoorOpened = false;
            IsDoorClosed = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCECloseDoorHandler(this, _scMotionTimeout.IntValue));
                //_lstHandler.AddLast(new SiasunVCERequestDoorStatusHandler(this));
            }

            return true;
        }

        public override bool OpenDoor(out string reason)
        {
            reason = string.Empty;
            IsIdle = false;
            IsDoorOpened = false;
            IsDoorClosed = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEOpenDoorHandler(this, _scMotionTimeout.IntValue));
                //_lstHandler.AddLast(new SiasunVCERequestDoorStatusHandler(this));
            }

            return true;
        }

        public override bool MapCassette(out string reason)
        {
            reason = string.Empty;
            IsMapped = false;
            IsIdle = false;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCESetMappingTeachFlagHandler(this, false));
                _lstHandler.AddLast(new SiasunVCEMappingHandler(this, _scMappingTimeout.IntValue));
            }

            return true;
        }

        public void Move(string axis, string type, string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEMoveHandler(this, axis, type, value));
            }
        }

        public override bool Home(out string reason)
        {
            IsHomed = false;
            IsIdle = false;
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunVCEResetErrorHandler(this));                          // S,ER
                _lstHandler.AddLast(new SiasunVCEOutsideSignaliInterlockHandler(this, true));                // S,FN,Y
                _lstHandler.AddLast(new SiasunVCEHomeRAxisHandler(this, _scHomeTimeout.IntValue));  // A,HM,R
                _lstHandler.AddLast(new SiasunVCEDoorInterlockHandler(this, false));                // S,ZD,N
                _lstHandler.AddLast(new SiasunVCECloseDoorHandler(this, _scMotionTimeout.IntValue));// A,DC
                _lstHandler.AddLast(new SiasunVCECassetteInterlockHandler(this, false));            // S,CE,N
                _lstHandler.AddLast(new SiasunVCEHomeZAxisHandler(this, _scHomeTimeout.IntValue));  // A,HM,Z
                _lstHandler.AddLast(new SiasunVCEZAxisMoveToLoadPositionHandler(this, _scMotionTimeout.IntValue));  // A,LP
                //_lstHandler.AddLast(new SiasunVCECassetteInterlockHandler(this, true));             // S,CE,Y
            }

            return true;
        }


        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (reason != null)
            {
                IsAlarm = true;
                var content = reason;
                if (ErrorCodeReference.ContainsKey(reason))
                    content = ErrorCodeReference[reason];
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason} {content}");
                }
                Error = reason;
            }
            else
            {
                Error = null;
            }
        }

        internal void NoteRawCommandInfo(string commandType, string command, string data)
        {
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommandType = commandType, SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        internal void NoteArmRPositon(string armRPosition)
        {
            ArmRPosition = armRPosition;
        }

        internal void NoteArmZPositon(string armPosition)
        {
            ArmZPosition = armPosition;
        }

    }


}
