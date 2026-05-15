using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.Core.IoProviders
{
    class MCSocket
    {
        private int m_nPort;
        private string m_strAddress;
        private Socket m_socket;
        private int m_nTimeOut;

        public MCSocket()
        {
            m_nTimeOut = 30000;
            //m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        ~MCSocket()
        {
        }

        public bool Connected
        {
            get { return (m_socket != null && m_socket.Connected); }
        }

        public bool Open(string strAddress, int nPort, string strLocalAddress)
        {
            if (Connected)
                return true;

            Close();

            m_strAddress = strAddress;
            m_nPort = nPort;

            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Bind to specific local endpoint
                IPAddress localAddress = IPAddress.Parse(strLocalAddress);
                IPEndPoint localEndPoint = new IPEndPoint(localAddress, 0);
                m_socket.Bind(localEndPoint);

                IPAddress ipAddress = IPAddress.Parse(m_strAddress);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, m_nPort);
                //m_socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                m_socket.SendTimeout = m_nTimeOut;
                m_socket.ReceiveTimeout = m_nTimeOut;
                m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                m_socket.Connect(ipEndPoint);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        public bool Close()
        {
            if (m_socket == null) return true;

            try
            {
                m_socket.Shutdown(SocketShutdown.Both);
                m_socket.Close();
                m_socket = null;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        public bool Read(byte[] buffer)
        {
            if (!Connected) return false;

            try
            {
                m_socket.Receive(buffer);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        public bool Read(byte[] buffer, int length)
        {
            if (!Connected) return false;

            try
            {
                if (length < 0) return false;

                m_socket.Receive(buffer, length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                LOG.Write($"Receive data failed, {ex.Message}, from {m_strAddress}:{m_nPort}");
                return false;
            }

            return true;
        }

        public bool Write(byte[] buffer)
        {
            if (!Connected) return false;

            try
            {
                m_socket.Send(buffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }

        public bool Write(byte[] buffer, int length)
        {
            if (!Connected) return false;

            try
            {
                m_socket.Send(buffer, length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }
    }
}
