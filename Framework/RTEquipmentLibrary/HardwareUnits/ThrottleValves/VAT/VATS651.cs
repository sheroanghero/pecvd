using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.VAT
{
    public class VATS651 : ThrottleValveBase, IConnection
    {
        public string Address => Connection?.Address;
        public override bool IsConnected => Connection != null && Connection.IsConnected && !_connection.IsCommunicationError;

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public string PortStatus { get; set; } = "Closed";

        private VATS651Connection _connection;
        public virtual VATS651Connection Connection
        {
            get { return _connection; }
        }

        public override AITThrottleValveData DeviceData
        {
            get
            {
                return new AITThrottleValveData()
                {
                    Module = Module,
                    DisplayName = Name,
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    UnitPosition = "%",
                    UnitPressure = "mTorr",
                    MaxValuePosition = 100,
                    MaxValuePressure = 1000,
                    PositionFeedback = PositionFeedback,
                    PositionSetPoint = PositionSetPoint,
                    PressureFeedback = PressureFeedback,
                    PressureSetPoint = PressureSetPoint,
                    Mode = (int)Mode,
                };
            }
        }

        private R_TRIG _trigError = new R_TRIG();
        private bool _isAlarm;

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        private SCConfigItem _scPressureSensorRange;
        private SCConfigItem _scPressureCommunicationRange;
        private SCConfigItem _scPositionCommunicationRange;
        private SCConfigItem _scSensorResponseTime;
        private SCConfigItem _scRampTime;
        private SCConfigItem _scGainFactor;
        private SCConfigItem _scValveSpeed;
        private SCConfigItem _scBaudRate;
        private SCConfigItem _scDataBits;
        private SCConfigItem _scParity;
        private SCConfigItem _scStopBits;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

         
        private Dictionary<string, string> _errorCodeReferenceDic;

        private object _locker = new object();
        private RD_TRIG _learnTrig = new RD_TRIG();

        private bool _enableLog;
        private string _scRoot;

        public VATS651(string module, string name, string scRoot="") : base()
        {
            _scRoot = scRoot;
            base.Module = module;
            base.Name = name;
        }
 
        public bool Initialize(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            base.Initialize();
            _enableLog = true;
            _connection = new VATS651Connection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);



            return true;
        }

        public override bool Initialize()
        {
            if (string.IsNullOrEmpty(_scRoot))
            {
                ScBasePath = $"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}";

                _scPressureSensorRange = SC.GetConfigItem($"{ScBasePath}.{Name}.PressureSensorRange");
                _scPressureCommunicationRange = SC.GetConfigItem($"{ScBasePath}.{Name}.PressureCommunicationRange");
                _scPositionCommunicationRange = SC.GetConfigItem($"{ScBasePath}.{Name}.PositionCommunicationRange");
                _scSensorResponseTime = SC.GetConfigItem($"{ScBasePath}.{Name}.SensorResponseTime");
                _scRampTime = SC.GetConfigItem($"{ScBasePath}.{Name}.RampTime");
                _scGainFactor = SC.GetConfigItem($"{ScBasePath}.{Name}.GainFactor");
                _scValveSpeed = SC.GetConfigItem($"{ScBasePath}.{Name}.ValveSpeed");

                string portName = SC.GetStringValue($"{ScBasePath}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

                _scBaudRate = SC.GetConfigItem($"{ScBasePath}.{Name}.BaudRate");
                _scDataBits = SC.GetConfigItem($"{ScBasePath}.{Name}.DataBits");
                _scParity = SC.GetConfigItem($"{ScBasePath}.{Name}.Parity");
                _scStopBits = SC.GetConfigItem($"{ScBasePath}.{Name}.StopBits");

                _connection = new VATS651Connection(portName
                    , _scBaudRate?.IntValue ?? 9600
                    , _scDataBits?.IntValue ?? 7
                    , (Parity)(_scParity?.IntValue ?? 2)
                    , (StopBits)(_scStopBits?.IntValue ?? 1));
            }
            else
            {
                _scPressureSensorRange = SC.GetConfigItem($"{_scRoot}.PressureSensorRange");
                _scPressureCommunicationRange = SC.GetConfigItem($"{_scRoot}.PressureCommunicationRange");
                _scPositionCommunicationRange = SC.GetConfigItem($"{_scRoot}.PositionCommunicationRange");
                _scSensorResponseTime = SC.GetConfigItem($"{_scRoot}.SensorResponseTime");
                _scRampTime = SC.GetConfigItem($"{_scRoot}.RampTime");
                _scGainFactor = SC.GetConfigItem($"{_scRoot}.GainFactor");
                _scValveSpeed = SC.GetConfigItem($"{_scRoot}.ValveSpeed");

                string portName = SC.GetStringValue($"{_scRoot}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");

                _scBaudRate = SC.GetConfigItem($"{_scRoot}.BaudRate");
                _scDataBits = SC.GetConfigItem($"{_scRoot}.DataBits");
                _scParity = SC.GetConfigItem($"{_scRoot}.Parity");
                _scStopBits = SC.GetConfigItem($"{_scRoot}.StopBits");

                int bautRate = SC.GetValue<int>($"{_scRoot}.BaudRate");
                int dataBits = SC.GetValue<int>($"{_scRoot}.DataBits");
                Enum.TryParse(SC.GetStringValue($"{_scRoot}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"{_scRoot}.StopBits"), out StopBits stopBits);

                _connection = new VATS651Connection(portName
                    , bautRate
                    , dataBits
                    , parity
                    , stopBits);
            }



            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            InitHandler();
            _lstMonitorHandler.AddLast(new VATS651QueryStatusHandler(this));
            _lstMonitorHandler.AddLast(new VATS651QueryLearnStatusHandler(this));
            _errorCodeReferenceDic = new Dictionary<string, string>()
            {
                {"000001", "Parity error" },
                {"000002", "Input buffer overflow (to many characters)" },
                {"000003", "Framing error (data length, number of stop bits)" },
                {"000004", "Overrun (Service interface: Input buffer register overflow)" },
                {"000010", "<CR> or <LF> missing" },
                {"000011", ": missing" },
                {"000012", "Invalid number of characters (between : and )" },
                {"000023", "Invalid value" },
                {"000030", "Value out of range" },
                {"000040", "Pressure mode, Zero or Learn without Sensor" },
                {"000041", "Command not applicable for hardware configuration" },
                {"000060", "ZERO disabled" },
                {"000080", "Command not accepted due to local operation" },
                {"000081", "Command not accepted, Service Interface locked" },
                {"000082", "Command not accepted due to synchronization, CLOSED or OPEN by digital input, safety mode or fatal error" },
                {"000089", "Not accepted calibration and test mode" },
            };

            InitializeBase();

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetPressure", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var pressure = Convert.ToSingle(param[0]);
                if (Math.Abs(PressureSetPoint - pressure) > 0.0001)
                    SetPressure(pressure);
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetPosition", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var position = Convert.ToSingle(param[0]);
                if (Math.Abs(PositionSetPoint - position) > 0.0001)
                {
                    PositionSetPoint = position;

                    lock (_locker)
                    {
                        _lstHandler.AddLast(new VATS651SetPositionHandler(this, (int)(PositionSetPoint * _scPositionCommunicationRange.IntValue / 100)));
                    }
                }

                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetMode", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                if (param[0].ToString().ToLower().Contains("pressure"))
                {
                    SetControlMode(PressureCtrlMode.TVPressureCtrl);
                }
                else
                {
                    SetControlMode(PressureCtrlMode.TVPositionCtrl);
                }

                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetGainFactor", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var gain = Convert.ToSingle(param[0]);
                if (gain > 0)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new VATS651SetGainFactorHandler(this, Convert.ToSingle(param[0])));
                    }
                }
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetSensorResponseTime", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                lock (_locker)
                {
                    _lstHandler.AddLast(new VATS651SetSensorDelayHandler(this, Convert.ToSingle(param[0])));
                }
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetSetpointRampTime", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                lock (_locker)
                {
                    _lstHandler.AddLast(new VATS651SetRampTimeHandler(this, Convert.ToSingle(param[0])));
                }
                return true;
            });

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetValveSpeed", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                var speed = (int)Convert.ToSingle(param[0]);
                if (speed >= 1)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new VATS651SetValveSpeedHandler(this, speed));
                    }
                }

                return true;
            });

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });

            
            return true;
        }

        public void InitializeBase()
        {
            base.Initialize();
        }


        protected override void InitSc()
        {
            _scEnableAlarm = SC.GetConfigItem($"{ScBasePath}.{Name}.EnableAlarm");
            _scPressureAlarmTime = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmTime");
            _scPressureAlarmRange = SC.GetConfigItem($"{ScBasePath}.{Name}.AlarmRange");
            _scPressureWarningTime = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningTime");
            _scPressureWarningRange = SC.GetConfigItem($"{ScBasePath}.{Name}.WarningRange");
            _scPressureFineTuningValue = SC.GetConfigItem($"{ScBasePath}.FineTuning.{Name}Pressure");
            _scPositionFineTuningValue = SC.GetConfigItem($"{ScBasePath}.FineTuning.{Name}Position");
            _scRecipeIgnoreTime = SC.GetConfigItem($"{ScBasePath}.{Name}.RecipeIgnoreTime");
            _scStableCriteria = SC.GetConfigItem($"{ScBasePath}.{Name}.StableCriteria");
            _scFineTuningEnable = SC.GetConfigItem($"{ScBasePath}.FineTuning.IsEnable");
        }

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _connection = new VATS651Connection(portName, bautRate, dataBits, parity, stopBits);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        private bool OnTimer()
        {
            try
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
                        _connection.SetPortAddress(SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            InitHandler();
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
                baseMonitor();

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

        public void baseMonitor()
        {
            base.Monitor();
        }

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            if (_isAlarm)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new VATS651ResetHandler(this));
                }
            }

            base.Reset();
        }

        private void InitHandler()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SetAccessModeHandler(this));
                _lstHandler.AddLast(new VATS651SetValveSpeedHandler(this, _scValveSpeed.IntValue));
                _lstHandler.AddLast(new VATS651SetSensorDelayHandler(this, (float)_scSensorResponseTime.DoubleValue));
                _lstHandler.AddLast(new VATS651SetRampTimeHandler(this, (float)_scRampTime.DoubleValue));
                _lstHandler.AddLast(new VATS651SetGainFactorHandler(this, (float)_scGainFactor.DoubleValue));
            }
        }

 
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651RawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651RawCommandHandler(this, command));
            }
        }

        public override void SetClose()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651CloseValveHandler(this));
            }
        }

        public override void SetOpen()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651OpenValveHandler(this));
            }
        }

        public void Hold()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651HoldValveHandler(this));
            }
        }

        public override void SetPosition(float position)
        {
            if (position < 0 || position > 100)
            {
                EV.PostWarningLog(Module, $"{Module} {Name} set position {position}, out of range [0, 100]");
                return;
            }
            PositionSetPoint = position;

            if (Math.Abs(position) < 0.001)
            {
                SetClose();
            }
            else
            {
                lock (_locker)
                {
                    if (_scPositionCommunicationRange != null)
                    {
                        _lstHandler.AddLast(new VATS651SetPositionHandler(this, (int)(position * _scPositionCommunicationRange.IntValue / 100)));

                    }
                    else
                    {
                        _lstHandler.AddLast(new VATS651SetPositionHandler(this, (int)(position)));

                    }
                }
            }
        }

        public override void SetPressure(float pressure)
        {
            if (pressure < 0 || (_scPressureSensorRange != null && pressure > _scPressureSensorRange.IntValue))
            {
                int value = _scPressureSensorRange == null ? 0 : _scPressureSensorRange.IntValue;
                EV.PostWarningLog(Module, $"{Module} {Name} set pressure {value}, out of range [0, {value}]");
                return;
            }

            PressureSetPoint = pressure;
            lock (_locker)
            {
                if (_scPressureCommunicationRange != null)
                {
                    _lstHandler.AddLast(new VATS651SetPressureHandler(this, (int)(pressure * _scPressureCommunicationRange.IntValue / _scPressureSensorRange.IntValue)));

                }
                else
                {
                    _lstHandler.AddLast(new VATS651SetPressureHandler(this, (int)(pressure)));

                }
            }
        }

        public void SetAcessMode(string mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SetAccessModeHandler(this));
            }
        }

        public override void SetControlMode(PressureCtrlMode mode)
        {
            if (mode == PressureCtrlMode.TVClose)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new VATS651CloseValveHandler(this));
                }
            }
            else if (mode == PressureCtrlMode.TVOpen)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new VATS651OpenValveHandler(this));
                }
            }
            else if (mode == PressureCtrlMode.TVPositionCtrl)
            {
                lock (_locker)
                {
                    var position = PositionSetPoint > 0 ? PositionSetPoint : 1;

                    if (_scPositionCommunicationRange != null)
                    {
                        _lstHandler.AddLast(new VATS651SetPositionHandler(this, (int)(position * _scPositionCommunicationRange.IntValue / 100)));
                    }
                    else
                    {
                        _lstHandler.AddLast(new VATS651SetPositionHandler(this, (int)(position)));
                    }

                }
            }
            else if (mode == PressureCtrlMode.TVPressureCtrl)
            {
                lock (_locker)
                {
                    var pressure = PressureSetPoint > 0 ? PressureSetPoint : 1;

                    if (_scPressureCommunicationRange != null)
                    {
                        _lstHandler.AddLast(new VATS651SetPressureHandler(this, (int)(pressure * _scPressureCommunicationRange.IntValue / _scPressureSensorRange.IntValue)));
                    }
                    else
                    {
                        _lstHandler.AddLast(new VATS651SetPressureHandler(this, (int)(pressure)));

                    }

                }
            }
        }

        public override void Learn()
        {
            if (_learnTrig.M)
            {
                EV.PostWarningLog(Module, $"{Module} {Name} is already at learning state");
                return;
            }
            _learnTrig.RST = true;
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651LearnHandler(this, _scPressureCommunicationRange.IntValue));
            }
        }

        public void SetValveConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:04", setVal));
            }
        }

        public void SetSensorConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:01", setVal));
            }
        }

        public void SetSensorScale(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:05", setVal));
            }
        }

        public void SetSensor1Linearization(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:17", setVal));
            }
        }

        public void SetSensor2Linearization(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:18", setVal));
            }
        }

        public void SetSensorAverage(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:19", setVal));
            }
        }

        public void SetCommRangeConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:21", setVal));
            }
        }

        public void SetInterfaceConfig(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:20", setVal));
            }
        }

        public void SetValveSpeed(int speed)
        {
            //valve speed, 1 ... 1000 (1 = min. speed, 1000 = max. speed)
            if (speed < 1 || speed > 1000)
            {
                EV.PostWarningLog(Module, $"{Module} {Name} set speed {speed}, out of range [1, 1000]");
                return;
            }
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SetValveSpeedHandler(this, speed));
            }
        }

        public void SetPressureControllerMode(string setVal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:02Z00", setVal));
            }
        }

        public void SetPressureControllerConfig(string pressureController, string parameterNumber, string parameterValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651SimpleActionHandler(this, "s:02", pressureController + parameterNumber + parameterValue));
            }
        }

        public void ResetError(string reset)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651ResetHandler(this));
            }
        }

        internal void SetPositionFeedback(float value)
        {
            if (_scPositionCommunicationRange != null)
                PositionFeedback = 100.0f * value / _scPositionCommunicationRange.IntValue;
            else
                PositionFeedback = value;
        }

        internal void SetPressureFeedback(float value)
        {
            if (_scPressureCommunicationRange != null && _scPressureSensorRange != null)
                PressureFeedback = 1.0f * value / _scPressureCommunicationRange.IntValue * _scPressureSensorRange.IntValue;
            else
            {
                PressureFeedback = value;

            }
        }

        internal void SetLearnStatus(bool isRunning, string info)
        {
            _learnTrig.CLK = isRunning;
            if (_learnTrig.R)
            {
                EV.PostInfoLog(Module, $"{Module} {Name} learning start");
            }
            if (_learnTrig.T)
            {
                EV.PostInfoLog(Module, $"{Module} {Name} learning finished, result {info}");
            }
        }

        internal virtual void NoteActionCompleted(string command)
        {
            if (command == "C:")
            {
                CloseValveCompleted = true;
            }
            else if (command == "O:")
            {
                OpenValveCompleted = true;
            }
            else if (command == "R:")
            {
                SetPositonCompleted = true;
            }
            else if (command == "S:")
            {
                SetPressureCompleted = true;
            }
            else if (command == "c:01")
            {
                SetAcessModeCompleted = true;
            }
            else if (command == "s:04")
            {
                SetValveConfigCompleted = true;
            }
            else if (command == "s:01")
            {
                SetSensorConfigCompleted = true;
            }
            else if (command == "s:05")
            {
                SetSensorScaleCompleted = true;
            }
            else if (command == "s:17")
            {
                SetSensor1LinearizationCompleted = true;
            }
            else if (command == "s:18")
            {
                SetSensor2LinearizationCompleted = true;
            }
            else if (command == "s:19")
            {
                SetSensorAverageCompleted = true;
            }
            else if (command == "s:21")
            {
                SetCommRangeConfigCompleted = true;
            }
            else if (command == "s:20")
            {
                SetInterfaceConfigCompleted = true;
            }
            else if (command == "V:")
            {
                SetValveSpeedCompleted = true;
            }
            else if (command == "c:82")
            {
                ResetErrorCompleted = true;
            }
            else if (command == "s:02Z00")
            {
                SetPressureControllerModeCompleted = true;
            }
            else if (command == "s:02")
            {
                SetPressureControllerConfigCompleted = true;
            }
        }

        internal virtual void NoteQueryResult(string command, string queryResult)
        {
            if (command == "A:")
            {
                Position = queryResult;
            }
            else if (command == "P:")
            {
                Pressure = queryResult;
            }
            else if (command == "i:60")
            {
                Sensor1Offset = queryResult;
            }
            else if (command == "i:61")
            {
                Sensor2Offset = queryResult;
            }
            else if (command == "i:64")
            {
                Sensor1Reading = queryResult;
            }
            else if (command == "i:65")
            {
                Sensor2Reading = queryResult;
            }
            else if (command == "i:30")
            {
                DeviceStatus = queryResult;
            }
            else if (command == "i:50")
            {
                VATError = queryResult;
            }
            else if (command == "i:51")
            {
                VATWarning = queryResult;
            }
            else if (command == "W:")
            {
                PressureSet = queryResult;
            }
            else if (command == "M:")
            {
                Mode = queryResult == "POS" ? PressureCtrlMode.TVPositionCtrl : PressureCtrlMode.TVPressureCtrl;
            }
        }

        internal void NoteSetCompleted(string command, string parameter)
        {
            //if(command == "SWAA")
            //{
            //    AlignAngleSet = double.Parse(parameter);
            //}
            //else if(command == "SWS")
            //{
            //    WaferSizeSet = double.Parse(parameter);
            //}
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651RawCommandHandler) && ((VATS651Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651RawCommandHandler(this, command, comandArgument));
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
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651RawCommandHandler) && ((VATS651Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651RawCommandHandler(this, command));
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

        public void RequestPositoin(bool isSelected)
        {
            SimpleRequest(isSelected, "A:");
        }

        public void RequestPressure(bool isSelected)
        {
            SimpleRequest(isSelected, "P:");
        }

        public void RequestSensor1Offset(bool isSelected)
        {
            SimpleRequest(isSelected, "i:60");
        }

        public void RequestSensor2Offset(bool isSelected)
        {
            SimpleRequest(isSelected, "i:61");
        }

        public void RequestSensor1Reading(bool isSelected)
        {
            SimpleRequest(isSelected, "i:64");
        }

        public void RequestSensor2Reading(bool isSelected)
        {
            SimpleRequest(isSelected, "i:65");
        }

        public void RequestDeviceStatus(bool isSelected)
        {
            SimpleRequest(isSelected, "i:30");
        }

        public void RequestError(bool isSelected)
        {
            SimpleRequest(isSelected, "i:50");
        }

        public void RequestWarnings(bool isSelected)
        {
            SimpleRequest(isSelected, "i:51");
        }

        public virtual void QueryAllStatus()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new VATS651QueryStatusHandler(this));
            }
        }

        private void SimpleRequest(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(VATS651SimpleQueryHandler) && ((VATS651SimpleQueryHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new VATS651SimpleQueryHandler(this, command));
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

 
        public bool SetPositonCompleted { get; internal set; }
        public bool SetPressureCompleted { get; internal set; }
        public bool SetAcessModeCompleted { get; internal set; }
        public bool ResetErrorCompleted { get; internal set; }
        public string Position { get; internal set; }
        public bool CloseValveCompleted { get; internal set; }
        public bool OpenValveCompleted { get; internal set; }
        public string Pressure { get; internal set; }
        public string PressureSet { get; internal set; }
        public string Sensor1Offset { get; internal set; }
        public string Sensor2Offset { get; internal set; }
        public string Sensor1Reading { get; internal set; }
        public string Sensor2Reading { get; internal set; }
        public string DeviceStatus { get; internal set; }
        public string VATError { get; internal set; }
        public string VATWarning { get; internal set; }
        public bool SetValveConfigCompleted { get; internal set; }
        public bool SetSensorConfigCompleted { get; internal set; }
        public bool SetPressureControllerModeCompleted { get; internal set; }
        public bool SetPressureControllerConfigCompleted { get; internal set; }
        public bool SetSensorScaleCompleted { get; internal set; }
        public bool SetSensor1LinearizationCompleted { get; internal set; }
        public bool SetSensor2LinearizationCompleted { get; internal set; }
        public bool SetSensorAverageCompleted { get; internal set; }
        public bool SetCommRangeConfigCompleted { get; internal set; }
        public bool SetInterfaceConfigCompleted { get; internal set; }
        public bool SetValveSpeedCompleted { get; internal set; }
 
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public virtual void NoteError(string errorCode)
        {
            if (errorCode != null)
            {
                _trigWarningMessage.CLK = true;
                string content = errorCode;
                if (_errorCodeReferenceDic != null && _errorCodeReferenceDic.ContainsKey(errorCode))
                    content = _errorCodeReferenceDic[errorCode];
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {content}");
                }
                _isAlarm = true;
            }
            else
            {
                _isAlarm = false;
            }
        }

        internal void NoteRawCommandInfo(string command, string data)
        {
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

 
    }
}