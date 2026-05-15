using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.HineAutomation
{
    public class HAtmRobot : Robot
    {
        #region properties
        public override bool Error
        {
            get
            {
                return _exceuteErr;
            }
        }

        public bool IsThetaBusy { get; set; }
        public bool IsZBusy { get; set; }
        public bool IsRadialBusy { get; set; }

        public override bool Busy
        {
            get
            {
                return IsBusy || IsThetaBusy || IsZBusy || IsRadialBusy || (!_motionTimer.IsIdle() && _motionTimer.GetElapseTime() < _motionTime) /*|| ThetaAxisAddressPosition != _targetThetaAxisAddressPosition || ZAxisAddressPosition != _targetZAxisAddressPosition*/;
            }
        }

        public override bool Moving
        {
            get
            {
                return IsBusy || IsThetaBusy || IsZBusy || IsRadialBusy || (!_motionTimer.IsIdle() && _motionTimer.GetElapseTime() < _motionTime) /*|| ThetaAxisAddressPosition != _targetThetaAxisAddressPosition || ZAxisAddressPosition != _targetZAxisAddressPosition*/;
            }
        }

        public override bool WaferOnBlade1 => WaferPresentOnBlade1;

        public bool IsBusy { private get; set; }
        public bool IsRetract { get;   set; }
        public bool IsUp { get;   set; }
        public int ThetaAxisAddressPosition { get; set; }
        public int ZAxisAddressPosition { get; set; }
        public int RetractReqPos { get; set; }
        public int RotateReqPos { get; set; }
        public int ZReqPos { get; set; }
        public int ZActPos { get; set; }
        public string SlotMap { get; set; }
        public Dictionary<string, string> ErrorCodeReference;
        public R_TRIG IsRotationAxisUnreferencedTrig { get; set; }
        public R_TRIG IsExtensionAxisUnreferencedTrig { get; set; }
        public R_TRIG IsZAxisUnreferencedTrig { get; set; }
        public bool IsHomed => _isHomed;
        #endregion

        #region fields
        private HAtmConnection _connection;
        private string _address;
        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _lockerHandlerList = new object();

        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();
        private string _lastError = string.Empty;
        private int _errorCode;

        private int _targetThetaAxisAddressPosition;
        private int _targetZAxisAddressPosition;
        private HandlerBase _waferMoveHandler;
        private HandlerBase _homeHandler;
        private bool _isHomed;
        private bool _isSimulator;
        //private bool _isFirstHome = true;
        private bool _enableLog;
        private bool _isPowerUp = true;

        private DeviceTimer _motionTimer = new DeviceTimer();
        private int _motionTime = 1000;
        private Dictionary<string, int> _motionTimeDic;
        private ModuleName _lastTarget;

        private R_TRIG _zAxisHomeTrig = new R_TRIG();
        #endregion


        public HAtmRobot(string module)
            : base(module, module, module, module, "", RobotType.HAtm)
        {
            IsRetract = true;
        }
        static bool CheckAddressPort(string s)
        {
            bool isLegal;
            Regex regex = new Regex(@"^((2[0-4]\d|25[0-5]|[1]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[1]?\d\d?)\:([1-9]|[1-9][0-9]|[1-9][0-9][0-9]|[1-9][0-9][0-9][0-9]|[1-6][0-5][0-5][0-3][0-5])$");//CheckAddressPort
            Match match = regex.Match(s);
            if (match.Success)
            {
                isLegal = true;
            }
            else
            {
                isLegal = false;
            }
            return isLegal;
        }
        public override bool Initialize()
        {
            _address = SC.GetStringValue($"{Module}.Address");
            _isSimulator = SC.GetValue<bool>("System.IsSimulatorMode");
            _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");

            _connection = new HAtmConnection(_address);
            if (CheckAddressPort(_address))
            {
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module} connected");
                }
            }
            else EV.PostWarningLog(Module, $"{Module} Adress is illegal");

            _thread = new PeriodicJob(50, OnTimer, $"{Module} MonitorHandler", true);

            DATA.Subscribe($"{Name}.IsHomed", () => _isHomed);
            DATA.Subscribe($"{Name}.IsConnected", () => _connection.IsConnected);

            OP.Subscribe("Robot.QueryOperationalStatus", (string cmd, object[] args) =>
            {
                QueryOperationalStatus();
                return true;
            });

            _motionTimeDic = new Dictionary<string, int>()
            {
                { $"{ModuleName.Cassette}.{ModuleName.PM}.Picking",4000},
                { $"{ModuleName.PM}.{ModuleName.PM}.Picking",1500},
                { $"{ModuleName.PM}.{ModuleName.Cooling}.Picking",2400},
                { $"{ModuleName.Cooling}.{ModuleName.Cooling}.Picking",1500},
                { $"{ModuleName.Cooling}.{ModuleName.PM}.Picking",2400},
                { $"{ModuleName.Cassette}.{ModuleName.Cooling}.Picking",2200},
                { $"{ModuleName.Cooling}.{ModuleName.Cassette}.Picking",2200},
                { $"{ModuleName.Cassette}.{ModuleName.Cassette}.Picking",1500},
                { $"{ModuleName.PM}.{ModuleName.Cassette}.Picking",4000},
                { $"{ModuleName.TMRobot}.{ModuleName.Cassette}.Picking",5000},
                { $"{ModuleName.TMRobot}.{ModuleName.Cooling}.Picking",3700},
                { $"{ModuleName.TMRobot}.{ModuleName.PM}.Picking",2200},
                { $"System.{ModuleName.Cassette}.Picking",5000},
                { $"System.{ModuleName.Cooling}.Picking",3700},
                { $"System.{ModuleName.PM}.Picking",2200},
            };

            ErrorCodeReference = new Dictionary<string, string>()
            {
                { "S0","Serial response timeout"},
                { "S1","Illegal sc interrupt"},
                { "S2","Parity Error"},
                { "S3","Input Buffer overflow"},
                { "C0","Command not recognized"},
                { "C1","Illegal Z-Axis Command Address"},
                { "C2","Illegal Radial Axis Command Address"},
                { "C3","Illegal Theta Axis Command Address"},
                { "C4","Illegal Pick – wafer on pan"},
                { "C5","Illegal Place – wafer not on pan"},
                { "C6","Illegal action command – error present"},
                { "C7","Command overrun – Hatm5.0 Busy"},
                { "C8","Illegal command – not in learn mode"},
                { "C9","Illegal Search – hardware not present"},
                { "CA","Illegal Fetch – hardware not present"},
                { "CB","Illegal Action – data not valid"},
                { "CC","Illegal Action – Request Queue overrun"},
                { "CD","Illegal Action – Overcurrent Latch Set"},
                { "CE","Illegal Action – Interlock Open"},
                { "CF","Loss of Vacuum Detect"},
                { "D0","Illegal Pitch"},
                { "D1","Illegal Offset"},
                { "D2","Illegal number of slots"},
                { "D3","Illegal rotate position"},
                { "D4","Illegal difference value"},
                { "D5","Parameter missing"},
                { "D6","Illegal Set Command"},
                { "D7","Illegal Option"},
                { "D8","Illegal Category"},
                { "D9","Illegal Bias"},
                { "DA","Illegal Bias Set, Z not valid"},
                { "DB","Invalid rotational speed data(exceeded the legal percent range)"},
                { "DC","Invalid z-axis speed data (exceeded the legal percent range)"},
                { "I0","Z requested position is out of range"},
                { "I1","Theta requested position is out of range"},
                { "I2","Radial requested position is out of range"},
                { "I3","Illegal Z Position"},
                { "I4","Illegal Theta Position"},
                { "I5","Illegal Data"},
                { "A0","Theta Axis action timeout"},
                { "A1","Z-Axis action timeout"},
                { "A2","Radial Axis action timeout"},
                { "A3","Theta overtravel"},
                { "A4","Theta Axis reference detect"},
                { "A5","Z-Axis reference detect"},
                { "A6","Function timeout"},
                { "A7","Loss of Retrace on Rotate"},
                { "A8","Cassette Interlock error"},
                { "A9","Overcurrent shutdown"},
                { "P0","Bad Checksum – Theta Parameters"},
                { "P1","Bad Checksum – Z Parameters"},
                { "P2","Bad Checksum – Radial Parameters"},
                { "F0","Flipper current limit activated"},
                { "F1","Flipper device error"},
                { "F2","Flipper over voltage"},
                { "F3","Flipper over temperature"},
                { "F4","Flipper continuous current limit"},
                { "F5","No Flipper is installed"},
            };

            IsRotationAxisUnreferencedTrig = new R_TRIG();
            IsExtensionAxisUnreferencedTrig = new R_TRIG();
            IsZAxisUnreferencedTrig = new R_TRIG();

            return base.Initialize();
        }

        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();



                _connection.MonitorConnection(SC.GetValue<int>("TMRobot.ReconnectCount"), out bool retried);

                if (retried)
                {
                    lock (_lockerHandlerList)
                    {
                        _lstHandler.Clear();
                    }
                }
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    return true;
                }
                if (_connection.IsBusy)
                    return true;

                HandlerBase handler = null;

                lock (_lockerHandlerList)
                {
                    if (_isPowerUp)
                    {
                        if(SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse"))
                            _lstHandler.AddFirst(new SetCommandResponseHandler(this));
                        _isPowerUp = false;
                    }
                    if (_lstHandler.Count == 0)
                    {
                        _lstHandler.AddLast(new QueryOperationalStatusHandler(this));
                        _lstHandler.AddLast(new QueryAllAxisStatusHandler(this));
                        _lstHandler.AddLast(new QueryThetaAxisStatusHandler(this));
                        _lstHandler.AddLast(new QueryZAxisStatusHandler(this));
                        _lstHandler.AddLast(new QueryRadialAxisStatusHandler(this));
                        _lstHandler.AddLast(new QueryPositionHandler(this));
                    }

                    if (_lstHandler.Count > 0)
                    {
                        handler = _lstHandler.First.Value;

                        _lstHandler.RemoveFirst();
                    }
                }

                if (handler != null)
                {
                    handler.SendText = ((HAtmHandler)handler).Package();
                    _connection.Execute(handler);
                }

                if (_waferMoveHandler != null)
                {
                    var pick = _waferMoveHandler as PickHandler;
                    if (pick != null && WaferOnBlade1 && WaferManager.Instance.CheckHasWafer(pick.Target, pick.Slot))
                    {
                        WaferManager.Instance.WaferMoved(pick.Target, pick.Slot, ModuleHelper.Converter(Name), (int)Hand.Blade1);
                        CmdBladeTarget = $"{pick.Target}.Retract";

                    }

                    var place = _waferMoveHandler as PlaceHandler;
                    if (place != null && !WaferOnBlade1 && WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(Name), (int)Hand.Blade1))
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(Name), (int)Hand.Blade1, place.Target, place.Slot);
                        CmdBladeTarget = $"{place.Target}.Retract";
                    }
                }

                if (_waferMoveHandler != null && !Moving)
                {
                    _lastTarget = ((HAtmHandler)_waferMoveHandler).Target;
                    ((HAtmHandler)_waferMoveHandler).Update();
                    _waferMoveHandler = null;
                    _motionTimer.Stop();
                }

                if (_homeHandler != null)
                {
                    _zAxisHomeTrig.CLK = ZReqPos == 0;
                    if(_zAxisHomeTrig.Q)//means ZAXis is homed
                    {
                        ChangeRobotBladeTarget();
                    }
                    
                    if (!Moving)
                    {
                        _lastTarget = ((HAtmHandler)_homeHandler).Target;
                        _isHomed = true;
                        _homeHandler = null;
                        _motionTimer.Stop();
                    }
                }
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        private void ChangeRobotBladeTarget()
        {
            //CmdBladeTarget = $"{ModuleHelper.Converter(RobotDevice.Name)}.Retract";
            int cassette = 180;
            int cooling = 110;
            int pm = 55;
            if(Math.Abs(RotateReqPos - cassette) < 10)
            {
                CmdBladeTarget = $"{ModuleName.Cassette}.{ModuleHelper.Converter(Name)}.Retract";
                return;
            }

            if (Math.Abs(RotateReqPos - cooling) < 10)
            {
                CmdBladeTarget = $"{ModuleName.Cooling}.{ModuleHelper.Converter(Name)}.Retract";
                return;
            }

            if (Math.Abs(RotateReqPos - pm) < 10)
            {
                CmdBladeTarget = $"{ModuleName.PM}.{ModuleHelper.Converter(Name)}.Retract";
                return;
            }

            if (Math.Abs(RotateReqPos) < 10)
            {
                CmdBladeTarget = $"{ModuleHelper.Converter(Name)}.{ModuleHelper.Converter(Name)}.Retract";
                return;
            }


            CmdBladeTarget = $"{ModuleHelper.Converter(Name)}.Retract";
        }

        public override void Monitor()
        {
            
        }

        public override void Reset()
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            IsRotationAxisUnreferencedTrig.RST = true;
            IsExtensionAxisUnreferencedTrig.RST = true;
            IsZAxisUnreferencedTrig.RST = true;

            _connection.Reset();

            _exceuteErr = false;

            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new ResetHandler(this));
            }

            _motionTimer.Stop();

            _zAxisHomeTrig.RST = true;

            _isHomed = false;

            base.Reset();
        }

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }

        public void SetError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module} error, {reason}");
                lock (_lockerHandlerList)
                {
                    _lstHandler.AddFirst(new QueryErrorHandler(this));
                }
            }

            _exceuteErr = true;
        }

        public override void OnDataChanged(string package)
        {

        }


        public override bool Home(out string reason)
        {
            reason = string.Empty;
            var handler = new HomeHandler(this);
            //if (_lstHandler.Count > 0 && Moving && !_isFirstHome)
            //{
            //    reason = "Robot busy, please wait or abort.";
            //    EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

            //    return false;
            //}

            SetBusy();

            _exceuteErr = false;
            _targetThetaAxisAddressPosition = 0;
            _targetZAxisAddressPosition = 0;
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
                if(SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse"))
                    _lstHandler.AddFirst(new SetCommandResponseHandler(this));
            }

            _homeHandler = handler;
            _isHomed = false;
            //_isFirstHome = false;

            _enableLog = SC.GetValue<bool>($"{Module}.EnableLogMessage");

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);
            _zAxisHomeTrig.RST = true;

            return true;
        }

        public bool Pick(ModuleName target, int slot, out string reason)
        {
            reason = string.Empty;
            var handler = new PickHandler(this, target, slot);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }
            _waferMoveHandler = handler;

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            if(_isSimulator)
                WaferPresentOnBlade1 = false;

            return true;
        }

        public bool Place(ModuleName target, int slot, out string reason)
        {
            reason = string.Empty;
            var handler = new PlaceHandler(this, target, slot);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            _waferMoveHandler = handler;

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            if (_isSimulator)
                WaferPresentOnBlade1 = true;

            return true;
        }

        public bool GotoExtend(ModuleName target, int slot, bool isPick, bool isUp, out string reason)
        {
            reason = string.Empty;
            var handler = new GotoHandler(this, target, slot, isPick, isUp);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            _waferMoveHandler = handler;

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            return true;
        }

        public bool ZAxisMove(ModuleName target, int slot, bool isUp, out string reason)
        {
            reason = string.Empty;
            var handler = new ZAxisMoveHandler(this, target, slot, isUp);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            IsZBusy = true;
            _motionTimer.Start(0);

            return true;
        }

        public bool PlaceRetract(ModuleName target, int slot, out string reason)
        {
            reason = string.Empty;
            var handler = new PlaceRetractHandler(this, target, slot);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            _waferMoveHandler = handler;

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            return true;
        }

        public bool PickRetract(ModuleName target, int slot, bool isNeedWaferMove, out string reason)
        {
            reason = string.Empty;
            var handler = new PickRetractHandler(this, target, slot, isNeedWaferMove);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _targetZAxisAddressPosition = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            _waferMoveHandler = handler;

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            return true;
        }

        public void Abort()
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new AbortHandler(this));
            }

            _waferMoveHandler = null;
            _homeHandler = null;
            _motionTimer.Stop();
            _zAxisHomeTrig.RST = true;
        }

        public void QueryAllAxisStatus()
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new QueryAllAxisStatusHandler(this));
            }
        }

        public void QueryError()
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new QueryErrorHandler(this));
            }
        }

        public void SetSpeed()
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new SetThetaSpeedHandler(this, SC.GetValue<int>("TMRobot.Speed"), SC.GetValue<int>("TMRobot.Speed")));
                _lstHandler.AddFirst(new SetReachSpeedHandler(this, SC.GetValue<int>("TMRobot.Speed"), SC.GetValue<int>("TMRobot.Speed")));
            }
        }

        public void QueryOperationalStatus()
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(new QueryOperationalStatusHandler(this));
            }
        }

        public bool VacuumOn(out string reason)
        {
            reason = string.Empty;
            var handler = new VacuumOnHandler(this);
            //if (_lstHandler.Count > 0)
            //{
            //    reason = "Robot busy, please wait or abort.";
            //    EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

            //    return false;
            //}

            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            return true;
        }

        public bool VacuumOff(out string reason)
        {
            reason = string.Empty;
            var handler = new VacuumOffHandler(this);
            //if (_lstHandler.Count > 0)
            //{
            //    reason = "Robot busy, please wait or abort.";
            //    EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

            //    return false;
            //}

            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            return true;
        }

        public bool WaferMap(ModuleName target, out string reason)
        {
            reason = string.Empty;
            var handler = new WaferMapHandler(this, target);
            if (_lstHandler.Count > 0 && Moving)
            {
                reason = "Robot busy, please wait or abort.";
                EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.Name, handler.Name.ToString(), reason));

                return false;
            }

            SetBusy();

            _targetThetaAxisAddressPosition = RobotConvertor.Chamber2ThetaAxisPosition(target);
            ThetaAxisAddressPosition = -1;
            ZAxisAddressPosition = -1;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
            _motionTimer.Start(0);

            return true;
        }

        public bool GetWaferMap(ModuleName target, out string reason)
        {
            reason = string.Empty;
            var handler = new QueryWaferMapHandler(this, target);
            SlotMap = string.Empty;

            lock (_lockerHandlerList)
            {
                _lstHandler.AddFirst(handler);
            }

            return true;
        }

        private void SetBusy()
        {
            //IsBusy || IsThetaBusy || IsZBusy || IsRadialBusy
            IsBusy = true;
            IsThetaBusy = true;
            IsZBusy = true;
            IsRadialBusy = true;
        }

    }
}
