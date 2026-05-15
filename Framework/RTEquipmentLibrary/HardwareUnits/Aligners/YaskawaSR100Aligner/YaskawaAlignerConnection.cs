using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.YaskawaAligner
{

    public class YaskawaRobotMessage : AsciiMessage
    {
        public int UNo { get; set; }
        public int SeqNo { get; set; }
        public string Status { get; set; }
        public string Ackcd { get; set; }
        public string Command { get; set; }
        public string[] Data { get; set; }
        public string ErrorCode { get; set; }
        public string EvNo { get; set; }
        public string EvDate { get; set; }
        public string EvData { get; set; }

    }
    public class YaskawaAlignerConnection : TCPPortConnectionBase
    {
        private YaskawaAligner _aligner;
        public YaskawaAlignerConnection(YaskawaAligner pa, string ipaddress)
            : base(ipaddress, "\r", true)
        {
            _aligner = pa;
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {
            YaskawaRobotMessage msg = new YaskawaRobotMessage();
            try
            {
                msg.RawMessage = rawMessage.Replace("\r", "");
                string[] msgdata = msg.RawMessage.Split(',');
                msg.UNo = Convert.ToInt16(msgdata[1]);
                msg.IsAck = false;
                msg.IsComplete = false;
                msg.IsFormatError = false;
                msg.IsEvent = false;

                if (msgdata[0] == "$") msg.IsAck = true;
                if (msgdata[0] == "!")
                {
                    msg.IsComplete = true;
                    SendAck();
                }
                if (msgdata[0] == "?")
                {
                    msg.IsFormatError = true;
                    msg.ErrorCode = msgdata[1];
                    return msg;
                }
                if (msgdata[0] == ">" && msgdata[2] == "EVNT")
                {
                    msg.IsEvent = true;
                    msg.EvNo = msgdata[3];
                    msg.EvDate = msgdata[4];
                    msg.EvData = msgdata[5];
                    return msg;
                }

                if (_aligner.IsEnableSeqNo)
                {
                    msg.SeqNo = Convert.ToInt16(msgdata[2]);
                    msg.Status = msgdata[3];
                    msg.Ackcd = msgdata[4];
                    msg.Command = msgdata[5];
                    msg.Data = msgdata.Skip(6).Take(msgdata.Length - 6 - (_aligner.IsEnableCheckSum ? 1 : 0)).ToArray();
                }
                else
                {
                    msg.Status = msgdata[2];
                    msg.Ackcd = msgdata[3];
                    msg.Command = msgdata[4];
                    msg.Data = msgdata.Skip(6).Take(msgdata.Length - 5 - (_aligner.IsEnableCheckSum ? 1 : 0)).ToArray();
                }
                _aligner.ParseStatus(msg.Status);

                return msg;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                msg.IsFormatError = true;
                return msg;
            }
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            YaskawaRobotMessage evt = msg as YaskawaRobotMessage;

            if (evt.EvNo == "100")
            {
                string errocode = evt.EvData.Substring(1, evt.EvData.Length - 1);
                _aligner.NotifyAlarmByErrorCode(errocode.ToUpper());
                if (Convert.ToInt32(errocode,16) > 0xF0)
                    _aligner.OnError($"Robot occurred error:{evt.EvData} on {evt.EvDate}.");
                else
                    EV.PostWarningLog("Robot", $"Robot occurred error:{evt.EvData} on {evt.EvDate}.");
                
            }
            if (evt.EvNo == "140")
            {

            }
        }

        public void SendAck()
        {
            if (_aligner.IsEnableSeqNo)
            {
                _aligner.CurrentSeqNo = _aligner.SeqnoGenerator.create();
                _aligner.SeqnoGenerator.release(_aligner.CurrentSeqNo);
            }
            string commandstr = $",{_aligner.UnitNumber}" + (_aligner.IsEnableSeqNo ? $",{_aligner.CurrentSeqNo:D2}" : "") + ",ACKN";
            if (_aligner.IsEnableCheckSum)
            {
                commandstr += ",";
                commandstr = commandstr + Checksum(Encoding.ASCII.GetBytes(commandstr)) + "\r";
            }
            commandstr = "$" + commandstr;
            SendMessage(commandstr);
        }

        private string Checksum(byte[] bytes)
        {
            int sum = 0;
            foreach (byte code in bytes)
            {
                sum += code;
            }
            string hex = String.Format("{0:X2}", sum % 256);
            return hex;
        }

    }
}
