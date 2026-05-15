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
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Event;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FY8300
{
    public class FY8300 : IDevice, IConnection
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

        private FY8300Connection _connection;
        public FY8300Connection Connection
        {
            get { return _connection; }
        }
        public bool IsReady()
        {
            return IsIdle && _lstHandler.Count == 0 && !_isError && !_connection.IsBusy && !_connection.IsCommunicationError && _connection.IsConnected;
        }
        public bool IsWaferPresence { get; set; }

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
        private string Module;
        private string Name;

        public virtual AITDeviceData DeviceData
        {
            get
            {
                AITDeviceData data = new AITDeviceData()
                {
                    UniqueName = _portName,
                    DeviceName = Name,
                    //DeviceSchematicId = DeviceID,
                    //DisplayName = DisplayName,
                    

                };
                //data.AttrValue.Add("CH1WAVEFORM",);
                //data.AttrValue.Add("CH1FREQUENCY",);
                //data.AttrValue.Add("CH1RANGE",);
                //data.AttrValue.Add("CH1OUTPUT",);
                //data.AttrValue.Add("CH2WAVEFORM",);
                //data.AttrValue.Add("CH2FREQUENCY",);
                //data.AttrValue.Add("CH2RANGE",);
                //data.AttrValue.Add("CH2OUTPUT",);
                //data.AttrValue.Add("CH3WAVEFORM",);
                //data.AttrValue.Add("CH3FREQUENCY",);
                //data.AttrValue.Add("CH3RANGE",);
                //data.AttrValue.Add("CH3OUTPUT",);
                return data;
            }
        }

        public FY8300(string module, string name, string scRoot)
        {
            _scRoot = scRoot;
            Module = module;
            Name = name;
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

            _connection = new FY8300Connection(_portName, 115200);
            _connection.IsEnableHandlerRetry = true;
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            DATA.Subscribe($"{module}.IsConnected", () => IsConnected);
            DATA.Subscribe($"{module}.Address", () => Address);
            OP.Subscribe($"{module}.Reconnect", (string cmd, object[] args) =>
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

            _connection = new FY8300Connection(portName, bautRate, dataBits, parity, stopBits);

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

        public void Monitor()
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

        #region Command Functions
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
        public bool IsIdle { get; private set; }
        string IDevice.Module { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        string IDevice.Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool HasAlarm => throw new NotImplementedException();

        #endregion


        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;


        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool Initialize()
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
