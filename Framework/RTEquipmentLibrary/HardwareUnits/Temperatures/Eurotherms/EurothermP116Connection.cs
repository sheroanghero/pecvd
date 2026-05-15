using System.Collections.Generic;
using System.Linq;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Eurotherms
{
    public class P116PIDTCMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }
    public class EurothermP116Connection:SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public EurothermP116Connection(string portName) : base(portName, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One, "\n", false)
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

            P116PIDTCMessage msg = new P116PIDTCMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            if (msg.FunctionCode !=0x03)
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

        private static byte ModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)~temp;
        }
    }

}
