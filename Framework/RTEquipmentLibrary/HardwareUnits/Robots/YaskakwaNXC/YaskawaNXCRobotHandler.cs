using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.YaskawaRobots
{
    //public class YaskawaProtocolTag
    //{
    //    public const string tag_end = "\r";
    //    public const string tag_cmd_start = "$";
    //    public const string cmd_token = ",";

    //    public const string resp_tag_normal = "$";
    //    public const string resp_tag_error = "?";
    //    public const string resp_tag_excute = "!";
    //    public const string resp_tag_event = ">";

    //    public const string resp_evt_error = "100";

    //}
    public abstract class YaskawaNXC100RobotHandler : HandlerBase
    {
        public YaskawaNXC100Robot Device { get; }

        public string _commandType;
        public string _command;
        public string _parameter;
        private int _seqNO;
        
        //protected YaskawaTokenGenerator _generator;
        public YaskawaNXC100RobotHandler(YaskawaNXC100Robot device,string command,string parameter =null)
            :base(BuildMessage(command,parameter))
        {
            Device = device;
            _command = command;            
            _parameter = parameter;
            Name = command;
            
        }
        protected static string BuildMessage(string command, string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return command;
            return $"{command}{parameter}"; 



        }
        private static string Checksum(byte[] bytes)
        {
            int sum = 0;
            foreach (byte code in bytes)
            {
                sum += code;
            }
            string hex = String.Format("{0:X2}", sum % 256);
            return hex;
        }


        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaNXC100Message response = msg as YaskawaNXC100Message;
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
                //Device.SenACK();
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }


        protected virtual void NoteCommandResult(YaskawaNXC100Message response)
        {
        }


    }

    public class NXC100RobotReadHandler: YaskawaNXC100RobotHandler
    {
        public NXC100RobotReadHandler(YaskawaNXC100Robot robot, string command,string parameter=null)
            :base(robot,command,parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute read command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaNXC100Message response = msg as YaskawaNXC100Message;            
            
            if(response.IsAck && Device.ParseReadData(_command,response.Data))
            {
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            response.IsError = true;
            return base.HandleMessage(response, out transactionComplete);
        }

    }
    public class NXC100RobotSetHandler:YaskawaNXC100RobotHandler
    {
        public NXC100RobotSetHandler(YaskawaNXC100Robot robot, string command, string parameter = null)
            : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute set command {command} {temp} in ASCII.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaNXC100Message response = msg as YaskawaNXC100Message;
            if (response.IsAck)
            {
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            return base.HandleMessage(response, out transactionComplete);
        }
    }

    public class NXC100RobotMotionHandler: YaskawaNXC100RobotHandler
    {
        public NXC100RobotMotionHandler(YaskawaNXC100Robot robot, string command, string parameter = null)
           : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute motion command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaNXC100Message response = msg as YaskawaNXC100Message;
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
            if (IsAcked && msg.IsComplete)
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

    public class NXC100RobotGripAndCheckWaferMotionHandler : YaskawaNXC100RobotHandler
    {
        public NXC100RobotGripAndCheckWaferMotionHandler(YaskawaNXC100Robot robot)
           : base(robot, "CSOL", "F,1,0")
        {
            Thread.Sleep(500);
            LOG.Write($"{robot.Name} execute Grip and Check wafer command CSOL in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaNXC100Message response = msg as YaskawaNXC100Message;
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
            if (msg.IsComplete)
            {
                //Device.SenACK();
            }
            if (IsAcked && msg.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                Device.CheckWaferPresentAndGrip();
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }
    }
}
