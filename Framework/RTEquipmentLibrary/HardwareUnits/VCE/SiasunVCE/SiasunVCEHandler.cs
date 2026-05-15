using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.SiasunVCE
{
    public abstract class SiasunVCEHandler : HandlerBase
    {
        public SiasunVCE Device { get; set; }

        protected string _commandType;
        protected string _command;
        protected string _parameter;

        public bool IsSendText(string commandType, string command, string commandArgumen)
        {
            return _commandType == commandType && _command == command && _parameter == commandArgumen;
        }
        protected SiasunVCEHandler(SiasunVCE device, string commandType, string command, string parameter = null)
            : base(BuildMessage(device.Address, commandType, command, parameter))
        {
            Device = device;
            _commandType = commandType;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage(string address, string commandType, string command, string parameter)
        {
            string strCommand = $"{address},{commandType}";
            if (!string.IsNullOrEmpty(command))
            {
                strCommand += "," + command;
            }
            if (!string.IsNullOrEmpty(parameter))
            {
                strCommand += "," + parameter;
            }
            return strCommand + "\r";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunVCEMessage;

            handled = msg.IsComplete;


            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            if (result.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
            }
            if (result.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (result.IsRDY)
            {
                if (IsAcked)
                {
                    SetState(EnumHandlerState.Completed);
                    handled = true;
                }
                else
                {
                    SetState(EnumHandlerState.Acked);
                }
            }

            ResponseMessage = msg;

            return handled;
        }

    }

    public class SiasunVCERawCommandHandler : SiasunVCEHandler
    {
        public SiasunVCERawCommandHandler(SiasunVCE device, string commandType, string command, string parameter = null)
            : base(device, commandType, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            base.HandleMessage(msg, out handled);

            var result = msg as SiasunVCEMessage;
            Device.NoteRawCommandInfo(_commandType, _command, result.RawMessage);
            return true;
        }


    }

    public class SiasunVCEAbortHandler : SiasunVCEHandler
    {
        public SiasunVCEAbortHandler(SiasunVCE device)
            : base(device, "E", "")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteIsHalted(true);
            }
            return true;
        }
    }


    public class SiasunVCECommonActionHandler : SiasunVCEHandler
    {
        public SiasunVCECommonActionHandler(SiasunVCE device, string command)
            : base(device, "A", command)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteCommonActionResult(_command);
            }
            return true;
        }
    }

    public class SiasunVCEMoveHandler : SiasunVCEHandler
    {
        public SiasunVCEMoveHandler(SiasunVCE device, string axis, string type, string value)
            : base(device, "A", "MOVE", axis + "," + type + "," + value)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteMoveResult(_parameter);
            }
            return true;
        }
    }
    public class SiasunVCECloseDoorHandler : SiasunVCEHandler
    {
        public SiasunVCECloseDoorHandler(SiasunVCE device)
            : base(device, "A", "DC")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteDoorClosed(true);
            }
            return true;
        }
    }
    public class SiasunVCEOpenDoorHandler : SiasunVCEHandler
    {
        public SiasunVCEOpenDoorHandler(SiasunVCE device)
            : base(device, "A", "DO")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteDoorClosed(false);
            }
            return true;
        }
    }

    public class SiasunVCEHomeHandler : SiasunVCEHandler
    {
        public SiasunVCEHomeHandler(SiasunVCE device)
            : base(device, "A", "HM", "")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteHomed(true);
            }
            return true;
        }
    }

    public class SiasunVCEGotoZHandler : SiasunVCEHandler
    {
        public SiasunVCEGotoZHandler(SiasunVCE device, int slot)
            : base(device, "A", "GOTO", slot.ToString())
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {

                Device.NoteZAxisPosition(_parameter.Split(',')[2]);
            }
            return true;
        }
    }
    public class SiasunVCESetCommunicationModeHandler : SiasunVCEHandler
    {
        public SiasunVCESetCommunicationModeHandler(SiasunVCE device, string commMode)
            : base(device, "S", "COMM", "FLOW," + commMode)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                if (result.IsComplete)
                {
                    Device.NoteCommunicationMode(this._parameter.Split(',')[2]);
                }
            }
            return true;
        }
    }


    public class BrooksACERequestArmRPositonHandler : SiasunVCEHandler
    {
        public BrooksACERequestArmRPositonHandler(SiasunVCE device, string axisPosition)
            : base(device, "R", "ARM", "R," + axisPosition)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteArmRPositon(result.Data.Split(',')[2]);
            }
            return true;
        }
    }
    public class BrooksACERequestArmZPositonHandler : SiasunVCEHandler
    {
        public BrooksACERequestArmZPositonHandler(SiasunVCE device, string axisPosition)
            : base(device, "R", "ARM", "Z," + axisPosition)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteArmZPositon(result.Data.Split(',')[2]);
            }
            return true;
        }
    }

    public class BrooksACERequestCommModeHandler : SiasunVCEHandler
    {
        public BrooksACERequestCommModeHandler(SiasunVCE device)
            : base(device, "R", "COMM", "FLOW")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteCurrentCommunicationMode(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestErrorStatusHandler : SiasunVCEHandler
    {
        public BrooksACERequestErrorStatusHandler(SiasunVCE device)
            : base(device, "R", "ER")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteCurrentErrorStatus(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestVCEStatusHandler : SiasunVCEHandler
    {
        public BrooksACERequestVCEStatusHandler(SiasunVCE device)
            : base(device, "R", "STAT", "ALL")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteCurrentACEStatus(result.Data);
            }
            return true;
        }
    }
    public class BrooksACERequestLoadPositionHandler : SiasunVCEHandler
    {
        public BrooksACERequestLoadPositionHandler(SiasunVCE device)
            : base(device, "R", "LP")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteIsLoadPositon(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestMappingInfoHandler : SiasunVCEHandler
    {
        public BrooksACERequestMappingInfoHandler(SiasunVCE device)
            : base(device, "R", "MI")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteCurrentMappingInfo(result.Data);
            }
            return true;
        }
    }
    public class BrooksACERequestPlatformPositionHandler : SiasunVCEHandler
    {
        public BrooksACERequestPlatformPositionHandler(SiasunVCE device)
            : base(device, "R", "LP")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteIsPlatformPosition(result.Data);
            }
            return true;
        }
    }

}
