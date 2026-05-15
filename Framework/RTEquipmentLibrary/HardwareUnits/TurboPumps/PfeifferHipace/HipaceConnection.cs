using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.PfeifferHipace
{
    public class HipaceMessage : AsciiMessage
    {
        public string DeviceAddress { get; set; }
        public string Action { get; set; }
        public string Parameter { get; set; }
        public int DataLength { get; set; }
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class HipaceConnection : SerialPortConnectionBase
    {
        public HipaceConnection(string portName) : base(portName)
        {

        }
        protected override MessageBase ParseResponse(string rawText)
        {
            HipaceMessage msg = new HipaceMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 14)
            {
                LOG.Error($"text length check failed, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            msg.MessagePart = new string[6];

            msg.MessagePart[0] = rawText.Substring(0, 3); //device address
            msg.MessagePart[1] = rawText.Substring(3, 2); // action
            msg.MessagePart[2] = rawText.Substring(5, 3); //parameter
            msg.MessagePart[3] = rawText.Substring(8, 2); //length

            if (!int.TryParse(msg.MessagePart[3], out int length))
            {
                LOG.Error($"text {msg.MessagePart[3]} not valid, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            msg.MessagePart[4] = rawText.Substring(10, length); //data

            msg.MessagePart[5] = rawText.Substring(10 + length, 3);

            if (!int.TryParse(msg.MessagePart[5], out int sum))
            {
                LOG.Error($"text {msg.MessagePart[5]} not valid, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            int checkSum = 0;
            string checkText = rawText.Substring(0, rawText.Length - 3 - 1);
 
                foreach (var item in checkText)
                {
                    checkSum += (int)item;
                }

            if (checkSum % 256 != sum)
            {
                LOG.Error($"check sum failed, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            msg.DeviceAddress = msg.MessagePart[0];
            msg.Action = msg.MessagePart[1];
            msg.Parameter = msg.MessagePart[2];
            msg.DataLength = length;
            msg.Data = msg.MessagePart[4];

            if (msg.Data == "NO_DEF" || msg.Data=="_RANGE" || msg.Data=="_LOGIC")
            {
                msg.IsError = true;
            }

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
 
    }
}
