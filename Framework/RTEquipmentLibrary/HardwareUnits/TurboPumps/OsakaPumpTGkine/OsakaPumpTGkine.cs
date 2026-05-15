using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.OsakaPumpTGkine
{
    public class HighLevelReader
    {
        public OsakaPumpTGkineLowerWriteHandler IndexWriter { get; set; }
        public OsakaPumpTGkineLowerWriteHandler CommandWriter { get; set; }
        public OsakaPumpTGkineLowerReadHandler CommandReader { get; set; }
        public OsakaPumpTGkineLowerReadHandler ValueReader { get; set; }

        public bool IsActive { get; set; }
        public OsakaPumpTGkineHandler ActiveHandler { get; set; }

        private OsakaPumpTGkine _device;
        private byte _indexNumberLow;

        private Stopwatch _timer = new Stopwatch();

        public void Reset(OsakaPumpTGkine device, byte indexNumberHigh, byte indexNumberLow)
        {
            IndexWriter = new OsakaPumpTGkineLowerWriteHandler(device, 0x80, 0x80, indexNumberHigh, indexNumberLow);

            CommandWriter = new OsakaPumpTGkineLowerWriteHandler(device, 0x80, 0x81, 0x00, 0x55);

            CommandReader = new OsakaPumpTGkineLowerReadHandler(device, 0x80, 0x81);

            ValueReader = new OsakaPumpTGkineLowerReadHandler(device, 0x80, 0x82);

            ActiveHandler = IndexWriter;

            _device = device;

            _indexNumberLow = indexNumberLow;

            IsActive = true;
        }

        public void Clear()
        {
            IsActive = false;

            IndexWriter = null;
            CommandWriter = null;
            CommandReader = null;
            ValueReader = null;
            _device = null;
            _indexNumberLow = 0;
        }

        public void ReadDone()
        {
            if (!IsActive)
                return;

            if (ActiveHandler == CommandReader)
            {
                ActiveHandler = ValueReader;
                return;
            }

            if (ActiveHandler == ValueReader)
            {
                _device.NoteReadResult(_indexNumberLow, ValueReader.ResultHigh, ValueReader.ResultLow);

                Clear();
                return;
            }
        }

        public void WriteDone()
        {
            if (!IsActive)
                return;

            if (ActiveHandler == IndexWriter)
            {
                ActiveHandler = CommandWriter;
                return;
            }

            if (ActiveHandler == CommandWriter)
            {
                ActiveHandler = CommandReader;
                return;
            }
        }
    }

    public class HighLevelWriter
    {
        public OsakaPumpTGkineLowerWriteHandler IndexWriter { get; set; }
        public OsakaPumpTGkineLowerWriteHandler ValueWriter { get; set; }
        public OsakaPumpTGkineLowerWriteHandler CommandWriter { get; set; }
        public OsakaPumpTGkineLowerReadHandler CommandReader { get; set; }

        public bool IsActive { get; set; }

        public OsakaPumpTGkineHandler ActiveHandler { get; set; }

        private OsakaPumpTGkine _device;
        private byte _indexNumberLow;
        private byte _valueHigh;
        private byte _valueLow;

        private Stopwatch _timer = new Stopwatch();

        public void Reset(OsakaPumpTGkine device, byte indexNumberHigh, byte indexNumberLow, byte valueHigh, byte valueLow)
        {
            IndexWriter = new OsakaPumpTGkineLowerWriteHandler(device, 0x80, 0x80, indexNumberHigh, indexNumberLow);

            ValueWriter = new OsakaPumpTGkineLowerWriteHandler(device, 0x80, 0x82, valueHigh, valueLow);

            CommandWriter = new OsakaPumpTGkineLowerWriteHandler(device, 0x80, 0x81, 0x00, 0xAA);

            CommandReader = new OsakaPumpTGkineLowerReadHandler(device, 0x80, 0x81);

            ActiveHandler = IndexWriter;

            _device = device;

            _indexNumberLow = indexNumberLow;
            _valueHigh = valueHigh;
            _valueLow = valueLow;

            IsActive = true;
        }

        public void Clear()
        {
            IsActive = false;

            IndexWriter = null;
            CommandWriter = null;
            CommandReader = null;
            ValueWriter = null;
            _device = null;
            _indexNumberLow = 0;
            _valueHigh = 0;
            _valueLow = 0;
        }

        public void ReadDone()
        {
            if (!IsActive)
                return;

            if (ActiveHandler == CommandReader)
            {
                Clear();

                return;
            }
        }

        public void WriteDone()
        {
            if (!IsActive)
                return;

            if (ActiveHandler == IndexWriter)
            {
                ActiveHandler = ValueWriter;
                return;
            }

            if (ActiveHandler == ValueWriter)
            {
                ActiveHandler = CommandWriter;
                return;
            }

            if (ActiveHandler == CommandWriter)
            {
                ActiveHandler = CommandReader;
                return;
            }
        }
    }

    public class OsakaPumpTGkine : PumpBase, IConnection
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

        private OsakaPumpTGkineConnection _connection;
        public OsakaPumpTGkineConnection Connection
        {
            get { return _connection; }
        }
        public override bool IsStable
        {
            get { return IsAtTargetSpeed && !IsBrake; }
        }

        public override AITPumpData DeviceData
        {
            get
            {
                AITPumpData data = new AITPumpData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    DeviceModule = Module,
                    IsError = IsFailure || IsAlarm,
                    IsOn = IsAtTargetSpeed,
                    Speed = (int)Speed,
                };

                return data;
            }
        }

        public bool IsRemoteControl { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFailure { get; private set; }
        public bool IsAlarm { get; private set; }
        public bool IsAtTargetSpeed { get; private set; }
        public bool IsAtRatedSpeed { get; private set; }
        public bool IsAtLowSpeed { get; private set; }
        public bool IsNotRotation { get; private set; }
        public bool IsBrake { get; private set; }
        public bool IsAccelerate { get; private set; }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
 
        private object _locker = new object();

        private bool _enableLog;
        private string _address;
        private string _scRoot;
        private R_TRIG _trigWarningMessage = new R_TRIG();


        private HighLevelReader _reader = new HighLevelReader();

        private HighLevelWriter _writer = new HighLevelWriter();


        private FixSizeQueue<byte> _readIndexNumber = new FixSizeQueue<byte>(10);
        private FixSizeQueue<Tuple<byte, byte, byte>> _writeIndexNumber = new FixSizeQueue<Tuple<byte, byte, byte>>(10);
        private Stopwatch _readSpeedTimer = new Stopwatch();

        public OsakaPumpTGkine(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;
        }


        public override bool Initialize()
        {
            base.Initialize();

            if (_connection != null && _connection.IsConnected)
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            _address = SC.GetStringValue($"{_scRoot}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");
            _connection = new OsakaPumpTGkineConnection(_address);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            _readSpeedTimer.Start();

            return true;
        }


        public bool Initialize(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            base.Initialize();

            //_address = SC.GetStringValue($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.Address");
            _enableLog = true;// SC.GetValue<bool>($"{(!string.IsNullOrEmpty(_scRoot) ? _scRoot + "." : "")}{Module}.{Name}.EnableLogMessage");

            _connection = new OsakaPumpTGkineConnection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(2000, OnTimer, $"{Module}.{Name} MonitorHandler", true);


            return true;
        }

        private bool OnTimer()
        {
            try
            {
                //_connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
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
                            //_lstHandler.AddLast(new OsakaPumpTGkineQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new OsakaPumpTGkineSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_writer.IsActive)
                        {
                            handler = _writer.ActiveHandler;
                        }else if (_reader.IsActive)
                        {
                            handler = _reader.ActiveHandler;
                        }else if (_writeIndexNumber.TryDequeue(out var writeTask))
                        {
                            _writer.Reset(this, 0x00, writeTask.Item1, writeTask.Item2, writeTask.Item3);
                            handler = _writer.ActiveHandler;
                        }else if (_readIndexNumber.TryDequeue(out var readTask))
                        {
                            _reader.Reset(this, 0x00, readTask);
                            handler = _reader.ActiveHandler;
                        }

                        if(handler == null)
                        {
                            if(_readSpeedTimer.ElapsedMilliseconds > 1000)
                            {
                                _readSpeedTimer.Restart();
                                ReadPumpSpeed();
                            }
                            else
                            {
                                ReadPumpStatus();
                            }
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

        internal void NoteLowerWriteDone()
        {
            _reader.WriteDone();
            _writer.WriteDone();
        }


        internal void NoteLowerReadDone()
        {
            _reader.ReadDone();
            _writer.ReadDone();
        }

        internal void NoteReadResult(byte indexNumberLow, byte valueHigh, byte valueLow)
        {
            if (indexNumberLow == 0x40)
            {
                IsRunning = false;
                IsFailure = false;
                IsAlarm = false;
                IsAtTargetSpeed = false;
                IsAtRatedSpeed = false;
                IsAtLowSpeed = false;
                IsNotRotation = false;
                IsBrake = false;
                IsAccelerate = false;
                if (valueHigh == 0x01 && valueLow == 0x20)
                {
                    IsRunning = true;
                    IsNotRotation = true;
                }
                else if (valueHigh == 0x03 && valueLow == 0x80)
                {
                    IsRunning = true;
                    IsAccelerate = true;
                }
                else if (valueHigh == 0x63 && valueLow == 0x00)
                {
                    IsRunning = true;
                    IsAtTargetSpeed = true;
                    IsAtRatedSpeed = true;
                }
                else if (valueHigh == 0x01 && valueLow == 0x40)
                {
                    IsRunning = true;
                    IsBrake = true;
                }
                else if (valueHigh == 0x05 && valueLow == 0x40)
                {
                    IsBrake = true;
                    IsFailure = true;
                    IsAlarm = true;
                }
                else if (valueHigh == 0x05 && valueLow == 0x02)
                {
                    IsNotRotation = true;
                    IsFailure = true;
                    IsAlarm = true;
                }
                else if (valueHigh == 0x05 && valueLow == 0x00)
                {
                    IsFailure = true;
                    IsAlarm = true;
                }
            }

            if (indexNumberLow == 0x01)
            {
                Speed =  (valueHigh << 8) + (valueLow & 0xFFFF);
            }

            if (indexNumberLow == 0x12)
            {
                Temperature = (float) (((valueHigh << 8) + (valueLow & 0xFFFF))/10.0);

            }
        }

        internal void NoteWrite()
        {
 
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

        public override void SetPumpOnOff(bool isOn)
        {
            byte valueHigh = 0;
            byte valueLow = 0;
            valueHigh = Converter.SetBit(valueHigh, 1, true);

            _writeIndexNumber.Enqueue(Tuple.Create(isOn ? (byte)0x08 : (byte)0x09, valueHigh, valueLow));
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

        public void ReadPumpStatus()
        {
            _readIndexNumber.Enqueue(0x40);
        }

        public void ReadPumpSpeed()
        {
            _readIndexNumber.Enqueue(0x01);
        }

        public void ReadPumpMotorTemp()
        {
            _readIndexNumber.Enqueue(0x12);
        }

        public void ReadPumpErrorCode()
        {
            _readIndexNumber.Enqueue(0x17);
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

            }
            else
            {

            }
        }

    }



}
