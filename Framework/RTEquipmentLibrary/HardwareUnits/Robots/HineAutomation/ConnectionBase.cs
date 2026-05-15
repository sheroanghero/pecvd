using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.HineAutomation
{
    public abstract class ConnectionBase : IConnection
    {
        public string Address
        {
            get { return _address; }
        }

        public bool IsConnected
        {
            get { return _socket == null ? false : _socket.IsConnected; }
        }

        public bool Connect()
        {
            if (_socket == null)
                return true;

            if (_socket.IsConnecting)
                return true;

            Disconnect();

            if (!string.IsNullOrEmpty(_address))
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                    _socket = new AsyncSocket(_address, "\r");
                else
                    _socket = new AsyncSocket(_address, "\n");
                _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataReceived);
                _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHappened);
                _socket.EnableLog = SC.GetValue<bool>($"TMRobot.EnableLogMessage");
            }

            _socket.Connect(_address);
            return true;
        }

        public bool Disconnect()
        {
            if (_socket != null)
            {                
                _socket.Dispose();
                _socket = null;
            }
                
            return true;
        }

        public bool IsBusy
        {
            get { return _activeHandler != null; }
        }

        public bool IsCommunicationError { get; private set; }
        public string LastCommunicationError { get; private set; }

        protected AsyncSocket _socket;

        protected HandlerBase _activeHandler; //set, control, 

        protected object _lockerActiveHandler = new object();

        private string _address;//ip:port

        private RD_TRIG _trigCommunicationError = new RD_TRIG();

        public bool EnableRetry { get; set; }
        public R_TRIG _trigRetryConnect = new R_TRIG();
        public int MaxRetryCount { get; set; }

        public ConnectionBase(string addr)
        {
            _address = addr;

            if (!string.IsNullOrEmpty(addr))
            {
                if (SC.GetValue<bool>("System.IsSimulatorMode"))
                    _socket = new AsyncSocket(addr, "\r");
                else
                    _socket = new AsyncSocket(addr, "\n");
                _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataReceived);
                _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHappened);
            }

        }

        private void OnErrorHappened(ErrorEventArgs args)
        {
        }

        public void Execute(HandlerBase handler)
        {
            if (_activeHandler != null)
                return;

            if (handler == null)
                return;

            if (IsConnected)
            {
                lock (_lockerActiveHandler)
                {
                    _activeHandler = handler;
                    _activeHandler.SetState(EnumHandlerState.Sent);
                }

                if (!_socket.Write(handler.SendText))
                {
                    lock (_lockerActiveHandler)
                    {
                        _activeHandler = null;
                    }

                    IsCommunicationError = true;
                }

                if (_activeHandler != null && !((HAtmHandler)_activeHandler).HasResponse)
                {
                    lock (_lockerActiveHandler)
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        protected abstract MessageBase ParseResponse(string rawMessage);

        protected virtual void OnEventArrived(MessageBase msg)
        {

        }

        protected abstract void OnDataReceived(string oneLineMessage);    

        public HandlerBase MonitorTimeout()
        {
            HandlerBase result = null;
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null && _activeHandler.CheckTimeout())
                {
                    result = _activeHandler;
                    _activeHandler = null;

                    SetCommunicationError(true, "receive response timeout");
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="retryConnectMaxCount">-1 keep retry, 0 no retry</param>
        public void MonitorConnection(int retryMaxCount, out bool retried)
        {
            retried = false;
            _trigRetryConnect.CLK = IsCommunicationError || !IsConnected;
            if (_trigRetryConnect.Q)
            {
                int retry = 0;
                while (retry < retryMaxCount || retryMaxCount < 0)
                {
                    LOG.Write($"Retry connect with {_address} for the {retry+1} time.");

                    Connect();

                    Thread.Sleep(retry * 1000);

                    retried = true;
                    lock (_lockerActiveHandler)
                    {
                        _activeHandler = null;
                    }

                    if (IsConnected)
                        break;

                    retry++;

                    //break;
                }
            }

            SetCommunicationError(false, "retried");
        }

        /// <summary>
        /// receive invalid packet
        /// send out timeout
        /// receive response timeout
        /// </summary>
        /// <param name="isError"></param>
        /// <param name="reason"></param>
        public void SetCommunicationError(bool isError, string reason)
        {
            IsCommunicationError = isError;
            LastCommunicationError = reason;

            _trigCommunicationError.CLK = isError;
            if (_trigCommunicationError.R)
            {
                LOG.Write($"{Address} communication error, {reason}");
            }
            
            if (_trigCommunicationError.T)
            {
                LOG.Write($"{Address} communication error recovered, {reason}");
            }
        }

        public void Reset()
        {
            _trigRetryConnect.RST = true;
        }
    }
}
