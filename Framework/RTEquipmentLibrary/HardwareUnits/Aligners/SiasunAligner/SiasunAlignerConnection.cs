using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.SiasunAligner
{
    public class SiasunAlignerMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class SiasunAlignerConnection : SerialPortConnectionBase
    {
        private string _cachedBuffer = string.Empty;

        public SiasunAlignerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            SiasunAlignerMessage msg = new SiasunAlignerMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Replace("\r", "").Replace("\n", "");
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
                msg.IsComplete = true;
            }
            else if (rawText.Contains("_RDY"))
            {
                msg.Data = rawText.Replace("_RDY", "");

                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }
            else
            {
                msg.Data = rawText;
                msg.IsResponse = true;
            }

            return msg;
        }
    }
}
