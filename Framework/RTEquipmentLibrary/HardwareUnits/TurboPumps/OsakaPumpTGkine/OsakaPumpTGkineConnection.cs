using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.OsakaPumpTGkine
{
    public class OsakaPumpTGkineMessage : BinaryMessage
    {
        public int Length { get; set; }
    }

    public class OsakaPumpTGkineConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public OsakaPumpTGkineConnection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
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

            OsakaPumpTGkineMessage msg = new OsakaPumpTGkineMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 8) 
                return msg;

            if (temps[6] == 0x03 && temps[7] == 0x03)
            {
                msg.IsResponse = true;
                msg.Length = 8;
                msg.RawMessage = _lstCacheBuffer.Take(8).ToArray();
            }else if (temps[8] == 0x03 && temps[9] == 0x03)
            {
                msg.IsResponse = true;
                msg.Length = 10;
                msg.RawMessage = _lstCacheBuffer.Take(10).ToArray();
            }

            _lstCacheBuffer.RemoveRange(0, msg.Length);

            return msg;
        }



    }
}
