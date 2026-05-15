using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public class JelC5000RobotFlippeMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class JelC5000RobotFlippeConnection : SerialPortConnectionBase
    {
        //private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;
        private List<byte> _lstCacheBuffer = new List<byte>();
        private RobotBaseDevice m_Robot;
        
        public JelC5000RobotFlippeConnection(RobotBaseDevice robot, string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits,"", true)
        {
            m_Robot = robot;
        }

        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        public override bool SendMessage(string message)
        {
            _cachedBuffer = string.Empty;
            while (m_Robot.CommandMessages.Count > 50)
                m_Robot.CommandMessages.RemoveLast();
            m_Robot.CommandMessages.AddFirst($"Send:{message.Replace("\r","")}");
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawmsg)
        {
            _cachedBuffer += rawmsg;
            JelC5000RobotFlippeMessage msg = new JelC5000RobotFlippeMessage();


            if (_activeHandler == null)
            {
                _cachedBuffer = string.Empty;
                return msg;
            }

            if (_cachedBuffer == ">" && (_activeHandler.GetType() != typeof(JelC5000RobotFlippeReadHandler)))
            {
                msg.IsAck = true;
                msg.Data = _cachedBuffer.Replace(">", "").Replace("\r", "");
                

                while (m_Robot.CommandMessages.Count > 50)
                    m_Robot.CommandMessages.RemoveLast();
                m_Robot.CommandMessages.AddFirst($"Received:{_cachedBuffer.Replace("\r", "")}");

                _cachedBuffer = string.Empty;
            }
            else if(_cachedBuffer.Contains("\r"))
            {
                msg.IsAck = true;
                msg.IsResponse = true;
                msg.Data = _cachedBuffer.Replace(">", "").Replace("\r", "");

                while (m_Robot.CommandMessages.Count > 50)
                    m_Robot.CommandMessages.RemoveLast();
                m_Robot.CommandMessages.AddFirst($"Received:{_cachedBuffer.Replace("\r", "")}");
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
