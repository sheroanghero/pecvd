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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA603
{
    public class PfeifferPumpA603 : PumpBase, IConnection
    {
        public string Address => Connection.Address;
        public bool IsConnected => Connection.IsConnected && !_connection.IsCommunicationError;
        public bool NeedGetCtrl { get; set; }

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public string PortStatus { get; set; } = "Closed";

        private PfeifferPumpA603Connection _connection;
        public PfeifferPumpA603Connection Connection
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

        public PfeifferPumpA603(string module, string name, string scRoot) : base(module, name)
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
            _connection = new PfeifferPumpA603Connection(_portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            if (NeedGetCtrl)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "CTRL", "ON"));
            }
            _lstMonitorHandler.AddLast(new PfeifferPumpA603ReadPumpStatusHandler(this));

            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{Module}.{Name}.Address", () => Address);
            OP.Subscribe($"{Module}.{Name}.Reconnect", (string cmd, object[] args) => {
                Disconnect();
                Connect();
                return true;
            });

            return true;
        }

        public bool InitConnection(string portName, int bautRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _connection = new PfeifferPumpA603Connection(portName, bautRate, dataBits, parity, stopBits);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        internal void NoteSetParaCompleted()
        {
            SetParaCompleted = true;
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
                _lstHandler.AddLast(new PfeifferPumpA603RawCommandHandler(this, command, null));
            }
        }

        public void PerformRawCommand(string command, string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603RawCommandHandler(this, command, parameter));
            }
        }

        public override void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "SYS", isOn ? "ON" : "OFF"));
            }
        }

        public void ControlOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "CTRL", parameter));
            }
        }

        public void EchoOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "ECH", parameter));
            }
        }

        public void RootsOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "ROO", parameter));
            }
        }

        public void PumpOnOff(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603SimpleSwitchHandler(this, "SYS", parameter));
            }
        }

        public void DisplayAlarmOrAlert(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpA603DisplayAlarmHandler(this, parameter));
            }
        }

        public void SetPumpParameter(string parameter)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, parameter));
            }
        }

        public void SetFBPowerAlert(string alertValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, "000" + alertValue));
            }
        }

        public void SetFBPowerAlarm(string alarmValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new PfeifferPumpSetPumpParameterHandler(this, "001" + alarmValue));
            }
        }

        internal void NoteSwitchCompleted(string command, bool value)
        {
            //if (command == "CTRL")
            //    SetControlOnOff = value;
            //else if (command == "ECH")
            //    SetEchoOnOff = value;
            //else if (command == "ROO")
            //    SetRootsOnOff = value;
            //else if (command == "SYS")
            //    SetPumpOnOff = value;
        }

        internal void NoteDisplayAlarmOrAlert(bool value)
        {
            DisplayAlarm = value;
            DisplayAlert = !value;
        }

        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(PfeifferPumpA603RawCommandHandler) && ((PfeifferPumpA603Handler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new PfeifferPumpA603RawCommandHandler(this, command, null));
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

        public void ReadPumpStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(PfeifferPumpA603ReadPumpStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new PfeifferPumpA603ReadPumpStatusHandler(this));
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

        #endregion Command Functions

        #region Properties

        public string Error { get; private set; }

        //public bool SetControlOnOff { get; private set; }
        //public bool SetEchoOnOff { get; private set; }
        //public bool SetRootsOnOff { get; private set; }
        //public bool SetPumpOnOff { get; private set; }
        public bool DisplayAlarm { get; private set; }
        public bool DisplayAlert { get; private set; }
        public bool SetParaCompleted { get; private set; }

        public PumpRunStatus FBStatus { get; private set; }
        public PumpRunStatus RootsStatus { get; private set; }
        public ValveStatus N2Valve { get; private set; }
        public ValveStatus StandbyValve { get; private set; }
        public ValveStatus WaterValve { get; private set; }
        public ValveStatus InletValve { get; private set; }
        public ValveStatus FBWaterValve { get; private set; }
        public ValveStatus PermitValve { get; private set; }
        public ValveStatus RootsWaterValve { get; private set; }
        public PumpLLStatus PumpLLStatus { get; private set; }
        public ControlMode ControlMode { get; private set; }
        public int Pressure { get; private set; }
        public int FBPower { get; private set; }
        public int FBTemp { get; private set; }
        public int AuxiliaryTemp { get; private set; }
        public int AnalogInput { get; private set; }
        public int N2Flow { get; private set; }
        public int N2Prolong { get; private set; }
        public FaultResult PressureFault { get; private set; }
        public FaultResult AuxiliaryFault { get; private set; }
        public FaultResult ConsumpationFault { get; private set; }
        public FaultResult EAnalogFault { get; private set; }
        public FaultResult E1LogicFault { get; private set; }
        public FaultResult E2LogicFault { get; private set; }
        public FaultResult FCFault { get; private set; }
        public FaultResult WaterFlowFault { get; private set; }
        public FaultResult PowerFail { get; private set; }
        public FaultResult TempMotFault { get; private set; }
        public FaultResult ValveFault { get; private set; }
        public FaultResult BreakerFault { get; private set; }
        public FaultResult N2PurgeFault { get; private set; }
        public FaultResult MaintenanceFault { get; private set; }
        public FaultResult ADPHighTempFault { get; private set; }
        public FaultResult ADPLowTempFault { get; private set; }
        public FaultResult RootsTempFault { get; private set; }
        public FaultResult LLPumpFault { get; private set; }
        public FaultResult E3LogicFault { get; private set; }

        internal void NotePumpStatus(string data)
        {
            try
            {
                var statusList = data.Split(' ');

                var status0Array = statusList[0];
                FBStatus = (PumpRunStatus)int.Parse(status0Array[0].ToString());
                RootsStatus = (PumpRunStatus)int.Parse(status0Array[1].ToString());
                N2Valve = (ValveStatus)int.Parse(status0Array[2].ToString());
                StandbyValve = (ValveStatus)int.Parse(status0Array[3].ToString());
                WaterValve = (ValveStatus)int.Parse(status0Array[4].ToString());
                InletValve = (ValveStatus)int.Parse(status0Array[5].ToString());
                FBWaterValve = (ValveStatus)int.Parse(status0Array[6].ToString());
                PermitValve = (ValveStatus)int.Parse(status0Array[7].ToString());
                RootsWaterValve = (ValveStatus)int.Parse(status0Array[8].ToString());
                PumpLLStatus = (PumpLLStatus)int.Parse(status0Array[9].ToString());
                ControlMode = (ControlMode)int.Parse(status0Array[10].ToString());

                IsOn = FBStatus == PumpRunStatus.Running;

                Pressure = Str2Int(statusList[1]);
                FBPower = Str2Int(statusList[2]);
                FBTemp = Str2Int(statusList[3]); ;
                AuxiliaryTemp = Str2Int(statusList[5]);
                AnalogInput = Str2Int(statusList[6]);
                N2Flow = Str2Int(statusList[7]);

                var status8Array = statusList[8];
                PressureFault = (FaultResult)int.Parse(status8Array[0].ToString());
                AuxiliaryFault = (FaultResult)int.Parse(status8Array[1].ToString());
                ConsumpationFault = (FaultResult)int.Parse(status8Array[2].ToString());
                EAnalogFault = (FaultResult)int.Parse(status8Array[3].ToString());
                E1LogicFault = (FaultResult)int.Parse(status8Array[4].ToString());
                E2LogicFault = (FaultResult)int.Parse(status8Array[5].ToString());
                FCFault = (FaultResult)int.Parse(status8Array[6].ToString());
                WaterFlowFault = (FaultResult)int.Parse(status8Array[7].ToString());
                PowerFail = (FaultResult)int.Parse(status8Array[8].ToString());
                TempMotFault = (FaultResult)int.Parse(status8Array[9].ToString());
                ValveFault = (FaultResult)int.Parse(status8Array[10].ToString());
                BreakerFault = (FaultResult)int.Parse(status8Array[11].ToString());
                N2PurgeFault = (FaultResult)int.Parse(status8Array[12].ToString());
                MaintenanceFault = (FaultResult)int.Parse(status8Array[13].ToString());
                ADPHighTempFault = (FaultResult)int.Parse(status8Array[14].ToString());
                ADPLowTempFault = (FaultResult)int.Parse(status8Array[15].ToString());
                RootsTempFault = (FaultResult)int.Parse(status8Array[16].ToString());
                LLPumpFault = (FaultResult)int.Parse(status8Array[17].ToString());
                E3LogicFault = (FaultResult)int.Parse(status8Array[18].ToString());

                IsError = status8Array.Contains("1") || status8Array.Contains("2");
                IsOverTemperature = TempMotFault != FaultResult.OK;

                N2Prolong = Str2Int(statusList[9]);
            }
            catch (Exception ex)
            {
                NoteError("Invalid Pump Status");
                LOG.Error($"Invalid Pump Status: {data}, Exception info: {ex}");
            }
        }

        private int Str2Int(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
                throw new Exception("empty int value");
            //if (sValue.Length > 1)
            //{
            //    sValue = sValue.TrimStart('0');
            //}
            return int.Parse(sValue);
        }

        #endregion Properties

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
            //var curIOResponse = IOResponseList.Find(res => res.SourceCommand == command);
            //if (curIOResponse != null)
            //{
            //    IOResponseList.Remove(curIOResponse);
            //}
            //IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }

        #endregion Note Functions
    }
}
