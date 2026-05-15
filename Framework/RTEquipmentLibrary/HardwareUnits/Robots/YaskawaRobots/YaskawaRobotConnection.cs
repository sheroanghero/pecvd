using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.YaskawaRobots
{
    public class YaskawaRobotMessage:AsciiMessage
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
    public class YaskawaRobotConnection: TCPPortConnectionBase
    {
        private YaskawaSR100Robot _robot;
        public YaskawaRobotConnection(YaskawaSR100Robot robot, string ipaddress)
            : base(ipaddress, "\r", true)
        {
            _robot = robot;
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {
            YaskawaRobotMessage msg = new YaskawaRobotMessage();
            try
            {                
                msg.RawMessage = rawMessage.Replace("\r","");
                string[] msgdata = rawMessage.Replace("\r", "").Split(',');
                msg.UNo = Convert.ToInt16(msgdata[1]);
                msg.IsAck = false;
                msg.IsComplete = false;
                msg.IsFormatError = false;
                msg.IsEvent = false;

                if (msgdata[0] == "$")
                {
                    msg.IsAck = true;
                    msg.IsComplete = false;

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

                if (_robot.IsEnableSeqNo)
                {
                    msg.SeqNo = Convert.ToInt16(msgdata[2]);
                    msg.Status = msgdata[3];
                    if(msg.IsAck)
                        msg.Ackcd = msgdata[4];
                    msg.Command = msgdata[5];
                    msg.Data = msgdata.Skip(6).Take(msgdata.Length -6 - (_robot.IsEnableCheckSum?1:0)).ToArray();
                }
                else
                {
                    msg.Status = msgdata[2];
                    if (msg.IsAck)
                        msg.Ackcd = msgdata[3];
                    msg.Command = msgdata[4];
                    msg.Data = msgdata.Skip(5).Take(msgdata.Length - 5 - (_robot.IsEnableCheckSum ? 1 : 0)).ToArray();
                }

                if (msgdata[0] == "!")
                {
                    SendAck();
                    msg.IsComplete = true;
                    if (_robot.IsEnableSeqNo)
                    {
                        msg.ErrorCode = msgdata[4];
                    }
                    else
                        msg.ErrorCode = msgdata[3];
                    if (msg.ErrorCode != "0000")
                    {
                        _robot.OnError($"Execution error,Error code is {msg.ErrorCode}");
                    }
                }
                _robot.ParseStatus(msg.Status);
                
                return msg;
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                msg.IsFormatError = true;
                return msg;
            }
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            YaskawaRobotMessage evt = msg as YaskawaRobotMessage;

            if(evt.EvNo == "100")
            {
                string errocode = evt.EvData.Substring(1, evt.EvData.Length-1);

                _robot.NotifyAlarmByErrorCode(errocode.ToUpper());
                if(Convert.ToInt32(errocode,16) > 0xF0)
                    _robot.OnError($"Robot occurred error:{evt.EvData} on {evt.EvDate}.");
                else
                    EV.PostWarningLog("Robot", $"Robot occurred error:{evt.EvData} on {evt.EvDate}.");
            }
            if(evt.EvNo == "140")
            {

            }
        }

        
        public void SendAck()
        {
            if (_robot.IsEnableSeqNo)
            {                
                _robot.CurrentSeqNo = _robot.SeqnoGenerator.create();
                _robot.SeqnoGenerator.release(_robot.CurrentSeqNo);
            }
            string commandstr = $",{_robot.UnitNumber}" + (_robot.IsEnableSeqNo? $",{_robot.CurrentSeqNo:D2}":"") + ",ACKN";
            if (_robot.IsEnableCheckSum)
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
