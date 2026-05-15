using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{

    public class CometRFMatchMessage : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class CometRFMatchConnection : SerialPortConnectionBase
    {
        private static byte _start = 0xAA;
        private static byte _address = 0x21;
        private List<byte> _msgBuffer = new List<byte>();

        public CometRFMatchConnection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            CometRFMatchMessage msg = new CometRFMatchMessage();
            if (rawMessage == null || rawMessage.Length < 1)
            {
                return msg;
            }
            _msgBuffer.AddRange(rawMessage);
            if (_msgBuffer.Count < 4)
            {
                return msg;
            }

            byte checkSum = 0;
            for (int i = 0; i < _msgBuffer.Count - 1; i++)
            {
                checkSum += _msgBuffer[i];
            }

            if (checkSum == _msgBuffer[_msgBuffer.Count - 1])
            {
                msg.RawMessage = _msgBuffer.ToArray();
                _msgBuffer.Clear();
            }
            else
            {
                var index = _msgBuffer.FindIndex(4, m => m == _start);
                if (index < 0)
                {
                    return msg;
                }

                msg.RawMessage = _msgBuffer.Take(index).ToArray();
                _msgBuffer.RemoveRange(0, msg.RawMessage.Length);
            }

            if (msg.RawMessage.Length < 4)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }

            if (msg.RawMessage[0] != _start)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid start byte");
                return msg;
            }
            if (msg.RawMessage[1] != _address)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid address byte");
                return msg;
            }

            msg.Data = msg.RawMessage.Skip(2).Take(msg.RawMessage.Length - 3).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}
