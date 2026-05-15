using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
    public abstract class SerialPortConnectionBase : IConnection
    {
        public string Address
        {
            get { return _address; }
        }

        public bool IsConnected
        {
            get { return _port.IsOpen(); }
        }

        public bool Connect()
        {
            return _port.Open();
        }

        public bool Disconnect()
        {
            _port.Close();
            return true;
        }

        public void TerminateCom()
        {
            _port.Dispose();
        }

        public bool IsBusy
        {
            get { return _activeHandler != null; }
        }

        public bool IsCommunicationError { get; private set; }
        public string LastCommunicationError { get; private set; }

        private AsyncSerialPort _port;

        protected HandlerBase _activeHandler; //set, control, 
        public HandlerBase ActiveHandler => _activeHandler;
        public HandlerBase HandlerInError;

        private object _lockerActiveHandler = new object();

        private string _address;

        private bool _isAsciiMode;

        public int retryTime = 0;
        public bool IsEnableHandlerRetry { get; set; }


        private PeriodicJob _thread;
        private object _locker = new object();
        private LinkedList<string> _lstAsciiMsgs = new LinkedList<string>();
        private LinkedList<byte[]> _lstBinsMsgs = new LinkedList<byte[]>();
        private string _newLine;

        public SerialPortConnectionBase(string port, int baudRate=9600, int dataBits=8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "\r", bool isAsciiMode = true)
        {
            _address = port;

            _isAsciiMode = isAsciiMode;
            _newLine = newline;
            _port = new AsyncSerialPort(port, baudRate, dataBits, parity, stopBits, newline, isAsciiMode);


            if (isAsciiMode)
                Console.WriteLine("");
            _port.OnDataChanged += _port_OnAsciiDataReceived;
            _port.OnBinaryDataChanged += _port_OnBinaryDataChanged;
            _port.OnErrorHappened += _port_OnErrorHappened;
            _thread = new PeriodicJob(2, OnTimer, $"{port}.MonitorHandler", true);

        }

        private bool OnTimer()
        {
            lock (_locker)
            {
                if (_isAsciiMode)
                {
                    while (_lstAsciiMsgs.Count > 0)
                    {
                        string asciiMsg = _lstAsciiMsgs.First.Value;
                        _port_HandleAsciiData(asciiMsg);
                        _lstAsciiMsgs.RemoveFirst();
                    }
                }
                else
                {
                    while (_lstBinsMsgs.Count > 0)
                    {
                        byte[] binMsg = _lstBinsMsgs.First.Value;
                        _port_HandleBinarayData(binMsg);
                        _lstBinsMsgs.RemoveFirst();
                    }
                }

            }
            return true;
        }

        public void SetPortAddress(string portName)
        {
            _port.PortName = portName;
        }

        private void _port_OnErrorHappened(string obj)
        {
            LOG.Error(obj);
        }

        public virtual bool SendMessage(string message)
        {
            if (_port != null && _port.IsOpen())
                return _port.Write(message);

            LOG.Error($"No connection writing message {message}");
            return false;
        }

        public virtual bool SendMessage(byte[] message)
        {
            if (_port != null && _port.IsOpen())
                return _port.Write(message);

            LOG.Error($"No connection writing message {string.Join(" ", Array.ConvertAll(message, x => x.ToString("X2")))}");
            return false;
        }
        public void ForceClear()
        {
            lock (_lockerActiveHandler)
            {
                IsCommunicationError = false;
                _activeHandler = null;
            }
        }

        public void Execute(HandlerBase handler)
        {
            if (_activeHandler != null)
                return;

            if (handler == null)
                return;

            if (_port.IsOpen())
            {
                lock (_lockerActiveHandler)
                {
                    retryTime = 0;
                    _activeHandler = handler;
                    _activeHandler.SetState(EnumHandlerState.Sent);
                }

                bool sendResult = _isAsciiMode ? SendMessage(handler.SendText) : SendMessage(handler.SendBinary);
                if (!sendResult)
                {
                    lock (_lockerActiveHandler)
                    {
                        _activeHandler = null;
                    }
                }

            }
        }

        protected virtual MessageBase ParseResponse(string rawMessage)
        {
            return null;
        }

        protected virtual MessageBase ParseResponse(byte[] rawMessage)
        {
            return null;
        }

        protected virtual void OnEventArrived(MessageBase msg)
        {

        }

        protected virtual void ActiveHandlerProceedMessage(MessageBase msg)
        {
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || (_activeHandler.HandleMessage(msg, out bool transactionComplete) && transactionComplete))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        public void EnableLog(bool enable)
        {
            _port.EnableLog = enable;
        }


        private void ProceedTransactionMessage(MessageBase msg)
        {
            if (msg == null || msg.IsFormatError)
            {
                SetCommunicationError(true, "received invalid response message.");
                return;
            }

            if (msg.IsEvent)
            {
                OnEventArrived(msg);
                //return;
            }

            //当前活动交互会话，继续执行
            ActiveHandlerProceedMessage(msg);
        }

        private void _port_OnBinaryDataChanged(byte[] binaryData)
        {
            lock(_locker)
            {
                _lstBinsMsgs.AddLast(binaryData);
            }
        }


        private void _port_HandleBinarayData(byte[] binaryData)
        {
            MessageBase msg = ParseResponse(binaryData);
            ProceedTransactionMessage(msg);
        }

        private void _port_OnAsciiDataReceived(string oneLineMessage)
        {
            lock(_locker)
            {
                if (string.IsNullOrEmpty(_newLine))
                {
                    _lstAsciiMsgs.AddLast(oneLineMessage);
                }
                else
                {
                    if (oneLineMessage == _newLine)
                    {
                        _lstAsciiMsgs.AddLast(oneLineMessage);
                    }
                    else
                    {
                        foreach (var message in oneLineMessage.Split(_newLine.ToCharArray()))
                        {
                            if (!string.IsNullOrEmpty(message))
                                _lstAsciiMsgs.AddLast(message + _newLine);
                        }
                    }
                }

            }
        }


        private void _port_HandleAsciiData(string oneLineMessage)
        {
            MessageBase msg = ParseResponse(oneLineMessage);
            ProceedTransactionMessage(msg);
        }

        public HandlerBase MonitorTimeout()
        {
            HandlerBase result = null;
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null && _activeHandler.CheckTimeout())
                {
                    if (IsEnableHandlerRetry &&  retryTime++ < 4) 
                        Retry();
                    else
                    {

                        LOG.Write($"{Address} receive {_activeHandler.Name} timeout");
                        result = _activeHandler;
                        HandlerInError = _activeHandler;
                        _activeHandler = null;
                        SetCommunicationError(true, "receive response timeout");


                    }
                }
            }

            return result;
        }
        public void Retry()
        {
            if (_activeHandler == null)
                return;


            if (_port.IsOpen())
            {

                //_activeHandler = handler;
                _activeHandler.SetState(EnumHandlerState.Sent);
                bool sendResult = _isAsciiMode ? SendMessage(_activeHandler.SendText) : SendMessage(_activeHandler.SendBinary);
                if (!sendResult) 
                    _activeHandler = null;

            }
        }

        public void SetCommunicationError(bool isError, string reason)
        {
            IsCommunicationError = isError;
            LastCommunicationError = reason;

        }
    }

    public abstract class TCPPortConnectionBase : IConnection
    {
        public string Address
        {
            get { return _address; }
        }

        public bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public bool Connect()
        {
            _socket.Connect();

            int iCount = 0;
            while(!IsConnected && iCount < 25)
            {
                Thread.Sleep(200);
                iCount++;
            }
            if(IsConnected)
            {
                return true;
            }
            else
            {
                Disconnect();
                return false;
            }
        }

        public bool Disconnect()
        {
            _socket.Dispose();
            return true;
        }

        public bool IsBusy
        {
            get { return _activeHandler != null; }
        }

        public bool IsCommunicationError { get; private set; }
        public string LastCommunicationError { get; private set; }

        private AsynSocketClient _socket;

        protected HandlerBase _activeHandler; //set, control, 
        public HandlerBase HandlerInError;
        public HandlerBase CurrentActiveHandler => _activeHandler;
        protected object _lockerActiveHandler = new object();

        private string _address;

        private bool _isAsciiMode;

        public int retryTime = 0;
        private PeriodicJob _thread;
        private object _locker = new object();
        private LinkedList<string> _lstAsciiMsgs = new LinkedList<string>();
        private LinkedList<byte[]> _lstBinsMsgs = new LinkedList<byte[]>();

        private string _newLine;
        public TCPPortConnectionBase(string address, string newline = "\r", bool isAsciiMode = true)
        {
            _address = address;
            _newLine = newline;
            _isAsciiMode = isAsciiMode;

            _socket = new AsynSocketClient(address, isAsciiMode, newline);

            _socket.OnDataChanged += _port_OnAsciiDataReceived;
            _socket.OnBinaryDataChanged += _port_OnBinaryDataChanged;
            _socket.OnErrorHappened += _port_OnErrorHappened;
            _thread = new PeriodicJob(1,OnTimer, $"{address}.MonitorHandler", true);
        }

        public TCPPortConnectionBase(AsynSocketClient socket,string deviceName, string newline = "\r", bool isAsciiMode = true)
        {
            _socket = socket;
            _address = socket.Address;
            _newLine = newline;
            _isAsciiMode = isAsciiMode;

            _socket.OnDataChanged += _port_OnAsciiDataReceived;
            _socket.OnBinaryDataChanged += _port_OnBinaryDataChanged;
            _socket.OnErrorHappened += _port_OnErrorHappened;
            _thread = new PeriodicJob(1, OnTimer, $"{deviceName}{_address}.MonitorHandler", true);
        }

        private bool OnTimer()
        {
            lock (_locker)
            {
                if (_isAsciiMode)
                {
                    while (_lstAsciiMsgs.Count > 0)
                    {
                        string asciiMsg = _lstAsciiMsgs.First.Value;
                        if (!string.IsNullOrEmpty(asciiMsg))
                        {
                            if(_socket.NeedLog)
                                LOG.Write($"Start handler message:{asciiMsg}");
                            _port_HandleAsciiData(asciiMsg);
                        }
                        _lstAsciiMsgs.RemoveFirst();
                    }
                }
                else
                {
                    while (_lstBinsMsgs.Count > 0)
                    {
                        byte[] binMsg = _lstBinsMsgs.First.Value;
                        _port_HandleBinarayData(binMsg);
                        _lstBinsMsgs.RemoveFirst();
                    }
                }

            }
            return true;
        }

        //public void SetPortAddress(string portName)
        //{
        //    _port.PortName = portName;
        //}

        private void _port_OnErrorHappened(TCPErrorEventArgs obj)
        {
            LOG.Error(obj.Reason);
        }

        public virtual bool SendMessage(string message)
        {
            if (_socket != null && _socket.IsConnected)
                return _socket.Write(message);

            LOG.Error($"No connection writing message {message}");
            return false;
        }

        public virtual bool SendMessage(byte[] message)
        {
            if (_socket != null && _socket.IsConnected)
                return _socket.Write(message);

            LOG.Error($"No connection writing message {string.Join(" ", Array.ConvertAll(message, x => x.ToString("X2")))}");
            return false;
        }
        public void ForceClear()
        {
            lock (_lockerActiveHandler)
            {
                _activeHandler = null;
                IsCommunicationError = false;
            }
        }

        public void Execute(HandlerBase handler)
        {
            if (_activeHandler != null)
                return;

            if (handler == null)
                return;

            if (_socket.IsConnected)
            {
                lock (_lockerActiveHandler)
                {
                    retryTime = 0;
                    _activeHandler = handler;
                    _activeHandler.SetState(EnumHandlerState.Sent);
                }

                bool sendResult = _isAsciiMode ? SendMessage(handler.SendText) : SendMessage(handler.SendBinary);
                if (!sendResult)
                {
                    lock (_lockerActiveHandler)
                    {
                        _activeHandler = null;
                    }
                }

            }
        }

        protected virtual MessageBase ParseResponse(string rawMessage)
        {
            return null;
        }

        protected virtual MessageBase ParseResponse(byte[] rawMessage)
        {
            return null;
        }

        protected virtual void OnEventArrived(MessageBase msg)
        {

        }

        protected virtual void ActiveHandlerProceedMessage(MessageBase msg)
        {
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || (_activeHandler.HandleMessage(msg, out bool transactionComplete) && transactionComplete))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        public void EnableLog(bool enable)
        {
            _socket.NeedLog = enable;
        }


        private void ProceedTransactionMessage(MessageBase msg)
        {
            if (msg == null || msg.IsFormatError)
            {
                SetCommunicationError(true, "received invalid response message.");
                return;
            }

            if (msg.IsEvent)
            {
                OnEventArrived(msg);
                //return;
            }

            //当前活动交互会话，继续执行
            ActiveHandlerProceedMessage(msg);
        }

        private void _port_OnBinaryDataChanged(byte[] binaryData)
        {
            lock (_locker)
            {
                _lstBinsMsgs.AddLast(binaryData);
            }
        }


        private void _port_HandleBinarayData(byte[] binaryData)
        {
            MessageBase msg = ParseResponse(binaryData);
            ProceedTransactionMessage(msg);
        }

        private void _port_OnAsciiDataReceived(string oneLineMessage)
        {
            lock (_locker)
            {
                if (string.IsNullOrEmpty(_newLine))
                {
                    _lstAsciiMsgs.AddLast(oneLineMessage);
                }
                else
                {
                    if (oneLineMessage == _newLine)
                    {
                        _lstAsciiMsgs.AddLast(_newLine);
                    }
                    else
                    {
                        foreach (var message in oneLineMessage.Split(_newLine.ToCharArray()))
                        {
                            if (!string.IsNullOrEmpty(message))
                                _lstAsciiMsgs.AddLast(message + _newLine);
                        }
                    }

                }

            }



            //lock (_locker)
            //{
            //    _lstAsciiMsgs.AddLast(oneLineMessage);
            //}
        }


        private void _port_HandleAsciiData(string oneLineMessage)
        {
            MessageBase msg = ParseResponse(oneLineMessage);
            ProceedTransactionMessage(msg);
        }

        public HandlerBase MonitorTimeout()
        {
            HandlerBase result = null;
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null && _activeHandler.CheckTimeout())
                {
                    //if (retryTime++ < 3) Retry();
                    //else
                    //{
                    EV.PostWarningLog("System", $"{Address} receive {_activeHandler.Name} timeout");
                    result = _activeHandler;
                    HandlerInError = _activeHandler;
                    _activeHandler = null;
                    SetCommunicationError(true, "receive response timeout");

                    //}
                }
            }

            return result;
        }
        public void Retry()
        {
            if (_activeHandler == null)
                return;


            if (_socket.IsConnected)
            {

                //_activeHandler = handler;
                _activeHandler.SetState(EnumHandlerState.Sent);
                bool sendResult = _isAsciiMode ? SendMessage(_activeHandler.SendText) : SendMessage(_activeHandler.SendBinary);
                if (!sendResult) _activeHandler = null;

            }
        }

        public void SetCommunicationError(bool isError, string reason)
        {
            IsCommunicationError = isError;
            LastCommunicationError = reason;

        }
    }
}
