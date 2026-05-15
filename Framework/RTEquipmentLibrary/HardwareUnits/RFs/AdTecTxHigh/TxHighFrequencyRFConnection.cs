using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxHigh
{
    public class HighFrequencyRFMessage : BinaryMessage
    {
        public byte Header { get; set; }
        public byte Command { get; set; }
        public byte FunctionCode { get; set; }
        public byte ExecutionCode { get; set; }
        public byte[] Parameter { get; set; }


        public byte CommandNumber { get; set; }

        public byte[] Data { get; set; }
        public byte CheckSum { get; set; }

        public int Address { get; set; }
        public int DataLength { get; set; }

        public int MessageLength { get; set; }
    }

    public class HighFrequencyRFConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public HighFrequencyRFConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
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

            HighFrequencyRFMessage msg = new HighFrequencyRFMessage();
            msg.RawMessage = rawBuffer;

            //if (_lstCacheBuffer.Count >= 1 && _lstCacheBuffer[0] == 0x06)
            //{
            //    msg.IsAck = true;
            //    _lstCacheBuffer.RemoveAt(0);
            //}

            //if (_lstCacheBuffer.Count < 4)
            //{
            //    return msg;
            //}

            //if (!(_lstCacheBuffer.Count > 2 && _lstCacheBuffer[0] == 0x40 && _lstCacheBuffer[1] == 0x55))
            //{
            //    return msg;
            //}

            if (_lstCacheBuffer[_lstCacheBuffer.Count - 1] != 0x0D)
            {
                return msg;
            }

            //msg.Header = _lstCacheBuffer[0];
            //msg.Command = _lstCacheBuffer[1];
            //msg.FunctionCode = _lstCacheBuffer[2];
            //msg.ExecutionCode = _lstCacheBuffer[3];

            msg.DataLength = _lstCacheBuffer.Count;
            msg.MessageLength = _lstCacheBuffer.Count;


            //msg.Parameter = _lstCacheBuffer.GetRange(4, _lstCacheBuffer.Count - 5).ToArray();



            msg.Data = _lstCacheBuffer.ToArray();

            msg.IsResponse = true;

            _lstCacheBuffer.RemoveRange(0, msg.MessageLength);

            return msg;
        }

    }
}
