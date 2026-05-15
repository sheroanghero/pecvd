using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Rorze
{
    public class RorzeRobot751ProtocolTag
    {
        public const string tag_order = "o";
        public const string response_ack = "a";
        public const string response_nak = "n";

        public const string response_cancel = "c";
        public const string response_event = "e";
    }
    public abstract class RorzeRobot751Handler : HandlerBase
    {
        public RorzeRobot751 Device { get; }

        protected string _commandType;
        protected string _command;
        protected string _parameter;
        private SCConfigItem _scMotionTimeout;
        public RorzeRobot751Handler(RorzeRobot751 device,string command,string parameter =null)
            :base(BuildMessage(command,parameter,device))
        {
            _scMotionTimeout = SC.GetConfigItem($"Robot.Robot.MotionTimeout");
            Device = device;
            _command = command;            
            _parameter = parameter;
            Name = command;
            AckTimeout = TimeSpan.FromSeconds(_scMotionTimeout.IntValue);
            CompleteTimeout = TimeSpan.FromSeconds(_scMotionTimeout.IntValue);//(120);

            
        }
        protected static string BuildMessage(string command, string parameter,RorzeRobot751 rbt)
        {          
                
            
            return $"oTRB{rbt.RobotBodyNumber}.{command}{parameter??""}\r"; 

        }     


        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RorzeRobot751Message response = msg as RorzeRobot751Message;
            ResponseMessage = msg;
            if (msg.IsError)
            {
                Device.NoteError(response.RawMessage);
                transactionComplete = true;
                return true;
            }
            if(msg.IsAck)
            {
                SetState(EnumHandlerState.Acked);                
            }

            if (msg.IsComplete)
            {
                SetState(EnumHandlerState.Completed);                
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }
    }

    public class RorzeRobot751ReadHandler : RorzeRobot751Handler
    {
        public RorzeRobot751ReadHandler(RorzeRobot751 robot, string command,string parameter=null)
            :base(robot,command,parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute read command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RorzeRobot751Message response = msg as RorzeRobot751Message;

            if (!response.RawMessage.Contains(_command))
            {
                transactionComplete = false;
                return false;
            }
            if(response.IsEvent)
            {
                transactionComplete = false;
                return false;
            }
            if (!response.IsAck)
            {
                Device.OnError($"GetErrorMsg:{response.RawMessage}");
                
                transactionComplete = true;
                return true;
            }
            Device.ParseReadData(_command, response.Data);
            //Device.OnActionDone(null);
            transactionComplete = true;
            return true;
           
        }

    }
    public class RorzeRobot751SetHandler : RorzeRobot751Handler
    {
        public RorzeRobot751SetHandler(RorzeRobot751 robot, string command, string parameter = null)
            : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute set command {command} {temp} in ASCII.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RorzeRobot751Message response = msg as RorzeRobot751Message;
            if (!response.RawMessage.Contains(_command))
            {
                transactionComplete = false;
                return false;
            }
            if (response.IsAck)
            {
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = true;
            return false;
        }
    }

    public class RorzeRobot751MotionHandler : RorzeRobot751Handler
    {
        public RorzeRobot751MotionHandler(RorzeRobot751 robot, string command, string parameter = null)
           : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute motion command {command} {temp} in ASCII.");
        }

        private string _motionCmd = "HOME,EXTD,LOAD, UNLD,TRNS, EXCH,CLMP, UCLM,WMAP, UTRN,MGET, MPUT,MGT1, " +
            "MGT2,MPT1, MPT2,WCHK, ALEX,ALLD, ALUL,ALGT, ALEA,ALMV,STEP,GCHK";
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RorzeRobot751Message response = msg as RorzeRobot751Message;
            ResponseMessage = msg;

            LOG.Write($"{Device.Name} handler message:{response.RawMessage} in ASCII.");
            if (msg.IsError)
            {
                Device.NoteError(response.RawMessage);
                transactionComplete = true;
                return true;
            }
            if (msg.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }
            if(msg.IsComplete)
            {
                //Device.SenACK();
            }

            if(response.IsEvent)
            {
                if(_command == "ORGN" && response.RawMessage.Contains("STAT")
                && Device.IsOrgshCompleted && Device.CurrentOpStatus == RROpStatusEnum.Stop)
                {
                    Device.OnActionDone(null);
                    transactionComplete = true;
                    return true;
                }

                if (_command == "INIT" && response.RawMessage.Contains("STAT")
                && Device.CurrentOpMode != RROpModeEnum.Initializing)
                {
                    Device.OnActionDone(null);
                    transactionComplete = true;
                    return true;
                }



                if (_motionCmd.Contains(_command))
                {
                    if(response.RawMessage.Contains("STAT") && !Device.IsOrgshCompleted)
                    {
                        Device.OnError($"GetErrorMsg:{ response.RawMessage} on command:{_command}");
                        transactionComplete = true;
                        return true;
                    }

                    if (response.RawMessage.Contains("STAT") && Device.CurrentOpStatus == RROpStatusEnum.Stop)
                    {
                        Device.OnActionDone(null);
                        transactionComplete = true;
                        return true;
                    }
                }
            }
            transactionComplete = false;
            return false;
        }
    }
   
}
