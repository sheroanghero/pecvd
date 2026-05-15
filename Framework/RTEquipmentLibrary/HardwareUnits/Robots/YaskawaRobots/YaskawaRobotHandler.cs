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
    public class YaskawaProtocolTag
    {
        public const string tag_end = "\r";
        public const string tag_cmd_start = "$";
        public const string cmd_token = ",";

        public const string resp_tag_normal = "$";
        public const string resp_tag_error = "?";
        public const string resp_tag_excute = "!";
        public const string resp_tag_event = ">";

        public const string resp_evt_error = "100";

    }
    public abstract class YaskawaSR100RobotHandler : HandlerBase
    {
        public YaskawaSR100Robot Device { get; }

        public string _commandType;
        public string _command;
        public string _parameter;
        private int _seqNO;
        
        //protected YaskawaTokenGenerator _generator;
        public YaskawaSR100RobotHandler(YaskawaSR100Robot device,string command,string parameter =null)
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
            return $"{command},{parameter}"; 



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
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
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

            if (msg.IsComplete && response.RawMessage.Split(',')[0]=="!")
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


        protected virtual void NoteCommandResult(YaskawaRobotMessage response)
        {
        }


    }

    public class SR100RobotReadHandler: YaskawaSR100RobotHandler
    {
        public SR100RobotReadHandler(YaskawaSR100Robot robot, string command,string parameter=null)
            :base(robot,command,parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute read command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
            
            if(!response.RawMessage.Contains(_command))
            {
                transactionComplete = false;
                return false;
            }
            if(Device.ParseReadData(_command,response.Data))
            {
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            response.IsError = true;
            return base.HandleMessage(response, out transactionComplete);
        }

    }
    public class SR100RobotSetHandler:YaskawaSR100RobotHandler
    {
        public SR100RobotSetHandler(YaskawaSR100Robot robot, string command, string parameter = null)
            : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute set command {command} {temp} in ASCII.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
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
            return base.HandleMessage(response, out transactionComplete);
        }
    }

    public class SR100RobotMotionHandler: YaskawaSR100RobotHandler
    {
        public SR100RobotMotionHandler(YaskawaSR100Robot robot, string command, string parameter = null)
           : base(robot, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{robot.Name} execute motion command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
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

                Device.HandlerMotion(_command, response.Data);
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }
    }

    public class SR100RobotGripAndCheckWaferMotionHandler : YaskawaSR100RobotHandler
    {
        public SR100RobotGripAndCheckWaferMotionHandler(YaskawaSR100Robot robot)
           : base(robot, "CSOL", "F,1,0")
        {            
            LOG.Write($"{robot.Name} execute Grip and Check wafer command CSOL in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
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
                Thread.Sleep(3000);                
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }
    }
    public class SR100RobotCheckWaferHandler : YaskawaSR100RobotHandler
    {
        public SR100RobotCheckWaferHandler(YaskawaSR100Robot robot)
           : base(robot, "RSTS", null)
        {
            LOG.Write($"{robot.Name} execute Check wafer command RSTS in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
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
            
                SetState(EnumHandlerState.Completed);                
                Device.ParseReadData(_command, response.Data);
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
