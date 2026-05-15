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
    public class HonghuAlignerMessage : AsciiMessage
    {
        public string Data { get; set; }
    }
    public class HonghuAlignerMessageBIN : BinaryMessage
    {
        public byte[] CMD { get; set; }
        public byte[] Data { get; set; }
    }

    public class HonghuAlignerConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public HonghuAlignerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
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
            HonghuAlignerMessageBIN msg = new HonghuAlignerMessageBIN();

            msg.RawMessage = _lstCacheBuffer.ToArray();

            if(temps.Length == 4 && temps.Last() == 0xA)
            {
                _lstCacheBuffer.Clear();
                msg.IsResponse = true;
                msg.CMD = temps.Take(3).ToArray();
            }

            return msg;
        }



    }
}
