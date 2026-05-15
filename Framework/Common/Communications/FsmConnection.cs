using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Fsm;

namespace MECF.Framework.Common.Communications
{
    public class FsmConnection : ActiveFsm
    {
        private enum ConnectionState
        {
            Init = 1000,
            NotConnect,
            Connecting,
            Connected,
            Error,
        }

        private enum MSG
        {
            Connect = 1000,
            Disconnect,
            ConnectFailed,
            Connected,
            Reset,
            CommunicationError,

            ReceiveBinaryData,
            SendBinaryData,

            ReceiveAsciiData,
            SendAsciiData,

            ConfirmConnection,
        }

        public bool IsConnected
        {
            get
            {
                return FsmState == (int)ConnectionState.Connected;
            }
        }

        public bool IsError
        {
            get
            {
                return FsmState == (int)ConnectionState.Error;
            }
        }

        public event Action<string> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        private IConnectable _connectable;
        private IConnectionContext _config;
        public string Name { get; set; }
        private string _lastError;

        public FsmConnection()
        {
        }

        public virtual void Initialize(int fsmInterval, IConnectable connectable, IConnectionContext config)
        {
            _connectable = connectable;
            _config = config;

            _connectable.OnBinaryDataReceived += _connectable_OnBinaryDataChanged;
            _connectable.OnAsciiDataReceived += _connectable_OnAsciiDataChanged;
            _connectable.OnCommunicationError += ConnectableOnCommunicationError;

            EnumLoop<ConnectionState>.ForEach((item) =>
            {
                MapState((int)item, item.ToString());
            });

            EnumLoop<MSG>.ForEach((item) =>
            {
                MapMessage((int)item, item.ToString());
            });

            //init
            EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Init, FsmEnterInit, FSM_MSG.NONE, null);

            Transition(ConnectionState.Init, MSG.Disconnect, null, ConnectionState.NotConnect);
            Transition(ConnectionState.Init, MSG.Connect, null, ConnectionState.Connecting);

            //Connecting
            EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Connecting, FsmEnterConnecting, FSM_MSG.NONE, null);

            Transition(ConnectionState.Connecting, MSG.ConnectFailed, null, ConnectionState.Error);
            Transition(ConnectionState.Connecting, MSG.Connected, null, ConnectionState.Connected);
            Transition(ConnectionState.Connecting, MSG.Disconnect, FsmDisconnect, ConnectionState.NotConnect);
            Transition(ConnectionState.Connecting, FSM_MSG.TIMER, FsmMonitorConnecting, ConnectionState.Connecting);

