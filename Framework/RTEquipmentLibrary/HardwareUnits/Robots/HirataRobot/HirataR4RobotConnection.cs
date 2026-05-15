using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotHirataR4
{
    public class HirataR4RobotMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class HirataR4RobotConnection : SerialPortConnectionBase
    {
        //private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;
        private List<byte> _lstCacheBuffer = new List<byte>();

        public HirataR4RobotConnection(string portName, int baudRate = 19200, int dataBits = 7, Parity parity = Parity.Even, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\0", false)
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
            HirataR4RobotMessage msg = new HirataR4RobotMessage();
            
            msg.IsFormatError = false;
            msg.IsComplete = false;
            msg.IsEvent = false;
            if (temps.Length <= 7) return msg;
            byte lrc = 0x00;
            for (int i=1;i<temps.Length -1;i++)
            {
                lrc ^= temps[i];
            }
            if (temps[temps.Length - 1] != lrc) return msg;

            string rawText = Encoding.ASCII.GetString(temps);
            
            msg.RawMessage = rawText;

            //remove '\0'
            rawText = rawText.Substring(0, rawText.Length - 1);

            string content = rawText.Substring(4, rawText.Length - 5).
                Replace(Encoding.ASCII.GetString(new byte[] { 0x3 }), ""); ;
            content = content.Trim();
            if (content.First() == 'E')
            {
                msg.IsError = true;
            }
            msg.Data = content;
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            _lstCacheBuffer.Clear();
            return msg;
        }
        private static byte CalcLRC(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }
    }
}
