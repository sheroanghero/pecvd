using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.Hanbell
{
    public class HanbellPumpMessage : BinaryMessage
    {
        public byte Address { get; set; }
        public byte FunctionCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class HanbellPumpConnection : SerialPortConnectionBase
    {
        private string _cachedBuffer = string.Empty;

        public HanbellPumpConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {

            HanbellPumpMessage msg = new HanbellPumpMessage();
            msg.RawMessage = rawMessage;

            //if (temps.Length < 4) return msg;

            msg.Address = rawMessage[0];
            msg.FunctionCode = rawMessage[1];
            //msg.Length = temps[2];

            if (rawMessage.Length < 4)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }

            msg.Data = rawMessage.Skip(2).Take(rawMessage.Length - 2).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }


    public class HanbellPumpTCPConnection : TCPPortConnectionBase
    {
        private List<byte> _msgBuffer = new List<byte>();

        public HanbellPumpTCPConnection(string address)
            : base(address, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            HanbellPumpMessage msg = new HanbellPumpMessage();
            if (rawMessage == null || rawMessage.Length == 0)
            {
                return msg;
            }
            _msgBuffer.AddRange(rawMessage);
            if(_msgBuffer.Count < 12)
            {
                return msg;
            }

            if(_msgBuffer[7] == 0x05)
            {
                msg.RawMessage = _msgBuffer.Take(12).ToArray();
                msg.Address = msg.RawMessage[6];
                msg.FunctionCode = msg.RawMessage[7];
                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
                _msgBuffer.RemoveRange(0,12);
            }
            else if(_msgBuffer[7] == 0x03)
            {
                if(_msgBuffer.Count >= 99)
                {
                    msg.RawMessage = _msgBuffer.Take(99).ToArray();
                    msg.Data = _msgBuffer.Skip(8).Take(91).ToArray();
                    msg.Address = msg.RawMessage[6];
                    msg.FunctionCode = msg.RawMessage[7];
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = true;
                    _msgBuffer.RemoveRange(0, 99);
                }
            }
                
            return msg;
        }
    }
}
