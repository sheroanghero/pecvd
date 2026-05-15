using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkyPump
{
    public class SkyPumpMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string Address { get; set; }
    }

    public class SkyPumpConnection : SerialPortConnectionBase
    {
        private static string _endLine = "\r\n";

        private string _cachedBuffer = string.Empty;

        public SkyPumpConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            SkyPumpMessage msg = new SkyPumpMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            if (rawText.Length <= 4)
            {
                LOG.Error($"too short response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Substring(1, rawText.Length - 4);//remove start char and end char
            msg.Address = rawText.Substring(0,2);
            msg.Data = rawText.Substring(2);
            msg.IsResponse = true;
            msg.IsAck = true;

            return msg;
        }

        private bool CheckSum(string responseContent)
        {
            string responseSum = responseContent.Substring(responseContent.Length - 2);
            string checkContent = responseContent.Substring(0, responseContent.Length - 2);
            var charArray = checkContent.ToArray();
            int sum = charArray.Sum(chr => (int)chr);
            string sSum = sum.ToString("X");

            if (sSum.Length < 2)
                sSum = "0" + sSum;
            else
                sSum = sSum.Substring(sSum.Length - 2);

            return responseSum == sSum;
        }


    }
}
