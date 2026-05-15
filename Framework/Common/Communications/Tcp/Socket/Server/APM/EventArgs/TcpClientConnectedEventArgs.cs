using System;

namespace MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs
{
    public class TcpClientConnectedEventArgs : System.EventArgs
    {
        public TcpClientConnectedEventArgs(TcpSocketSession session)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            this.Session = session;
        }

        public TcpSocketSession Session { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}", this.Session);
        }
    }
}
