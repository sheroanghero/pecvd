using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public class JelRobotMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class JelRobotConnection : SerialPortConnectionBase
    {
        //private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;
        private List<byte> _lstCacheBuffer = new List<byte>();

        public JelRobotConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        public override bool SendMessage(string message)
        {
            _cachedBuffer = string.Empty;
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawmsg)
        {
            _cachedBuffer += rawmsg;
            JelRobotMessage msg = new JelRobotMessage();

            if (_cachedBuffer == ">")
            {
                msg.IsAck = true;
                msg.Data = _cachedBuffer.Replace(">", "").Replace("\r", "");
                _cachedBuffer = string.Empty;
            }
            else if (_cachedBuffer.Contains("\r"))
            {
                msg.IsAck = true;
                msg.IsResponse = true;
                msg.Data = _cachedBuffer.Replace(">", "").Replace("\r", "");
                _cachedBuffer = string.Empty;
            }
            //else if(_cachedBuffer.Contains("\r"))
            //{
            //    msg.IsAck = true;
            //    msg.Data = rawmsg.Replace(">", "").Replace("\r", "");
            //    _cachedBuffer = string.Empty;
            //}
            return msg;
        }
       
    }
}
