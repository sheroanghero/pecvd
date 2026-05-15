using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.YaskawaAligner
{
    public abstract class YaskawaAlignerHandler : HandlerBase
    {
        public YaskawaAligner Device { get; }

        public string _commandType;
        public string _command;
        public string _parameter;
        private int _seqNO;

        public YaskawaAlignerHandler(YaskawaAligner device, string command, string parameter = null)
                : base(BuildMessage(command, parameter))
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
                Device.OnError(response.RawMessage);
                transactionComplete = true;
                return true;
            }
            if (msg.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (response.IsComplete)
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

    public class YaskawaAlignerReadHandler : YaskawaAlignerHandler
    {
        public YaskawaAlignerReadHandler(YaskawaAligner aligner, string command, string parameter = null)
            : base(aligner, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{aligner.Name} execute read command {command} {temp} in ASCII.");
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;

            if (Device.ParseReadData(_command, response.Data))
            {
                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            response.IsError = true;
            return base.HandleMessage(response, out transactionComplete);
        }

    }
    public class YaskawaAlignerSetHandler : YaskawaAlignerHandler
    {
        public YaskawaAlignerSetHandler(YaskawaAligner pa, string command, string parameter = null)
            : base(pa, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{pa.Name} execute set command {command} {temp} in ASCII.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
            if (response.IsComplete)
            {

                Device.OnActionDone(null);
                transactionComplete = true;
                return true;
            }
            return base.HandleMessage(response, out transactionComplete);
        }
    }

    public class YaskawaAlignerMotionHandler : YaskawaAlignerHandler
    {
        public YaskawaAlignerMotionHandler(YaskawaAligner pa, string command, string parameter = null)
           : base(pa, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{pa.Name} execute motion command {command} {temp} in ASCII.");
        }
    }

    public class YaskawaAlignerGripAndDetectHandler : YaskawaAlignerHandler
    {
        public YaskawaAlignerGripAndDetectHandler(YaskawaAligner pa)
           : base(pa, "CSOL", "1,1,0")
        {
            
            LOG.Write($"{pa.Name} execute motion command CSOL 1,1,0 in ASCII.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            YaskawaRobotMessage response = msg as YaskawaRobotMessage;
            ResponseMessage = msg;
            if (msg.IsError)
            {
                Device.OnError(response.RawMessage);
                transactionComplete = true;
                return true;
            }
            if (msg.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                //Device.SenACK();
                Thread.Sleep(1000);
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
