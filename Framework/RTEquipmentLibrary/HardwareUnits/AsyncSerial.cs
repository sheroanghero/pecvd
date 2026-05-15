using System;
using System.IO.Ports;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{  
    public class AsyncSerial : ICommunication, IDisposable
    {
        public delegate void ErrorHandler(ErrorEventArgs args);
        public event ErrorHandler OnErrorHappened;
        
        public delegate void MessageHandler(string message);
        public event MessageHandler OnDataChanged;

        private static Object _locker = new Object();

        private SerialPort _port;
        private string _buff = "";
        
        public AsyncSerial(string name, int baudRate, int dataBits, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline ="\r")
        {
            _port = new SerialPort();
            _port.PortName = name;
            _port.BaudRate = baudRate;
            _port.DataBits = dataBits;
            _port.Parity   = parity;
            _port.StopBits = stopBits;


            _port.RtsEnable = false;
            _port.DtrEnable = false;

            _port.ReadTimeout  = 1000;
            _port.WriteTimeout = 1000;

            _port.NewLine = newline;
            _port.Handshake = Handshake.None;

            _port.DataReceived  += new SerialDataReceivedEventHandler(DataReceived);
            _port.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceived);
        }

        public void Dispose()
        {
            Close();
        }


        public bool Open()
        {
            if (_port.IsOpen)  Close();

            try
            {
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                _buff = "";
            }
            catch (Exception e)
            {
               // LOG.Write($"Port open failed, {e.Message}");

                string reason = _port.PortName + " port open failed，please check configuration." + e.Message;

                //OnErrorHappened(new ErrorEventArgs(reason));
                LOG.Write(reason);
                return false;
            }

            return true;
        }

        public bool Open(int retryTime, int delayTime )
        {
            if (_port.IsOpen) Close();

            try
            {
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                _buff = "";
            }
            catch (Exception e)
            {
                LOG.Write($"Port open failed, {e.Message}");

                string reason = _port.PortName + " port open failed，please check configuration." + e.Message;

                OnErrorHappened(new ErrorEventArgs(reason));
                return false;
            }

            return true;
        }

        public bool IsOpen()
        {
            return _port.IsOpen;
        }

        public bool Close()
        {
            if (_port.IsOpen)
            {
                try
                {
                    _port.Close();
                }
                catch (Exception e)
                {
                    string reason = _port.PortName + " port close failed." + e.Message;
                    OnErrorHappened(new ErrorEventArgs(reason));
                    return false;
                }
            }
            return true;
        }

        public bool Write(string msg)
        {
            try
            {
                lock (_locker)
                {
                    if (_port.IsOpen)
                    {
                        _port.Write(msg);

                        if (msg.Contains("\r"))
                            msg = msg.Replace("\r", "\\r");
                        if (msg.Contains("\n"))
                            msg = msg.Replace("\n", "\\n");

                        LOG.Info(string.Format("Communication {0} Sent {1}.", _port.PortName, msg));
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                string reason = string.Format("Communication {0} Send {1} failed. {2}.", _port.PortName, msg, e.Message);
                LOG.Info(reason);
                OnErrorHappened(new ErrorEventArgs(reason));
                return false;
            }
        }
        
        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port.IsOpen)     
            {
                string str = _port.ReadExisting();//字符串方式读
                _buff += str;

                int index = _buff.LastIndexOf(_port.NewLine);

                if (index > 0)
                {
                    index += _port.NewLine.Length;
                    string msg = _buff.Substring(0,index);
                    _buff = _buff.Substring(index);

                    var raw = msg;
                    if (raw.Contains("\r"))
                        raw = raw.Replace("\r", "\\r");
                    if (raw.Contains("\n"))
                        raw = raw.Replace("\n", "\\n");

                    LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, raw));

                    OnDataChanged(msg);



                }
            }   
        }


        void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
			string reason = string.Format("Communication {0} {1}.", _port.PortName, e.EventType.ToString());
            LOG.Error(reason);
            OnErrorHappened(new ErrorEventArgs(reason));
        }


        public void ClearPortBuffer()
        {
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
        }

    }
}
