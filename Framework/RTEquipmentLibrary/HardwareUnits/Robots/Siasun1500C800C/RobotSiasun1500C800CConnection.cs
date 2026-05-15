using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Siasun1500C800C
{
    public class RobotSiasun1500C800CMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class RobotSiasun1500C800CConnection : SerialPortConnectionBase
    {
        private bool _isAckMode = false;//should read from config file

        private string _cachedBuffer = string.Empty;

        public RobotSiasun1500C800CConnection(string portName, int baudRate = 19200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            RobotSiasun1500C800CMessage msg = new RobotSiasun1500C800CMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Substring(0, rawText.Length - 1);
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
            }
            else if (rawText == "_RDY")
            {
                msg.Data = rawText;

                if (_isAckMode)
                {
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = false;
                }
                else
                {
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = true;
                }
            }
            else if(rawText == "BG_RDY")
            {
                if (!_isAckMode)
                {
                    LOG.Error($"response format check failed, BG_RDY is not valid in Mode 1");
                    msg.IsFormatError = true;
                    return msg;
                }
                msg.Data = rawText;
                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }

            else
            {
                msg.Data = rawText;
                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }

            return msg;
        }

    }

    public class RobotSiasun1500C800CTCPConnection : TCPPortConnectionBase
    {
        private bool _isAckMode = false;//should read from config file

        public RobotSiasun1500C800CTCPConnection(string address)
            : base(address, "\r", true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            RobotSiasun1500C800CMessage msg = new RobotSiasun1500C800CMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Substring(0, rawText.Length - 1);
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
                msg.IsComplete = true;
            }
            else if (rawText == "_RDY")
            {
                msg.Data = rawText;

                if (_isAckMode)
                {
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = false;
                }
                else
                {
                    msg.IsResponse = true;
                    msg.IsAck = true;
                    msg.IsComplete = true;
                }
            }
            else if (rawText == "BG_RDY")
            {
                if (!_isAckMode)
                {
                    LOG.Error($"response format check failed, BG_RDY is not valid in Mode 1");
                    msg.IsFormatError = true;
                    return msg;
                }
                msg.Data = rawText;
                msg.IsResponse = true;
                msg.IsAck = true;
                msg.IsComplete = true;
            }

            else
            {
                msg.Data = rawText;
                msg.IsResponse = true;
            }

            return msg;
        }
    }
}
