using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Rorze
{
    public class RorzeRobot751Message:AsciiMessage
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
    public class RorzeRobot751Connection: TCPPortConnectionBase
    {
        private RorzeRobot751 _robot;
        public RorzeRobot751Connection(RorzeRobot751 robot, string ipaddress)
            : base(ipaddress, "\r", true)
        {
            _robot = robot;
        }
        protected override MessageBase ParseResponse(string rawMessage)
        {


            RorzeRobot751Message msg = new RorzeRobot751Message();
            try
            {                
                msg.RawMessage = rawMessage.Replace("\r","");

                
                msg.IsAck = false;
                msg.IsComplete = false;
                msg.IsFormatError = false;
                msg.IsEvent = false;

                switch(rawMessage[0])
                {
                    case 'a':
                        msg.IsAck = true;
                        msg.IsComplete = false;
                        msg.Data = rawMessage.Split(';').LastOrDefault().Split('/');
                        break;
                    case 'n':
                        msg.IsNak = true;
                        msg.IsComplete = false;
                        break;
                    case 'c':
                        msg.IsError = true;
                        msg.IsComplete = false;
                        break;
                    case 'e':
                        msg.IsEvent = true;
                        msg.IsComplete = false;
                        _robot.OnEventReceived(rawMessage);

                        break;
                } 
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
            RorzeRobot751Message evt = msg as RorzeRobot751Message;

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


    }

    
}
