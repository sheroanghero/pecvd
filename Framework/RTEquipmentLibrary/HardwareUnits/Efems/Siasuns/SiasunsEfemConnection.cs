using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Siasuns
{
    public class SiasunsEfemMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }
    public class SiasunsEfemConnection : TCPPortConnectionBase
    {
        private string _cachedBuffer = string.Empty;
        private List<byte> _lstCacheBuffer = new List<byte>();
        private SiasunsEfem _efem;
        public SiasunsEfemConnection(string address, string newline = "\r", bool isAsciiMode = true) : base(address, newline, isAsciiMode)
        {
        }

        protected override MessageBase ParseResponse(string rawMessage)
        {
            SiasunsEfemMessage msg = new SiasunsEfemMessage();
            _cachedBuffer += rawMessage;

            msg.RawMessage = _cachedBuffer;
            if (_activeHandler == null)
            {
                _cachedBuffer = string.Empty;
                return msg;
            }

            if (_cachedBuffer.Contains("ACK"))
            {
                msg.IsAck = true;
                string message = _cachedBuffer.Replace("\r", "");
                string[] msList = message.Split(':');
                msg.Data = rawMessage;
                msg.MessagePart = msList;

                //while (_vtm.CommandMessages.Count > 50)
                //    _vtm.CommandMessages.RemoveLast();
                //_vtm.CommandMessages.AddFirst($"Received:{_cachedBuffer.Replace("\r", "")}");

                _cachedBuffer = string.Empty;
            }
            else if (_cachedBuffer.Contains("INF"))
            {
                msg.IsAck = true;
                msg.IsResponse = true;
                string message = _cachedBuffer.Replace("\r", "");
                string[] msList = message.Split(':');
                msg.Data = rawMessage;
                msg.MessagePart = msList;

                //while (m_Robot.CommandMessages.Count > 50)
                //    m_Robot.CommandMessages.RemoveLast();
                //m_Robot.CommandMessages.AddFirst($"Received:{_cachedBuffer.Replace("\r", "")}");
                _cachedBuffer = string.Empty;

            }

            return msg;
        }

        public void SendAck(string cmd, string param)
        {
            string commandstr = $"ACK:{cmd}/{param}";
            SendMessage(commandstr);
        }
    }
}
