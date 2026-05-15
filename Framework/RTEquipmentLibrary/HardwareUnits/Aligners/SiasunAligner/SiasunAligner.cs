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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.SiasunAligner
{
    public class SiasunAligner : AlignerBaseDevice, IConnection
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

        private SiasunAlignerConnection _connection;
        public SiasunAlignerConnection Connection
        {
            get { return _connection; }
        }
        public override bool IsReady()
        {
            return IsIdle && _lstHandler.Count == 0 && !_isError && !_connection.IsBusy && !_connection.IsCommunicationError && _connection.IsConnected;
        }
        public bool IsWaferPresence { get; set; }
        public override float RadialOffset { set; get; }//extend
        public override float ThetaOffset { set; get; }//rotate
        public override bool IsAlignError => _isError;

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
        private string _portName;
        private bool _isError;

        public SiasunAligner(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

            if (!string.IsNullOrEmpty(_scRoot) && SC.ContainsItem($"{_scRoot}.{Name}.PortName"))
            {
                _portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            }
            else
            {
                _portName = SC.GetStringValue($"{_scRoot}.PortName");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");
            }

            _connection = new SiasunAlignerConnection(_portName, 19200);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            DATA.Subscribe($"{Module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.Address", () => Address);
            OP.Subscribe($"{Module}.Reconnect", (string cmd, object[] args) =>
            {
                Disconnect();
                Connect();
                return true;
            });
        }
        private void ResetPropertiesAndResponses()
        {

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
        }

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {

            _connection = new SiasunAlignerConnection(portName, bautRate, dataBits, parity, stopBits);

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
                        _connection.SetPortAddress(_portName);
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new SiasunAlignerQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new SiasunAlignerSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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
            _isError = false;
            _trigWarningMessage.RST = true;

            base.Reset();
        }
        public override bool IsWaferPresent(int slotindex)
        {
            return IsWaferPresence;
        }

        #region Command Functions
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunAlignerRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunAlignerRawCommandHandler(this, command));
            }
        }
        internal void NoteActionCompleted(string command)
        {
            if (command == "ALIGN")
            {
                AlignCompleted = true;
            }
            else if (command == "ROTATE")
            {
                RotatedCompleted = true;
            }
            else if (command == "XFER")
            {
                XferCompleted = true;
            }
            else if (command == "ALIGNER HOME/SCAN/ALGN")
            {
                HomeCompleted = true;
            }
        }

        internal void NoteQueryResult(string command, string parameter, string queryResult)
        {
            if (command == "HELLO")
            {
                if (queryResult == "AG")
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
                if (resultArray.Count() == 1)
                {
                    AlignedAngle = double.Parse(queryResult);
                }
            }
            else if (command == "RWS")
            {
                var resultArray = queryResult.Split(',');
                if (resultArray.Count() == 1)
                {
                    //WaferSize = double.Parse(queryResult);
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
                    //IsReady = queryResult == "1";
                }
            }
        }

        internal void NoteSetCompleted(string command, string parameter)
        {
            if (command == "SWAA")
            {
                AlignAngleSet = double.Parse(parameter);
            }
            else if (command == "SWS")
            {
                WaferSizeSet = double.Parse(parameter);
            }
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SiasunAlignerRawCommandHandler) && ((SiasunAlignerHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunAlignerRawCommandHandler(this, command, comandArgument));
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
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SiasunAlignerRawCommandHandler) && ((SiasunAlignerHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunAlignerRawCommandHandler(this, command));
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


        public void RequestAlignAngle(bool isSelected)
        {
            SimpleRequest(isSelected, "RWAA");
        }
        public void RequestWaferSize(bool isSelected)
        {
            SimpleRequest(isSelected, "RWS");
        }
        public void RequestBiasXY(bool isSelected)
        {
            SimpleRequest(isSelected, "RBIASXY");
        }
        public void RequestBiasRT(bool isSelected)
        {
            SimpleRequest(isSelected, "RBIASRT");
        }

        public void RequestNotchAngle(bool isSelected)
        {
            SimpleRequest(isSelected, "RNOTCH");
        }
        public void RequestErrorCode(bool isSelected)
        {
            SimpleRequest(isSelected, "REER");
        }
        public void RequestHello(bool isSelected)
        {
            SimpleRequest(isSelected, "HELLO");
        }

        public void RequestWaferOnOff(bool isSelected)
        {
            SimpleRequest(isSelected, "RWK");
        }
        public void RequestReady(bool isSelected)
        {
            SimpleRequest(isSelected, "RAR");
        }

        private void SimpleRequest(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SiasunAlignerSimpleQueryHandler) && ((SiasunAlignerSimpleQueryHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SiasunAlignerSimpleQueryHandler(this, command));
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
                //_trigWarningMessage.CLK = true;
                _trigWarningMessage.CLK = RetryCounter >= MaxRetryCounter;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                Error = reason;
                _isError = true;
            }
        }

        public void ResetError()
        {
            _isError = false;
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
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        protected override bool fStartLiftup(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartLiftdown(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartAlign(object[] param)
        {
            float.TryParse(param[0].ToString(), out float aligneAngle);


            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunAlignerAlignHandler(this));
            }


            return true;
        }

        protected override bool fStartSetAlign(object[] param)
        {
            float.TryParse(param[0].ToString(), out float aligneAngle);

            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunAlignerSetAngleHandler(this, (float)aligneAngle));
            }



            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;

            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStop(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool FsmAbort(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fClear(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartReadData(object[] param)
        {
            switch (param[0].ToString())
            {
                case "QueryOffset":
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new SiasunAlignerQueryBiasHandler(this));
                    }
                    break;
                case "QueryWaferPresence":
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new SiasunAlignerQueryWaferPresentHandler(this));
                    }
                    break;
            }
            return true;
        }
        protected override bool fMonitorReadData(object[] param)
        {
            IsBusy = false;

            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }
        protected override bool fStartSetParameters(object[] param)
        {
            var currentSetParameter = param[0].ToString();
            switch (currentSetParameter)
            {
                case "WaferSize":
                    WaferSize currentSetWaferSize = (WaferSize)param[1];
                    switch (currentSetWaferSize)
                    {
                        case WaferSize.WS12:
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new SiasunAlignerSimpleSetHandler(this, "SWS", "300"));
                            }
                            break;

                        case WaferSize.WS8:
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new SiasunAlignerSimpleSetHandler(this, "SWS", "200"));
                            }
                            break;
                        case WaferSize.WS6:
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new SiasunAlignerSimpleSetHandler(this, "SWS", "150"));
                            }
                            break;
                        case WaferSize.WS4:
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new SiasunAlignerSimpleSetHandler(this, "SWS", "100"));
                            }
                            break;

                        default:
                            break;
                    }
                    break;
                case "AlignAngle":
                    float.TryParse(param[1].ToString(), out float angle);
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new SiasunAlignerSimpleSetHandler(this, "SWAA", angle.ToString("f2")));
                    }
                    break;
            }
            return true;
        }
        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;

            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartGrip(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fResetToReady(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fReset(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SiasunAlignerSevoOnOffHandler(this, true));
                //_lstHandler.AddLast(new SiasunAlignerQueryWaferPresentHandler(this));
            }
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;

            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fError(object[] param)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
