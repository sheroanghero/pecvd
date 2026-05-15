using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners
{
    public class TazmoAlignerMessage:AsciiMessage
    {
        public string Data { get; set; }
    }
    public class TazmoAlignerMessageBIN:BinaryMessage
    {
        public byte[] CMD { get; set; }
        public byte[] Data { get; set; }
    }

    public class TazmoAlignerConnection: SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();

        public TazmoAlignerConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            LOG.Write($"{Address} send message:{Encoding.ASCII.GetString(message)}");
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
            
        }

        protected override MessageBase ParseResponse(byte[] rawBuffer)
        {

            _lstCacheBuffer.AddRange(rawBuffer);
            byte[] temps = _lstCacheBuffer.ToArray();
            TazmoAlignerMessageBIN msg = new TazmoAlignerMessageBIN();

            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps[0] ==0x6)
            {
                msg.IsAck = true;
                _lstCacheBuffer.RemoveRange(0,1);
            }
            if (temps.Length == 1 && temps[0] == 0x15 )
            {
                msg.IsNak = true;
                _lstCacheBuffer.Clear();
            }
            if (temps.Length == 1 && temps[0] == 0x11)
            {
                msg.IsBusy = true;
                _lstCacheBuffer.Clear();
            }
            if (temps.Length >= 3 && (Encoding.Default.GetString(temps.Take(3).ToArray()) == "ERR") && temps.LastOrDefault() == 0xD)
            {
                int index1 = Array.IndexOf(temps, (byte)44);
                int index2 = Array.IndexOf(temps, (byte)0xD);
                msg.Data = temps.Skip(index1 + 1).Take(index2 - index1 - 1).ToArray();
                msg.IsError = true;
                _lstCacheBuffer.Clear();
            }
            if(temps.Length >3 && temps.LastOrDefault() == 0xD && Encoding.Default.GetString(temps.Take(3).ToArray()) != "ERR")
            {
                _lstCacheBuffer.Clear();
                msg.IsResponse  = true;
                if(Array.IndexOf(temps,(byte)44)!=-1)    // to ,
                {
                    msg.CMD = temps.Take(Array.IndexOf(temps, (byte)44)).ToArray();
                    int index1 = Array.IndexOf(temps, (byte)44);
                    int index2 = Array.IndexOf(temps, (byte)0xD);
                    msg.Data = temps.Skip(index1 + 1).Take(index2 - index1 - 1).ToArray();
                }
                else
                {
                    msg.CMD = temps.Take(temps.Length - 1).ToArray();
                }
            }
            LOG.Write($"{Address} received message:{Encoding.ASCII.GetString(temps)}");
            return msg;
        }



    }
}
