using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.JelAligner
{
    public abstract class JelAlignerHandler : HandlerBase
    {
        public JelAligner Device { get; }

        protected string _command;
        protected string _parameter;
        protected JelAlignerHandler(JelAligner device, string command, string parameter = null)
            : base(BuildMessage(device.BodyNumber, command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        protected JelAlignerHandler(string command)
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
            JelAlignerMessage response = msg as JelAlignerMessage;
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

    public class JelAlignerReadHandler : JelAlignerHandler
    {

        public JelAlignerReadHandler(JelAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {

            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute read command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelAlignerMessage response = msg as JelAlignerMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                Device.ParseData(_command, _parameter, response.Data);
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }

    public class JelAlignerSetHandler : JelAlignerHandler
    {

        public JelAlignerSetHandler(JelAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {

            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute set command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            JelAlignerMessage response = msg as JelAlignerMessage;
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
    public class JelAlignerMoveHandler : JelAlignerHandler
    {
        public JelAlignerMoveHandler(JelAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }
    }

    public class JelAlignerRawCommandHandler : JelAlignerHandler
    {
        public JelAlignerRawCommandHandler(JelAligner device, string command)
            : base(command)
        {

            LOG.Write($"{device.Name} execute move command {command} in byte.");
        }
    }


}
