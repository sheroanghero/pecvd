using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.HineAutomation
{
    public class HAtmMessage : AsciiMessage
    {
        public string DeviceAddress { get; set; }
        public string Action { get; set; }
        public string Parameter { get; set; }
        public int DataLength { get; set; }
        public string Data { get; set; }
        public string ErrorText { get; set; }


        public string ResponseRecordType { get; set; }
        public string Category { get; set; }
    }

    public class HAtmConnection : ConnectionBase
    {
        private const string ResponseRecordType = "X";

        public HAtmConnection(string addr) : base(addr)
        {

        }

        public void EnableLog(bool enable)
        {
            _socket.EnableLog = enable;
        }

        protected override void OnDataReceived(string oneLineMessage)
        {
            MessageBase msg = ParseResponse(oneLineMessage);
            if (msg.IsFormatError)
            {
                SetCommunicationError(true, "received invalid response message.");
            }

            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || (((HAtmHandler)_activeHandler).HasResponse && (_activeHandler.HandleMessage(msg, out bool completed) && completed)))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            HAtmMessage msg = new HAtmMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = rawText;
            rawText = rawText.Replace("\r", "").Replace("\n", "");          
            string[] words = Regex.Split(rawText, ",");

            if (words.Length < 2 || words[0] != ResponseRecordType)
            {
                msg.IsFormatError = true;
                LOG.Error($"Record type check failed, " + rawText);
                return msg;
            }

            msg.ResponseRecordType = words[0];
            msg.Category = words[1];
            if(words.Length > 2)
            {
                msg.Data = words[2];

                msg.MessagePart = new string[words.Length - 2];
                for (int i = 0; i < msg.MessagePart.Length; i++)
                {
                    msg.MessagePart[i] = words[2 + i];
                }
            }          

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            return msg;          
        }
    }
}
