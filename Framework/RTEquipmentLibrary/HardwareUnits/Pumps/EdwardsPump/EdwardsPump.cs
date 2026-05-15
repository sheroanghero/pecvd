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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump
{
    public class EdwardsPump  : PumpBase, IConnection
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

        private EdwardsPumpConnection _connection;
        public EdwardsPumpConnection Connection
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

        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;
        private string _portName;
        private string _scRoot;
        public EdwardsPump(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
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

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            _portName = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");
            _connection = new EdwardsPumpConnection(_portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            lock (_locker)
            {
                _lstHandler.AddLast(new EdwardsGetControlHandler(this));
                _lstHandler.AddLast(new EdwardsFormatModeHandler(this, true));
            }
            _lstMonitorHandler.AddLast(new EdwardsQueryPumpStatusHandler(this));

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
                            lock (_locker)
                            {
                                _lstHandler.AddLast(new EdwardsGetControlHandler(this));
                                _lstHandler.AddLast(new EdwardsFormatModeHandler(this, true));
                            }
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

        internal void NoteSwitchCompleted()
        {
            ActionCommandCompleted = true;
        }
        public override void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {
                if(isOn)
                    _lstHandler.AddLast(new EdwardsSwitchOnPumpHandler(this));
                else
                    _lstHandler.AddLast(new EdwardsSwitchOffPumpHandler(this));
            }
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        public bool ActionCommandCompleted { get; private set; }

        private string _systemType;
        //private Dictionary<string, Dictionary<string, ParaResponse>> AlarmStatusDict = new Dictionary<string, Dictionary<string, ParaResponse>>();
        private Dictionary<int, ParaResponse> AlarmStatusDict = new Dictionary<int, ParaResponse>();
        private Dictionary<int, ParaResponse> BitfieldStatusDict = new Dictionary<int, ParaResponse>();
        private Dictionary<int, ParaResponse> ParaValueDict = new Dictionary<int, ParaResponse>();

        public ParaResponse GetAlarmStatus(int parameter)
        {
            if (AlarmStatusDict.ContainsKey(parameter))
                return AlarmStatusDict[parameter];
            else
                return null;
        }

        internal void NoteAlarmStatus(string parameter, string data)
        {
            try
            {
                int iPara = int.Parse(parameter);
                if (AlarmStatusDict.ContainsKey(iPara))
                {
                    AlarmStatusDict[iPara] = new ParaResponse(parameter);
                }
                else
                {
                    AlarmStatusDict.Add(iPara, new ParaResponse(data));
                }
            }
            catch (Exception ex)
            {
                NoteError("Invalid Alarm Status");
                LOG.Error($"Invalid Alarm Status: {data}, Exception info: {ex}");
            }

        }        

        private int Str2Int(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
                throw new Exception("empty int value");
            if(sValue.Length > 1)
            {
                sValue = sValue.TrimStart('0');
            }
            return int.Parse(sValue);
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
            var curIOResponse = IOResponseList.Find(res => res.SourceCommand == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion
    }

    public enum PumpRunStatus
    {
        Stopped,
        Running
    }
    public enum ValveStatus
    {
        Closed,
        Opened
    }
    public enum PumpLLStatus
    {
        Off,
        On
    }
    public enum ControlMode
    {
        Local,
        Remote
    }
    public enum DefectResult
    {
        OK,
        Warning,
        Hazard
    }
    public class ParaResponse
    {
        public ParaResponse(string paraRes)
        {
            var paraArray = paraRes.Split(',');
            priorityLevel = int.Parse(paraArray[0]);
            alarmType = int.Parse(paraArray[1]);
            bitfiledStatus = int.Parse(paraArray[2]);
        }
        public int priorityLevel;
        public int alarmType;
        public int bitfiledStatus;
    }


}