            //Connected
            EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Connected, FsmEnterConnected, FSM_MSG.NONE, FsmExitConnected);

            Transition(ConnectionState.Connected, MSG.Disconnect, FsmDisconnect, ConnectionState.NotConnect);
            Transition(ConnectionState.Connected, MSG.CommunicationError, null, ConnectionState.Error);
            Transition(ConnectionState.Connected, MSG.ConnectFailed, null, ConnectionState.Error);
            Transition(ConnectionState.Connected, FSM_MSG.TIMER, FsmMonitorConnected, ConnectionState.Connected);
            Transition(ConnectionState.Connected, MSG.ConfirmConnection, FsmMonitorConnected, ConnectionState.Connected);

            Transition(ConnectionState.Connected, MSG.ReceiveBinaryData, FsmReceiveBinaryData, ConnectionState.Connected);
            Transition(ConnectionState.Connected, MSG.SendBinaryData, FsmSendBinaryData, ConnectionState.Connected);
            Transition(ConnectionState.Connected, MSG.ReceiveAsciiData, FsmReceiveAsciiData, ConnectionState.Connected);
            Transition(ConnectionState.Connected, MSG.SendAsciiData, FsmSendAsciiData, ConnectionState.Connected);

            //NotConnect
            Transition(ConnectionState.NotConnect, MSG.Connect, null, ConnectionState.Connecting);

            //Error
            EnterExitTransition<ConnectionState, FSM_MSG>(ConnectionState.Error, FsmEnterError, FSM_MSG.NONE, null);

            Transition(ConnectionState.Error, MSG.Disconnect, null, ConnectionState.NotConnect);
            Transition(ConnectionState.Error, MSG.Reset, null, ConnectionState.Connecting);

            StartFsm(Name, fsmInterval, (int)ConnectionState.Init);
        }

        public virtual void Terminate()
        {
            StopFsm();
            try
            {
                _connectable.Disconnect(out _);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        protected virtual void OnReceiveData(string message)
        {

        }

        protected virtual void OnReceiveData(byte[] message)
        {

        }

        protected virtual void OnConnect()
        {

        }

        protected virtual void OnDisconnect()
        {

        }


        protected virtual void OnConnectMonitor()
        {

        }


        private bool FsmSendAsciiData(object[] param)
        {
            if (!_connectable.SendAsciiData((string)param[0]))
            {
                PostMsg(MSG.ConfirmConnection);
            }

            return true;
        }

        private bool FsmReceiveAsciiData(object[] param)
        {
            try
            {
                OnReceiveData((string)param[0]);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        private bool FsmSendBinaryData(object[] param)
        {
            if (!_connectable.SendBinaryData((byte[])param[0]))
            {
                PostMsg(MSG.ConfirmConnection);
            }

            return true;
        }

        private bool FsmReceiveBinaryData(object[] param)
        {
            try
            {
                OnReceiveData((byte[])param[0]);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        private void ConnectableOnCommunicationError(string obj)
        {
            if (!CheckToPostMessage((int)MSG.CommunicationError, out string reason, obj))
            {
                LOG.Write(reason);
            }
        }

        private void _connectable_OnAsciiDataChanged(string obj)
        {
            if (!CheckToPostMessage((int)MSG.ReceiveAsciiData, out string reason, obj))
            {
                LOG.Write(reason);
            }
        }

        private void _connectable_OnBinaryDataChanged(byte[] obj)
        {
            if (!CheckToPostMessage((int)MSG.ReceiveBinaryData, out string reason, obj))
            {
                LOG.Write(reason);
            }
        }

        private bool FsmDisconnect(object[] param)
        {
            if (!_connectable.Disconnect(out string reason))
            {
                LOG.Write(reason);
                return false;
            }

            return true;
        }

        private bool FsmExitConnected(object[] param)
        {
            if (OnDisconnected != null)
            {
                OnDisconnected();
            }

            OnDisconnect();

            return true;
        }

        private bool FsmEnterError(object[] param)
        {
            if (OnError != null)
            {
                OnError(_lastError);
            }

            return true;
        }

        private bool FsmEnterConnecting(object[] param)
        {
            if (!_connectable.Connect(out string reason))
            {
                _lastError = reason;
                PostMsg(MSG.ConnectFailed);
            }

            return true;
        }

        private bool FsmMonitorConnecting(object[] param)
        {
            if (_connectable.CheckIsConnected())
            {
                PostMsg(MSG.Connected);
                return true;
            }

            if (!PeformConnect())
            {
                _lastError = $"Can not connect with {Name}";
                PostMsg(MSG.ConnectFailed);
                return true;
            }

            return true;
        }

        private bool FsmMonitorConnected(object[] param)
        {
            if (_config.EnableCheckConnection)
            {
                if (!_connectable.CheckIsConnected())
                {
                    if (!PeformConnect())
                    {
                        _lastError = $"Can not connect with {Name}";
                        PostMsg(MSG.ConnectFailed);
                        return false;
                    }
                }
            }

            OnConnectMonitor();

            return true;
        }

        private bool PeformConnect()
        {
            int retryTime = 0;
            do
            {
                if (!_connectable.Connect(out string reason))
                {
                    _lastError = reason;
                    return false;
                }

                Thread.Sleep(10);

                if (_connectable.CheckIsConnected())
                {
                    return true;
                }

                if (retryTime < _config.MaxRetryConnectCount)
                {
                    retryTime++;

                    LOG.Write($"{Name} retry connect {retryTime} /{_config.MaxRetryConnectCount} time");

                    Thread.Sleep(_config.RetryConnectIntervalMs);
                }

            } while (retryTime < _config.MaxRetryConnectCount);


            if (!_connectable.CheckIsConnected())
            {
                LOG.Error($"Can not connect with {Name}");
                return false;
            }

            return true;
        }


        private bool FsmEnterConnected(object[] param)
        {
            if (OnConnected != null)
                OnConnected();

            OnConnect();

            return true;
        }


        private bool FsmEnterInit(object[] param)
        {
            if (_config.IsEnabled)
            {
                PostMsg(MSG.Connect);
            }
            else
            {
                PostMsg(MSG.Disconnect);
            }

            return true;
        }

        public void InvokeSendData<T>(T data)
        {
            if (!CheckToPostMessage((int)MSG.SendAsciiData, out string reason, data))
            {
                LOG.Write(reason);
            }
        }

        public void InvokeConnect()
        {
            if (!CheckToPostMessage((int)MSG.Connect, out string reason))
            {
                LOG.Write(reason);
            }
        }

        public void InvokeDisconnect()
        {
            if (!CheckToPostMessage((int)MSG.Disconnect, out string reason))
            {
                LOG.Write(reason);
            }
        }

        public void InvokeError()
        {
            if (!CheckToPostMessage((int)MSG.CommunicationError, out string reason))
            {
                LOG.Write(reason);
            }
        }


        public void InvokeReset()
        {
            if (!CheckToPostMessage((int)MSG.Reset, out string reason))
            {
                LOG.Write(reason);
            }
        }
    }

}
