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
    public class AsynSocketServer : IDisposable
    {
        public delegate void ErrorHandler(string args);
        public event ErrorHandler OnErrorHappened;

        public delegate void MessageHandler(string message);
        public event MessageHandler OnDataChanged;

        public delegate void BinaryMessageHandler(byte[] message);
        public event BinaryMessageHandler OnBinaryDataChanged;



        private static Object _locker = new Object();


        public string NewLine { get; set; }
        public bool NeedLog { get; set; } = true;

        private string _ip;
        private int _port;
        private bool _isAscii;
        private static int _bufferSize = 1021;

        public bool IsConnected { get { return (_serverSocket != null /*&& _serverSocket.Connected*/); }  }

        public AsynSocketServer(string ip, int port, bool isAscii,string newline = "\r")
        {
            _ip = ip;
            _port = port;
            _isAscii = isAscii;

            _serverSocket = null;
            NewLine = newline;
        }


        ~AsynSocketServer()
        {
            Dispose();
        }

        Socket _clientSocket;
        Socket _serverSocket;
        public void Start()
        {
            if (_serverSocket != null)
                return;

            //创建套接字
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(_ip), _port);
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //绑定端口和IP
            _serverSocket.Bind(ipe);
            //设置监听
            _serverSocket.Listen(10);
            //连接客户端
            AsyncAccept(_serverSocket);
        }

        /// <summary>
        /// 连接到客户端
        /// </summary>
        /// <param name="serverSocket"></param>
        private void AsyncAccept(Socket serverSocket)
        {
            serverSocket.BeginAccept(asyncResult =>
            {
                //获取客户端套接字
                _clientSocket = serverSocket.EndAccept(asyncResult);
                LOG.Info(string.Format("Received client {0} connect request", _clientSocket.RemoteEndPoint));
                AsyncReveive();
            }, null);
        }

        byte[] _buffer;

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="client"></param>
        private void AsyncReveive()
        {
            _buffer = new byte[_bufferSize];
            try
            {
                //开始接收消息
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("TCP Socket recevice data failed：{0}", ex.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(reason);
            }
        }
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            int length = _clientSocket.EndReceive(asyncResult);

            if(length > 0)
            {
                if (_isAscii)
                {
                    string message = Encoding.ASCII.GetString(_buffer, 0, length);
                    LOG.Info(string.Format("Client message:{0}", message));
                    OnDataChanged(message);
                }
                else
                {
                    byte[] recvBuff = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        recvBuff[i] = _buffer[i];
                    }
                    LOG.Info($"Client message: {string.Join(" ", recvBuff)}.");

                    OnBinaryDataChanged(recvBuff);
                }
            }
            else
            {
                //client disconnect, will....
            }
            //清空数据，重新开始异步接收
            _buffer = new byte[_bufferSize];
            _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), _clientSocket);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="_clientSocket"></param>
        /// <param name="sendMessage"></param>
        public void Write(string sendMessage)
        {
            if (_clientSocket == null || sendMessage == string.Empty) return;
            //数据转码
            byte[] data = new byte[1024];
            data = Encoding.ASCII.GetBytes(sendMessage);
            try
            {
                //开始发送消息
                _clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, asyncResult =>
                {
                    //完成消息发送
                    int length = _clientSocket.EndSend(asyncResult);
                    //输出消息
                    LOG.Info(string.Format("Communication {0}:{1:D} Send {2}.", _ip, _port, data));
                }, null);
            }
            catch (Exception e)
            {
                LOG.Write(e);
                string reason = string.Format("TCP连接发生错误：{0}", e.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(reason);
            }
        }

        public void Write(byte[] data)
        {
            if (_clientSocket == null || data.Count() == 0) return;

            try
            {
                //开始发送消息
                _clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, asyncResult =>
                {
                    //完成消息发送
                    int length = _clientSocket.EndSend(asyncResult);
                    //输出消息
                    LOG.Info(string.Format("Communication {0}:{1:D} Send {2}.", _ip, _port, data));
                }, null);
            }
            catch (Exception e)
            {
                LOG.Write(e);
                string reason = string.Format("TCP连接发生错误：{0}", e.Message);
                LOG.Error(string.Format("Communication  {0}:{1:D} {2}.", _ip, _port, reason));
                OnErrorHappened(reason);
            }
        }

        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_serverSocket != null)
                {
                    if (IsConnected)
                    {
                        _serverSocket.Shutdown(SocketShutdown.Both);
                    }

                    _serverSocket.Close();
                    _serverSocket.Dispose();
                    _serverSocket = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                string reason = string.Format("释放socket资源失败:{0}", ex.Message);
                OnErrorHappened(reason);
            }
        }
    }

}
