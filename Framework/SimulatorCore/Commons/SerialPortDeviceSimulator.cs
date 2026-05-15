using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Communications.Tcp.Socket.Framing;

namespace MECF.Framework.Simulator.Core.Driver
{
    public class SerialPortDeviceSimulator : DeviceSimulator
    {
        public override bool IsEnabled
        {
            get { return _serialPort!=null && _serialPort.IsOpen(); }
        }

        public override bool IsConnected
        {
            get { return IsEnabled; }
        }

        public string   PortName
        {
            get { return _serialPort.PortName; }
            set { _serialPort.PortName = value; }
        }

        private AsyncSerialPort _serialPort;

 
         public string RemoteConnection { get; set; }

        public SerialPortDeviceSimulator(string port, int commandIndex, string lineDelimiter, char msgDelimiter, bool isAscii=true)
                :base(commandIndex, lineDelimiter, msgDelimiter)
        {
            _serialPort = new AsyncSerialPort(port, 9600, 8, Parity.None, StopBits.One, lineDelimiter, isAscii);
            _serialPort.OnDataChanged += OnReadMessage;
            _serialPort.OnBinaryDataChanged += OnReadMessage;
            _serialPort.OnErrorHappened += OnErrorMessage;  

            
        }

        public SerialPortDeviceSimulator(string port,int baud, int commandIndex, string lineDelimiter, char msgDelimiter, bool isAscii = true)
                : base(commandIndex, lineDelimiter, msgDelimiter)
        {
            _serialPort = new AsyncSerialPort(port, baud, 8, Parity.None, StopBits.One, lineDelimiter, isAscii);
            _serialPort.OnDataChanged += OnReadMessage;
            _serialPort.OnBinaryDataChanged += OnReadMessage;
            _serialPort.OnErrorHappened += OnErrorMessage;


        }

        public SerialPortDeviceSimulator(string port, int commandIndex, string lineDelimiter, char msgDelimiter, bool isAscii, int dataBits)
        : base(commandIndex, lineDelimiter, msgDelimiter)
        {
            _serialPort = new AsyncSerialPort(port, 9600, dataBits, Parity.Even, StopBits.One, lineDelimiter, isAscii);
            _serialPort.OnDataChanged += OnReadMessage;
            _serialPort.OnBinaryDataChanged += OnReadMessage;
            _serialPort.OnErrorHappened += OnErrorMessage;
        }


        public void Enable()
        {
            _serialPort.Open();
         }

        public void Disable()
        {
            _serialPort.Close();
        }

        protected override void ProcessWriteMessage(string msg)
        {
            _serialPort.Write(msg);
        }
        protected override void ProcessWriteMessage(byte[] msg)
        {
            _serialPort.Write(msg);
        }
       
    }
}

