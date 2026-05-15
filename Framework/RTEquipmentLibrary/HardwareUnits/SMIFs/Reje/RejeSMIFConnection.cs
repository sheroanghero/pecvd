using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Reje
{
    public class RejeSMIFMessage : AsciiMessage
    {
        public string CommandType { get; set; }

        public string Command { get; set; }
        public string Data { get; set; }

        public string Error { get; set; }
    }

    public class RejeSMIFConnection : SerialPortConnectionBase
    {
        private RejeSMIF _device;
        public RejeSMIFConnection(RejeSMIF device, string port) : base(port, 9600, 8, Parity.None, StopBits.One, "\r", true)
        {
            _device = device;
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            RejeSMIFMessage msg = new RejeSMIFMessage();
            msg.RawMessage = rawText;
            msg.IsAck = false;
            msg.IsResponse = false;
            msg.IsComplete = false;
            msg.IsNak = false;
            msg.IsError = false;
            msg.CommandType = msg.RawMessage.Split(':')[0].Replace("s00", "");
            msg.Command = Regex.Match(msg.RawMessage, "(?<=:).*?(?=;)").Value;
            if (rawText.Contains('/'))
            {
                msg.Data = rawText.Split('/')[1].Replace(";", "").Replace("\r", "");
            }

            if (msg.CommandType.Contains("ACK"))
            {
                msg.IsAck = true;
            }
            if (msg.CommandType.Contains("NAK"))
            {
                _device.OnNak(rawText);
                msg.IsNak = true;

            }
            if (msg.CommandType.Contains("INF"))
            {
                msg.IsEvent = true;
            }
            if (msg.CommandType.Contains("ABS"))
            {
                _device.OnAbs(rawText);
                msg.IsError = true;
            }
            LOG.Write($"{Address} received message:{rawText}");
            return msg;
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            RejeSMIFMessage message = msg as RejeSMIFMessage;
            _device.OnEventArrived(message.Command);
        }



    }

}


