using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkySGMPump
{
    public class SkySGMPump : PumpBase, IConnection
    {
        public string Address { get { return _address; } }

        public string PortName { get; set; }
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

        private SkySGMPumpConnection _connection;
        public SkySGMPumpConnection Connection
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

        public bool HasAlarm => throw new NotImplementedException();

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

        private object _locker = new object();

        private bool _enableLog;
        private string _address;
        private string _scRoot;

        public SkySGMPump(string module, string name, string scRoot, string portName) : base()
        {
            Module = module;
            Name = name;
            _scRoot = scRoot;
            PortName = portName;


        }

        private bool OnTimer()
        {
            try
            {
                //return true;
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
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {

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

        public override bool Initialize()
        {

            _connection = new SkySGMPumpConnection(_scRoot, "\r\n");
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            else
            {
                PortStatus = "Close";
                EV.PostInfoLog(Module, $"{Module}.{Name} connect failed");
            }

            //_lstMonitorHandler.AddLast(new SkySGMPumpStartHandler(this));
            //_lstMonitorHandler.AddLast(new SkySGMPumpStopHandler(this));
            _lstMonitorHandler.AddLast(new SkySGMPumpReadRunParaHandler(this));

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            return true;
        }

        public override void Monitor()
        {
            return;
        }

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            _trigWarningMessage.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            //base.Reset();
        }

        #region Command Functions
        public void PerformRawCommand(string command, string completeEvent, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkySGMPumpRawCommandHandler(this, command, completeEvent));
            }
        }



        public void Start()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkySGMPumpStartHandler(this));
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SkySGMPumpStopHandler(this));
            }
        }



        internal void NoteStart(bool value)
        {
            PumpStarted = value;
        }

        public void MonitorRawCommand(bool isSelected, string command, string completeEvent, string parameter)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SkySGMPumpRawCommandHandler) && ((SkySGMPumpHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SkySGMPumpRawCommandHandler(this, command, completeEvent));
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
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(SkySGMPumpReadRunParaHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new SkySGMPumpReadRunParaHandler(this));
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

        public void Terminate()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
