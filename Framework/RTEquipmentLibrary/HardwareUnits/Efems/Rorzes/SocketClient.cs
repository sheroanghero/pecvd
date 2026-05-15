using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public class SocketClient : IDisposable, IConnectable
    {
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

        public event Action<string> OnCommunicationError;
        public event Action<string> OnAsciiDataReceived;
        public event Action<byte[]> OnBinaryDataReceived;
 
        public bool IsConnected { get { return (_socket != null && _socket.Connected); } }


        private Socket _socket;
 
        private int _bufferSize = 256;
        private static Object _lockerSocket = new Object();
 
        private IConnectionContext _config;

        public SocketClient(IConnectionContext config)
        {
            _config = config;
        }


        ~SocketClient()
        {
            Dispose();
        }

        public void Connect()
        {
            try
            {
                string  ip = _config.Address.Split(':')[0];
                int port = int.Parse(_config.Address.Split(':')[1]);

                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                lock (_lockerSocket)
                {
                    Dispose();

                    if (_socket == null)
                    {
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }

                    _socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), _socket);
                }
            }
            catch (Exception ex)
            {
                LOG.Error($"Failed connect, " + ex);

            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                if (client.Connected)
                {
                    client.EndConnect(ar);

                    ClientStateObject state = new ClientStateObject(_bufferSize);
                    state.workSocket = _socket;

                    _socket.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }



            }
            catch (Exception ex)
            {
                LOG.Error($"Failed connect, " + ex);
            }
        }

        public bool Connect(out string reason)
        {
            Connect( );
            reason = string.Empty;
            return true;
        }

        public bool Disconnect(out string reason)
        {
            Dispose();

            reason = string.Empty;
            return true;
        }

        public bool CheckIsConnected()
        {
            if (!IsConnected)
                return false;

            lock (_lockerSocket)
            {
                if ((_socket.Poll(0, SelectMode.SelectWrite)) && (!_socket.Poll(0, SelectMode.SelectError)))
                {
                    //byte[] buffer = new byte[1];
                    //_socket.ReceiveTimeout
                    //if (_socket.Receive(buffer, SocketFlags.Peek) == 0)
                    //{
                    //    return false;
                    //}
                    //else
                    //{
                        return true;
                    //}
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SendBinaryData(byte[] data)
        {
            try
            {
                lock (_lockerSocket)
                {
                    _socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), _socket);

                    if (_config.EnableLog)
                    {
                        LOG.Info($"Communication {_config.Address} Send {string.Join(" ", data)}." );
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LOG.Info($"Failed send {string.Join(" ", data)}, " + ex);
                NotifyError(ex.Message);
            }

            return false;
        }

        public bool SendAsciiData(string data)
        {
            try
            {
                lock (_lockerSocket)
                {
                    byte[] byteData = Encoding.ASCII.GetBytes(data);
                    _socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _socket);

                    if (_config.EnableLog)
                    {
                        LOG.Info($"Communication {_config.Address} Send {string.Join(" ", data.TrimEnd('\r', '\n'))}");
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                LOG.Info($"Failed send {string.Join(" ", data)}, " + ex);
                NotifyError(ex.Message);
            }

            return false;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!IsConnected)
                {
                    return;
                }

                ClientStateObject state = (ClientStateObject)ar.AsyncState;
                Socket client = state.workSocket;

                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    string receiveMessage = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    state.sb.Append(receiveMessage);

                    if (!_config.IsAscii)
                    {
                        byte[] recvBuff = new byte[bytesRead];
                        for (int i = 0; i < bytesRead; i++)
                        {
                            recvBuff[i] = state.buffer[i];
                        }
                        if (_config.EnableLog)
                        {
                            LOG.Info($"Communication {_config.Address} receive {string.Join(" ", recvBuff)}.");
                        }

                        if (OnBinaryDataReceived != null)
                        {
                            OnBinaryDataReceived(recvBuff);
                        }

                        state.sb.Clear();
                    }
                    else if (state.sb.Length > _config.NewLine.Length)
                    {
                        if (state.sb.ToString().Substring(state.sb.Length - _config.NewLine.Length).Equals(_config.NewLine))
                        {
                            string msg = state.sb.ToString();
                            if (_config.EnableLog)
                            {
                                LOG.Info($"Communication {_config.Address} receive {msg.TrimEnd('\r','\n')}");
                            }

                            if (OnAsciiDataReceived != null)
                            {
                                OnAsciiDataReceived(state.sb.ToString());
                            }
                            state.sb.Clear();
                        }
                    }
                    // Get the rest of the data.     
                    client.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception ex)
            {
                LOG.Info($"Failed receive data, " + ex);
                NotifyError(ex.Message);
            }
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
                LOG.Info($"Failed send data, " + ex);
                NotifyError(ex.Message);
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
                LOG.Write($"Dispose {_config.Address} resource exception, {ex}");
            }
        }


        private void NotifyError(string reason)
        {
            if (OnCommunicationError != null)
                OnCommunicationError(reason);
        }

        private bool PingConnect()
        {
            bool result = true;

            if (_socket != null && _socket.Connected)
            {

                try
                {
                    Ping pingTest = new Ping();
                    PingReply reply = pingTest.Send(_config.Address.Split(':')[0]);
                    if (reply.Status != IPStatus.Success)
                        result = false;
                }
                catch (PingException) { result = false; }

                if (_socket.Poll(1000, SelectMode.SelectRead) && (_socket.Available == 0))
                {
                    result = false;
                }
            }
            else
            {
                result = false;
            }

            return result;
        }
    }


}
