using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RemoteControl
{

    public class RemoteControlMessage : AsciiMessage
    {
        public uint message_id { get; set; }
        public uint command_id { get; set; }
        public string sender_id { get; set; }
        public string receiver_id { get; set; }
        public uint body_item_qty { get; set; }

        public int UNo { get; set; }
        public int SeqNo { get; set; }
        public string Status { get; set; }
        public string Ackcd { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }

    }
    public class RemoteControlConnection : TCPPortConnectionBase
    {
        private RemoteControl _remoteControl;
        public RemoteControlConnection(RemoteControl pa, string ipaddress)
            : base(ipaddress, "\r\n", true)
        {
            _remoteControl = pa;
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {
            RemoteControlMessage msg = new RemoteControlMessage();
            try
            {
                msg.RawMessage = rawMessage.Replace("\0\r\n", "");
                string msgdata = rawMessage.Replace("\0\r\n", "");
                //string[] msgArray = msgdata.Split('\0');
                //string startTag = msgdata.Substring(0, 1);
                //msg.UNo = Convert.ToInt16(msgdata.Substring(1, 1));
                msg.IsAck = false;
                msg.IsComplete = false;
                msg.IsFormatError = false;
                msg.IsEvent = false;

                msg.MessagePart = rawMessage.Split('\0');
                if (msg.MessagePart.Length <= 0)
                {   // nok, no items splitted
                    return msg;
                }

                msg.message_id = UInt32.Parse(msg.MessagePart[0]);
                msg.command_id = UInt32.Parse(msg.MessagePart[1]);
                msg.sender_id = msg.MessagePart[2];
                msg.receiver_id = msg.MessagePart[3];

                if(msg.command_id == 5)
                {
                    msg.IsEvent = true;
                    msg.RawMessage = msg.MessagePart[5];
                    _remoteControl.ParseStatus(msg);
                    //return msg;
                }
                else
                {

                }
                //msg.body_item_qty = UInt32.Parse(msg.MessagePart[4]);

                //_remoteControl.ParseStatus(msg);
                return msg;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                msg.IsFormatError = true;
                return msg;
            }
        }
    }
}
