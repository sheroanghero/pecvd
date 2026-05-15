using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.KITZ
{
    public class KITZMessage : AsciiMessage
    {
        public string Command { get; set; }
        public string Parameter { get; set; }
    }

    public class KITZConnection : SerialPortConnectionBase
    {
        public KITZConnection(string portName) : base(portName,115200, 8, Parity.None, StopBits.One, "\r")
        {

        }
        protected override MessageBase ParseResponse(string rawText)
        {
            KITZMessage msg = new KITZMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = rawText.Replace("\r", "").Replace("\n", "");
            
            if(string.IsNullOrEmpty(msg.RawMessage) || !msg.RawMessage.Contains(":"))
            {
                LOG.Error($"text length check failed, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            string[] words = Regex.Split(msg.RawMessage, ":");
            msg.Command = words[0];

            if(words.Length > 1)
                msg.Parameter = words[1];

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }

    }
}
