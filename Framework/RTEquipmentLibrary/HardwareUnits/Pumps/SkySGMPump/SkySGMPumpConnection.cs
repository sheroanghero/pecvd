using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkySGMPump
{
    public class SkySGMPumpMessage : AsciiMessage
    {
        public string Data { get; set; }

        public string Address { get; set; }

        public string ErrorText { get; set; }
    }
    public class SkySGMPumpConnection: TCPPortConnectionBase
    {
        private bool _isAckMode = false;//should read from config file
        public SkySGMPumpConnection(string adress, string endof) : base(adress, endof, true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override void ActiveHandlerProceedMessage(MessageBase msg)
        {
            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || (_activeHandler.HandleMessage(msg, out bool transactionComplete) && transactionComplete))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }


        protected override MessageBase ParseResponse(string rawText)
        {
            SkySGMPumpMessage msg = new SkySGMPumpMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }
            if (rawText.Length <= 4)
            {
                LOG.Error($"too short response,");
                msg.IsFormatError = true;
                return msg;
            }
            rawText = rawText.Substring(3, rawText.Length - 3);//remove start char and end char
            //msg.Address = rawText.Substring(0, 2);
            msg.Data = rawText.Substring(0,rawText.Length-2);
            msg.IsResponse = true;
            msg.IsAck = true;

            return msg;
        }




    }
}
