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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FujikinMFC
{
    public class FujikinMFC  : SerialPortDevice, IConnection
    {
        public string Address { get { return _address; } }

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

        private FujikinMFCConnection _connection;
        public FujikinMFCConnection Connection
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
        public FujikinMFC(string module, string name, string scRoot, string portName) : base(module, name)
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
            _connection = new FujikinMFCConnection(PortName);
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
 
            _connection = new FujikinMFCConnection(portName, bautRate, dataBits, parity, stopBits);
 
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
                            //_lstHandler.AddLast(new FujikinMFCQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new FujikinMFCSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        internal void NoteNAK()
        {
            ResponseNAK = true;
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

        internal void NoteActionCompleted()
        {
        }

        public override bool Home(out string reason)
        {
            return base.Home(out reason);
        }


        #region Command Functions

        public void PerformRawCommand(string commandType, string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCRawCommandHandler(this, commandType, command, comandArgument));
            }
        }



        internal void NoteExcetionInfo(byte value)
        {
            //parse exception status here
            ExceptionInfo = value;
        }



        public void ResetMFC()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCResetHandler(this));
            }
        }

        public void SetFlowTotalizerAlarmEnable(bool enableValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSetEnableHandler(this, "65,01,B5", enableValue.ToString()));
            }
        }

        public void SetFlowTotalizerAlarmEnable(string enableValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSetEnableHandler(this, "65,01,B5", enableValue));
            }
        }

        public void SetFlowTotalizerAlarmLevel(int value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet32BitValueHandler(this, "65,01,B6", value));
            }
        }
        public void SetFlowTotalizerAlarmLevel(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet32BitValueHandler(this, "65,01,B6", int.Parse(value)));
            }
        }

        public void SetFlowMeterAlarmEnable(string enableValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSetEnableHandler(this, "65,01,B6", enableValue));
            }
        }
        public void SetFlowMeterAlarmTripPointHigh(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet16BitValueHandler(this, "68,01,11", short.Parse(value)));
            }
        }
        public void SetFlowMeterZeroEnable(string enableValue)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSetEnableHandler(this, "68,01,A5", enableValue));
            }
        }

        public void SetFlowMeterRequestedZero(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet8BitValueHandler(this, "68,01,BA", byte.Parse(value)));
            }
        }
        public void SetFlowControllerControlModeSelection(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet8BitValueHandler(this, "69,01,03", byte.Parse(value)));
            }
        }
        public void SetFlowControllerDefaultControlMode(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet8BitValueHandler(this, "69,01,04", byte.Parse(value)));
            }
        }
        public void SetFlowControllerFreezeFollow(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet8BitValueHandler(this, "69,01,05", byte.Parse(value)));
            }
        }
        public void SetFlowControllerSetPoint(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet16BitValueHandler(this, "69,01,A4", short.Parse(value)));
            }
        }
        public void SetValveDriverValveOverride(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet8BitValueHandler(this, "6A,01,01", byte.Parse(value)));
            }
        }

        public void SetValveDriverRampTime(string value)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new FujikinMFCSet32BitValueHandler(this, "6A,01,A4", int.Parse(value)));
            }
        }

        internal void NoteAlarmDetail(short value)
        {
            //parse
            AlarmDetail = value;
        }

        public void ReadExceptionStatus(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadExceptionStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadExceptionStatusHandler(this));
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

        internal void NoteWarningDetail(short value)
        {
            //parse
            WarningDetail = value;
        }

        public void ReadAlarmDetail(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadExceptionStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadAlarmDetailHandler(this));
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
        public void ReadWarningDetail(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadExceptionStatusHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadWarningDetailHandler(this));
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

        public bool FlowTotalizerAlarmEnable { get; set; }
        public void ReadFlowTotalizerAlarmEnable(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowTotalizerAlarmEnable";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadEnableHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadEnableHandler(this, "65,01,B5", propName));
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

        public int FlowTotalizerAlarmLevel { get; set; }
        public void ReadFlowTotalizerAlarmLevel(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowTotalizerAlarmLevel";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead32BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead32BitHandler(this, "65,01,B6", propName));
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


        //parse set
        public short FlowMeterStatus { get; set; }
        public void ReadFlowMeterStatus(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterStatus";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "68,01,07", propName));
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

        public bool FlowMeterAlarmEnable { get; set; }
        public void ReadFlowMeterAlarmEnable(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterAlarmEnable";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadEnableHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadEnableHandler(this, "68,01,08", propName));
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

        public short FlowMeterAlarmTripPointHigh { get; set; }
        public void ReadFlowMeterAlarmTripPointHigh(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterAlarmTripPointHigh";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead16BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead16BitHandler(this, "68,01,11", propName));
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

        public bool FlowMeterZeroEnable { get; set; }
        public void ReadFlowMeterZeroEnable(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterZeroEnable";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCReadEnableHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCReadEnableHandler(this, "68,01,A5", propName));
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

        public short FlowMeterRequestedZero { get; set; }
        public void ReadFlowMeterRequestedZero(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterRequestedZero";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "68,01,BA", propName));
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

        public short FlowControllerControlModeSelection { get; set; }
        public void ReadFlowControllerControlModeSelection(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowControllerControlModeSelection";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "69,01,03", propName));
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

        public short FlowControllerFreezeFollow { get; set; }
        public void ReadFlowControllerFreezeFollow(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowControllerFreezeFollow";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "69,01,05", propName));
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

        //parse set
        public short FlowControllerStatus { get; set; }
        public void ReadFlowControllerStatus(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowControllerStatus";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "69,01,0A", propName));
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

        public short FlowControllerDefaultControlMode { get; set; }
        public void ReadFlowControllerDefaultControlMode(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowControllerDefaultControlMode";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "69,01,04", propName));
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

        public int FlowMeterTotalizerData { get; set; }
        public void ReadFlowMeterTotalizerData(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowMeterTotalizerData";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead32BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead32BitHandler(this, "68,01,B2", propName));
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

        public short FlowControllerSetPoint { get; set; }
        public void ReadFlowControllerSetPoint(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "FlowControllerSetPoint";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead16BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead16BitHandler(this, "69,01,A4", propName));
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


        public short ValveDriverValveOverride { get; set; }
        public void ReadValveDriverValveOverride(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverValveOverride";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "6A,01,01", propName));
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

        //parse set
        public short ValveDriverStatus { get; set; }
        public void ReadValveDriverStatus(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverStatus";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "6A,01,07", propName));
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

        public short ValveDriverValveType { get; set; }
        public void ReadValveDriverValveType(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverValveType";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead8BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead8BitHandler(this, "6A,01,A0", propName));
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

        public short ValveDriverFilteredSetpoint { get; set; }
        public void ReadValveDriverFilteredSetpoint(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverFilteredSetpoint";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead16BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead16BitHandler(this, "6A,01,A6", propName));
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
        public short ValveDriverIndicatedFlow { get; set; }
        public void ReadValveDriverIndicatedFlow(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverIndicatedFlow";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead16BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead16BitHandler(this, "6A,01,A9", propName));
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
        public short ValveDriverValveVoltage { get; set; }
        public void ReadValveDriverValveVoltage(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverValveVoltage";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead16BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead16BitHandler(this, "6A,01,B6", propName));
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

        public int ValveDriverRampTime { get; set; }
        public void ReadValveDriverRampTime(bool isSelected)
        {
            lock (_locker)
            {
                string propName = "ValveDriverRampTime";
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRead32BitHandler) && ((FujikinMFCHandler)handler).DevicePropName == propName);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRead32BitHandler(this, "6A,01,A4", propName));
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

        internal void NoteResetCompleted()
        {
            ResetCompleted = true;
        }

        public void MonitorRawCommand(bool isSelected,  string commandType, string command, string comandArgument)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(FujikinMFCRawCommandHandler) && ((FujikinMFCHandler)handler)._commandType == commandType && ((FujikinMFCHandler)handler)._command == command);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new FujikinMFCRawCommandHandler(this, commandType, command, comandArgument));
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



        public short ExceptionInfo { get; private set; }
        public bool ResetCompleted { get; private set; }
        public bool ResponseNAK { get; private set; }
        public short WarningDetail { get; private set; }
        public short AlarmDetail { get; private set; }



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
        internal void NoteRawCommandInfo(string commandType, string command, string data, bool isAck)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandType == commandType && res.SourceCommand == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommandType = commandType, SourceCommand = command, ResonseContent = data, IsAck = isAck, ResonseRecievedTime = DateTime.Now });
        }


        #endregion
    }




}
