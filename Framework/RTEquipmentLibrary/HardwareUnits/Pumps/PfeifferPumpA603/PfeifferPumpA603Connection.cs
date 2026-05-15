using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA603
{
    public class PfeifferPumpA603Message : AsciiMessage
    {
        public string Data { get; set; }
        public string Address { get; set; }
    }

    public class PfeifferPumpA603Connection : SerialPortConnectionBase
    {
        private static string _startLine = "#";
        private static string _endLine = "\r";


        public PfeifferPumpA603Connection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            PfeifferPumpA603Message msg = new PfeifferPumpA603Message();
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
            if (rawText[0].ToString() != _startLine)
            {
                LOG.Error($"invalid format response");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Replace("\r", "").Replace("\n", "").Replace(_startLine, "");//remove start char and end char
            msg.Address = rawText.Substring(0, 3);
            msg.Data = rawText.Substring(4);
            msg.IsResponse = true;
            msg.IsAck = true;

            return msg;
        }



    }
}
