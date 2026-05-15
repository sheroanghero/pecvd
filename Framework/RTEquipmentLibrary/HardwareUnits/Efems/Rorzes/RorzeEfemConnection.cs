using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{

    public class RorzeEfemMessage : AsciiMessage
    {
        public ModuleName TargetModule { get; set; }

        public RorzeEfemMessageType MessageType { get; set; }

        public RorzeEfemBasicMessage BasicMessage { get; set; }

        public string Parameter { get; set; }

        public string NakFactor { get; set; }

        public bool WaitAck { get; set; }

        public bool WaitInf { get; set; }

        //ABS : Message* | ERROR / Parameter1 / Parameter2 ;
        //ERROR / Parameter1 / Parameter2
        public string AbsError { get; set; }

        //CAN : Message＊ | Factor / Place ;
        //Factor / Place
        public string CanError { get; set; }
    }

    public class RorzeEfemConnection : FsmConnection
    {
        private string _cachedBuffer = string.Empty;

        private object _lockerHandler = new object();

        private IConnectionContext _config;

        List<HandlerBase> _handlers = new List<HandlerBase>();

        private Dictionary<RorzeEfemBasicMessage, HandlerBase> _eventHandler = new Dictionary<RorzeEfemBasicMessage, HandlerBase>();

        public RorzeEfemConnection(IConnectionContext config)
        {
            _config = config;
        }


        public bool IsBusy(int mutexId)
        {
            lock (_lockerHandler)
            {
                foreach (var handlerBase in _handlers)
                {
                    if (handlerBase.MutexId == mutexId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddEventHandler(RorzeEfemBasicMessage type, RorzeEfemHandler handler)
        {
            System.Diagnostics.Debug.Assert(!_eventHandler.ContainsKey(type));

            _eventHandler[type] = handler;
        }

        public void Initialize()
        {
            base.Initialize(100, new SocketClient(_config), _config);
        }

        protected override void OnConnect()
        {

        }

        protected override void OnDisconnect()
        {
            lock (_lockerHandler)
            {
                _cachedBuffer = string.Empty;
                _handlers.Clear();
            }
        }

        protected override void OnConnectMonitor()
        {
            MonitorTimeout();

            base.OnConnectMonitor();
        }

        private HandlerBase MonitorTimeout()
        {
            HandlerBase result = null;
            lock (_lockerHandler)
            {
                foreach (var handlerBase in _handlers)
                {
                    if (handlerBase.CheckTimeout())
                    {
                        EV.PostWarningLog("System", $"execute {handlerBase.Name} timeout");
                        result = handlerBase;
                        break;
                    }
                }

                if (result != null)
                {
                    _handlers.Remove(result);
                }
            }

            return result;
        }

        public bool Execute(HandlerBase handler, out string reason)
        {
            lock (_lockerHandler)
            {
                foreach (var handlerBase in _handlers)
                {
                    if (handlerBase.MutexId == handler.MutexId && handler.MutexId!=-1)
                    {
                        reason = $"Can not execute {handler.Name} while execute {handlerBase.Name}";
                        return false;
                    }
                }

                _handlers.Add(handler);

                handler.SetState(EnumHandlerState.Sent);

                if (_config.IsAscii)
                {
                    InvokeSendData(handler.SendText);
                }
                else
                {
                    InvokeSendData(handler.SendBinary);
                }
            }

            reason = string.Empty;
            return true;
        }


        public void Reset()
        {
            InvokeReset();
        }

        protected override void OnReceiveData(string message)
        {
            _cachedBuffer += message;

            int enderIndex = _cachedBuffer.IndexOf(";\r");
            while (enderIndex != -1)
            {
                if (enderIndex > 0)
                {
                    ProceedResponse(_cachedBuffer.Substring(0, enderIndex));
                }

                _cachedBuffer = _cachedBuffer.Remove(0, enderIndex + 2);
                enderIndex = _cachedBuffer.IndexOf(";\r");
            }
        }

        private void AckInfo(string message)
        {
            if (message.StartsWith("INF:"))
            {
                InvokeSendData(message.Replace("INF:", "ACK:") + ";\r");
            }
            else if (message.StartsWith("ABS:"))
            {
                InvokeSendData(message.Replace("ABS:", "ACK:") + ";\r");
            }
            else if (message.StartsWith("NAK:"))
            {
                InvokeSendData(message.Replace("NAK:", "ACK:") + ";\r");
            }
            else if (message.StartsWith("CAN:"))
            {
                InvokeSendData(message.Replace("CAN:", "ACK:") + ";\r");
            }
        }
        protected void ProceedResponse(string rawMessage)
        {
            RorzeEfemMessage msg = new RorzeEfemMessage();
            msg.RawMessage = rawMessage;
            if (rawMessage == "INF:READY/COMM")
            {
                _eventHandler[RorzeEfemBasicMessage.READY].HandleMessage(msg, out _);

                AckInfo(rawMessage);

                return;
            }

            string[] words = rawMessage.Split(new char[5] { ':', '/', '>', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            msg.MessagePart = words;

            if (!Enum.TryParse<RorzeEfemMessageType>(words[0], out RorzeEfemMessageType type))
            {
                LOG.Write($"{rawMessage} is not a valid EFEM message format");
                return;
            }

            if (type == RorzeEfemMessageType.MOV || type == RorzeEfemMessageType.GET ||
                type == RorzeEfemMessageType.SET)
            {
                LOG.Write($"{rawMessage} is not a valid EFEM message format, {type} is TO EFEM");
                return;
            }

            msg.MessageType = type;
            msg.IsEvent = msg.MessageType == RorzeEfemMessageType.EVT;

            int messageIndex = msg.MessageType == RorzeEfemMessageType.NAK ? 2 : 1;

            if (words.Length <= messageIndex || !Enum.TryParse<RorzeEfemBasicMessage>(words[messageIndex], out RorzeEfemBasicMessage basicMessage))
            {
                LOG.Write($"{rawMessage} is not a valid EFEM message format");
                return;
            }
            msg.BasicMessage = basicMessage;

            if (msg.MessageType == RorzeEfemMessageType.NAK)
            {
                msg.NakFactor = words[1];
                msg.Parameter = rawMessage.Substring(rawMessage.IndexOf('/') + 1);
            }
            else if (msg.MessageType == RorzeEfemMessageType.CAN)
            {
                msg.CanError = rawMessage.Substring(rawMessage.IndexOf('|'));
                msg.Parameter = rawMessage.Substring(rawMessage.IndexOf('/') + 1, rawMessage.IndexOf('|') - rawMessage.IndexOf('/'));
            }
            else if (msg.MessageType == RorzeEfemMessageType.ABS)
            {
                msg.AbsError = rawMessage.Substring(rawMessage.IndexOf('|'));
                msg.Parameter = rawMessage.Substring(rawMessage.IndexOf('/') + 1, rawMessage.IndexOf('|') - rawMessage.IndexOf('/'));
            }
            else
            {
                msg.Parameter = rawMessage.Substring(rawMessage.IndexOf('/') + 1);
            }

            if (msg.IsEvent)
            {
                ProceedEvent(msg);
                return;
            }

            //Report of ERROR
            if (msg.MessageType == RorzeEfemMessageType.INF && words.Length>=2 && words[1].ToUpper() == "ERROR")
            {
                AckInfo(rawMessage);
                return;
            }

            ProceedMessage(msg);
        }

        private void ProceedMessage(RorzeEfemMessage msg)
        {
            lock (_lockerHandler)
            {
                HandlerBase handlerDone = null;

                bool matchMessage = false;
                foreach (var handler in _handlers)
                {
                    matchMessage = handler.HandleMessage(msg, out bool completed);
                    if (completed)
                    {
                        handlerDone = handler;
                        AckInfo(msg.RawMessage);
                        break;
                    }

                    if (matchMessage)
                        break;
                }

                if (!matchMessage)
                {
                    LOG.Write($"No handler for message, {msg.RawMessage}");
                    return;
                }

                _handlers.Remove(handlerDone);
            }
        }

        private void ProceedEvent(RorzeEfemMessage msg)
        {
            if (_eventHandler.ContainsKey(msg.BasicMessage))
            {
                _eventHandler[msg.BasicMessage].HandleMessage(msg, out _);
            }
            else
            {
                LOG.Write($"No handler for event {msg.RawMessage}");
            }
        }

    }
}
