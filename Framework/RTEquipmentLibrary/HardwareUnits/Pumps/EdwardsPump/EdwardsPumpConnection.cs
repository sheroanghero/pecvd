using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump
{
    public class EdwardsPumpMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class EdwardsPumpConnection : SerialPortConnectionBase
    {
        private static string _endLine = "\r\n";
        public EdwardsPumpConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            EdwardsPumpMessage msg = new EdwardsPumpMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response");
                msg.IsFormatError = true;
                return msg;
            }
            if (rawText.Length <= 4)
            {
                LOG.Error($"too short response");
                msg.IsFormatError = true;
                return msg;
            }
            msg.Data = rawText.Replace("\r", "").Replace("\n", "");
            if (msg.Data.ToUpper().StartsWith("ERR"))
            {
                var errorCodeString = msg.Data.ToUpper().Replace("ERR", "").Replace(" ", "");
                int.TryParse(errorCodeString, out int errorCode);
                if (errorCode > 0)
                {
                    msg.ErrorText = errorCodeString;
                    msg.IsError = true;
                }
            }
           
            msg.IsResponse = true;
            msg.IsAck = true;

            return msg;
        }



    }
}
