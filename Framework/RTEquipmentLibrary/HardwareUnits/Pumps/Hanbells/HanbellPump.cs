using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.Hanbell
{
    public class HanbellPump : PumpBase, IConnection
    {
        public string Address { get { return _slaveID; } }
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

        private HanbellPumpTCPConnection _connection;
        public HanbellPumpTCPConnection Connection
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
                    DeviceName = "Pump",
                };
                return data;
            }
        }

        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();

        private object _locker = new object();

        private bool _enableLog;
        private string _scRoot;
        private string _slaveID = "158";
        public HanbellPump(string module, string name, string scRoot) : base(module, name)
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

            string deviceIP = "";
            if (!string.IsNullOrEmpty(_scRoot))
            {
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
                deviceIP = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress");
            }
            else
            {
                _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
                deviceIP = SC.GetStringValue($"{Module}.{Name}.DeviceAddress");
            }

            _connection = new HanbellPumpTCPConnection(deviceIP);
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
                //return false;
            }
            System.Threading.Thread.Sleep(500);

            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            //RequestRegisters(true, 2201, 45);
            _lstMonitorHandler.AddFirst(new HanbellPumpRequestRegistersHandler(this, byte.Parse(_slaveID), 2201, 45));

            return true;
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
                        //_connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new HanbellPumpQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new HanbellPumpSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
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

        internal void NoteDPCurrentHigh(int value)
        {
            DPCurrentHigh = value == 1 ? "true" : "false";
        }
        internal void NoteBPCurrentHigh(int value)
        {
            BPCurrentHigh = value == 1 ? "true" : "false";
        }
        internal void NoteDPPower(int power)
        {
            DPPower = power;
        }
        internal void NoteBPPower(int power)
        {
            BPPower = power;
        }

        internal void NoteDPSpeed(int speed)
        {
            DPSpeed = speed;
        }
        internal void NoteBPSpeed(int speed)
        {
            BPSpeed = speed;
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

        internal void NoteVaccumPumpCompleted(bool isOn)
        {
            if (isOn)
            {
                VaccumPumpOnCompleted = true;
            }
            else
            {
                VaccumPumpOffCompleted = true;
            }
        }
        internal void NoteBPPumpOnOff(bool isOn)
        {
            if (isOn)
            {
                BPPumpOnCompleted = true;
            }
            else
            {
                BPPumpOffCompleted = true;
            }
        }
        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;
            IsError = false;

            base.Reset();
        }

        #region Command Functions


        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HanbellPumpRawCommandHandler(this, byte.Parse(_slaveID), command, comandArgument));
            }
        }

        public override void SetPumpOnOff(bool isOn)
        {
            if (isOn)
                StartVaccumPump();
            else
                StopVaccumPump();
        }

        public void StartVaccumPump()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new HanbellPumpStartVaccumPumpHandler(this, byte.Parse(_slaveID)));
            }
        }
        public void StopVaccumPump()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new HanbellPumpStopVaccumPumpHandler(this, byte.Parse(_slaveID)));
            }
        }
        public void StartBPPump()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new HanbellPumpStartBPPumpHandler(this, byte.Parse(_slaveID)));
            }
        }
        public void StopBPPump()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new HanbellPumpStopBPPumpHandler(this, byte.Parse(_slaveID)));
            }
        }
        public void ResetPump()
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new HanbellPumpResetHandler(this, byte.Parse(_slaveID)));
            }
        }



        #endregion

        #region Properties
        public string CommunicationMode { get; private set; }
        public int DPPower { get; private set; }
        public int BPPower { get; private set; }
        public int DPSpeed { get; private set; }
        public int BPSpeed { get; private set; }
        public string BPCurrentHigh { get; private set; }
        public string DPCurrentHigh { get; private set; }
        public override bool IsOn { get; set; }
        public bool VaccumPumpOnCompleted { get; private set; }
        public bool VaccumPumpOffCompleted { get; private set; }
        public bool BPPumpOnCompleted { get; private set; }
        public bool BPPumpOffCompleted { get; private set; }

        #endregion

        #region Note Functions


        internal void NoteRawCommandInfo(string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommand == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommand = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }
        public void NoteError(string reason)
        {
            if (reason != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
                }
                IsError = true;
            }
        }

        #endregion
    }


}
