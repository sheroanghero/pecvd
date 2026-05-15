using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.SiasunPhoenixB
{
    public class RobotSiasunPhoenixB : RobotBaseDevice, IConnection
    {
        public string Address => Connection.Address;
        public bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public string PortStatus { get; set; } = "Closed";
        public Dictionary<string, int> ModuleAssociateStationDic
        {
            get => _moduleAssociateStationDic;
            set
            {
                _moduleAssociateStationDic = value;
            }
        }

        private RobotSiasunPhoenixBTCPConnection _connection;
        public RobotSiasunPhoenixBTCPConnection Connection
        {
            get { return _connection; }
        }

        private R_TRIG _trigError = new R_TRIG();
        private bool _isAlarm;

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        private Dictionary<string, string> _errorCodeReferenceDic;
        private Dictionary<string, int> _moduleAssociateStationDic;

        private object _locker = new object();
        private SCConfigItem _scHomeTimeout;
        private SCConfigItem _scMotionTimeout;
        private SCConfigItem _scBladeSlots;
        private List<ModuleName> _disabledIntlkModules = new List<ModuleName>();

        public int BladeSlots => _scBladeSlots == null ? 1 : _scBladeSlots.IntValue;
        private bool _enableLog;
        private string _scRoot;
        public RobotSiasunPhoenixB(string module, string name, string scRoot, string endof = "\r") : base(module, name)
        {
            _scRoot = scRoot;

            ResetPropertiesAndResponses();

            string deviceIP = "";
            if (string.IsNullOrEmpty(_scRoot))
            {
                deviceIP = SC.GetValue<bool>("System.IsSimulatorMode") ? "127.0.0.1:1103" : SC.GetStringValue($"{Module}.Address");
                _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"{Module}.HomeTimeout");
                _scMotionTimeout = SC.GetConfigItem($"{Module}.MotionTimeout");
                _scBladeSlots = SC.GetConfigItem($"{Module}.BladeSlots");
            }
            else
            {
                deviceIP = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
                _scHomeTimeout = SC.GetConfigItem($"{_scRoot}.{Module}.{Module}.HomeTimeout");
                _scMotionTimeout = SC.GetConfigItem($"{_scRoot}.{Module}.{Module}.MotionTimeout");
                _scBladeSlots = SC.GetConfigItem($"{_scRoot}.{Module}.{Module}.BladeSlots");
            }

            WaferManager.Instance.SubscribeLocation(Name, BladeSlots * 2);
            _connection = new RobotSiasunPhoenixBTCPConnection(deviceIP, endof);
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
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            DATA.Subscribe($"{Module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.Address", () => Address);

            DATA.Subscribe($"{Name}.BladeAEnable", () => BladeAEnable);
            DATA.Subscribe($"{Name}.BladeBEnable", () => BladeBEnable);

            OP.Subscribe($"{Module}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            _moduleAssociateStationDic = new Dictionary<string, int>()
            {
                {$"{ModuleName.PM1 }", 2 },
                {$"{ModuleName.PM2}", 3 },
                {$"{ModuleName.PM3}", 4 },
                {$"{ModuleName.PM4}", 5 },
                {$"{ModuleName.PM5}", 6 },
                {$"{ModuleName.PM6}", 7 },
                {$"{ModuleName.VCEA}", 1 },
                {$"{ModuleName.VCEB}", 8 },
                {$"{ModuleName.Aligner}", 9 },
                {$"{ModuleName.Cooler}", 10 },
                 {$"{ModuleName.PM7}", 11 },
                {$"{ModuleName.LL1}", 3 },//A1=5,A2=6,A both=7
                {$"{ModuleName.LLA}", 1 },//A1=5,A2=6,A both=7
                {$"{ModuleName.LLA}.01", 5 },//A1=5,A2=6,A both=7
                {$"{ModuleName.LLA}.02", 6 },//A1=5,A2=6,A both=7
                {$"{ModuleName.LLA}.Both", 7 },//A1=5,A2=6,A both=7
                {$"{ModuleName.LLB}", 8 },//B1=8,B2=9,B both=10
                {$"{ModuleName.LLB}.01", 8 },//B1=8,B2=9,B both=10
                {$"{ModuleName.LLB}.02", 9 },//B1=8,B2=9,B both=10
                {$"{ModuleName.LLB}.Both", 10 },//B1=8,B2=9,B both=10
            };

            _errorCodeReferenceDic = new Dictionary<string, string>()
            {
                {"901", "Main power EMO" },
                {"902", "Teach pendant EMO" },
                {"962", "Driver RDY sensor disconnect" },
                {"3001", "Robot collision" },
                {"7300", "Rotation sensor forbidden" },
                {"7301", "Extend sensor forbidden" },
                {"2200", "Output port Number not exist" },
                {"3120", "Joint N speed out of range" },
                {"3100", "Joint N position out of range" },
                {"100", "Robot power on fail" },
                {"7307", "GOTO station out of range" },
                {"7308", "Sensor type not support" },
                {"7312", "PICK station out of range" },
                {"7313", "PLACE station out of range" },
                {"7314", "XFER station out of range" },
                {"7315", "REMOVE not support IO type" },
                {"7316", "RQ INTLCK parameter invalid" },
                {"7319", "RQ STN station out of range" },
                {"7320", "wafre(WAF_SEN) parameter not set" },
                {"7321", "wafex(RETRACT_PIN) parameter not set" },
                {"7322", "svlv(SBIT_SVLV_SEN) parameter not set" },
                {"7323", "ens(EX_ENABLE) parameter not set" },
                {"7324", "RQ parameter invalid" },
                {"7325", "SET INTLOCK WAF_SEN parameter invalid" },
                {"7326", "SET INTLOCK RZ parameter invalid" },
                {"7327", "SET INTLOCK parameter invalid" },
                {"7328", "SET IO ECHO parameter invalid" },
                {"7329", "SET IO STATE not support" },
                {"7330", "SET IO parameter invalid" },
                {"7331", "SET STN station out of range" },
                {"7332", "Robot read parameter fail" },
                {"7333", "WAF_SEN parameter invalid" },
                {"7334", "SET sensor type not support" },
                {"7335", "SET parameter invalid" },
                {"7336", "STORE IO parameter invalid" },
                {"7337", "STORE LOAD parameter invalid" },
                {"7338", "STORE STN station out of range" },
                {"7339", "STORE STN parameter invalid" },
                {"7340", "STORE sensor type not support" },
                {"7341", "STORE parameter invalid" },
                {"7342", "Command invalid" },
                {"7343", "HOME parameter not set" },
                {"7344", "GOTO R axis parameter not set" },
                {"7345", "GOTO Z axis parameter not set" },
                {"7346", "ARM parameter invalid" },
                {"7347", "GOTO parameter not set" },
                {"7349", "MOVE parameter not set" },
                {"7350", "MOVE parameter invalid" },
                {"7351", "PICK parameter not set" },
                {"7352", "PLACE parameter not set" },
                {"7353", "REMOVE parameter not set" },
                {"7354", "Command excute not finished" },
                {"7355", "GOTO station parameter not set" },
                {"7356", "PICK station parameter not set" },
                {"7357", "PLACE station parameter not set" },
                {"7358", "ABS parameter not set" },
                {"7359", "REL parameter not set" },
                {"7360", "Robot not set main thread" },
                {"7361", "Robot not open main thread" },
                {"7362", "Current thread not main thread" },
                {"7363", "ex_ena(EX_ENABLE_SEN) parameter not set" },
                {"7364", "stable(STABLE_ SIGNAL) parameter not set" },
                {"7365", "MOVE parameter invalid" },
                {"7366", "System not on" },
                {"7367", "extend(EX_SIGNAL) parameter not set" },
                {"7368", "retract(RE_ SIGNAL) parameter not set" },
                {"7371", "Wafer not present before place" },
                {"7372", "Wafer present before pick" },
                {"7373", "Wafer present after place" },
                {"7374", "Wafer not present after pick" },
                {"7375", "Servo not on" },
                {"7376", "Exist not number parameter" },
                {"7385", "Driver stop abnormal" },
                {"7387", "Driver ID1 alarm" },
                {"7388", "Driver ID2 alarm" },
                {"7389", "Driver ID3 alarm" },
                {"7391", "AWC station out of range" },
                {"7392", "AWC out of range alarm" },
                {"7393", "AWC sensor set fail" },
                {"7398", "AWC calculate fail" },
                {"7399", "AWC trig number error" },
                {"7401", "Wafer maybe present on blade" },
                {"7402", "Wafer maybe not present on blade" },
                {"7403", "Load status in ON, not correct" },
                {"7404", "Load status in OFF, not correct" },
                {"7405", "Slot not exist" },
                {"7495", "ID1 encoder error" },
                {"7496", "ID2 encoder error" },
                {"7497", "ID3 encoder error" },
            };
        }
        private void ResetPropertiesAndResponses()
        {
            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }


        public override bool BladeAEnable
        {
            get
            {


                return SC.GetValue<bool>($"TMRobot.BladeAEnable");
            }
           
        }
        public override bool BladeBEnable
        {
            get
            {

                return SC.GetValue<bool>($"TMRobot.BladeBEnable");
            }

        }

        public override bool IsReady()
        {
            return ((!_connection.IsBusy) && (_lstHandler.Count == 0) && (!_connection.IsCommunicationError) && (!IsBusy) && (RobotState == RobotStateEnum.Idle)) && !_isAlarm;
        }

        protected override bool Init()
        {
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
                            //_lstHandler.AddLast(new RobotSiasunPhoenixBQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RobotSiasunPhoenixBSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        protected override void SubscribeWaferLocation()
        {
            //do nothing
        }

        public override void Reset()
        {
            _trigError.RST = true;

            IsBusy = false;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            _trigWarningMessage.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public void SetDisabledIntlkModules(IEnumerable<ModuleName> units)
        {
            _disabledIntlkModules.AddRange(units);
        }

        #region Command Functions
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBRawCommandHandler(this, command));
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
                //_lstHandler.AddLast(new RobotSiasunPhoenixBGotoHandler(this, JsonConvert.SerializeObject(gotoArg)));
            }
        }
        public void Goto(object gotoArg)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBGotoHandler(this, gotoArg.ToString()));
            }
        }

        public void SevoOn()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBSevoOnOffHandler(this, true));
            }
        }
        public void SevoOff()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBSevoOnOffHandler(this, false));
            }
        }

        public void Halt()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBHaltHandler(this));
            }
        }

        public void HomeAxis(string axis)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBHomeAxisHandler(this, axis));
            }
        }
        public void Pick(PickPlaceArgument pickArg)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBPickHandler(this, JsonConvert.SerializeObject(pickArg)));
            }
        }
        public void Pick(object gotoArg)
        {
            lock (_locker)
            {
                // _lstHandler.AddLast(new RobotSiasunPhoenixBPickHandler(this, gotoArg.ToString()));
            }
        }
        public void Place(PickPlaceArgument placeArg)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBPlaceHandler(this, JsonConvert.SerializeObject(placeArg)));
            }
        }
        public void Place(object placeArg)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBPlaceHandler(this, placeArg.ToString()));
            }
        }

        public void Transfer(string arm, string fromStation, string toStation)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBTransferHandler(this, fromStation, toStation, arm));
            }
        }


        public void Transfer(string fromStation, string toStation)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBTransferHandler(this, fromStation, toStation));
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
                _lstHandler.AddLast(new RobotSiasunPhoenixBRetractHandler(this, _scMotionTimeout.IntValue));
            }
        }

        public void SetCommunicationEcho(string echoStatus)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBSetCommunicationEchoHandler(this, echoStatus));
            }
        }

        public void SetLoad(string arm, string status)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBSetLoadHandler(this, arm, status));
            }
        }
        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                string msg = comandArgument == null ? $"{command}\r" : $"{command} {comandArgument}\r";

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBRawCommandHandler) && handler.SendText == msg);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBRawCommandHandler(this, command, comandArgument));
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

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBRawCommandHandler) && handler.SendText == msg);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBRawCommandHandler(this, command));
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
                //var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBQueryWaferOnOffHandler) && handler.SendText.Contains(" " + load.ToString()));
                //if (isSelected)
                //{
                //    if (!existHandlers.Any())
                //        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBQueryWaferOnOffHandler(this, load));
                //}
                //else
                //{
                //    if (existHandlers.Any())
                //    {
                //        _lstMonitorHandler.Remove(existHandlers.First());
                //    }
                //}
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
                //var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBQueryWaferOnOffHandler) && handler.SendText == "RQ LOAD\r");
                //if (isSelected)
                //{
                //    if (!existHandlers.Any())
                //        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBQueryWaferOnOffHandler(this, null));
                //}
                //else
                //{
                //    if (existHandlers.Any())
                //    {
                //        _lstMonitorHandler.Remove(existHandlers.First());
                //    }
                //}
            }
        }

        internal void NoteWafeCenData(string requestResponse)
        {
            WafeCenData = requestResponse;
            var dataArray = requestResponse.Split(' ');
            if (dataArray.Length == 15)
            {
                Offset_R = dataArray[10];
                Offset_T = dataArray[11];

                EV.PostInfoLog(Module, $"AWC CenterOffsetR: {Offset_R} , CenterOffsetT: {Offset_T}");
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
                //var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBRequestCommunicationEchoHandler));
                //if (isSelected)
                //{
                //    if (!existHandlers.Any())
                //        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBRequestCommunicationEchoHandler(this));
                //}
                //else
                //{
                //    if (existHandlers.Any())
                //    {
                //        _lstMonitorHandler.Remove(existHandlers.First());
                //    }
                //}
            }
        }

        public void RequestWaferCentData(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RobotSiasunPhoenixBRequestWaferCentDataHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RobotSiasunPhoenixBRequestWaferCentDataHandler(this));
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
        public bool SevoOnOff { get; private set; }
        public bool CommEchoOn { get; private set; }
        public bool SetCommEchoCompleted { get; private set; }
        public string WafeCenData { get; private set; }
        public string Offset_R { get; private set; }
        public string Offset_T { get; private set; }

        #endregion

        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                var content = reason;
                if (_errorCodeReferenceDic.ContainsKey(reason))
                    content = _errorCodeReferenceDic[reason];
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason} {content}");
                }
                _isAlarm = true;
                ErrorCode = reason;
            }
            else
            {
                _isAlarm = false;
            }
        }

        internal void NoteSevoOnOff(bool isOn)
        {
            SevoOnOff = isOn;
        }
        internal void NoteActionCompleted()
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.ActionDone);
        }
        internal void NoteAxisHomed()
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.InitComplete);
        }
        internal void NoteReadDataComplete()
        {
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.ReadDataComplete);
        }
        internal void NoteSetParametersComplete()
        {
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.SetParametersComplete);
        }
        internal void NoteWafeOnOff(string arm, bool isOn, bool isUnknown)
        {
            IsBusy = false;
            if (isUnknown)
            {
                if (arm == "A")
                {
                    IsWaferPresenceOnBlade1 = false;
                    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Module), 0);
                    EV.PostWarningLog(Module, $"{Module}.{Name} arm {arm} status is unkonwn");
                }

                if (arm == "B")
                {
                    IsWaferPresenceOnBlade2 = false;
                    WaferManager.Instance.DeleteWafer(ModuleHelper.Converter(Module), 1);
                    EV.PostWarningLog(Module, $"{Module}.{Name} arm {arm} status is unkonwn");
                }
            }
            else
            {
                if (arm == "A")
                {
                    IsWaferPresenceOnBlade1 = isOn;
                }
                else
                {
                    IsWaferPresenceOnBlade2 = isOn;
                }
            }

        }
        internal void NoteRawCommandInfo(string command, string data)
        {
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommandName = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                //_lstHandler.AddLast(new RobotSiasunPhoenixBSetCommunicationEchoHandler(this, false));
                //_lstHandler.AddLast(new RobotSiasunPhoenixBSevoOnOffHandler(this, true));
                _lstHandler.AddLast(new RobotSiasunPhoenixBHomeAxisHandler(this, _scHomeTimeout.IntValue));
            }
            return true;
        }

        protected override bool fClear(object[] param)
        {
            //lock (_locker)
            //{
            //    _lstHandler.AddLast(new RobotSiasunPhoenixBResetHandler(this));
            //}

            return true;
        }

        public override bool ReadParameter(object[] param)
        {
            IsBusy = true;
            return CheckToPostMessage((int)RobotMsg.ReadData, param);
        }

        public override bool GoTo(object[] param)
        {
            if (param.Length < 1) return false;
            string command = param[0].ToString();
            var length = param.Length;
            var args = param.Skip(1).Take(length - 1).ToArray();
            if (!IsReady())
                return false;
            switch (command)
            {
                case "GoTo":
                    if (PostMessageWithoutCheck((int)RobotMsg.GoToPosition, args))
                    {
                        IsBusy = true;
                        string log = "";
                        foreach (var obj in args)
                        {
                            log += obj.ToString() + ",";
                        }
                        LOG.Write($"{RobotModuleName} start go to {log}");
                        return true;
                    }
                    break;
                case "ExtendForPick":
                    if (PostMessageWithoutCheck((int)RobotMsg.ExtendForPick, args))
                    {
                        IsBusy = true;
                        string log = "";
                        foreach (var obj in args)
                        {
                            log += obj.ToString() + ",";
                        }
                        LOG.Write($"{RobotModuleName} start extend for pick {log}");
                        return true;
                    }
                    break;
                case "ExtendForPlace":
                    if (PostMessageWithoutCheck((int)RobotMsg.ExtendForPlace, args))
                    {
                        IsBusy = true;
                        string log = "";
                        foreach (var obj in args)
                        {
                            log += obj.ToString() + ",";
                        }
                        LOG.Write($"{RobotModuleName} start extend for place {log}");
                        return true;
                    }
                    break;
                case "RetractFromPick":
                    if (PostMessageWithoutCheck((int)RobotMsg.RetractFromPick, args))
                    {
                        IsBusy = true;
                        string log = "";
                        foreach (var obj in args)
                        {
                            log += obj.ToString() + ",";
                        }
                        LOG.Write($"{RobotModuleName} start retract from pick {log}");
                        return true;
                    }
                    break;
                case "RetractFromPlace":
                    if (PostMessageWithoutCheck((int)RobotMsg.RetractFromPlace, args))
                    {
                        IsBusy = true;
                        string log = "";
                        foreach (var obj in args)
                        {
                            log += obj.ToString() + ",";
                        }
                        LOG.Write($"{RobotModuleName} start retract from place {log}");
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        protected override bool fStartReadData(object[] param)
        {
            if (param.Length < 1) return false;
            string readcommand = param[0].ToString();
            switch (readcommand)
            {
                case "QueryWaferPresent":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        _lstHandler.AddLast(new RobotSiasunPhoenixBQueryWaferPresentHandler(this, arm));
                    }
                    break;
                case "QueryWaferCentData":
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new RobotSiasunPhoenixBRequestWaferCentDataHandler(this));
                    }
                    break;
                case "CheckLoad":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        int checkLoadStation = 12;
                        _lstHandler.AddLast(new RobotSiasunPhoenixBCheckLoadHandler(this, checkLoadStation, arm, _scHomeTimeout.IntValue * 2));
                    }
                    break;
                case "CheckLoadAB":
                    lock (_locker)
                    {
                        int checkLoadStation = 11;
                        _lstHandler.AddLast(new RobotSiasunPhoenixBCheckLoadABHandler(this, checkLoadStation, _scHomeTimeout.IntValue * 2));
                    }
                    break;
                case "CheckLoad1":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        int checkLoadStation = 20;
                        _lstHandler.AddLast(new RobotSiasunPhoenixBCheckLoadHandler(this, checkLoadStation, arm, _scHomeTimeout.IntValue * 2));
                    }
                    break;
                case "CheckLoad2":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        int checkLoadStation = 12;
                        _lstHandler.AddLast(new RobotSiasunPhoenixBCheckLoadHandler(this, checkLoadStation, arm, _scHomeTimeout.IntValue * 2));
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        protected override bool fStartSetParameters(object[] param)
        {
            if (param.Length < 1) return false;
            string readcommand = param[0].ToString();
            switch (readcommand)
            {
                case "SetLoad":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        bool.TryParse(param[2].ToString(), out bool isOn);
                        _lstHandler.AddLast(new RobotSiasunPhoenixBSetLoadHandler(this, arm, isOn));
                        //_lstHandler.AddLast(new RobotSiasunPhoenixBStoreLoadHandler(this, arm));
                    }
                    break;
                case "StoreLoad":
                    lock (_locker)
                    {
                        RobotArmEnum arm = (RobotArmEnum)param[1];
                        
                        _lstHandler.AddLast(new RobotSiasunPhoenixBStoreLoadHandler(this, arm));
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            return true;
        }

        //(arm, module, slot, isRetract, isZaxisDown)
        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (arm == RobotArmEnum.Blade1)
                    Blade1Target = module;
                if (arm == RobotArmEnum.Blade2)
                    Blade2Target = module;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;
                if (BladeSlots == 1)
                {
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;
                    //if (param.Length >= 3)
                    //    slot = (int)param[2] + 1;
                }
                else if (BladeSlots == 2)
                {
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;

                    slot = (int)param[2] / 2 + 1;
                }

                bool isRetract = true;
                bool isZaxisDown = true;
                if (param.Length >= 5)
                {
                    bool.TryParse(param[3].ToString(), out isRetract);
                    bool.TryParse(param[4].ToString(), out isZaxisDown);
                }

                int station = 0;
                if (!_moduleAssociateStationDic.ContainsKey(module.ToString()))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                station = _moduleAssociateStationDic[module.ToString()];

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBGotoHandler(this, module, slot, arm, isRetract, isZaxisDown, _scMotionTimeout.IntValue));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartMapWafer(object[] param)
        {
            return false;
        }

        protected override bool fStartSwapWafer(object[] param)
        {
            return false;
        }

        //(arm, module, slot, ro, to)
        protected override bool fStartPlaceWafer(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = ModuleName.System;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                if (BladeSlots == 1)
                {
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;
                    //if (param.Length >= 3)
                    //    slot = (int)param[2] + 1;
                }
                else if (BladeSlots == 2)
                {
                    var paramArray = param[1].ToString().Split('.');
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), paramArray[0].ToString());
                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;

                    if (paramArray.Length > 1)
                    {
                        slot = (int)param[2] / 2 + 1;
                    }
                }

                int ro = 0;
                int to = 0;
                if (param.Length >= 5)
                {
                    int.TryParse(param[3].ToString(), out ro);
                    int.TryParse(param[4].ToString(), out to);
                }

                bool isDisableInterlock = _disabledIntlkModules.Contains(module);

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPlaceHandler(this,
                        ModuleHelper.IsLoadLock(module) ? param[1].ToString() : module.ToString(),
                        slot, arm, ro, to, _scMotionTimeout.IntValue, isDisableInterlock));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        //(arm, module, slot, ro, to)
        protected override bool fStartPickWafer(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = ModuleName.System;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                if (BladeSlots == 1)
                {
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;
                    //if (param.Length >= 3)
                    //    slot = (int)param[2] + 1;
                }
                else if (BladeSlots == 2)
                {
                    var paramArray = param[1].ToString().Split('.');
                    module = (ModuleName)Enum.Parse(typeof(ModuleName), paramArray[0].ToString());
                    if (arm == RobotArmEnum.Blade1)
                        Blade1Target = module;
                    if (arm == RobotArmEnum.Blade2)
                        Blade2Target = module;

                    if (paramArray.Length > 1)
                    {
                        slot = (int)param[2] / 2 + 1;
                    }
                }

                float ro = 0;
                float to = 0;
                if (param.Length >= 5)
                {
                    float.TryParse(param[3].ToString(), out ro);
                    float.TryParse(param[4].ToString(), out to);
                }

                bool isDisableInterlock = _disabledIntlkModules.Contains(module);

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPickHandler(this,
                        ModuleHelper.IsLoadLock(module) ? param[1].ToString() : module.ToString(),
                        slot, arm, (int)ro, (int)to, _scMotionTimeout.IntValue, isDisableInterlock));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fPlaceComplete(object[] param)
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;

            int Sourceslotindex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
            if (BladeSlots == 1)
            {
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                if (arm == RobotArmEnum.Blade1)
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                if (arm == RobotArmEnum.Blade2)
                    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
            }
            else if (BladeSlots == 2)
            {
                var array = CurrentParamter[1].ToString().Split('.');
                if (!Enum.TryParse(array[0].ToString(), out sourcemodule)) return false;
                if (ModuleHelper.IsPm(sourcemodule))
                {
                    if (arm == RobotArmEnum.Blade1)
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, 0);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, 1);
                    }
                    if (arm == RobotArmEnum.Blade2)
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 2, sourcemodule, 0);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 3, sourcemodule, 1);
                    }
                }

                if (ModuleHelper.IsLoadLock(sourcemodule))
                {
                    if (arm == RobotArmEnum.Blade1)
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex / 2 * 2);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex / 2 * 2 + 1);
                    }
                    if (arm == RobotArmEnum.Blade2)
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 2, sourcemodule, Sourceslotindex / 2 * 2);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 3, sourcemodule, Sourceslotindex / 2 * 2 + 1);
                    }
                }
            }

            return base.fPlaceComplete(param);
        }

        protected override bool fPickComplete(object[] param)
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;

            int SourceslotIndex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
            if (BladeSlots == 1)
            {
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                if (arm == RobotArmEnum.Blade1)
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                if (arm == RobotArmEnum.Blade2)
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
            }
            else if (BladeSlots == 2)
            {
                var array = CurrentParamter[1].ToString().Split('.');
                if (!Enum.TryParse(array[0].ToString(), out sourcemodule)) return false;
                if (ModuleHelper.IsPm(sourcemodule))
                {
                    if (arm == RobotArmEnum.Blade1)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, 0, RobotModuleName, 0);
                        WaferManager.Instance.WaferMoved(sourcemodule, 1, RobotModuleName, 1);
                    }
                    if (arm == RobotArmEnum.Blade2)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, 0, RobotModuleName, 2);
                        WaferManager.Instance.WaferMoved(sourcemodule, 1, RobotModuleName, 3);
                    }
                }

                if (ModuleHelper.IsLoadLock(sourcemodule))
                {
                    if (arm == RobotArmEnum.Blade1)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex / 2 * 2, RobotModuleName, 0);
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex / 2 * 2 + 1, RobotModuleName, 1);
                    }
                    if (arm == RobotArmEnum.Blade2)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex / 2 * 2, RobotModuleName, 2);
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex / 2 * 2 + 1, RobotModuleName, 3);
                    }
                }
            }

            return base.fPickComplete(param);
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;
            if (_isAlarm)
            {
                //lock (_locker)
                //{
                //    _lstHandler.AddLast(new RobotSiasunPhoenixBResetHandler(this));
                //}

                //Thread.Sleep(100);
                CheckToPostMessage((int)RobotMsg.ActionDone);
            }

            IsBusy = false;

            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RobotSiasunPhoenixBSetCommunicationEchoHandler(this, false));
                if (!SevoOnOff)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBSevoOnOffHandler(this, true));
                }
                _lstHandler.AddLast(new RobotSiasunPhoenixBHomeAxisHandler(this, _scHomeTimeout.IntValue));
            }
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPick(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (arm == RobotArmEnum.Blade1)
                    Blade1Target = module;
                if (arm == RobotArmEnum.Blade2)
                    Blade2Target = module;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                int station = 0;
                if (!_moduleAssociateStationDic.ContainsKey(module.ToString()))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                station = _moduleAssociateStationDic[module.ToString()];

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPickExtendHandler(this, module, slot, arm, 0, 0, _scMotionTimeout.IntValue));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (arm == RobotArmEnum.Blade1)
                    Blade1Target = module;
                if (arm == RobotArmEnum.Blade2)
                    Blade2Target = module;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                int station = 0;
                if (!_moduleAssociateStationDic.ContainsKey(module.ToString()))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                station = _moduleAssociateStationDic[module.ToString()];

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPlaceExtendHandler(this, module, slot, arm, 0, 0, _scMotionTimeout.IntValue));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (arm == RobotArmEnum.Blade1)
                    Blade1Target = module;
                if (arm == RobotArmEnum.Blade2)
                    Blade2Target = module;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                int station = 0;
                if (!_moduleAssociateStationDic.ContainsKey(module.ToString()))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                station = _moduleAssociateStationDic[module.ToString()];

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPickRetractHandler(this, module, slot, arm, 0, 0, _scMotionTimeout.IntValue));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (arm == RobotArmEnum.Blade1)
                    Blade1Target = module;
                if (arm == RobotArmEnum.Blade2)
                    Blade2Target = module;

                int slot = 1;
                //if (param.Length >= 3)
                //    slot = (int)param[2] + 1;

                int station = 0;
                if (!_moduleAssociateStationDic.ContainsKey(module.ToString()))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                station = _moduleAssociateStationDic[module.ToString()];

                lock (_locker)
                {
                    _lstHandler.AddLast(new RobotSiasunPhoenixBPlaceRetractHandler(this, module, slot, arm, 0, 0, _scMotionTimeout.IntValue));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Blade1)
            {
                return IsWaferPresenceOnBlade1 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if (arm == RobotArmEnum.Blade2)
            {
                return IsWaferPresenceOnBlade2 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }

            return RobotArmWaferStateEnum.Unknown;
        }

        protected override bool fStop(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                _connection.ForceClear();
                _lstHandler.Clear();
            }
            return true;
        }

        protected override bool fAbort(object[] param)
        {
            lock (_locker)
            {
                IsBusy = false;
                _connection.ForceClear();
                _lstHandler.Clear();
                _lstHandler.AddLast(new RobotSiasunPhoenixBHaltHandler(this));
            }
            return true;
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
