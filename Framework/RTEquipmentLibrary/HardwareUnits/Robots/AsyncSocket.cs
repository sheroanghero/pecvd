using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

using Aitex.Sorter.Common;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using System.Configuration;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{  
    public class AsyncSocket :  ICommunication, IDisposable
    {
        public delegate void ErrorHandler(ErrorEventArgs args);
        public event ErrorHandler OnErrorHappened;

        public delegate void MessageHandler(string message);
        public event MessageHandler OnDataChanged;

        private static Object _lockerSocket = new Object();

        public class ClientStateObject
        {
            // Client socket.     
            public Socket workSocket = null;
            // Size of receive buffer.     
            public const int BufferSize = 256;
            // Receive buffer.     
            public byte[] buffer = new byte[BufferSize];
            // Received data string.     
            public StringBuilder sb = new StringBuilder();
        }

        public string NewLine { get; set; }

        public bool EnableLog { get; set; }

        public bool IsConnecting
        {
            get
            {
                return _isConnecting;
            }
        }

        private Socket _socket;

        private string _ip;
        private int _port;

        private bool _isConnecting;

        public bool IsConnected { get { return (_socket != null && _socket.Connected); } }

        public AsyncSocket(string address, string newline ="\r")
        {
            //        Connect(address);
            _socket = null;
            NewLine = newline;
            EnableLog = true;
        }

        ~AsyncSocket()
        {
            Dispose();
        }

        public void Connect(string address)
        {
            try
            {
                if (_isConnecting)
                {
                    return;
                }

                _isConnecting = true;

                _ip =address.Split(':')[0];
                _port =int.Parse(address.Split(':')[1]);
                IPAddress ipAddress = IPAddress.Parse(_ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, _port);

                //Dispose current socket and create a TCP/IP socket.
                Dispose();

                if(_socket == null)
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.     
                _socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), _socket);

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
                // Retrieve the socket from the state object.     
                Socket client = (Socket)ar.AsyncState;
                if (client == null || !client.Connected || _socket == null || !_socket.Connected)
                {
                    _isConnecting = false;
                    return;
                }
                // Complete the connection.   
                client.EndConnect(ar);
                
                EV.PostMessage(ModuleName.Robot.ToString(), EventEnum.TCPConnSucess, _ip, _port.ToString());      

                // Receive the response from the remote device. 
                Receive(_socket);

                _isConnecting = false;
            }
            catch(Exception e)
            {
                LOG.Write(e);
                string reason = string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, e.Message);
                LOG.Error(reason);
         //       EV.PostMessage(UnitName.Transfer.ToString(), EventEnum.RobotCommandFailed, reason);
                OnErrorHappened(new ErrorEventArgs(reason));

                _isConnecting = false;
            }

            
        }
        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.     
                ClientStateObject state = new ClientStateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.     
                client.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                LOG.Write(e);
                string reason = string.Format("TCP连接发生错误：{0}", e.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(new ErrorEventArgs(reason));
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

                // Read data from the remote device.     
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.     

                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    if (state.sb.Length > NewLine.Length)
                    {
                        if (state.sb.ToString().Substring(state.sb.Length - NewLine.Length).Equals(NewLine))
                        {
                            string msg =state.sb.ToString();

                            string raw = msg;
                            if (raw.Contains("\r"))
                                raw = raw.Replace("\r", "\\r");
                            if (raw.Contains("\n"))
                                raw = raw.Replace("\n", "\\n");

                            if(EnableLog)
                                LOG.Info(string.Format("Communication {0}:{1:D} receive {2}.", _ip, _port, raw));

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
                OnErrorHappened(new ErrorEventArgs(reason));
            }
        }

        public bool Write(string data)
        {
            try
            {
                lock (_lockerSocket)
                {
                    // Convert the string data to byte data using ASCII encoding.     
                    byte[] byteData = Encoding.ASCII.GetBytes(data);
                    _socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _socket);

                    if (data.Contains("\r"))
                        data = data.Replace("\r", "\\r");
                    if (data.Contains("\n"))
                        data = data.Replace("\n", "\\n");

                    if(EnableLog)
                        LOG.Info(string.Format("Communication {0}:{1:D} Send {2}.", _ip, _port, data));
                }

                return true;
                
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                LOG.Info(string.Format("Communication {0}:{1:D} Send {2}. failed", _ip, _port, data));

                string reason = string.Format("Send command failed:{0}", ex.Message);
                OnErrorHappened(new ErrorEventArgs(reason));
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

                OnErrorHappened(new ErrorEventArgs(reason));
            }
        }

        /// <summary>
        /// 释放资源（Dispose）
        /// </summary>
        public void Dispose()
        {
            try
            {
                _isConnecting = false;
                if (_socket != null)
                {
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
                OnErrorHappened(new ErrorEventArgs(reason));
            }
        }
    }
}
