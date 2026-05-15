using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public abstract class JelRobotHandler:HandlerBase
    {
        public JelRobot Device { get; }

        protected string _command;
        protected string _parameter;
        protected JelRobotHandler(JelRobot device, string command, string parameter = null)
            : base(BuildMessage(device.BodyNumber, command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        protected JelRobotHandler(string command)
            : base(command)
        {            
            _command = command;            
        }
        private static string BuildMessage(int bodyno, string command, string parameter)
        {
            return $"${bodyno}{command}{parameter}\r";
            
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelRobotMessage response = msg as JelRobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class JelRobotReadHandler: JelRobotHandler
    {
        
        public JelRobotReadHandler(JelRobot device, string command, string parameter = null)
            : base(device, command, parameter)
        {
           
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute read command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelRobotMessage response = msg as JelRobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                Device.ParseData(_command,_parameter,response.Data);
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }

    public class JelRobotSetHandler : JelRobotHandler
    {

        public JelRobotSetHandler(JelRobot device, string command, string parameter = null)
            : base(device, command, parameter)
        {

            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute read command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelRobotMessage response = msg as JelRobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {                
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }
    public class JelRobotMoveHandler : JelRobotHandler
    {
        public JelRobotMoveHandler(JelRobot device, string command, string parameter = null)
            : base(device, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }
    }

    public class JelRobotRawCommandHandler : JelRobotHandler
    {
        public JelRobotRawCommandHandler(JelRobot device, string command)
            : base(command)
        {
            
            LOG.Write($"{device.Name} execute move command {command} in byte.");
        }
    }


    public class JelRobotCompaundCommandHandler : JelRobotHandler
    {
        public JelRobotCompaundCommandHandler(JelRobot device, string cmdNo, string parameter = null)
            : base(device, $"G{cmdNo}", parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute compaund command {cmdNo} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelRobotMessage response = msg as JelRobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                //if (_command == "G")
                //    Device.CurrentCompoundCommandStatus = JelCommandStatus.None;

                SetState(EnumHandlerState.Acked);
                //transactionComplete = true;
                //return true;
            }
            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                if (response.Data.Contains("END"))
                {
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("ERR"))
                {
                    Device.HandlerError(response.Data);
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("POK"))
                {
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("PNG"))
                {
                    Device.HandlerError(response.Data);
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("GOK"))
                {
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("GNG"))
                {
                    Device.HandlerError(response.Data);
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("OK"))
                {
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("NG"))
                {
                    Device.HandlerError(response.Data);
                    transactionComplete = true;
                    return true;
                }
            }

            transactionComplete = false;
            return false;
        }



    }

    public class JelRobotCheckWaferCommandHandler : JelRobotHandler
    {
        public JelRobotCheckWaferCommandHandler(JelRobot device, string cmdNo, RobotArmEnum arm, string parameter = null)
            : base(device, $"G{cmdNo}", parameter)
        {
            _arm = arm;
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute compaund command {cmdNo} {temp} in byte.");
        }
        private RobotArmEnum _arm;

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelRobotMessage response = msg as JelRobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                //if (_command == "G")
                //    Device.CurrentCompoundCommandStatus = JelCommandStatus.None;

                SetState(EnumHandlerState.Acked);
                //transactionComplete = true;
                //return true;
            }
            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                if (response.Data.Contains("WCH0"))
                {
                    Device.HandlerWaferCheckResult(false,_arm,false);
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("WCH1"))
                {
                    Device.HandlerWaferCheckResult(false, _arm, true);
                    transactionComplete = true;
                    return true;
                }
                if (response.Data.Contains("WCH9"))
                {
                    Device.HandlerWaferCheckResult(true, _arm, true);
                    transactionComplete = true;
                    return true;
                }
            }

            transactionComplete = false;
            return false;
        }



    }
}
