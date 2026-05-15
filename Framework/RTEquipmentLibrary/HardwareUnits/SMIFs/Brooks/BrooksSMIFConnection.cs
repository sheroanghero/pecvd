using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks
{
    public class BrooksSMIFMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }

        public bool IsAlarm { get; set; }
    }

    public class BrooksSMIFConnection : SerialPortConnectionBase
    {
        //private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;
        BrooksSMIF _device;
        public BrooksSMIFConnection(BrooksSMIF device, string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r\n", true)
        {
            _device = device;
        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            BrooksSMIFMessage msg = new BrooksSMIFMessage();
            msg.RawMessage = rawText;

            rawText = rawText.Replace("\r","").Replace("\n", "");
            if (rawText.Length <= 4)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }

            msg.MessagePart = rawText.Split(' ');
            if (msg.MessagePart.Length < 2)
            {
                LOG.Error($"too short response,");
                msg.IsFormatError = true;
                return msg;
            }

            if(msg.MessagePart[0] == "HCA")
            {
                if(msg.MessagePart[1] == "OK")
                {
                    msg.IsAck = true;
                }
                else
                {
                    msg.IsError = true;
                }
                msg.Data = msg.MessagePart[1];
            }
            else if(msg.MessagePart[0] == "AERS")
            {
                msg.IsEvent = true;
                msg.Data = msg.MessagePart[1];
            }
            else if (msg.MessagePart[0] == "ARS")
            {
                if(msg.MessagePart[1] != "0000") //0000 means no alarm
                {
                    msg.IsAlarm = true;
                    OnAlarmArrived(msg);
                }
                msg.Data = msg.MessagePart[1];
            }
            else if (msg.MessagePart[0] == "ECD")
            {
                msg.Data = msg.MessagePart[1];
            }
            else if (msg.MessagePart[0].Contains("FSD"))
            {
                msg.Data = rawText.Substring(5);
            }
            return msg;
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            BrooksSMIFMessage message = msg as BrooksSMIFMessage;
            _device.OnEventArrived(message.Data);
        }

        private void OnAlarmArrived(BrooksSMIFMessage msg)
        {
            _device.OnAlarmArrived(msg.Data);
        }

    }
}
