using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Account;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs;

namespace MECF.Framework.Simulator.Core.Driver
{
    public class SocketClientSimulator : DeviceSimulator, IDisposable
    {
        public override bool IsEnabled
        {
            get { return _socket.IsConnected; }
        }

        public override bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public int LocalPort
        {
            get { return _port; }
            set
            {
                _port = value;
            }
        }

        public string RemoteConnection
        {
            get
            {
                

                return string.Format("{0}:{1}", "127.0.0.1", _port);
            }
        }

      

        private AsynSocketClient _socket;

        private LinkedList<string> _lstAsciiMsgs = new LinkedList<string>();
        private LinkedList<byte[]> _lstBinsMsgs = new LinkedList<byte[]>();


        private PeriodicJob _thread;
        private object _locker = new object();
        private string _newLine;

        private int _port = 0;
        private bool _isAsciiMode;
        //private bool _enable;

        public SocketClientSimulator(int port, int commandIndex, string lineDelimiter, char msgDelimiter, int cmdMaxLength = 4)
            : base(commandIndex, lineDelimiter, msgDelimiter, cmdMaxLength)
        {
            _port = port;


            _newLine = lineDelimiter;
            _socket = new AsynSocketClient($"127.0.0.1:{_port}", true, lineDelimiter);
            _isAsciiMode = true;
            _socket.OnDataChanged += _port_OnAsciiDataReceived;
            _socket.OnBinaryDataChanged += _port_OnBinaryDataChanged;
            _socket.OnErrorHappened += _port_OnErrorHappened;

            //TcpSocketServerConfiguration config = new TcpSocketServerConfiguration()
            //{
            //    FrameBuilder = new LineBasedFrameBuilder(new LineDelimiter(lineDelimiter)),
            //};
            //_server = new TcpSocketServer(IPAddress.Parse("127.0.0.1"), port, config);
            //_server.ClientConnected += new EventHandler<TcpClientConnectedEventArgs>(ClientConnected);
            //_server.ClientDisconnected += new EventHandler<TcpClientDisconnectedEventArgs>(ClientDisconnected);
            //_server.ClientDataReceived += new EventHandler<TcpClientDataReceivedEventArgs>(ClientDataReceived);


            _thread = new PeriodicJob(100, OnMonitor, "SocketSeverLisener", true);
        }

        private void _port_OnErrorHappened(TCPErrorEventArgs args)
        {
            
        }

        private void _port_OnBinaryDataChanged(byte[] binaryData)
        {
            lock (_locker)
            {
                _lstBinsMsgs.AddLast(binaryData);
            }
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
                    foreach (var message in oneLineMessage.Split(_newLine.ToCharArray()))
                    {
                        if (!string.IsNullOrEmpty(message))
                            _lstAsciiMsgs.AddLast(message + _newLine);
                    }
                }

            }
        }

        private bool OnMonitor()
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
                            if (_socket.NeedLog)
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

        private void _port_HandleBinarayData(byte[] binMsg)
        {
            string asciiMsg = Encoding.ASCII.GetString(binMsg);
            if (ProcessReceivedData(asciiMsg))
                return;


            OnReadMessage(asciiMsg);

            if (!BinaryDataMode)
                OnReadMessage(asciiMsg);
        }

        private void _port_HandleAsciiData(string asciiMsg)
        {
            if (ProcessReceivedData(asciiMsg))
                return;


            OnReadMessage(asciiMsg);

            if (!BinaryDataMode)
                OnReadMessage(asciiMsg);
        }

        public void Enable()
        {

            //_enable = true;
        }

        public void Disable()
        {

            //_enable = false;
        }

        //void ClientDataReceived(object sender, TcpClientDataReceivedEventArgs e)
        //{


        //    if (ProcessReceivedData(e))
        //        return;


        //    OnReadMessage(Encoding.UTF8.GetString(e.Data, e.DataOffset, e.DataLength));

        //    if (BinaryDataMode)
        //        OnReadMessage(e.Data.Skip(e.DataOffset).Take(e.DataLength).ToArray());
        //}

        public virtual bool ProcessReceivedData(string msg)
        {
            return false;
        }

        void ClientDisconnected(object sender, TcpClientDisconnectedEventArgs e)
        {

            //_server.CloseSession(_session.SessionKey);
            //_session = null;

        }

        public virtual void ClientConnected(object sender, TcpClientConnectedEventArgs e)
        {

            //_session = e.Session;

        }




        protected override void ProcessWriteMessage(string msg)
        {
            _socket.Write(msg);
        }
        protected override void ProcessWriteMessage(byte[] msg)
        {
            _socket.Write(msg);
        }

        public void Dispose()
        {
            Disable();
        }

        public void Connect()
        {
            _socket.Connect();
        }

        public void DisConnect()
        {
            
        }
    }
}

