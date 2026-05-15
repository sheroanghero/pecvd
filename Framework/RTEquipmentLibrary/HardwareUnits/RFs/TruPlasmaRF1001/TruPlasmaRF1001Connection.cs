using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{
    public class TruPlasmaRF1001Message : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class TruPlasmaRF1001Connection : SerialPortConnectionBase
    {
        private static byte _start = 0xAA;
        //private static byte _address = 0x02;
        //private static byte _GS = 0x00;
        private static byte _stop = 0x55;
        private static byte _ack = 0x06;
        private List<byte> _msgBuffer = new List<byte>();

        public TruPlasmaRF1001Connection(string portName,
                                         int baudRate = 57600,
                                         int dataBits = 8,
                                         Parity parity = Parity.None,
                                         StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "")
        {
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            if (rawMessage == null || rawMessage.Length == 0)
            {
                LOG.Error("报文长度为空");
                return null;
            }

            TruPlasmaRF1001Message msg = new TruPlasmaRF1001Message();
            if (rawMessage.Length == 1 && rawMessage[0] == _ack)
            {
                msg.Data = rawMessage;
                msg.IsResponse = true;
                msg.IsAck = true;
                return msg;
            }

            _msgBuffer.AddRange(rawMessage);

            if (rawMessage[rawMessage.Length - 1] != _stop)
                return msg;

            if (_msgBuffer[0] == _ack)
                _msgBuffer.RemoveAt(0);

            msg.RawMessage = _msgBuffer.ToArray();
            _msgBuffer.Clear();

            if (msg.RawMessage.Length < 11)
            {
                msg.IsFormatError = true;
                LOG.Error($"报文中数据信息长度错误");
                return msg;
            }

            //if ((msg.RawMessage[3] & 0b_1000_0000) > 0 || (msg.RawMessage[3] & 0b_0100_0000) > 0)
            //{
            //    msg.IsError = true;
            //    msg.ErrorCode = $"Alarms or error messages are pending";
            //    LOG.Error($"Alarms or error messages are pending");
            //    return msg;
            //}

            if ((msg.RawMessage[3] & 0x08) == 0)
            {
                msg.IsFormatError = true;
                LOG.Error($"RS232 interface is not active");
                return msg;
            }

            if (msg.RawMessage[4] == 0xFE)
            {
                msg.IsFormatError = true;
                LOG.Error($"telegram error");
                return msg;
            }

            if (msg.RawMessage[0] != _start)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid start byte");
                return msg;
            }
            if (msg.RawMessage.Last() != _stop)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid stop byte");
                return msg;
            }

            var contentBuffer = msg.RawMessage.Take(msg.RawMessage.Length - 3).ToArray();

            var checkSum = Crc16.Crc16Ccitt(contentBuffer);
            var recSum = msg.RawMessage[msg.RawMessage.Length - 3] + msg.RawMessage[msg.RawMessage.Length - 2] * 256;
            if (recSum != checkSum)
            {
                LOG.Error($"check sum failed,");
                msg.IsFormatError = true;
                return msg;
            }

            if (msg.RawMessage[8] != 0xFF && msg.RawMessage[8] != 0x00)
            {
                msg.IsError = true;
                msg.ErrorCode = msg.RawMessage[8].ToString("X2");
            }

            var msgArray = msg.RawMessage.Select(bt => bt.ToString("X2")).ToArray();
            string msgCommand = string.Join(",", msgArray, 4, 4);
            msg.Command = msgCommand;

            if (msg.RawMessage.Length > 12 && msg.RawMessage[8] == 0x00)
                msg.Data = msg.RawMessage.Skip(10).Take(msg.RawMessage.Length - 13).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}