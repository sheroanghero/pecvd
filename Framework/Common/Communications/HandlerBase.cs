using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Communications
{
    public enum EnumHandlerState
    {
        Create,
        Sent,
        Acked,
        Completed,
        Nak,
        Can,
        Abs,
    }

    public abstract class HandlerBase
    {
        public bool IsComplete
        {
            get { return _state == EnumHandlerState.Completed; }
        }
        public bool IsCompleteTimeout { get; set; }
        public bool IsAcked
        {
            get { return _state == EnumHandlerState.Acked; }
        }

        public bool IsAckTimeout { get; set; }

        private EnumHandlerState _state;
        public EnumHandlerState CurrentState => _state;

        public string Name { get; set; }

        public int MutexId { get; set; }

        public TimeSpan AckTimeout { get; set; }
        public TimeSpan CompleteTimeout { get; set; }

        public MessageBase ResponseMessage { get; set; }

        public string SendText { get; set; }
        public byte[] SendBinary { get; set; }

        protected Stopwatch _timerAck = new Stopwatch();
        protected Stopwatch _timerComplete = new Stopwatch();

        protected HandlerBase(string text)
        {
            SendText = text;
            SendBinary = Encoding.ASCII.GetBytes(text);

            AckTimeout = TimeSpan.FromSeconds(15);
            CompleteTimeout = TimeSpan.FromSeconds(60);
            IsAckTimeout = false;
            IsCompleteTimeout = false;

            _state = EnumHandlerState.Create;
        }

        protected HandlerBase(byte[] buffer, int ackTimeout = 2)
        {
            SendText = "";
            SendBinary = buffer;

            AckTimeout = TimeSpan.FromSeconds(ackTimeout);
            CompleteTimeout = TimeSpan.FromSeconds(90);
            IsAckTimeout = false;
            IsCompleteTimeout = false;
            _state = EnumHandlerState.Create;
        }

        public abstract bool HandleMessage(MessageBase msg, out bool transactionComplete);

        public void OnSent()
        {
            SetState(EnumHandlerState.Sent);
        }

        public void OnAcked()
        {
            SetState(EnumHandlerState.Acked);
        }

        public void OnComplete()
        {
            SetState(EnumHandlerState.Completed);
        }

        public void SetState(EnumHandlerState state)
        {
            _state = state;

            if (_state == EnumHandlerState.Sent) //connection's responsibility
            {
                _timerAck.Restart();
                _timerComplete.Restart();
            }

            if (_state == EnumHandlerState.Acked)  //handler's responsibility
            {
                _timerAck.Stop();
            }

            if (_state == EnumHandlerState.Completed)//handler's responsibility
            {
                _timerAck.Stop();
                _timerComplete.Stop();
            }
        }
        public bool CheckTimeout( )
        {
            if (_state == EnumHandlerState.Sent && _timerAck.IsRunning && _timerAck.Elapsed > AckTimeout)
            {                
                _timerAck.Stop();
                _state = EnumHandlerState.Completed;
                IsAckTimeout = true;
                return true;
            }

            if ((_state == EnumHandlerState.Sent || _state==EnumHandlerState.Acked) && _timerComplete.IsRunning && _timerComplete.Elapsed > CompleteTimeout)
            {
                _timerAck.Stop();
                _timerComplete.Stop();
                _state = EnumHandlerState.Completed;
                IsCompleteTimeout = true;
                return true;
            }

            return false;
        }
        public virtual bool MatchMessage(MessageBase msg)
        {
            return false;
        }
    }

}
