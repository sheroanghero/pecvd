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
    public class YaskawaNXC100Message : AsciiMessage
    {
        public int UNo { get; set; }
        public int SeqNo { get; set; }
        public string Status { get; set; }
        public string Ackcd { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
        public string ErrorCode { get; set; }
        public string EvNo { get; set; }
        public string EvDate { get; set; }
        public string EvData { get; set; }

    }
    public class YaskawaNXC100Connection: TCPPortConnectionBase
    {
        private YaskawaNXC100Robot _robot;
        public YaskawaNXC100Connection(YaskawaNXC100Robot robot, string ipaddress)
            : base(ipaddress, "\r", true)
        {
            _robot = robot;
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {
            YaskawaNXC100Message msg = new YaskawaNXC100Message();
            try
            {                
                msg.RawMessage = rawMessage.Replace("\r","");
                string msgdata = rawMessage.Replace("\r", "");
                string startTag = msgdata.Substring(0, 1);
                msg.UNo = Convert.ToInt16(msgdata.Substring(1,1));
                msg.IsAck = false;
                msg.IsComplete = false;
                msg.IsFormatError = false;
                msg.IsEvent = false;

                if (startTag == "$")
                {
                    msg.IsAck = true;
                    msg.IsComplete = false;                   

                }
                
                if (startTag == "?")
                {
                    msg.IsFormatError = true;                    
                }
                if (startTag == ">" && msgdata.Substring(4,4) == "EVNT")
                {
                    msg.IsEvent = true;
                    msg.EvNo = msgdata.Substring(8, 3);
                    msg.EvData = msgdata.Substring(11, msgdata.Length -13);
                    msg.EvDate = DateTime.Now.ToString();
                    return msg;
                }
                if (startTag == "!")
                {
                    SendAck();
                    msg.IsComplete = true;    
                }
                if (_robot.IsEnableSeqNo)
                {
                    msg.SeqNo = Convert.ToInt16(msgdata.Substring(2, 2));
                    msg.Status = msgdata.Substring(4, 2);
                    msg.Data = msgdata.Substring(4, msgdata.Length -6);
                    if (msg.IsAck)
                    {
                        msg.Ackcd = msgdata.Substring(6, 4);
                        if (msg.Ackcd != "0000")
                        {
                            _robot.OnError($"Execution error,Ack code is {msg.ErrorCode}");
                        }
                    }
                    if(msg.IsComplete)
                    {
                        msg.ErrorCode = msgdata.Substring(6, 4);
                        if (msg.ErrorCode != "0000")
                        {
                            _robot.OnError($"Execution error,Error code is {msg.ErrorCode}");
                        }
                    }
                    


                    //msg.Command = msgdata[5];
                    //msg.Data = msgdata.Skip(6).Take(msgdata.Length -6 - (_robot.IsEnableCheckSum?1:0)).ToArray();
                }
                else
                {
                    //msg.SeqNo = Convert.ToInt16(msgdata.Substring(2, 2));
                    msg.Status = msgdata.Substring(2, 2);
                    msg.Data = msgdata.Substring(2, msgdata.Length - 4);
                    if (msg.IsAck)
                    {
                        msg.Ackcd = msgdata.Substring(4, 4);
                        if (msg.Ackcd != "0000")
                        {
                            _robot.OnError($"Execution error,Ack code is {msg.ErrorCode}");
                        }
                    }
                    if (msg.IsComplete)
                    {
                        msg.ErrorCode = msgdata.Substring(4, 4);
                        if (msg.ErrorCode != "0000")
                        {
                            _robot.OnError($"Execution error,Error code is {msg.ErrorCode}");
                        }
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

                _robot.NotifyAlarmByErrorCode(errocode);
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
            string commandstr = $"{_robot.UnitNumber}" + (_robot.IsEnableSeqNo? $"{_robot.CurrentSeqNo:D2}":"") + "ACKN";
            if (_robot.IsEnableCheckSum)
            {
                commandstr += "";
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
