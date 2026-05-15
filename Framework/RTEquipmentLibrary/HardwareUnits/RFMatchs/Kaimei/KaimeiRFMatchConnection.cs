using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Kaimei
{
    public class KaimeiRFMatchMessage : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class KaimeiRFMatchConnection : SerialPortConnectionBase
    {
        private List<byte> _msgBuffer = new List<byte>();

        public KaimeiRFMatchConnection(string portName, int baudRate = 38400, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            KaimeiRFMatchMessage msg = new KaimeiRFMatchMessage();
            if (rawMessage == null || rawMessage.Length < 1)
            {
                return null;
            }
            _msgBuffer.AddRange(rawMessage);
            if (_msgBuffer.Count < 5)
            {
                return msg;
            }

            if(_msgBuffer[1] == 0x03)
            {
                var dataLength = _msgBuffer[2];
                if(_msgBuffer.Count < 3 + dataLength + 2)
                {
                    return msg;
                }
                else
                {
                    msg.RawMessage = _msgBuffer.Take(3 + dataLength + 2).ToArray();
                    msg.Data = _msgBuffer.Skip(3).Take(dataLength).ToArray();
                    _msgBuffer.RemoveRange(0, msg.RawMessage.Length);
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = true;

                    return msg;
                }
            }
            else if(_msgBuffer[1] == 0x10)
            {
                //SlaveAddress,FunctionCode,StartingAddressHigh,StartingAddressLow,NumberAddressHigh,NumberAddressLow,CRC,CRC
                if(_msgBuffer.Count < 8)
                {
                    return msg;
                }
                else
                {
                    msg.RawMessage = _msgBuffer.Take(8).ToArray();
                    _msgBuffer.RemoveRange(0, msg.RawMessage.Length);
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = true;

                    return msg;
                }
            }
            else
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }
        }
    }
}
