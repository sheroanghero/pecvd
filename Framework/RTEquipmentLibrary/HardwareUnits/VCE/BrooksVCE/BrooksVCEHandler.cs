using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.BrooksVCE
{
    public abstract class BrooksVCEHandler : HandlerBase
    {
        public BrooksVCE Device { get; set; }

        protected string _commandType;
        protected string _command;
        protected string _parameter;

        public bool IsSendText(string commandType, string command, string commandArgumen)
        {
            return _commandType == commandType && _command == command && _parameter == commandArgumen;
        }
        protected BrooksVCEHandler(BrooksVCE device, string commandType, string command, string parameter = null)
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
            if(!string.IsNullOrEmpty(command))
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
            var result = msg as BrooksVCEMessage;

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

    public class BrooksVCERawCommandHandler : BrooksVCEHandler
    {
        public BrooksVCERawCommandHandler(BrooksVCE device, string commandType, string command, string parameter = null)
            : base(device, commandType, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            base.HandleMessage(msg, out handled);

            var result = msg as BrooksVCEMessage;
            Device.NoteRawCommandInfo(_commandType,_command, result.RawMessage);
            return true;
        }


    }

    public class BrooksVCEAbortHandler : BrooksVCEHandler
    {
        public BrooksVCEAbortHandler(BrooksVCE device)
            : base(device, "E", "")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                Device.NoteIsHalted(true);
            }
            return true;
        }
    }


    public class BrooksVCECommonActionHandler : BrooksVCEHandler
    {
        public BrooksVCECommonActionHandler(BrooksVCE device, string command)
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

    public class BrooksVCEMoveHandler : BrooksVCEHandler
    {
        public BrooksVCEMoveHandler(BrooksVCE device, string axis, string type, string value)
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
    public class BrooksVCECloseDoorHandler : BrooksVCEHandler
    {
        public BrooksVCECloseDoorHandler(BrooksVCE device)
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
    public class BrooksVCEOpenDoorHandler : BrooksVCEHandler
    {
        public BrooksVCEOpenDoorHandler(BrooksVCE device)
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

    public class BrooksVCEHomeHandler : BrooksVCEHandler
    {
        public BrooksVCEHomeHandler(BrooksVCE device, string axis)
            : base(device, "A", "HM", axis)
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

    public class BrooksVCEGotoZHandler : BrooksVCEHandler
    {
        public BrooksVCEGotoZHandler(BrooksVCE device, string direction)
            : base(device, "A", "GOTO", "ARM,Z," + direction)
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
    public class BrooksVCESetCommunicationModeHandler : BrooksVCEHandler
    {
        public BrooksVCESetCommunicationModeHandler(BrooksVCE device, string commMode)
            : base(device, "S", "COMM", "FLOW," + commMode)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                if (result.IsComplete)
                {
                    Device.NoteCommunicationMode(this._parameter.Split(',')[2]);
                }
            }
            return true;
        }
    }


    public class BrooksACERequestArmRPositonHandler : BrooksVCEHandler
    {
        public BrooksACERequestArmRPositonHandler(BrooksVCE device, string axisPosition)
            : base(device, "R", "ARM", "R,"+ axisPosition)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteArmRPositon(result.Data.Split(',')[2]);
            }
            return true;
        }
    }
    public class BrooksACERequestArmZPositonHandler : BrooksVCEHandler
    {
        public BrooksACERequestArmZPositonHandler(BrooksVCE device, string axisPosition)
            : base(device, "R", "ARM", "Z," + axisPosition)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteArmZPositon(result.Data.Split(',')[2]);
            }
            return true;
        }
    }

    public class BrooksACERequestCommModeHandler : BrooksVCEHandler
    {
        public BrooksACERequestCommModeHandler(BrooksVCE device)
            : base(device, "R", "COMM", "FLOW")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteCurrentCommunicationMode(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestErrorStatusHandler : BrooksVCEHandler
    {
        public BrooksACERequestErrorStatusHandler(BrooksVCE device)
            : base(device, "R", "ER")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteCurrentErrorStatus(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestVCEStatusHandler : BrooksVCEHandler
    {
        public BrooksACERequestVCEStatusHandler(BrooksVCE device)
            : base(device, "R", "STAT", "ALL")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteCurrentACEStatus(result.Data);
            }
            return true;
        }
    }
    public class BrooksACERequestLoadPositionHandler : BrooksVCEHandler
    {
        public BrooksACERequestLoadPositionHandler(BrooksVCE device)
            : base(device, "R", "LP")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteIsLoadPositon(result.Data);
            }
            return true;
        }
    }

    public class BrooksACERequestMappingInfoHandler : BrooksVCEHandler
    {
        public BrooksACERequestMappingInfoHandler(BrooksVCE device)
            : base(device, "R", "MI")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteCurrentMappingInfo(result.Data);
            }
            return true;
        }
    }
    public class BrooksACERequestPlatformPositionHandler : BrooksVCEHandler
    {
        public BrooksACERequestPlatformPositionHandler(BrooksVCE device)
            : base(device, "R", "LP")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksVCEMessage;
                Device.NoteIsPlatformPosition(result.Data);
            }
            return true;
        }
    }

}
