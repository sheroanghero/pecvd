using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA100
{
    public class PfeifferPumpA100Message : BinaryMessage
    {
        //public string Data { get; set; }
        public string Address { get; set; }
        public byte[] Data { get; set; }
    }

    public class PfeifferPumpA100Connection : SerialPortConnectionBase
    {
        //private static string _startLine = "#";
        private static string _endLine = "\r";
        private List<byte> _msgBuffer = new List<byte>();
        private byte _end = 0x0d;

        public PfeifferPumpA100Connection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, false)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            PfeifferPumpA100Message msg = new PfeifferPumpA100Message();
            if (rawMessage == null || rawMessage.Length == 0)
            {
                return msg;
            }

            _msgBuffer.AddRange(rawMessage);
            var index = _msgBuffer.IndexOf(_end);

            if (index > -1)
            {
                msg.RawMessage = _msgBuffer.Take(index).ToArray();
                msg.Data = _msgBuffer.Skip(4).Take(index - 4).ToArray();
                _msgBuffer.RemoveRange(0, index);
                _msgBuffer.Remove(0x0a);
                _msgBuffer.Remove(_end);

                msg.IsResponse = true;
                msg.IsAck = true;

                return msg;
            }
            return msg;
        }

        //protected override MessageBase ParseResponse(string rawText)
        //{
        //    PfeifferPumpA100Message msg = new PfeifferPumpA100Message();
        //    msg.RawMessage = rawText;

        //    if (rawText.Length <= 0)
        //    {
        //        LOG.Error($"empty response");
        //        msg.IsFormatError = true;
        //        return msg;
        //    }
        //    if (rawText.Length <= 4)
        //    {
        //        LOG.Error($"too short response");
        //        msg.IsFormatError = true;
        //        return msg;
        //    }
        //    if (rawText[0].ToString() != _startLine)
        //    {
        //        LOG.Error($"invalid format response");
        //        msg.IsFormatError = true;
        //        return msg;
        //    }
        //    rawText = rawText.Replace("\r", "").Replace("\n", "").Replace(_startLine, "");//remove start char and end char
        //    msg.Address = rawText.Substring(0, 3);
        //    msg.Data = rawText.Substring(4);
        //    msg.IsResponse = true;
        //    msg.IsAck = true;

        //    return msg;
        //}



    }
}
