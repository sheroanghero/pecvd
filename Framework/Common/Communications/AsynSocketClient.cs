using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Communications
{
    public class AsynSocketClient : IDisposable
    {
        public delegate void ErrorHandler(TCPErrorEventArgs args);
        public event ErrorHandler OnErrorHappened; 

        public delegate void MessageHandler(string message);
        public event MessageHandler OnDataChanged;

        public delegate void BinaryMessageHandler(byte[] message);
        public event BinaryMessageHandler OnBinaryDataChanged;
        

        private static Object _locker = new Object();

        public class ClientStateObject
        {
            // Client socket.     
            public Socket workSocket = null;
            // Size of receive buffer.     
            public static int BufferSize = 256;
            // Receive buffer.     
            public byte[] buffer = new byte[BufferSize];
            // Received data string.     
            public StringBuilder sb = new StringBuilder();

            public ClientStateObject(int bufferSize = 256)
            {
                BufferSize = bufferSize;
                buffer = new byte[bufferSize];
            }
        }

        public string NewLine { get; set; }
        public bool NeedLog { get; set; } = true;

        private Socket _socket;

        private string _ip;
        private int _port;
        private string _address;
        public string Address => _address;
        private int _bufferSize = 256;
        private bool _isEndConnect = false;

        public bool IsConnected { get { return (_socket != null && _socket.Connected && _isEndConnect); } } //&& !_socket.Poll(100, SelectMode.SelectRead)); } }
        public bool IsHstConnected { get { return (_socket != null && IsSocketConnected(_socket)); } }
        private bool IsSocketConnected(Socket client)
        {
            try
            {
                byte[] tmp = new byte[] { 0x0 };
                //int a= newclient.Receive(tmp);
                int a = client.Send(tmp);
                if (a == 1)
                    return true;
                else
                    return false;
            }
            catch (SocketException e)
            {
                LOG.Write(e.Message);
                return false;
            }

        }
        bool _isAsciiMode;

        public AsynSocketClient(string address, bool isAsciiMode, string newline = "\r")
        {
            //        Connect(address);
            _socket = null;
            NewLine = newline;
            _address = address;
            _isAsciiMode = isAsciiMode;
        }

        public AsynSocketClient(string address, int bufferSize, string newline = "\r")
        {
            _socket = null;
            NewLine = newline;
            _address = address;
            _bufferSize = bufferSize;
        }

        ~AsynSocketClient()
        {
            Dispose();
        }

        public void Connect()
        {
            try
            {
                _ip = _address.Split(':')[0];
                _port = int.Parse(_address.Split(':')[1]);
                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, _port);

                lock (_locker)
                {

                    _isEndConnect = false;
                    //Dispose current socket and create a TCP/IP socket.
                    Dispose();

                    if (NeedLog)
                    {
                        LOG.Info(string.Format("Start new socket of {0}.", _address));
                    }
                    if (_socket == null)
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.     
                    _socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), _socket);
                }
            }
            catch (Exception e)
            {
                LOG.Write(e);
                throw new Exception(e.ToString());
            }

        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (NeedLog)
                {
                    LOG.Info(string.Format("ConnectCallback {0}", _address));
                }
                // Retrieve the socket from the state object.     
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.   
                if (!client.Connected)
                    return;
                client.EndConnect(ar);
                _isEndConnect = true;

                if (NeedLog)
                {
                    LOG.Info(string.Format("EndConnect"));
                }


                EV.PostMessage(ModuleName.Robot.ToString(), EventEnum.TCPConnSucess, _ip, _port.ToString());

                // Receive the response from the remote device. 
                Receive(_socket);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, ex);
                LOG.Error(reason);
                //       EV.PostMessage(UnitName.Transfer.ToString(), EventEnum.RobotCommandFailed, reason);
                //OnErrorHappened(new TCPErrorEventArgs(reason));

                Thread.Sleep(1000);
                Connect();
            }
        }
        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.     
                ClientStateObject state = new ClientStateObject(_bufferSize);
                state.workSocket = client;

                // Begin receiving the data from the remote device.     
                client.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                LOG.Write(e);
                string reason = string.Format("TCP连接发生错误：{0}", e.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(new TCPErrorEventArgs(reason));
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!IsConnected) { return; }

                // Retrieve the state object and the client socket     
                // from the asynchronous state object.     
                ClientStateObject state = (ClientStateObject)ar.AsyncState;
                Socket client = state.workSocket;
                if (client == null || !client.Connected)
                    return;

                // Read data from the remote device.     
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.     

                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    string receiveMessage = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

                    if (!_isAsciiMode)
                    {
                        //string msg = state.sb.ToString();
                        //if (NeedLog)
                        //    LOG.Info(string.Format("Communication {0}:{1:D} receive {2}.", _ip, _port, msg.TrimEnd('\n').TrimEnd('\r')));

                        byte[] recvBuff = new byte[bytesRead];
                        for(int i = 0; i < bytesRead; i++)
                        {
                            recvBuff[i] = state.buffer[i];
                        }
                        if (NeedLog)
                        {
                            LOG.Info(string.Format("Communication {0}:{1:D} receive {2}.", _ip, _port, string.Join(" ", Array.ConvertAll(recvBuff, x => x.ToString("X2")))));
                            LOG.Info(string.Format("Communication {0}:{1:D} receive {2} in ASCII.", _ip, _port, Encoding.ASCII.GetString(recvBuff)));
                        }
                        OnBinaryDataChanged(recvBuff);

                        state.sb.Clear();
                    }
                    else if (state.sb.Length > NewLine.Length)
                    {
                        if (state.sb.ToString().Substring(state.sb.Length - NewLine.Length).Equals(NewLine))
                        {
                            string msg = state.sb.ToString();
                            if (NeedLog)
                            {
                                LOG.Info(string.Format("Communication {0}:{1:D} receive {2}.", _ip, _port, msg.TrimEnd('\n').TrimEnd('\r')));
                                LOG.Info(string.Format("Communication {0}:{1:D} receive {2}. in BIN", _ip, _port, string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(msg), x => x.ToString("X2")))));
                            }
                            OnDataChanged(state.sb.ToString());
                            state.sb.Clear();
                        }
                    }
                    // Get the rest of the data.     
                    client.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("TCP Socket recevice data failed：{0}", ex.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(new TCPErrorEventArgs(reason));
            }
        }

        public bool Write(string data)
        {
            try
            {
                lock (_locker)
                {
                    // Convert the string data to byte data using ASCII encoding.     
                    byte[] byteData = Encoding.ASCII.GetBytes(data);
                    _socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _socket);
                    if (NeedLog)
                    {
                        LOG.Info(string.Format("Communication {0}:{1:D} Send {2}.", _ip, _port, data));
                        string dataString = string.Join(" ", Array.ConvertAll(Encoding.ASCII.GetBytes(data), x => x.ToString("X2")));
                        LOG.Info(string.Format("Communication {0}:{1:D} Send {2} in Bin.", _ip, _port, dataString));
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                LOG.Info(string.Format("Communication {0}:{1:D} Send {2}. failed", _ip, _port, data));

                string reason = string.Format("Send command failed:{0}", ex.Message);
                OnErrorHappened(new TCPErrorEventArgs(reason));
            }

            return false;
        }

        public bool Write(byte[] byteData)
        {
            try
            {
                lock (_locker)
                {
                    // Convert the string data to byte data using ASCII encoding.     
                    //byte[] byteData = Encoding.ASCII.GetBytes(data);

                    _socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _socket);

                    if (NeedLog)
                    {
                        string dataString = string.Join(" ", Array.ConvertAll(byteData, x => x.ToString("X2")));
                        LOG.Info(string.Format("Communication {0}:{1:D} Send {2}.", _ip, _port, dataString));
                        LOG.Info(string.Format("Communication {0}:{1:D} Send {2} in ASCII.", _ip, _port, Encoding.ASCII.GetString(byteData)));

                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                string dataString = string.Join(" ", Array.ConvertAll(byteData, x => x.ToString("X2")));
                LOG.Info(string.Format("Communication {0}:{1:D} Send {2}. failed", _ip, _port, dataString));

                string reason = string.Format("Send command failed:{0}", ex.Message);
                OnErrorHappened(new TCPErrorEventArgs(reason));
            }

            return false;
        }


        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.     
                Socket client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.     
                int bytesSent = client.EndSend(ar);

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("Send command failed:{0}", ex.Message);

                OnErrorHappened(new TCPErrorEventArgs(reason));
            }
        }

        /// <summary>
        /// 释放资源（Dispose）
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_socket != null)
                {
                    if (NeedLog)
                    {
                        LOG.Info(string.Format("Dispose current socket of {0}", _address));
                    }

                    if (IsConnected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }

                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("释放socket资源失败:{0}", ex.Message);
                OnErrorHappened(new TCPErrorEventArgs(reason));
            }
        }
    }


    public class TCPErrorEventArgs : EventArgs
    {
        public readonly string Reason;
        public readonly string Code;

        public TCPErrorEventArgs(string reason, string code = "")
        {
            Reason = reason;
            Code = code;
        }
    }

}
