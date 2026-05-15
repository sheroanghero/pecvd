using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.SkyPump
{
    public abstract class SkyPumpHandler : HandlerBase
    {
        public SkyPump Device { get; }

        public string _command;
        protected string _completeEvent;


        protected SkyPumpHandler(SkyPump device, string command,string completeEvent)
            : base(BuildMessage(device.Address, command))
        {
            Device = device;
            _command = command;
            _completeEvent = completeEvent;
            Name = command;
        }

        private static string _startLine = "@";
        private static string _endLine = "\r";
        private static string BuildMessage(string address, string command)
        {
            return _startLine + address + command + _endLine;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SkyPumpMessage response = msg as SkyPumpMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (response.Address != Device.Address)
                {
                    Device.NoteError("Invalid address");
                    msg.IsFormatError = true;
                }
                else if (!response.Data.Contains(_completeEvent))
                {
                    Device.NoteError("Invalid response, no expected command feedback");
                    msg.IsFormatError = true;
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    Device.NoteError(null);
                    return true;
                }
            }

            transactionComplete = false;
            return false;
        }

    }

    public class SkyPumpRawCommandHandler : SkyPumpHandler
    {
        public SkyPumpRawCommandHandler(SkyPump device, string command, string completeEvent)
            : base(device, command, completeEvent)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as SkyPumpMessage;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }


    }

    public class SkyPumpStartHandler : SkyPumpHandler
    {
        public SkyPumpStartHandler(SkyPump device)
            : base(device, "CON_FDP_ON", "FDP_ONOK")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                Device.NoteStart(true);
            }
            return true;
        }
    }
    public class SkyPumpStopHandler : SkyPumpHandler
    {
        public SkyPumpStopHandler(SkyPump device)
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
    public class SkyPumpReadRunParaHandler : SkyPumpHandler
    {
        public SkyPumpReadRunParaHandler(SkyPump device)
            : base(device, "READ_RUN_PARA", "RUN_PARA")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SkyPumpMessage;

                if(result.Data.Length != 40)
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
