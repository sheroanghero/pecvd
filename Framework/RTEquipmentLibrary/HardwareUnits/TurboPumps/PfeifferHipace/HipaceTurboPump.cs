using System;
using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.PfeifferHipace
{
    public class HipaceTurboPump : PumpBase
    {
        public  override  bool IsError
        {
            get { return _errorCode != 0; }
        }

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

        public override  bool IsStable
        {
            get { return _isAtSpeed; }
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
        private HipaceConnection _connection;

        private string _deviceAddress = "001";

        private int _speed;
        private int _temperature;
        private bool _isOn;
        private int _power;
        private bool _isAccelerate;
        private bool _isAtSpeed;
        private bool _isOverTemp;

        private bool _activeMonitorStatus;

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

        private string _scRoot;

        public HipaceTurboPump(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

            _activeMonitorStatus = true;

            _power = 0;
        }

        public override bool Initialize()
        {
            if(_connection != null && _connection.IsConnected)
            {
                return true;
            }

            base.Initialize();
 
            DATA.Subscribe($"{Module}.{Name}.Power", () => _power);
 
            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);
 
            string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            int address = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.DeviceAddress");
            _deviceAddress = address.ToString("D3");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");

            _connection = new HipaceConnection(portName);
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
                //temp comment ??
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
                            _lstHandler.AddLast(new HipaceQueryErrorHandler(this, _deviceAddress));
                            _lstHandler.AddLast(new HipaceQuerySpeedHandler(this, _deviceAddress));

                            _lstHandler.AddLast(new HipaceQueryTemperatureHandler(this, _deviceAddress));
                            _lstHandler.AddLast(new HipacePumpStationHandler(this, _deviceAddress, true, true));

                            _lstHandler.AddLast(new HipaceAtSpeedHandler(this, _deviceAddress));
                            _lstHandler.AddLast(new HipaceAccelerateHandler(this, _deviceAddress));
                            _lstHandler.AddLast(new HipaceOverTempHandler(this, _deviceAddress));
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
                _lstHandler.AddLast(new HipacePumpStationHandler(this, _deviceAddress, isOn, false));
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

 
        public void SetSpeed(int speed)
        {
            _speed = speed;
        }

        public void SetTemperature(int temperature)
        {
            _temperature = temperature;
        }

        public void SetErrorCode(int errorCode)
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

        public void SetIsAccelerate(bool isAccelerate)
        {
            _isAccelerate = isAccelerate;
        }

        public void SetError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");

            }
        }
    }




}
