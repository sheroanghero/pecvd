using System;
using System.IO.Ports;
using System.Text;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{
    
    public class AsyncSerialPort : ICommunication, IDisposable
    {
        public string PortName { get { return _port.PortName; } set {
        {
            _port.PortName = value;
        } } }

        public event Action<string> OnErrorHappened;

        public event Action<string> OnDataChanged;

        public event Action<byte[]> OnBinaryDataChanged;

        private static Object _locker = new Object();

        protected SerialPort _port;

        private string _buff = "";

        public bool EnableLog { get; set; }

        private bool _isAsciiMode;
        public bool IsAsciiMode
        {
            get => _isAsciiMode;
            set { _isAsciiMode = value; }
        }
        private bool _isLineBased;

        public AsyncSerialPort(string name, int baudRate, int dataBits, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "\r", bool isAsciiMode=true)
        {
            _isAsciiMode = isAsciiMode;
            _isLineBased = !string.IsNullOrEmpty(newline);

            _port = new SerialPort();
            _port.PortName = name;
            _port.BaudRate = baudRate;
            _port.DataBits = dataBits;
            _port.Parity = parity;
            _port.StopBits = stopBits;

            _port.RtsEnable = false;
            _port.DtrEnable = false;

            _port.ReadTimeout = 1000;
            _port.WriteTimeout = 1000;

            if (!string.IsNullOrEmpty(newline))
            {
                _port.NewLine = newline;
            }
            _port.Handshake = Handshake.None;

            _port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            _port.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceived);
        }

        public void Dispose()
        {
            Close();
        }


        public bool Open()
        {
            
            if (_port.IsOpen) Close();
            //Close();
            try
            {
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                _buff = "";

            }
            catch (Exception e)
            {
                string reason = _port.PortName + " port open failed，please check configuration." + e.Message;
                 //ProcessError( reason );
                LOG.Write(reason);
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
                    ProcessError( reason );
                    return false;
                }
            }
            return true;
        }

        //结束符号, 由调用者 负责加上
        public bool Write(string msg)
        {
            try
            {
                lock (_locker)
                {
                    if (_port.IsOpen)
                    {
                        _port.Write(msg);
                        if (EnableLog)
                        {
                            LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, msg));
                            LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(msg), x => x.ToString("X2")))));

                        }

                    }
                }
                return true;
            }
            catch (Exception e)
            {
                string reason = string.Format("Communication {0} Send {1} failed. {2}.", _port.PortName, msg, e.Message);
                LOG.Info(reason);
                ProcessError( reason );
                return false;
            }
        }


        public bool Write(byte[] msg)
        {
            try
            {
                lock (_locker)
                {
                    if (_port.IsOpen)
                    {
                        _port.Write(msg, 0, msg.Length);

                        if (EnableLog)
                        {
                            LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, string.Join(" ", Array.ConvertAll(msg, x => x.ToString("X2")))));
                            LOG.Info(string.Format("Communication {0} Send {1} succeeded.", _port.PortName, Encoding.ASCII.GetString(msg)));
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                string reason = string.Format("Communication {0} Send {1} failed. {2}.", _port.PortName, string.Join(" ", Array.ConvertAll(msg, x => x.ToString("X2"))), e.Message);
                LOG.Info(reason);
                ProcessError(reason);
                return false;
            }
        }


        public void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_port.IsOpen)
            {
                LOG.Write($"discard {_port.PortName} received data, but port not open");
                return;
            }

            if (_isAsciiMode)
            {
                AsciiDataReceived();
            }
            else
            {
                BinaryDataReceived();
            }
        }

        private void AsciiDataReceived()
        {
            string str = _port.ReadExisting(); //字符串方式读

            if (_isLineBased)
            {
                _buff += str;

                int index = _buff.LastIndexOf(_port.NewLine, StringComparison.Ordinal);

                if (index >= 0)
                {
                    index += _port.NewLine.Length;

                    string msg = _buff.Substring(0, index);

                    _buff = _buff.Substring(index);

                    if (EnableLog)
                    {
                        LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, msg));
                        LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(msg), x => x.ToString("X2")))));

                    }

                    if (OnDataChanged != null)
                        OnDataChanged(msg);
                }
            }
            else
            {
                string msg = str;
                if (EnableLog)
                {
                    LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, msg));
                    LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(msg), x => x.ToString("X2")))));

                }

                if (OnDataChanged != null)
                    OnDataChanged(msg);
            }

        }

        public void BinaryDataReceived( )
        {
            byte[] readBuffer = new byte[_port.BytesToRead];
            int readCount = _port.Read(readBuffer, 0, readBuffer.Length);

            if (readCount == 0)
            {
                //LOG.Write($"read zero length data, {_port.PortName}");
                return;
            }

            byte[] buffer = new byte[readCount];
            System.Buffer.BlockCopy(readBuffer, 0, buffer, 0, readCount);

            if (EnableLog)
            {
                StringBuilder str = new StringBuilder(512);
                Array.ForEach(buffer, x => str.Append(x.ToString("X2") + " "));
                LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, str));
                LOG.Info(string.Format("Communication {0} Receive {1}.", _port.PortName, Encoding.ASCII.GetString(buffer)));

            }

            if (OnBinaryDataChanged != null)
                OnBinaryDataChanged(buffer);
        }

        void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            string reason = string.Format("Communication {0} {1}.", _port.PortName, e.EventType.ToString());
            LOG.Error(reason);
            ProcessError( reason );
        }


        public void ClearPortBuffer()
        {
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
        }

        void ProcessError(string reason)
        {
            if (OnErrorHappened != null)
                OnErrorHappened(reason);
        }
    }
}

