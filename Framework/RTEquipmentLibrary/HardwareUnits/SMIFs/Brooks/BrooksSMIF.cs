using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
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
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks
{

    public class BrooksSMIF : SerialPortDevice, IConnection
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

        private BrooksSMIFConnection _connection;
        public BrooksSMIFConnection Connection
        {
            get { return _connection; }
        }

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        public int Axis { get; private set; }
        public bool IsIdle { get; private set; }
        public bool IsHomed { get; private set; }
        public bool IsPodPresent { get; private set; }
        public bool IsArmRetract { get; private set; }
        public bool IsAlarm { get; private set; }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog;
        private string _errorCode="";
        private DeviceTimer _setStatusTimer = new DeviceTimer();
        private int SetStatusTime = 800;

        private string _scRoot;
        public BrooksSMIF(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }
        private void ResetPropertiesAndResponses()
        {
            Connected = null;

            RobotStatus = null;
            IsOnLine = null;
            IsManual = null;
            IsES = null;
            MovingCompleted = null;
            ACalCompleted = null;
            AddressOutSide = null;
            PositionOutSide = null;
            EmergencyStop = null;
            XLowerSide = null;
            XUpperSide = null;
            YLowerSide = null;
            YUpperSide = null;
            ZLowerSide = null;
            ZUpperSide = null;


            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        internal void OnEventArrived(string eventData)
        {
            if (eventData == "POD_ARRIVED")
            {
                PodArrived = true;
            }
            else if (eventData == "POD_REMOVED")
            {
                PodRemoved = true;
            }
            else if (eventData == "BEGIN_FETCH")
            {
                FetchBegun = true;
            }
            else if (eventData == "CMPL_FETCH")
            {
                FetchCompleted = true;
            }
            else if (eventData == "BEGIN_LOAD")
            {
                LoadBegun = true;
            }
            else if (eventData == "ABORT_LOAD")
            {
                LoadAbort = true;
            }
            else if (eventData == "CMPL_LOAD")
            {
                LoadCompleted = true;
            }
            else if (eventData == "BEGIN_RECV_CASSETTE")
            {
                UnloadBegun = true;
            }
            else if (eventData == "ABORT_UNLOAD")
            {
                UnloadAbort = true;
            }
            else if (eventData == "RECV_CASSETTE")
            {
                UnloadCompleted = true;
            }
            else if (eventData == "BEGIN_HOME")
            {
                HomeBegun = true;
            }
            else if (eventData == "REACH_HOME")
            {
                HomeCompleted = true;
            }
            else if (eventData == "BEGIN_OPEN")
            {
                OpenBegun = true;
            }
            else if (eventData == "CMPL_OPEN")
            {
                OpenCompleted = true;
            }
            else if (eventData == "BEGIN_UNLOAD")
            {
                CloseBegun = true;
            }
            else if (eventData == "CMPL_UNLOAD")
            {
                CloseCompleted = true;
            }
        }

        private static Dictionary<string, string> _RealyMessageDict = new Dictionary<string, string>()
            {
                {"OK","Comment accepted" },
                {"BUSY","Command rejected, LPI is busy" },
                {"ALARM","Command rejected, LPI in alarm state" },
                {"NO_POD","Command rejected, Pod not on LPI" },
                {"NOT_READY","Command rejected, Host interlock not enabled" },
                {"INVALID_ARG","Command rejected, at least one invalid parameter" },
                {"CANNOT_PERFORM","LPI not in the proper state to perform the Host command" },
                {"DENIED","Command rejected for other reason" },
            };

        private static Dictionary<string, string> _AlarmTimeCodeDict = new Dictionary<string, string>()
            {
                {"00","No alarm" },
                {"41","Alarm occurred during a fetch or open operation" },
                {"42","Alarm occurred during a cassette placement on the process tool" },
                {"43","Alarm occurred during a returning of the minienvironment to Home" },
                {"44","Alarm occurred during a retrieval of the cassette from the Tool" },
                {"45","Alarm occurred during a placement of the cassette on the Pod door" },
                {"46","Non-recoverable fatal error" }
            };

        private static Dictionary<string, string> _ErrorCodeDict = new Dictionary<string, string>()
            {
                {"00","No error" },
                {"01","Position following error" },
                {"02","LPI not Home" },
                {"03","LPI busy" },
                {"04","Pod removed/missing elevator door" },
                {"05","Aborted by user" },
                {"07","Protrusion sensor failure" },
                {"08","Slot sensor failure" },
                {"09","Wafer presence sensor failure" },
                {"10","Flash sensor failure" },
                {"11"," Elevator over-travel limit trip" },
                {"13","Excessive wafer protrusion" },
                {"14","System internal time-out" },
                {"15","Servo command error" },
                {"16","Pod door open time-out" },
                {"17","Pod door close time-out" },
                {"18","Pod hold-down open time-out" },
                {"19","Pod hold-down close time-out" },
                {"20","Wafer seater failed to move toward wafer" },
                {"21","Wafer seater failed to return to Home" },
                {"22","Elevator failed to reach target position" },
                {"24","System error" },
                {"27","Loss of configuration data" },
                {"28","Cassette not present" },
                {"29","Loss of air flow" },
                {"31","Gripper open time out" },
                {"32","Gripper close time out" },
                {"33","Interprocessor communication error" },
                {"34","Gripper overtravel" },
                {"35","Cassette found during unload" },
            };

        internal void OnAlarmArrived(string alarmData)
        {
            var alarmArray = alarmData.Split(' ');
            if (alarmArray.Count() != 2)
                return;

            string alarmTimeCode = alarmArray[0].Substring(0, 2);
            string alarmInfoCode = alarmArray[0].Substring(2, 2);

            AlarmOccurTime = alarmTimeCode + "=" + _AlarmTimeCodeDict[alarmTimeCode];
            AlarmInfo = alarmInfoCode + "=" + alarmArray[1];
            IsAlarm = true;
        }

        public void SetError(string errorCode)
        {
            string alarmTimeCode = errorCode.Substring(0, 2);
            string alarmInfoCode = errorCode.Substring(2, 2);
            if (_AlarmTimeCodeDict.ContainsKey(alarmTimeCode))
                AlarmOccurTime = _AlarmTimeCodeDict[alarmTimeCode];
            else
                AlarmOccurTime = alarmTimeCode;

            if (_ErrorCodeDict.ContainsKey(alarmInfoCode))
                AlarmInfo = _ErrorCodeDict[alarmInfoCode];
            else
                AlarmInfo = alarmInfoCode;

            if(_errorCode != errorCode)
            {
                EV.PostAlarmLog(Module, $"{Module} {AlarmOccurTime}, for {AlarmInfo}");
            }

            _errorCode = errorCode;
            IsAlarm = true;
        }

        public void SetStatus(bool isReady, bool isHomed, bool isPodPresent, bool isArmRetract)
        {
            if(_setStatusTimer.IsIdle() || _setStatusTimer.GetElapseTime() > SetStatusTime)
            {
                IsIdle = isReady;
                IsHomed = isHomed;
                IsPodPresent = isPodPresent;
                IsArmRetract = isArmRetract;
            }
        }

        private string _address = "";
        public override bool Initialize()
        {
            ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected)
                return true;

            if(string.IsNullOrEmpty(_scRoot))
            {
                PortName = SC.GetStringValue($"{Module}.DeviceAddress");
                _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");
            }
            else
            {
                PortName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            }
            
            _connection = new BrooksSMIFConnection(this, PortName);
            _connection.EnableLog(_enableLog);

            base.Initialize(PortName);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(400, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            RequestStatus();

            OP.Subscribe($"{Module}.Home", (string cmd, object[] args) =>
            {
                HomeSmif(out _);
                return true;
            });

            OP.Subscribe($"{Module}.Abort", (string cmd, object[] args) =>
            {
                Stop();
                return true;
            });

            OP.Subscribe($"{Module}.Reset", (string cmd, object[] args) =>
            {
                Reset();
                return true;
            });

            OP.Subscribe($"{Module}.Unload", (string cmd, object[] args) =>
            {
                UnloadCassette(out _);
                return true;
            });

            OP.Subscribe($"{Module}.Load", (string cmd, object[] args) =>
            {
                LoadCassette(out _);
                return true;
            });

            return true;
        }

        internal void NoteConstant(string constantResult)
        {
            var constantResultArray = constantResult.Split('=');
            if(constantResultArray.Count() != 2)
            {
                return;
            }
            if(constantResultArray[0] == "P13")
            {
                MSCInterlockMode = constantResultArray[1];
            }
            //else if....
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
                        if (string.IsNullOrEmpty(_scRoot))
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{Module}.DeviceAddress"));
                        }
                        else
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress"));
                        }
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new BrooksSMIFQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new BrooksSMIFSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        internal void NoteStatus(string formCode, string status)
        {
            if(formCode == "FSD0")
            {
                SmifStatus = status;
            }
            else if (formCode == "FSD2")
            {
                CassetteMapInfo = status;
            }
        }

        internal void NoteEnable(string parameter, bool isEnable)
        {
            if(parameter == "FETCH")
            {
                FetchEnable = isEnable;
            }
            else if (parameter == "LOAD")
            {
                LoadEnable = isEnable;
            }
            else if (parameter == "HOME")
            {
                HomeEnable = isEnable;
            }
            else if (parameter == "OPEN")
            {
                OpenEnable = isEnable;
            }
            else if (parameter == "UNLOAD")
            {
                UnloadEnable = isEnable;
            }
            else if (parameter == "CLOSE")
            {
                CloseEnable = isEnable;
            }
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

            if(IsAlarm)
            {
                lock (_locker)
                {
                    _lstHandler.AddFirst(new BrooksSMIFResetHandler(this));
                }
            }

            IsAlarm = false;

            base.Reset();
        }

        public override bool Home(out string reason)
        {
            return base.Home(out reason);
        }


        #region Command Functions
        public void PerformRawCommand(string commandType,string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksSMIFRawCommandHandler(this, commandType, command, completeEvent, comandArgument));
            }
        }

        public void EnableAction(string action, string isEnable)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new BrooksSMIFEnableActionHandler(this, action, bool.Parse(isEnable)));
            }
        }

        public void FetchCassette()
        {
            lock (_locker)
            {
                if(!PodArrived || !FetchEnable )
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Fetch Cassette conditon error");
                    return;
                }
                _lstHandler.AddFirst(new BrooksSMIFFetchCassetteHandler(this));
            }

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);
        }
        public bool LoadCassette(out string reason)
        {
            reason = string.Empty;
            if (!IsIdle)
            {
                reason = $"{Module} is busy";
                return false;
            }
            lock (_locker)
            {
                //if (!LoadEnable)
                //{
                //    EV.PostAlarmLog(Module, $"{Module}.{Name} Load Cassette conditon error");
                //    return;
                //}
                _lstHandler.AddFirst(new BrooksSMIFLoadCassetteHandler(this));               
            }

            IsIdle = false;
            IsArmRetract = false;
            IsPodPresent = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            return true;
        }

        public bool HomeSmif(out string reason)
        {
            reason = string.Empty;
            if(!IsIdle)
            {
                reason = $"{Module} is busy";
                return false;
            }
            lock (_locker)
            {
                //if (!HomeEnable)
                //{
                //    EV.PostAlarmLog(Module, $"{Module}.{Name} Home conditon error");
                //    return;
                //}
                //_lstHandler.AddLast(new BrooksSMIFHomeHandler(this));
                _lstHandler.AddFirst(new BrooksSMIFRecoveryHandler(this));
            }

            IsIdle = false;
            IsArmRetract = false;
            IsHomed = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            return true;
        }

        public void Stop()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new BrooksSMIFStopHandler(this));
            }

            IsIdle = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
        }

        public void OpenPod()
        {
            lock (_locker)
            {
                if (!PodArrived || !OpenEnable)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} OpenPod conditon error");
                    return;
                }
                _lstHandler.AddFirst(new BrooksSMIFOpenPodHandler(this));
            }

            IsIdle = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);
        }

        public bool UnloadCassette(out string reason)
        {
            reason = string.Empty;
            if (!IsIdle)
            {
                reason = $"{Module} is busy";
                return false;
            }
            lock (_locker)
            {
                //if (!UnloadEnable)
                //{
                //    EV.PostAlarmLog(Module, $"{Module}.{Name} Unload Cassette conditon error");
                //    return;
                //}
                _lstHandler.AddFirst(new BrooksSMIFUnloadCassetteHandler(this));
            }

            IsIdle = false;
            IsArmRetract = false;
            IsPodPresent = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);

            return true;
        }

        public void ClosePod()
        {
            lock (_locker)
            {
                if (!CloseEnable)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} ClosePod conditon error");
                    return;
                }
                _lstHandler.AddFirst(new BrooksSMIFClosePodHandler(this));
            }

            IsIdle = false;

            if (!_setStatusTimer.IsIdle())
                _setStatusTimer.Stop();
            _setStatusTimer.Start(0);
        }
        public void SendEvent(string command)
        {
            _connection.SendMessage("AERS " + command + "\r\n");
        }

        public void MonitorRawCommand(bool isSelected, string commandType, string command, string commandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksSMIFRawCommandHandler) 
                                                            && ((BrooksSMIFRawCommandHandler)handler)._commandType == commandType
                                                            && ((BrooksSMIFRawCommandHandler)handler)._command == command
                                                            );
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksSMIFRawCommandHandler(this, commandType, command, commandArgument));
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

        public void RequestConstant(bool isSelected, string constantId)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksSMIFRequestConstantHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksSMIFRequestConstantHandler(this, constantId));
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


        public void RequestStatus(bool isSelected = true)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksSMIFRequestStatusHandler)
                && ((BrooksSMIFRequestStatusHandler)handler)._parameter.Contains("0"));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksSMIFRequestStatusHandler(this, 0));
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
        public void RequestCassetteMap(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(BrooksSMIFRequestStatusHandler)
                && ((BrooksSMIFRequestStatusHandler)handler)._parameter.Contains("2"));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new BrooksSMIFRequestStatusHandler(this, 2));
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


        #endregion

        #region Properties

        public string Connected { get; private set; }

        public string RobotStatus { get; private set; }
        public string IsOnLine { get; private set; }
        public string IsManual { get; private set; }
        public string IsStop { get; private set; }
        public string IsES { get; private set; }
        public string MovingCompleted { get; private set; }
        public string ACalCompleted { get; private set; }
        public string AddressOutSide { get; private set; }
        public string PositionOutSide { get; private set; }
        public string EmergencyStop { get; private set; }
        public string XLowerSide { get; private set; }
        public string XUpperSide { get; private set; }
        public string YLowerSide { get; private set; }
        public string YUpperSide { get; private set; }
        public string ZLowerSide { get; private set; }
        public string ZUpperSide { get; private set; }

        public bool PodArrived { get; private set; }
        public bool PodRemoved { get; private set; }
        public bool FetchBegun { get; private set; }
        public bool FetchCompleted { get; private set; }
        public bool FetchEnable { get; private set; }
        public bool LoadEnable { get; private set; }
        public bool LoadBegun { get; private set; }
        public bool LoadAbort { get; private set; }
        public bool LoadCompleted { get; private set; }
        public bool UnloadEnable { get; private set; }
        public bool UnloadBegun { get; private set; }
        public bool UnloadAbort { get; private set; }
        public bool UnloadCompleted { get; private set; }

        public bool HomeEnable { get; private set; }
        public bool HomeBegun { get; private set; }
        public bool HomeCompleted { get; private set; }

        public bool OpenEnable { get; private set; }
        public bool OpenBegun { get; private set; }
        public bool OpenCompleted { get; private set; }

        public bool CloseEnable { get; private set; }
        public bool CloseBegun { get; private set; }
        public bool CloseCompleted { get; private set; }
        public string AlarmOccurTime { get; private set; }
        public string AlarmInfo { get; private set; }
        public string MSCInterlockMode { get; private set; }
        public string SmifStatus { get; private set; }
        public string CassetteMapInfo { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();


        public void NoteError(string errorData)
        {
            if (errorData != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module} error, {(_RealyMessageDict.ContainsKey(errorData) ? _RealyMessageDict[errorData] : errorData)}");
                }
                if (errorData.ToUpper() == "ALARM")
                    IsAlarm = true;
            }
        }

        internal void NoteRawCommandInfo(string commandType, string command, string data, bool isAck)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandType == commandType && res.SourceCommand == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommandType = commandType, SourceCommand = command, ResonseContent = data, IsAck = isAck, ResonseRecievedTime = DateTime.Now });
        }


        #endregion
    }

}
