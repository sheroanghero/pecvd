using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AE
{
    public class AeRfPowerMessage : BinaryMessage
    {
        public byte Header { get; set; }
        public byte CommandNumber { get; set; }
        public byte OptionLength { get; set; }
        public byte[] Data { get; set; }
        public byte CheckSum { get; set; }

        public int  Address { get; set; }
        public int DataLength { get; set; }

        public int MessageLength { get; set; }
    }

    public class AeRfPowerConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public AeRfPowerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
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

            AeRfPowerMessage msg = new AeRfPowerMessage();
            msg.RawMessage = rawBuffer;

            if (_lstCacheBuffer.Count >= 1 && _lstCacheBuffer[0] == 0x06)
            {
                msg.IsAck = true;
                _lstCacheBuffer.RemoveAt(0);
            }

            if (_lstCacheBuffer.Count < 4)
            {
                return msg;
            }

            msg.Header = _lstCacheBuffer[0];
            msg.CommandNumber = _lstCacheBuffer[1];
            msg.OptionLength = _lstCacheBuffer[2];
            msg.Address = _lstCacheBuffer[0] >> 3;
            msg.DataLength = (byte)(0x07 & _lstCacheBuffer[0]);
            msg.MessageLength = 3 + msg.DataLength ;//3=包头+Cmd+校验
            if (msg.DataLength >= 7)
            {
                msg.DataLength = _lstCacheBuffer[2];
                msg.MessageLength = 4 +  msg.DataLength ;//4=包头+Cmd+length+校验
            }

            if (_lstCacheBuffer.Count < msg.MessageLength)
                return msg;

            msg.Data = _lstCacheBuffer.GetRange(msg.DataLength >= 7 ? 3 : 2, msg.DataLength).ToArray();

            byte sum = 0x00;
            for (var i = 0; i < msg.MessageLength - 1; i++)
            {
                sum ^= _lstCacheBuffer[i];
            }
 
            if (_lstCacheBuffer[msg.MessageLength - 1] != sum)
            {
                LOG.Error($"check sum failed, ");
                msg.IsFormatError = true;
                _lstCacheBuffer.Clear();
                return msg;
            }

            msg.IsResponse = true;
 
            _lstCacheBuffer.RemoveRange(0, msg.MessageLength);

            return msg;
        }

    }
}
