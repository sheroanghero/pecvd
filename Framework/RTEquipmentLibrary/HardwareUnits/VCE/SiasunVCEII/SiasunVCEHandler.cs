using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.SiasunVCEII
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
            : base(BuildMessage(commandType, command, parameter))
        {
            Device = device;
            _commandType = commandType;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string BuildMessage(string commandType, string command, string parameter)
        {
            string strCommand = $"{commandType}";
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
                Device.IsIdle = true;
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
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEZAxisMoveToLoadPositionHandler : SiasunVCEHandler
    {
        public SiasunVCEZAxisMoveToLoadPositionHandler(SiasunVCE device, int timeout)
            : base(device, "A", "LP")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsZAxisAtLoadPostion = true;
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEPlatformInHandler : SiasunVCEHandler
    {
        public SiasunVCEPlatformInHandler(SiasunVCE device, int timeout)
            : base(device, "A", "PI")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsPlatformIn = true;
                Device.IsPlatformOut = false;
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEMappingHandler : SiasunVCEHandler
    {
        public SiasunVCEMappingHandler(SiasunVCE device, int timeout)
            : base(device, "A", "MP")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsIdle = true;
                Device.IsMapped = true;
            }
            return true;
        }
    }

    public class SiasunVCEPlatformOutHandler : SiasunVCEHandler
    {
        public SiasunVCEPlatformOutHandler(SiasunVCE device, int timeout)
            : base(device, "A", "PO")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsPlatformOut = true;
                Device.IsPlatformIn = false;
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCECloseDoorHandler : SiasunVCEHandler
    {
        public SiasunVCECloseDoorHandler(SiasunVCE device, int timeout)
            : base(device, "A", "DC")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsDoorClosed = true;
                Device.IsDoorOpened = false;
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEOpenDoorHandler : SiasunVCEHandler
    {
        public SiasunVCEOpenDoorHandler(SiasunVCE device, int timeout)
            : base(device, "A", "DO")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsDoorOpened = true;
                Device.IsDoorClosed = false;
                Device.IsMapped = false;
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEHomeHandler : SiasunVCEHandler
    {
        public SiasunVCEHomeHandler(SiasunVCE device, int timeout)
            : base(device, "A", "HM", "ALL")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteHomed(true);
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEHomeRAxisHandler : SiasunVCEHandler
    {
        public SiasunVCEHomeRAxisHandler(SiasunVCE device, int timeout)
            : base(device, "A", "HM", "R")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEHomeZAxisHandler : SiasunVCEHandler
    {
        public SiasunVCEHomeZAxisHandler(SiasunVCE device, int timeout)
            : base(device, "A", "HM", "Z")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteHomed(true);
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCEZAxisGotoSlotHandler : SiasunVCEHandler
    {
        public SiasunVCEZAxisGotoSlotHandler(SiasunVCE device, int slot, int timeout)
            : base(device, "A", "GO", (slot + 1).ToString())
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteZAxisPosition(_parameter);
                Device.IsIdle = true;
            }
            return true;
        }
    }

    //motor motion scope
    public class SiasunVCERequestZAxisMotionScopenHandler : SiasunVCEHandler
    {
        public SiasunVCERequestZAxisMotionScopenHandler(SiasunVCE device, bool isUp)
            : base(device, "R", "ARM", "Z," + (isUp ? "UP" : "DN"))
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

    public class SiasunVCEResetErrorHandler : SiasunVCEHandler
    {
        public SiasunVCEResetErrorHandler(SiasunVCE device)
            : base(device, "S", "ER")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsAlarm = false;
            }
            return true;
        }
    }

    public class SiasunVCECassetteInterlockHandler : SiasunVCEHandler
    {
        public SiasunVCECassetteInterlockHandler(SiasunVCE device, bool isEnable)
            : base(device, "S", "CE", isEnable ? "Y" : "N")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsAlarm = false;
            }
            return true;
        }
    }

    public class SiasunVCEDoorInterlockHandler : SiasunVCEHandler
    {
        public SiasunVCEDoorInterlockHandler(SiasunVCE device, bool isEnable)
            : base(device, "S", "ZD", isEnable ? "Y" : "N")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsAlarm = false;
            }
            return true;
        }
    }

    public class SiasunVCEOutsideSignaliInterlockHandler : SiasunVCEHandler
    {
        public SiasunVCEOutsideSignaliInterlockHandler(SiasunVCE device, bool isEnable)
            : base(device, "S", "FN", isEnable ? "Y" : "N")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.IsAlarm = false;
            }
            return true;
        }
    }

    public class SiasunVCERequestErrorInfoHandler : SiasunVCEHandler
    {
        public SiasunVCERequestErrorInfoHandler(SiasunVCE device)
            : base(device, "R", "ER")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.NoteCurrentErrorStatus(result.Data.Replace("ER", ""));//X,ERcode
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCERequestWaferSlideOutHandler : SiasunVCEHandler
    {
        public SiasunVCERequestWaferSlideOutHandler(SiasunVCE device)
            : base(device, "R", "SO")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.IsWaferSlideOut = result.Data.Replace("SO", "").ToUpper() == "Y";
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCERequestCassettePresentHandler : SiasunVCEHandler
    {
        public SiasunVCERequestCassettePresentHandler(SiasunVCE device)
            : base(device, "R", "W2")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                Device.IsCassettePresent = result.Data.Replace("W2", "").ToUpper() == "Y";
                Device.IsIdle = true;
                Device.IsCassettePresentChecked = true;
            }
            return true;
        }
    }

    // R,STAT,DOOR
    public class SiasunVCERequestDoorStatusHandler : SiasunVCEHandler
    {
        public SiasunVCERequestDoorStatusHandler(SiasunVCE device)
            : base(device, "R", "STAT", "DOOR")
        {
        }

        // X,STATC
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                string resp = result.Data.Replace("STAT", "").ToUpper();
                Device.IsDoorOpened = resp.Last() == 'O';
                Device.IsDoorClosed = resp.Last() == 'C';
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCERequestArmPositionHandler : SiasunVCEHandler
    {
        public SiasunVCERequestArmPositionHandler(SiasunVCE device)
            : base(device, "R", "STAT", "ARM")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                var pos = result.Data.Replace("STAT", "").ToUpper();
                if (int.TryParse(pos, out int slot) && slot > 0)
                {
                    //slot
                    Device.IsZAxisAtLoadPostion = false;
                    Device.IsZAxisAtHomePostion = false;
                    Device.ZAxisAtSlot = slot - 1;
                }
                else if (pos == "M")
                {
                    //Mapping end position
                    Device.IsZAxisAtLoadPostion = false;
                    Device.IsZAxisAtHomePostion = false;
                    Device.ZAxisAtSlot = -1;
                }
                else if (pos == "H")
                {
                    //Home position
                    Device.IsZAxisAtLoadPostion = false;
                    Device.IsZAxisAtHomePostion = true;
                    Device.ZAxisAtSlot = -1;
                }
                else if (pos == "L")
                {
                    //Home position
                    Device.IsZAxisAtLoadPostion = true;
                    Device.IsZAxisAtHomePostion = false;
                    Device.ZAxisAtSlot = -1;
                }
                else
                {
                    //unknown position
                    Device.IsZAxisAtLoadPostion = false;
                    Device.IsZAxisAtHomePostion = false;
                    Device.ZAxisAtSlot = -1;
                }

                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCERequestMappingInfoHandler : SiasunVCEHandler
    {
        //S:single wafer
        //O;no wafer
        //T:double wafer
        //C:cross wafer
        //?:slot spare
        //X,MI****
        public SiasunVCERequestMappingInfoHandler(SiasunVCE device)
            : base(device, "R", "MI")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as SiasunVCEMessage;
                var arr = result.Data.Replace("MI", "").Replace("?", "").ToCharArray();
                Array.Reverse(arr);//  slot25->slot1 to   slot1->slot25
                Device.NoteCurrentMappingInfo(new string(arr));
                Device.IsIdle = true;
            }
            return true;
        }
    }

    public class SiasunVCESetCassetteSlotNumberHandler : SiasunVCEHandler
    {
        // slot number [1, 25]
        public SiasunVCESetCassetteSlotNumberHandler(SiasunVCE device, int slotNum)
            : base(device, "S", "CF,NS", slotNum.ToString())
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }

    public class SiasunVCESetMappingTeachFlagHandler : SiasunVCEHandler
    {
        public SiasunVCESetMappingTeachFlagHandler(SiasunVCE device, bool isMappingTeach)
            : base(device, "S", "MP", (isMappingTeach ? 1 : 0).ToString())
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }

    public class SiasunVCESetZAxisSpeedHandler : SiasunVCEHandler
    {
        //speed range (0,99] %
        public SiasunVCESetZAxisSpeedHandler(SiasunVCE device, int speed)
            : base(device, "S", "SPEED,Z", speed.ToString())
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }

    public class SiasunVCESetWaferDetectHandler : SiasunVCEHandler
    {
        public SiasunVCESetWaferDetectHandler(SiasunVCE device, bool isDetect)
            : base(device, "S", "WS", isDetect ? "Y" : "N")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }
}
