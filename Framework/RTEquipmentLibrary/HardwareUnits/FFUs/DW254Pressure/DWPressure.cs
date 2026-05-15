using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.MayAir;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public class DWPressure : BaseDevice, IConnection
    {
        private DWPressureConnection _connection;
        private bool _activeMonitorStatus;
        private int _errorCode;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private int pressureCount = 1;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog
        {
            get
            {
                if(SC.ContainsItem($"{_scRoot}.{Name}.EnableLogMessage"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                return false;
            }
        }

        private string _scRoot;

        private int _pressureValue1 = 0;
        private int _pressureValue2 = 0;
        private int _pressureValue3 = 0;

        public int PressureValue1
        {
            get { return _pressureValue1; }
            set { _pressureValue1 = value; }
        }
        public int PressureValue2
        {
            get { return _pressureValue2; }
            set { _pressureValue2 = value; }
        }
        public int PressureValue3
        {
            get { return _pressureValue3; }
            set { _pressureValue3 = value; }
        }

        public string Address
        {
            get;set;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
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

        public DWPressure(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        ~DWPressure()
        {
            _connection.Disconnect();

        }

        public DWPressure(string module, string name, string scRoot, int count) : base(module, name, name, name)
        {
            pressureCount = count;
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        public void QueryPressure()
        {
            if (pressureCount == 1)
            { _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 1)); }
            if (pressureCount == 2)
            {
                _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 1));
                _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 3));
            }
            if (pressureCount == 3)
            {
                _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 1));
                _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 3));
                _lstHandler.AddLast(new DWPressureQueryPressureHandler(this, 5));
            }
        }


        public void ResetDevice()
        {

        }

        public void QueryError()
        {


            EV.PostInfoLog(Module, "Query error");
        }

        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.Power", () => _power);

            //DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            //DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            //DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);

            string portName = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            Address = portName;
            int address = SC.GetValue<int>($"{_scRoot}.{Name}.DeviceAddress");            

            _connection = new DWPressureConnection(portName);
            _connection.EnableLog(_enableLog);



            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;
            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);

            _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            DATA.Subscribe($"{Name}.DiffPressure1", () => _pressureValue1);
            DATA.Subscribe($"{Name}.DiffPressure2", () => _pressureValue2);
            DATA.Subscribe($"{Name}.DiffPressure3", () => _pressureValue3);
            DATA.Subscribe($"{Name}.DiffPressureCount", () => pressureCount);

            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.EnableLog(_enableLog);
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
                        _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }

                    _connection.ForceClear();
                    return true;

                }

                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if(_lstHandler.Count == 0)
                        QueryPressure();
                    if(_lstHandler.Count > 0 && !_connection.IsBusy)
                    {                        
                        handler = _lstHandler.First.Value;
                        _lstHandler.RemoveFirst();
                        if (handler != null)
                        {
                            _connection.Execute(handler);
                        }
                    }
                }


                //}
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

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

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

        public void Reset()
        {
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;


        }

        public void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {

            }
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }

        public void Terminate()
        {
            _connection.Disconnect();
        }
       

    }
}
