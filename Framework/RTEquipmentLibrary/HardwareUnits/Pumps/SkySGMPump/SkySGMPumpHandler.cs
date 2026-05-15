using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkySGMPump
{
    public class SkySGMPumpHandler : HandlerBase
    {

        public SkySGMPump Device { get; }

        public string _command;
        protected string _completeEvent;
        protected SkySGMPumpHandler(SkySGMPump device, string command, string completeEvent) : base(BuildMessage(device.Address, command))
        {
            Device = device;
            _command = command;
            _completeEvent = completeEvent;
            Name = command;
        }

        private static string _startLine = "@00";
        private static string _endLine = "\r\n";
        private static string BuildMessage(string address, string command)
        {
            return _startLine + address + command + _endLine;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SkySGMPumpMessage response = msg as SkySGMPumpMessage;
            if (response.IsAck)
            {

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteError(null);
                return true;
            }

            transactionComplete = false;
            return false;
        }


    }

    public class SkySGMPumpRawCommandHandler : SkySGMPumpHandler
    {
        public SkySGMPumpRawCommandHandler(SkySGMPump device, string command, string completeEvent)
            : base(device, command, completeEvent)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SkySGMPumpMessage;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }
    }

    public class SkySGMPumpStartHandler : SkySGMPumpHandler
    {
        public SkySGMPumpStartHandler(SkySGMPump device)
            : base(device, "CON_FDP_ON", "FDP_ONOK")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteStart(true);
            }
            return true;
        }
    }
    public class SkySGMPumpStopHandler : SkySGMPumpHandler
    {
        public SkySGMPumpStopHandler(SkySGMPump device)
            : base(device, "CON_FDP_OFF", "FDP_OFFOK")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteStart(false);
            }
            return true;
        }
    }
    public class SkySGMPumpReadRunParaHandler : SkySGMPumpHandler
    {
        public SkySGMPumpReadRunParaHandler(SkySGMPump device)
            : base(device, "READ_RUN_PARA", "RUN_PARA")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SkySGMPumpMessage;

                if (result.Data.Length != 40)
                {
                    Device.NoteError("Invalid 'RunParameter'response, the length is not 40");
                }
                else
                {
                    Device.NoteRunPara(result.Data);
                }
            }
            return true;

        }

    }
}
