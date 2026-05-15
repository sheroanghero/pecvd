using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA100
{
    public class PfeifferPumpA100 : PumpBase, IConnection
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

        private PfeifferPumpA100Connection _connection;
        public PfeifferPumpA100Connection Connection
        {
            get { return _connection; }
        }

        public override AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    IsOn = IsOn,
                    IsError = IsError,
                    OverTemp = IsOverTemperature,
                    DeviceModule = Module,
                    DeviceName = Name,
                };
                return data;
            }
        }

        public string ADR = "000";

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;
        //private string _address;
        private string _scRoot;
        private string _portName;
        public PfeifferPumpA100(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
            //_address = "000";
        }
        private void ResetPropertiesAndResponses()
        {

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

            _portName = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new PfeifferPumpA100Connection(_portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _lstMonitorHandler.AddLast(new PfeifferPumpA100ReadPumpStatusHandler(this));

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

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {

            _connection = new PfeifferPumpA100Connection(portName, bautRate, dataBits, parity, stopBits);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        internal void NoteSetParaCompleted()
        {
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
                        _connection.SetPortAddress(_portName);
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new PfeifferPumpA100QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new PfeifferPumpA100SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        #region Command Functions

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100RawCommandHandler(this, command, null));
            }
        }
        public void PerformRawCommand(string command, string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100RawCommandHandler(this, command, parameter));
            }
        }

        public override void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100SimpleSwitchHandler(this, "SYS", isOn ? "ON" : "OFF"));
            }
        }

        public void EchoOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA100SimpleSwitchHandler(this, "ECH", parameter));
            }
        }

        internal void NoteSwitchCompleted(string command, bool value)
        {
            //if (command == "ECH")
            //    SetEchoOnOff = value;
            //else if (command == "SYS")
            //    SetPumpOnOff = value;
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteError(string reason)
        {
            if (reason != null)
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


        #endregion
    }
}
