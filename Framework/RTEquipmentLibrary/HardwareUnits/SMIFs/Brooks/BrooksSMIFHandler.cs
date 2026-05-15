using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Brooks
{
    public abstract class BrooksSMIFHandler : HandlerBase
    {
        public BrooksSMIF Device { get; }

        public string _commandType;
        public string _command;
        public string _parameter;
        protected string _completeEvent;

        protected BrooksSMIFHandler(BrooksSMIF device, string commandType,string command, string completeEvent = null,string parameter = null)
            : base(BuildMessage(commandType, command, parameter))
        {
            Device = device;
            _commandType = commandType;
            _command = command;
            _completeEvent = completeEvent;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage(string commandType, string command, string parameter)
        {
            string commandStr = commandType;
            if(command != null)
            {
                commandStr += " " + command;
            }
            if (parameter != null)
            {
                commandStr += " " + parameter;
            }
            return commandStr + "\r\n";
        }
        protected static string F2S(float value)
        {
            return value < 0 ? value.ToString() : " " + value.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            BrooksSMIFMessage response = msg as BrooksSMIFMessage;
            ResponseMessage = msg;


            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                if(msg.IsAck)
                {
                    this.SetState(EnumHandlerState.Acked);
                }

                if (_completeEvent == null)
                {
                    if (this.IsAcked)
                    {
                        SetState(EnumHandlerState.Completed);
                        transactionComplete = true;
                        return true;
                    }
                }
                else if (this.IsAcked && msg.IsEvent && _completeEvent == response.Data)
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    return true;
                }
            }

            transactionComplete = false;
            return false;
        }

    }

    public class BrooksSMIFRawCommandHandler : BrooksSMIFHandler
    {
        public BrooksSMIFRawCommandHandler(BrooksSMIF device, string commandType, string command, string completeEvent,string parameter = null)
            : base(device, commandType, command, completeEvent,parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksSMIFMessage;
                Device.NoteRawCommandInfo(_commandType,_command, result.RawMessage, msg.IsAck);

                ResponseMessage = msg;
                handled = true;
            }
            return true;
        }


    }

    public class BrooksSMIFEnableActionHandler : BrooksSMIFHandler
    {
        public BrooksSMIFEnableActionHandler(BrooksSMIF device, string action, bool isEnable)
            : base(device, "HCS", isEnable? "ENABLE":"DISABLE",null, action)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as BrooksSMIFMessage;
                Device.NoteEnable(_parameter, _command == "ENABLE");
            }
            return true;
        }
    }

    public class BrooksSMIFFetchCassetteHandler : BrooksSMIFHandler
    {
        public BrooksSMIFFetchCassetteHandler(BrooksSMIF device)
            : base(device, "HCS", "FETCH", "CMPL_FETCH")
        {
        }
    }
    public class BrooksSMIFLoadCassetteHandler : BrooksSMIFHandler
    {
        public BrooksSMIFLoadCassetteHandler(BrooksSMIF device)
            : base(device, "HCS", "LOAD", null)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (!base.HandleMessage(msg, out handled))
            {
                BrooksSMIFMessage response = msg as BrooksSMIFMessage;
                if (response.Data.Contains("ABORT_LOAD"))
                {
                    Device.NoteError("ABORT_LOAD");
                    handled = true;
                }
            }
            return true;
        }
    }

    public class BrooksSMIFHomeHandler : BrooksSMIFHandler
    {
        public BrooksSMIFHomeHandler(BrooksSMIF device)
            : base(device, "HCS", "HOME", null)
        {
        }
    }

    public class BrooksSMIFRecoveryHandler : BrooksSMIFHandler
    {
        public BrooksSMIFRecoveryHandler(BrooksSMIF device)
            : base(device, "HCS", "RECOVERY", null)
        {
        }
    }

    public class BrooksSMIFStopHandler : BrooksSMIFHandler
    {
        public BrooksSMIFStopHandler(BrooksSMIF device)
            : base(device, "HCS", "STOP", null)
        {
        }
    }

    public class BrooksSMIFResetHandler : BrooksSMIFHandler
    {
        public BrooksSMIFResetHandler(BrooksSMIF device)
            : base(device, "HCS", "RESET", null)
        {
        }
    }

    public class BrooksSMIFOpenPodHandler : BrooksSMIFHandler
    {
        public BrooksSMIFOpenPodHandler(BrooksSMIF device)
            : base(device, "HCS", "OPEN", "CMPL_OPEN")
        {
        }
    }
    public class BrooksSMIFUnloadCassetteHandler : BrooksSMIFHandler
    {
        public BrooksSMIFUnloadCassetteHandler(BrooksSMIF device)
            : base(device, "HCS", "UNLOAD", null)
        {
        }
        //mabye need and the guide document miss this info ??
        //public override bool HandleMessage(MessageBase msg, out bool handled)
        //{
        //    if (!base.HandleMessage(msg, out handled))
        //    {
        //        BrooksSMIFMessage response = msg as BrooksSMIFMessage;
        //        if (response.Data.Contains("ABORT_UNLOAD"))
        //        {
        //            Device.NoteError("ABORT_UNLOAD");
        //            handled = true;
        //        }
        //    }
        //    return true;
        //}
    }
    public class BrooksSMIFClosePodHandler : BrooksSMIFHandler
    {
        public BrooksSMIFClosePodHandler(BrooksSMIF device)
            : base(device, "HCS", "CLOSE", "CMPL_UNLOAD")
        {
        }
    }
    public class BrooksSMIFRequestConstantHandler : BrooksSMIFHandler
    {
        public BrooksSMIFRequestConstantHandler(BrooksSMIF device, string constantId)
            : base(device, "ECR", null, null, constantId)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as BrooksSMIFMessage;

            if (result.MessagePart[0] == "ECD")
            {
                msg.IsAck = true;
            }
            else
            {
                handled = false;
                return false;
            }

            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class BrooksSMIFRequestStatusHandler : BrooksSMIFHandler
    {
        int _formCode;
        string AlarmId = "ALMID";//0000 = No alarm
        string Mode = "MODE";//AUTO,MANUAL
        string PodPresent = "PIP";//FALSE=no pod present,TRUE=pod present
        string PortStatus = "PRTST";//UNLK=port unlocked,LOCK=port locked,OTHER=none of the above
        string GripperStatus = "GRPST";//OPEN=gripper opened;CLOSE=gripper closed;OT=overtraveled;OTHER=none of the above
        string Ready = "READY";//FALSE=busy;TRUE=ready for new command
        string Home = "HOME";//FALSE=not home;TRUE=home
        string Elevator = "ELUP";//FALSE=elevator not at home limit;TRUE=elevator at home limit
        string ArmRetract = "ARM_RETR";//TRUE=arm retract;FALSE=arm not retract
        public BrooksSMIFRequestStatusHandler(BrooksSMIF device, int formCode)
            : base(device, "FSR", null, null, "FC="+formCode.ToString())
        {
            _formCode = formCode;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as BrooksSMIFMessage;

            if (result.MessagePart[0].Contains($"FSD{_formCode}"))
            {
                msg.IsAck = true;
            }
            else
            {
                handled = false;
                return false;
            }

            bool isPodPresent = false;
            bool isReady = false;
            bool isArmRetract = false;
            bool isHomed = false;
            for (int i = 0;i< result.MessagePart.Length;i++)
            {
                var statusArray = result.MessagePart[i].Split('=');
                if (statusArray.Length != 2)
                    continue;

                if (statusArray[0] == AlarmId)
                {
                    if (statusArray[1] != "0000")
                        Device.SetError(statusArray[1]);
                }
                else if (statusArray[0] == Mode)
                {

                }
                else if (statusArray[0] == PodPresent)
                {
                    if (statusArray[1].ToUpper() == "TRUE")
                        isPodPresent = true;
                }
                else if (statusArray[0] == PortStatus)
                {

                }
                else if (statusArray[0] == GripperStatus)
                {

                }
                else if (statusArray[0] == Ready)
                {
                    if (statusArray[1].ToUpper() == "TRUE")
                        isReady = true;
                }
                else if (statusArray[0] == Home)
                {
                    if (statusArray[1].ToUpper() == "TRUE")
                        isHomed = true;
                }
                else if (statusArray[0] == Elevator)
                {

                }
                else if (statusArray[0] == ArmRetract)
                {
                    if (statusArray[1].ToUpper() == "TRUE")
                        isArmRetract = true;
                }
            }

            Device.SetStatus(isReady, isHomed, isPodPresent, isArmRetract);

            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteStatus(result.MessagePart[0], result.Data);
            }
            return true;
        }
    }

}
