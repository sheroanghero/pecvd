using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.RisshiChiller
{
    public class RisshiChillerMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class RisshiChillerConnection : SerialPortConnectionBase
    {
        private static char _endLine = (char)3;
        //private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;

        public RisshiChillerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine.ToString(), true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            RisshiChillerMessage msg = new RisshiChillerMessage();
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
            rawText = rawText.Substring(1, rawText.Length - 2);//remove start char and end char

            if(!CheckSum(rawText))
            {
                LOG.Error($"check sum failed,");
                msg.IsFormatError = true;
                return msg;
            }

            rawText = rawText.Substring(2, rawText.Length - 4);// remove length and sum
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
            }
            else if (rawText == "_RDY")
            {
                msg.Data = rawText;

                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }
            else
            {
                msg.Data = rawText;
                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }

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
