using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public abstract class JelC5000RobotHandler:HandlerBase
    {
        public JelC5000Robot Device { get; }

        protected string _command;
        protected string _parameter;
        protected JelC5000RobotHandler(JelC5000Robot device, string command, string parameter = null)
            : base(BuildMessage(device.BodyNumber, command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        protected JelC5000RobotHandler(string command)
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
            JelC5000RobotMessage response = msg as JelC5000RobotMessage;
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

    public class JelC5000RobotReadHandler: JelC5000RobotHandler
    {
        
        public JelC5000RobotReadHandler(JelC5000Robot device, string command, string parameter = null)
            : base(device, command, parameter)
        {
           
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute read command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelC5000RobotMessage response = msg as JelC5000RobotMessage;
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

    public class JelC5000RobotSetHandler : JelC5000RobotHandler
    {

        public JelC5000RobotSetHandler(JelC5000Robot device, string command, string parameter = null)
            : base(device, command, parameter)
        {

            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute read command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelC5000RobotMessage response = msg as JelC5000RobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                switch(_command)
                {
                    case "BC":
                        Device.ReadBankNumber = _parameter;
                        break;
                    case "WCP":
                        Device.ReadCassetNumber = Convert.ToInt32(_parameter);
                        break;
                    case "WCD":
                        Device.ReadSlotNumber = Convert.ToInt32(_parameter);
                        break;

                }



                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }
    public class JelC5000RobotMoveHandler : JelC5000RobotHandler
    {
        public JelC5000RobotMoveHandler(JelC5000Robot device, string command, string parameter = null)
            : base(device, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelC5000RobotMessage response = msg as JelC5000RobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                //if (_command == "G")
                //    Device.CurrentCompoundCommandStatus = JelCommandStatus.None;
                    
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }



    }

    public class JelC5000RobotRawCommandHandler : JelC5000RobotHandler
    {
        public JelC5000RobotRawCommandHandler(JelC5000Robot device, string command)
            : base(device,command)
        {
            
            LOG.Write($"{device.Name} execute move command {command} in byte.");
        }
    }

    public class JelC5000RobotCompaundCommandHandler : JelC5000RobotHandler
    {
        public JelC5000RobotCompaundCommandHandler(JelC5000Robot device, string cmdNo, string parameter = null)
            : base(device, $"G{cmdNo}", parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute compaund command {cmdNo} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelC5000RobotMessage response = msg as JelC5000RobotMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                //if (_command == "G")
                //    Device.CurrentCompoundCommandStatus = JelCommandStatus.None;

                SetState(EnumHandlerState.Acked);
                //transactionComplete = true;
                //return true;
            }
            if(response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                if(response.Data.Contains("END"))
                {
                    transactionComplete = true;
                    return true;
                }
                if(response.Data.Contains("ERR"))
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


}
