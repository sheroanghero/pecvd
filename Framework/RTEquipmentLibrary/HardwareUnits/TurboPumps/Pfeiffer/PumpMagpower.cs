using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.Pfeiffer
{

    public class PumpMagpower : PumpBase
    {
        public override bool IsError
        {
            get { return !string.IsNullOrEmpty(_errorCode); }
        }

        public string ErrorCode  { get { return _errorCode; } }

        public override bool IsOn
        {
            get { return _isOn; }
        }

        public override float Speed
        {
            get { return _speed; }
        }

        public override float Temperature
        {
            get { return _temperature; }
        }

        public override bool IsStable
        {
            get { return _isStable; }
        }

        public  bool IsRemote
        {
            get { return _isRemote; }
        }

        public override AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    DeviceModule = Module,

                    IsError = IsError,
                    IsOn = _isOn,

                    Speed = _speed,
                    Temperature = _temperature,

                    AtSpeed = _isAtSpeed,
                    OverTemp = _isOverTemp,

                };

                return data;
            }
        }
        private PumpMagpowerConnection _connection;

        public string DeviceAddress { get; set; } = "001";

        //private string DeviceAddress = "001";

        private int _speed;
        private int _temperature;
        private bool _isOn;
        private int _power;
        private bool _isAccelerate;
        private bool _isAtSpeed;
        private bool _isOverTemp;
        private bool _isStable;
        private bool _isRemote;
        private bool _activeMonitorStatus;

        private string _errorCode;

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

        private string _scRoot;
        private bool _isNormalSpeed;

        public bool IsNormalSpeed { get { return _isNormalSpeed; } }

        public PumpMagpower(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

            _activeMonitorStatus = true;

            _power = 0;
        }

        public override bool Initialize()
        {
            if (_connection != null && _connection.IsConnected)
            {
                return true;
            }

            base.Initialize();

            DATA.Subscribe($"{Module}.{Name}.Power", () => _power);

            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);

            string portName = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot+"." : "")}{Module}.{Name}.Address");
            int address = SC.ContainsItem($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.DeviceAddress") ? SC.GetValue<int>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.DeviceAddress") : 0;
            DeviceAddress = address.ToString("D3");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");

            _connection = new PumpMagpowerConnection(portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(300, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
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
                        if (_lstHandler.Count == 0 && _activeMonitorStatus)
                        {
                            _lstHandler.AddLast(new MagpowerQueryErrorHandler(this));
                            _lstHandler.AddLast(new MagpowerQuerySpeedHandler(this));
                            _lstHandler.AddLast(new MagpowerQueryStatusHandler(this));
                            _lstHandler.AddLast(new MagpowerQueryOptionHandler(this));
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

                _trigPumpOnOff.CLK = _isOn;
                if (_trigPumpOnOff.R)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is on");
                }

                if (_trigPumpOnOff.T)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is off");
                }

                _trigError.CLK = IsError;
                if (_trigError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} is error, error code {_errorCode:D3}");
                }

                _trigOverTemp.CLK = _isOverTemp;
                if (_trigOverTemp.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} is over temperature");
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
            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }

        public override void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new MagpowerPumpStationHandler(this, isOn));
            }
        }


        public void Echo(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new MagpowerPumpStationHandler(this, isOn));
            }
        }
        public void SwitchSpeed(bool isNormal)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new MagpowerSwitchSpeedHandler(this, isNormal));
            }
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }


        internal void NoteOnOff(bool isOn)
        {
            _isOn = isOn;
        }
        internal void NoteRemote(bool isRemote)
        {
            _isRemote = isRemote;
        }
        internal void NoteNormalSpeed(bool isNormal)
        {
            _isNormalSpeed = isNormal;
        }

        public void SetSpeed(int speed)
        {
            _speed = speed;
        }

        public void SetTemperature(int temperature)
        {
            _temperature = temperature;
        }

        public void SetErrorCode(string errorCode)
        {
            _errorCode = errorCode;
        }

        public void SetAtSpeed(bool atSpeed)
        {
            _isAtSpeed = atSpeed;
        }

        public void SetOverTemp(bool overTemp)
        {
            _isOverTemp = overTemp;
        }

        public void NoteStable(bool stable)
        {
            if(_isStable != stable)
                _isStable = stable;
        }

        public void NoteAccelerate(bool isAccelerate)
        {
            _isAccelerate = isAccelerate;
        }

        public void NoteError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");

            }
        }

        public void NoteInfo(string info)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} info, {info}");

            }
        }
    }

}
