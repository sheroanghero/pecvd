using MECF.Framework.Common.Communications;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SimpleSocketDeviceSimulator : DeviceSimulator
    {
        public override bool IsEnabled
        {
            get { return _socket != null && _socket.IsConnected; }
        }

        public override bool IsConnected
        {
            get { return IsEnabled; }
        }

        public string PortName
        {
            get { return _port.ToString(); }
            set { _port = int.Parse(value); }
        }
        public int LocalPort
        {
            get { return _port; }
            set
            {
                {
                    _port = value;
                }
            }
        }
        private AsynSocketServer _socket;
        int _port;

        public SimpleSocketDeviceSimulator(int port, int commandIndex, string lineDelimiter, char msgDelimiter, bool isAscii = true)
                : base(commandIndex, lineDelimiter, msgDelimiter)
        {
            _port = port;

            _socket = new AsynSocketServer("127.0.0.1" , port, isAscii, lineDelimiter);

            _socket.OnDataChanged += OnReadMessage;
            _socket.OnBinaryDataChanged += OnReadMessage;
            _socket.OnErrorHappened += OnErrorMessage;


        }

        public void Enable()
        {
            _socket.Start();
        }

        public void Disable()
        {
            _socket.Dispose();
        }

        protected override void ProcessWriteMessage(string msg)
        {
            _socket.Write(msg);
        }
        protected override void ProcessWriteMessage(byte[] msg)
        {
            _socket.Write(msg);
        }

    }

}
