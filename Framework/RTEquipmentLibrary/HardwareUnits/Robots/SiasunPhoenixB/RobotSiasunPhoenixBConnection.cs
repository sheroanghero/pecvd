using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.SiasunPhoenixB
{
    public class RobotSiasunPhoenixBMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class RobotSiasunPhoenixBTCPConnection : TCPPortConnectionBase
    {
        private bool _isAckMode = false;//should read from config file

        public RobotSiasunPhoenixBTCPConnection(string address, string endof)
            : base(address, endof, true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }

        protected override void ActiveHandlerProceedMessage(MessageBase msg)
        {
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || !((RobotSiasunPhoenixBHandler)_activeHandler).HasResponse || (_activeHandler.HandleMessage(msg, out bool transactionComplete) && transactionComplete))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            RobotSiasunPhoenixBMessage msg = new RobotSiasunPhoenixBMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Replace("\r", "").Replace("\n", "");
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
                msg.IsComplete = true;
            }
            else if (rawText.Contains("_BG_RDY"))
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
            else if (rawText.Contains("_RDY"))
            {
                msg.Data = rawText.Replace("_RDY", "");

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
