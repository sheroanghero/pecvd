using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.KITZ
{
    public class KITZThrottleValve : ThrottleValveBase
    {
        #region properties


        public int TVAlarm
        {
            get
            {
                //return _aiThrottleValveAlarm.Value;
                return 0;
            }
        }

        public override AITThrottleValveData DeviceData
        {
            get
            {
                AITThrottleValveData data = new AITThrottleValveData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    UnitPressure = "Torr",
                    UnitPosition = "%",
                    Module = Module,

                    PositionFeedback = PositionFeedback,
                    PressureFeedback = PressureFeedback,
                    PressureSetPoint = PressureSetPoint,
                    PositionSetPoint = PositionSetPoint,
                    MaxValuePressure = PressureRange,
                    MaxValuePosition = 100,
                    Mode = (int)Mode,
                };

                return data;
            }
        }

        public double PressureRange
        {
            get
            {
                if (_scPressureRange == null)
                    return 0;
                return _scPressureRange.DoubleValue;
            }
        }

        public bool IsError
        {
            get { return _errorCode != 0; }
        }

        public int DBNumber { get; private set; } = 6;
        #endregion
        private KITZConnection _connection;


        private const int VlaveClose = 1;
        private const int VlaveOpen = 2;
        private const int VlavePositionControl = 3;
        private const int VlavePressureControl = 4;

        private int _errorCode;

        private RD_TRIG _trigPumpOnOff = new RD_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigOverTemp = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();
        private string _lastError = string.Empty;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
        private bool _isFirstTime = true;

        private SCConfigItem _scPressureRange;

        public KITZThrottleValve(string module, string name) : base(module, name)
        {

        }

        public override bool Initialize()
        {
            base.Initialize();

            _scPressureRange = SC.GetConfigItem($"{Module}.{Name}.PressureRange");

            //for recipe
            OP.Subscribe($"{Module}.{Name}.SetPressure", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                SetPressure(Convert.ToSingle(param[0]));
                return true;
            });

            string portName = SC.GetStringValue($"{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _connection = new KITZConnection(portName);
            _connection.EnableLog(_enableLog);

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
                        _connection.SetPortAddress(SC.GetStringValue($"{Module}.{Name}.Address"));
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
                            if(_isFirstTime)
                            {
                                _lstHandler.AddFirst(new RemoteHandler(this));
                                _lstHandler.AddLast(new CalibrationHandler(this));
                                Mode = PressureCtrlMode.TVCalib;
                                _isFirstTime = false;
                            }
                            _lstHandler.AddLast(new QueryStateHandler(this));

                            if(Mode != PressureCtrlMode.TVCalib)
                            {
                                _lstHandler.AddLast(new QueryPressureHandler(this));

                                _lstHandler.AddLast(new QueryPositionHandler(this));
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
                _connection.EnableLog(_enableLog);

                _trigError.CLK = IsError;
                if (_trigError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} is error, error code {_errorCode:D3}");
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

        public override void Reset()
        {
            _tolerancePressureAlarmChecker.Reset(PressureAlarmTime);
            _tolerancePressureWarningChecker.Reset(PressureWarningTime);
            _tolerancePositionAlarmChecker.Reset(PositionAlarmTime);
            _tolerancePositionWarningChecker.Reset(PositionWarningTime);

            if (_trigError.M || _trigWarningMessage.M)
                _isFirstTime = true;

            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;


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
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");

            }
        }

        public override void SetControlMode(PressureCtrlMode mode)
        {
            if(Mode == PressureCtrlMode.TVCalib)
            {
                EV.PostWarningLog(Module, $"Throttle valve is in calibration mode, pls wait");
                return;
            }

            if (mode == PressureCtrlMode.TVClose)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new CloseHandler(this));
                }
            }
            else if (mode == PressureCtrlMode.TVOpen)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new OpenHandler(this));
                }
            }
            else if (mode == PressureCtrlMode.TVPositionCtrl)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new SetPositionHandler(this, 0));
                }
            }
            else if (mode == PressureCtrlMode.TVPressureCtrl)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new SetPressureHandler(this, 0, DBNumber));
                }
            }
        }

        public override void SetPressure(float pressure)
        {
            if (Mode == PressureCtrlMode.TVCalib)
            {
                EV.PostWarningLog(Module, $"{Name} is in calibration mode, pls wait");
                return;
            }

            if (pressure < 0 || pressure > PressureRange)
            {
                EV.PostWarningLog(Module, $"{Name} invalid pressure {pressure}, not in the scope (0, {PressureRange})");
                return;
            }
            PressureSetPoint = pressure;
            lock (_locker)
            {
                _lstHandler.AddLast(new SetPressureHandler(this, pressure, DBNumber));
            }
        }

        public void SetPressure(float pressure, int dbNo)
        {
            if (Mode == PressureCtrlMode.TVCalib)
            {
                EV.PostWarningLog(Module, $"{Name} is in calibration mode, pls wait");
                return;
            }

            if (pressure < 0 || pressure > PressureRange)
            {
                EV.PostWarningLog(Module, $"{Name} invalid pressure {pressure}, not in the scope (0, {PressureRange})");
                return;
            }
            PressureSetPoint = pressure;
            DBNumber = dbNo;
            lock (_locker)
            {
                _lstHandler.AddLast(new SetPressureHandler(this, pressure, dbNo));
            }
        }

        public override void SetPosition(float position)
        {
            if (Mode == PressureCtrlMode.TVCalib)
            {
                EV.PostWarningLog(Module, $"{Name} is in calibration mode, pls wait");
                return;
            }

            if (position < 0 || position > 100)
            {
                EV.PostWarningLog(Module, $"{Name} invalid position {position}, not in the scope (0, 100)");
                return;
            }
            PositionSetPoint = position;
            lock (_locker)
            {
                _lstHandler.AddLast(new SetPositionHandler(this, position));
            }
        }
    }
}
