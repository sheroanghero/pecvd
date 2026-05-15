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
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Siasun1500C800C
{


    public class RobotSiasun1500C800C : TCPSocketDevice, IConnection
    {
        public string Address { get; }
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

        private RobotSiasun1500C800CTCPConnection _connection;
        public RobotSiasun1500C800CTCPConnection Connection
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
        public RobotSiasun1500C800C(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }
        private void ResetPropertiesAndResponses()
        {
            LoadAWaferStatus = false;
            LoadBWaferStatus = false;

            ArmAPosition = null;
            ArmBPosition = null;
            IsHalted = false;
            AllHomed = false;
            RHomed = false;
            THomed = false;
            ZHomed = false;
            LastPickInfo = null;
            LastPlaceInfo = null;
            ArmRetracted = false;

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        public override bool Initialize()
        {
            base.Initialize();

            ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected)
                return true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            string deviceIP = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceIP");

            _connection = new RobotSiasun1500C800CTCPConnection(deviceIP);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            else
            {
                PortStatus = "Close";
                EV.PostInfoLog(Module, $"{Module}.{Name} connect failed");
                return false;
            }


            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
 
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
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new RobotSiasun1500C800CQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RobotSiasun1500C800CSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CRawCommandHandler(this, command));
            }
        }

        public void ResetRobot()
        {
            _connection.SendMessage("RESET\r");
        }
        public void Goto(GotoArgument gotoArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CGotoHandler(this, JsonConvert.SerializeObject(gotoArg)));
            }
        }
        public void Goto(object gotoArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CGotoHandler(this, gotoArg.ToString()));
            }
        }

        public void SevoOn()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CSevoOnOffHandler(this, true));
            }
        }
        public void SevoOff()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CSevoOnOffHandler(this, false));
            }
        }

        public void Halt()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CHaltHandler(this));
            }
        }

        public void HomeAxis(string axis)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CHomeAxisHandler(this, axis));
            }
        }
        public void Pick(PickPlaceArgument pickArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CPickHandler(this, JsonConvert.SerializeObject(pickArg)));
            }
        }
        public void Pick(object pickArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CPickHandler(this, pickArg.ToString()));
            }
        }
        public void Place(PickPlaceArgument placeArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CPlaceHandler(this, JsonConvert.SerializeObject(placeArg)));
            }
        }
        public void Place(object placeArg)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CPlaceHandler(this, placeArg.ToString()));
            }
        }

        public void Transfer(string arm, string fromStation, string toStation)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CTransferHandler(this, fromStation, toStation,arm));
            }
        }


        public void Transfer(string fromStation, string toStation)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CTransferHandler(this, fromStation, toStation));
            }
        }

        internal void NoteSetCommEchoCompleted(bool value)
        {
            SetCommEchoCompleted = value;
        }

        public void Retract()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CRetractHandler(this));
            }
        }

        public void SetCommunicationEcho(string echoStatus)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CSetCommunicationEchoHandler(this, echoStatus));
            }
        }

        internal void NoteSetLoadCompleted(bool value)
        {
            SetLoadCompleted = value;
        }

        public void SetLoad(string arm, string status)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasun1500C800CSetLoadHandler(this, arm, status));
            }
        }
        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                string msg = comandArgument == null ? $"{command}\r" : $"{command} {comandArgument}\r";

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CRawCommandHandler) && handler.SendText == msg);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CRawCommandHandler(this, command, comandArgument));
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
        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                string msg = $"{command}\r";

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CRawCommandHandler) && handler.SendText == msg);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CRawCommandHandler(this, command));
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

        public void RequestWaferOnOff(bool isSelected, string load)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CQueryWaferOnOffHandler) && handler.SendText.Contains(" " + load.ToString()));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CQueryWaferOnOffHandler(this, load));
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

        internal void NoteCommEchoStatus(bool echoStatus)
        {
            CommEchoOn = echoStatus;
        }

        public void RequestWaferOnOff(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CQueryWaferOnOffHandler) && handler.SendText == "RQ LOAD\r");
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CQueryWaferOnOffHandler(this, null));
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

        internal void NoteWafeCenData(string requestResponse)
        {
            WafeCenData  = requestResponse;
            var dataArray = requestResponse.Split(' ');
            if(dataArray.Length == 15)
            {
                Offset_R = dataArray[10];
                Offset_T = dataArray[11];
            }
            else
            {
                NoteError("RequestWafeCenData Response Error");
            }
        }

        public void RequestCommunicationEcho(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CRequestCommunicationEchoHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CRequestCommunicationEchoHandler(this));
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

        public void RequestWaferCentData(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasun1500C800CRequestWaferCentDataHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasun1500C800CRequestWaferCentDataHandler(this));
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
        public bool LoadAWaferStatus { get; private set; }
        public bool LoadBWaferStatus { get; private set; }

        public string ArmAPosition { get; private set; }
        public string ArmBPosition { get; private set; }
        public bool IsHalted { get; private set; }
        public bool AllHomed { get; private set; }
        public bool RHomed { get; private set; }
        public bool THomed { get; private set; }
        public bool ZHomed { get; private set; }

        public string LastPickInfo { get; private set; }
        public string LastPlaceInfo { get; private set; }
        public bool ArmRetracted { get; private set; }
        public string Error { get; private set; }
        public bool SevoOnOff { get; private set; }
        public bool CommEchoOn { get; private set; }
        public bool SetCommEchoCompleted { get; private set; }
        public bool SetLoadCompleted { get; private set; }
        public string WafeCenData { get; private set; }
        public string Offset_R { get; private set; }
        public string Offset_T { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();



        public void NoteError(string reason)
        {
            if(reason != null)
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

        internal void NoteArmAPosition(string position)
        {
            ArmAPosition = position;
        }
        internal void NoteArmBPosition(string position)
        {
            ArmBPosition = position;
        }

        internal void NoteIsHalted(bool isHalted)
        {
            IsHalted = isHalted;
        }
        internal void NoteSevoOnOff(bool isOn)
        {
            SevoOnOff = isOn;
        }
        internal void NoteAxisHomed(string parameter)
        {
            if (parameter == null || parameter == "ALL")
            {
                AllHomed = true;
                RHomed = true;
                THomed = true;
                ZHomed = true;
            }
            else if (parameter == "R")
                RHomed = true;
            else if (parameter == "T")
                THomed = true;
            else if (parameter == "Z")
                ZHomed = true;
            else
                NoteError("Wrong 'HOME' parameter");
        }

        internal void NoteWafeOnOff(string arm, bool isOn)
        {
            if(arm == "A")
                LoadAWaferStatus = isOn;
            else
                LoadBWaferStatus = isOn; 
        }
        internal void NoteRawCommandInfo(string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommandName = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        internal void NoteLastPickInfo(string sendText)
        {
            LastPickInfo = sendText;
        }
        internal void NoteLastPlaceInfo(string sendText)
        {
            LastPlaceInfo = sendText;
        }
        internal void NoteRetracted(bool retracted)
        {
            ArmRetracted = retracted;
        }

        #endregion
    }


    public class GotoArgument
    {
        /// <summary>
        /// 
        /// </summary>
        public int Station { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string R { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Z { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Slot { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Arm { get; set; }
    }
    public class PickPlaceArgument
    {

        /// <summary>
        /// 
        /// </summary>
        public int Station { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Slot { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Arm { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Step { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Offset { get; set; }
    }







}
