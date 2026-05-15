using System;
using System.Collections.Generic;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.MayAir
{
    public class FfuMayAir : BaseDevice, IConnection
    {
        private FfuMayAirConnection _connection;

        private string _deviceAddress = "001";
 
        private bool _isOn;
 
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

        public string Address
        {
            get
            {
                return "";
            }
        }

        public bool IsConnected
        {
            get
            {
                return _connection!=null && _connection.IsConnected;
            }
        }
 

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public FfuMayAir(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;

            _activeMonitorStatus = true;
        }

        public void QuerySpeed()
        {
            
        }

        public void ResetDevice()
        {
            
        }

       public   void QueryError()
        {
            _lstHandler.AddLast(new FfuMayAirQueryStatusHandler(this, 1, 0));

            EV.PostInfoLog(Module, "Query error");
        }

        public   bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.Power", () => _power);
 
            //DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            //DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            //DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);
 
            string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");

            int address = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.DeviceAddress");

            _deviceAddress = address.ToString("D3");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");

            _connection = new FfuMayAirConnection(portName);
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
                        if (_lstHandler.Count == 0 && _activeMonitorStatus)
                        {
 
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

        internal void NoteError()
        {
             
        }

        public   void Monitor()
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

        public   void Reset()
        {
            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

             
        }
 
        public   void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {
                 
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

 
        public void SetSpeed(float speed)
        {
             
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

    }




}
