using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Omrons
{
    public class OmronEJ1Message : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }

    public class OmronEJ1Connection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public OmronEJ1Connection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "", bool isAsciiMode = false) 
            : base(portName, baudRate, dataBits, parity, stopBits, newline, isAsciiMode)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();

            OmronEJ1Message msg = new OmronEJ1Message();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            if (msg.FunctionCode != 0x03)
            {
                msg.Data = _lstCacheBuffer.ToArray();
            }
            else
            {
                msg.Length = temps[2];

                if (temps.Length < msg.Length + 5)
                {
                    //msg.IsFormatError = true; 
                    return msg;
                }

                msg.Data = _lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();
            }

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
 
    }
}