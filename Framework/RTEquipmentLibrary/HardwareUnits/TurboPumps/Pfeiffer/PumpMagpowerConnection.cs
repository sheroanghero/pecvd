using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.Pfeiffer
{

    public class MagpowerMessage : AsciiMessage
    {
        public string DeviceAddress { get; set; }
        //public string Action { get; set; }
        //public string Parameter { get; set; }
        //public int DataLength { get; set; }
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    public class PumpMagpowerConnection : SerialPortConnectionBase
    {
        public PumpMagpowerConnection(string portName) : base(portName)
        {

        }
        //#adr,ok
        //#adr,nnnnn,sssss,00000,0,ccccc,eeeee,ddddd,pppp,qqqq,jj,kk,lll,mmm
        //#adr,nnnnn rpm
        protected override MessageBase ParseResponse(string rawText)
        {
            MagpowerMessage msg = new MagpowerMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 4)
            {
                LOG.Error($"response length check failed, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }

            msg.MessagePart = new string[2];
            int seperatorIndex = rawText.IndexOf(',');
            if(seperatorIndex != 4)
            {
                LOG.Error($"response format check failed, " + rawText);
                msg.IsFormatError = true;
                return msg;
            }
            msg.MessagePart[0] = rawText.Substring(1, 3); //device address
            msg.MessagePart[1] = rawText.Substring(5, rawText.Length - 6); //data
            msg.DeviceAddress = msg.MessagePart[0];
            msg.Data = msg.MessagePart[1];

            if (msg.Data.Contains("Err"))
            {
                msg.IsError = true;
            }

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }

    }

}
