using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.Common;
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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkyPump
{


    public class SkyPump  : SerialPortDevice, IConnection
    {
        public string Address { get { return _address; }}
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

        private SkyPumpConnection _connection;
        public SkyPumpConnection Connection
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
        private string _address;
        private string _scRoot;
        public SkyPump(string module, string name, string scRoot, string portName) : base(module, name)
        {
            _scRoot = scRoot;
            PortName = portName;
        }
        private void ResetPropertiesAndResponses()
        {

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        public override bool Initialize(string portName)
        {
            base.Initialize(portName);

            ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected && PortName == portName)
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            PortName = portName;

            _address = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new SkyPumpConnection(PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
 
            return true;
        }

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {
 
            _connection = new SkyPumpConnection(portName, bautRate, dataBits, parity, stopBits);
 
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
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new SkyPumpQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new SkyPumpSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
        public void PerformRawCommand(string command,  string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkyPumpRawCommandHandler(this, command, completeEvent));
            }
        }



        public void Start()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkyPumpStartHandler(this));
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkyPumpStopHandler(this));
            }
        }



        internal void NoteStart(bool value)
        {
            PumpStarted = value;
        }

        public void MonitorRawCommand(bool isSelected, string command, string completeEvent,string parameter)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SkyPumpRawCommandHandler) && ((SkyPumpHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SkyPumpRawCommandHandler(this, command, completeEvent));
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


        public void ReadRunParameters(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SkyPumpReadRunParaHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SkyPumpReadRunParaHandler(this));
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
        public string Error { get; private set; }
        public bool PumpStarted { get; private set; }
        public string FDP_Current { get; private set; }
        public string ROOTS_Current { get; private set; }
        public string FDP_Temp { get; private set; }
        public string ROOTS_Temp { get; private set; }
        public string N2_Flow { get; private set; }
        public string Pressure { get; private set; }
        public string RunTime { get; private set; }
        public string ErrSta1 { get; private set; }
        public string ErrSta2 { get; private set; }
        public string RunSta1 { get; private set; }
        public string RunSta2 { get; private set; }
        public string ErrSta3 { get; private set; }

        internal void NoteRunPara(string data)
        {
            var str = data.ToArray();
            FDP_Current = data.Substring(11, 2);
            ROOTS_Current = data.Substring(13, 2);
            FDP_Temp = data.Substring(15, 3);
            ROOTS_Temp = data.Substring(18, 3);
            N2_Flow = data.Substring(21, 2);
            Pressure = data.Substring(23, 4);
            RunTime = data.Substring(27, 5);
            ErrSta1 = data.Substring(32, 1);
            ErrSta2 = data.Substring(33, 1);
            RunSta1 = data.Substring(34, 1);
            RunSta2 = data.Substring(35, 1);
            ErrSta3 = data.Substring(36, 1);
        }


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


        internal void NoteRawCommandInfo(string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
    }

}
