using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public class FfuAAFMessage : BinaryMessage
    {
        public byte Preamble { get; set; }
        public byte Command { get; set; }
        public byte GroupAddress { get; set; }       
        public byte Data1 { get; set; }
        public byte Data2 { get; set; }
    }

    public class FfuAAFConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public FfuAAFConnection(string portName) : base(portName,9600,8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One,"\r",false)
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

            FfuAAFMessage msg = new FfuAAFMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            if (temps.LastOrDefault() != ModRTU_CRC(temps.Take(temps.Length - 1).ToArray()))
                return msg;          

            msg.Preamble = temps[0];
            msg.Command = temps[1];
            msg.GroupAddress = temps[2];
            if (temps.Length > 4) msg.Data1 = temps[3];
            if (temps.Length > 5) msg.Data2 = temps[4];


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
