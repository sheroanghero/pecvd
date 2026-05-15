using System;
using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.DCPowers.AE
{
    public class DxkdpRfPowerMessage : BinaryMessage
    {
        public byte Header { get; set; }
        public byte CommandNumber { get; set; }
        public byte OptionLength { get; set; }
        public byte[] Data { get; set; }
        public byte CheckSum { get; set; }

        public int Address { get; set; }
        public int DataLength { get; set; }

        public int MessageLength { get; set; }
    }

    public class DxkdpDcPowerConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public DxkdpDcPowerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
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
            try
            {
                _lstCacheBuffer.AddRange(rawBuffer);

                DxkdpRfPowerMessage msg = new DxkdpRfPowerMessage();
                msg.RawMessage = rawBuffer;

                if (_lstCacheBuffer.Count >= 1 && _lstCacheBuffer[0] == 0x06)
                {
                    msg.IsAck = true;
                    msg.IsResponse = true;
                    _lstCacheBuffer.RemoveAt(0);
                    return msg;
                }
                else if (_lstCacheBuffer.Count > 1 && _lstCacheBuffer[0] == 0xAA)
                {
                    msg.IsAck = true;
                }
                else if (_lstCacheBuffer.Count == 0)
                {
                    return msg;
                }

                bool isGetHead = false;
                while (!isGetHead && _lstCacheBuffer.Count > 5)
                {
                    if (_lstCacheBuffer[0] != 0xAA)
                        _lstCacheBuffer.RemoveAt(0);
                    else
                        isGetHead = true;
                }
                if (!isGetHead || _lstCacheBuffer.Count < 5)
                {
                    return msg;
                }

                msg.Header = _lstCacheBuffer[0];
                msg.Address = _lstCacheBuffer[1];
                msg.CommandNumber = _lstCacheBuffer[2];
                msg.DataLength = _lstCacheBuffer[3];
                msg.MessageLength = 5 + msg.DataLength;

                if (_lstCacheBuffer.Count < msg.MessageLength)
                    return msg;

                msg.Data = _lstCacheBuffer.GetRange(4, msg.DataLength).ToArray();

                byte sum = 0x00;
                for (var i = 1; i < msg.MessageLength - 1; i++)
                {
                    sum += _lstCacheBuffer[i];
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
            catch (Exception ex)
            {

                throw;
            }
        }

    }
}
