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
 
using MECF.Framework.Common.Communications.Tcp.Socket.Framing;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs;

namespace MECF.Framework.Simulator.Core.Driver
{
    public class SocketDeviceSimulator : DeviceSimulator, IDisposable
    {
        public override bool IsEnabled
        {
            get { return _server != null && _server.IsListening; }
        }

        public override bool IsConnected
        {
            get { return IsEnabled && _session != null; }
        }

        public int LocalPort
        {
            get { return _port; }
            set {
            {
                IPEndPoint ip = _server.ListenedEndPoint;
                ip.Port = value;
                _server.ListenedEndPoint = ip;
            } }
        }

        public string RemoteConnection
        {
            get
            {
                if (!IsConnected)
                    return "--";

                TcpSocketSession session = _session;

                return string.Format("{0}:{1}", session.RemoteEndPoint.Address, session.RemoteEndPoint.Port);
            }
        }

        private TcpSocketServer _server;
        private TcpSocketSession _session;


        private PeriodicJob _thread;

        private int _port = 0;

        private bool _enable;

        public SocketDeviceSimulator(int port, int commandIndex, string lineDelimiter, char msgDelimiter, int cmdMaxLength = 4) 
            : base(commandIndex, lineDelimiter, msgDelimiter, cmdMaxLength)
        {
            _port = port;

            TcpSocketServerConfiguration config = new TcpSocketServerConfiguration()
            {
                FrameBuilder = new LineBasedFrameBuilder(new LineDelimiter(lineDelimiter)),
            };
            _server = new TcpSocketServer(IPAddress.Parse("127.0.0.1"), port, config);
            _server.ClientConnected += new EventHandler<TcpClientConnectedEventArgs>(ClientConnected);
            _server.ClientDisconnected += new EventHandler<TcpClientDisconnectedEventArgs>(ClientDisconnected);
            _server.ClientDataReceived += new EventHandler<TcpClientDataReceivedEventArgs>(ClientDataReceived);


            _thread = new PeriodicJob(100, OnMonitor, "SocketSeverLisener", true);
         }

        private bool OnMonitor()
        {
            if (_enable)
            {
                if (!_server.IsListening)
                {
                    _server.Listen();
                }
            }
            else
            {
                if (_server.IsListening)
                {
                    _server.Shutdown();
                }
            }

            return true;
        }

        public void Enable()
        {
 
            _enable = true;
        }

        public void Disable()
        {
 
            _enable = false;
        }

        void ClientDataReceived(object sender, TcpClientDataReceivedEventArgs e)
        {


            if (ProcessReceivedData(e))
                return;

            
            OnReadMessage(Encoding.UTF8.GetString(e.Data, e.DataOffset, e.DataLength));

            if (BinaryDataMode)
                OnReadMessage(e.Data.Skip(e.DataOffset).Take(e.DataLength).ToArray());
        }

        public virtual bool ProcessReceivedData(TcpClientDataReceivedEventArgs e)
        {
            return false;
        }

        void ClientDisconnected(object sender, TcpClientDisconnectedEventArgs e)
        {
 
            _server.CloseSession(_session.SessionKey);
            _session = null;
 
        }

        public virtual void ClientConnected(object sender, TcpClientConnectedEventArgs e)
        {

            _session = e.Session;

        }




        protected override void ProcessWriteMessage(string msg)
        {
            if (IsEnabled && _session != null)
            {
                _session.Send(Encoding.ASCII.GetBytes(msg));
            }
        }
        protected override void ProcessWriteMessage(byte[] msg)
        {
            if (IsEnabled && _session != null)
            {
                _session.Send(msg);
            }
        }

        public void Dispose()
        {
            Disable();
        }
    }
}

