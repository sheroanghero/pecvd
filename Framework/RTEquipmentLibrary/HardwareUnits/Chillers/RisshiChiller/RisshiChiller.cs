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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.RisshiChiller
{
    public class RisshiChiller  : SerialPortDevice, IConnection
    {
        public string Address { get; }
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

        private RisshiChillerConnection _connection;
        public RisshiChillerConnection Connection
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
        private string _scRoot;
        public RisshiChiller(string module, string name, string scRoot, string portName) : base(module, name)
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

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new RisshiChillerConnection(PortName);
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
 
            _connection = new RisshiChillerConnection(portName, bautRate, dataBits, parity, stopBits);
 
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
                            //_lstHandler.AddLast(new RisshiChillerQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RisshiChillerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerRawCommandHandler(this, command));
            }
        }


        public void SetTemperature(string angle)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempHighWarning(string angle)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetTempLowWarning(string angle)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWAA", angle));
            }
        }
        public void SetRemoteMode(string angle)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWS", angle));
            }
        }

        public void SetRunMode(string waferSize)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RisshiChillerSimpleSetHandler(this, "SWS", waferSize));
            }
        }



        internal void NoteActionCompleted(string command)
        {
            if(command == "ALIGN")
            {
                AlignCompleted = true;
            }
            else if(command == "ROTATE")
            {
                RotatedCompleted = true;
            }
            else if(command == "XFER")
            {
                XferCompleted = true;
            }
            else if(command == "ALIGNER HOME/SCAN/ALGN")
            {
                HomeCompleted = true;
            }
        }

        internal void NoteQueryResult(string command, string parameter, string queryResult)
        {
            if (command == "HELLO")
            {
                if(queryResult == "AG")
                {
                    CommunicationIsOK = true;
                }
                else
                {
                    CommunicationIsOK = false;
                }
            }
            else if (command == "RWAA")
            {
                var resultArray = queryResult.Split(',');
                if(resultArray.Count() == 1)
                {
                    AlignedAngle = double.Parse(queryResult);
                }
            }
            else if (command == "RWS")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    WaferSize = double.Parse(queryResult);
                }
            }
            else if (command == "RBIASXY")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 2)
                {
                    BaisX = double.Parse(queryResult.Split(',')[0]);
                    BaisY = double.Parse(queryResult.Split(',')[1]);
                }
            }
            else if (command == "RBIASRT")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 2)
                {
                    BaisR = double.Parse(queryResult.Split(',')[0]);
                    BaisT = double.Parse(queryResult.Split(',')[1]);
                }
            }
            else if (command == "RNOTCH")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    Notch = double.Parse(queryResult);
                }
            }
            else if (command == "REER")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    Error = "_ERR " + queryResult;
                }
            }
            else if (command == "RWK")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    WafeOnOff = queryResult == "1";
                }
            }
            else if (command == "RAR")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    IsReady = queryResult == "1";
                }
            }
        }

        internal void NoteSetCompleted(string command, string parameter)
        {
            if(command == "SWAA")
            {
                AlignAngleSet = double.Parse(parameter);
            }
            else if(command == "SWS")
            {
                WaferSizeSet = double.Parse(parameter);
            }
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RisshiChillerRawCommandHandler) && ((RisshiChillerHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RisshiChillerRawCommandHandler(this, command, comandArgument));
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
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RisshiChillerRawCommandHandler) && ((RisshiChillerHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RisshiChillerRawCommandHandler(this, command));
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


        public void RequestTemprature(bool isSelected)
        {
            SimpleRequest(isSelected, "RWAA");
        }
        public void RequestCoolFlowRate(bool isSelected)
        {
            SimpleRequest(isSelected, "RWS");
        }
        public void RequestResistivity(bool isSelected)
        {
            SimpleRequest(isSelected, "RBIASXY");
        }
        public void RequestOperationStatus(bool isSelected)
        {
            SimpleRequest(isSelected, "RBIASRT");
        }

        public void RequestAlarmStatus(bool isSelected)
        {
            SimpleRequest(isSelected, "RNOTCH");
        }
        public void RequestWarningStatus(bool isSelected)
        {
            SimpleRequest(isSelected, "REER");
        }
        

        private void SimpleRequest(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(RisshiChillerSimpleQueryHandler) && ((RisshiChillerSimpleQueryHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new RisshiChillerSimpleQueryHandler(this, command));
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
        public bool SevoOnOff { get; private set; }
        public bool AlignCompleted { get; private set; }
        public bool RotatedCompleted { get; private set; }
        public double AlignAngleSet { get; private set; }
        public double WaferSizeSet { get; private set; }
        public double WaferSize { get; private set; }
        public bool XferCompleted { get; private set; }
        public bool HomeCompleted { get; private set; }
        public bool CommunicationIsOK { get; private set; }
        public double AlignedAngle { get; private set; }
        public double BaisX { get; private set; }
        public double BaisY { get; private set; }
        public double BaisR { get; private set; }
        public double BaisT { get; private set; }
        public double Notch { get; private set; }
        public bool WafeOnOff { get; private set; }
        public bool IsReady { get; private set; }
        public string LastPickInfo { get; private set; }
        public string LastPlaceInfo { get; private set; }

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();



        public void NoteError(string reason)
        {
            CommunicationIsOK = false;
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

        internal void NoteLastPickInfo(string sendText)
        {
            LastPickInfo = sendText;
        }
        internal void NoteLastPlaceInfo(string sendText)
        {
            LastPlaceInfo = sendText;
        }
        internal void NoteSevoOnOff(bool isOn)
        {
            SevoOnOff = isOn;
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


    public class XferArgument
    {
        public string Arm { get; set; }
        public int FromStation { get; set; }
        public int FromSlot { get; set; }
        public int ToStation { get; set; }
        public int ToSlot { get; set; }
        public string AlignCommand { get; set; }
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

        public string AlignCommand { get; set; }

    }







}
