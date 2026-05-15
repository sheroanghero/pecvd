using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners
{
    public class FuqiAlignerMessage : AsciiMessage
    {
        public string Data { get; set; }
    }
    public class FuqiAlignerMessageBIN : BinaryMessage
    {
        public byte[] CMD { get; set; }
        public byte[] Data { get; set; }
    }

    public class FuqiAlignerConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public FuqiAlignerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawBuffer)
        {

            _lstCacheBuffer.AddRange(rawBuffer);
            byte[] temps = _lstCacheBuffer.ToArray();
            FuqiAlignerMessageBIN msg = new FuqiAlignerMessageBIN();
            msg.IsResponse = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Last() == 0xA)
            {
                _lstCacheBuffer.Clear();
                msg.IsResponse = true;
                msg.CMD = temps;
            }

            return msg;
        }



    }
}
