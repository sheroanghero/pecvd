using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump2205
{
    public class EdwardsPump2205Message : BinaryMessage
    {
        public byte FunctionCode { get; set; }
        public int DataLength { get; set; }
        public string ErrorText { get; set; }
        public byte[] Datas { get; set; }

        public string AsciiMessage { get; set; }
    }

    public class EdwardsPump2205Connection : SerialPortConnectionBase
    {
        private static string _endLine = "\r\n";
        private List<byte> _lstCacheBuffer = new List<byte>();
        private const int STX = 0x02;
        private const int ETX = 0x03;
        private const int NAK = 0x15;
        private const int ACK = 0x06;
        private const int Etb = 0x17;
        private const int @HEX = 0x40;
        private const int NOWLEDGMENT = 0x23;
        private const int NONNOWLEDGMENT = 0x21;
        public EdwardsPump2205Connection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, _endLine, false)
        {

        }

        public override bool SendMessage(string message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
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

            EdwardsPump2205Message msg = new EdwardsPump2205Message();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();
            if (temps.Length > 0 && (int)temps[0] == ACK)
            {
                //LOG.Error($"ACK, " + temps);
                msg.IsAck = true;
                _lstCacheBuffer.RemoveAt(0);

                if (_lstCacheBuffer.Count > 0)
                {
                    temps = _lstCacheBuffer.ToArray();
                }
                else
                    return msg;
            }
            if (_lstCacheBuffer.Count < 4 || _lstCacheBuffer[_lstCacheBuffer.Count - 2] != ETX)
                return msg;

            if (temps.Length > 0 && (int)temps[0] == NAK)
            {
                LOG.Error($"NAK, " + temps);
                msg.IsNak = true;
                return msg;
            }

            

            if (temps.Length < 4)
            {
                LOG.Error($"text length check failed");
                msg.IsFormatError = true;
                return msg;
            }

            if ((int)temps[0] != STX)
            {
                LOG.Error($"text check STX failed");
                msg.IsFormatError = true;
                return msg;
            }

            int etxIndex = 0;
            foreach (var item in temps)
            {
                etxIndex++;
                if ((int)item == ETX)
                    break;
            }

            if (etxIndex >= temps.Length)
            {
                LOG.Error($"text check ETX failed");
                msg.IsFormatError = true;
                return msg;
            }

            msg.DataLength = etxIndex ;
            msg.Datas = new byte[etxIndex];

            ///STX msg ETX
            Array.Copy(temps, 0, msg.Datas, 0, etxIndex );

            var lrc = temps[etxIndex];

            var checkLrc = CheckLrc(msg.Datas);

            if (checkLrc != lrc)
            {
                LOG.Error($"check LRC failed");
                msg.IsFormatError = true;
                return msg;
            }
            _lstCacheBuffer.RemoveRange(0, etxIndex + 1);
            msg.FunctionCode = temps[4];
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            msg.AsciiMessage = System.Text.ASCIIEncoding.ASCII.GetString(temps.Take(etxIndex+1).ToArray());
            return msg;
        }

        private static byte CheckLrc(byte[] cmdBytes)
        {
            byte rt = 0;
            for (int i = 0; i < cmdBytes.Length; i++)
            {
                rt ^= cmdBytes[i];
            }
            rt ^= 0xFF;
            return rt;
        }

    }
}
