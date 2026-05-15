using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.PfeifferPumpA603
{
    public abstract class PfeifferPumpA603Handler : HandlerBase
    {
        public PfeifferPumpA603 Device { get; }

        public string _command;
        protected string _parameter;


        protected PfeifferPumpA603Handler(PfeifferPumpA603 device, string command, string parameter)
            : base(BuildMessage(device.ADR, command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string _startLine = "#";
        private static string _endLine = "\r";
        private static string BuildMessage(string address, string command, string parameter)
        {
            if (string.IsNullOrEmpty(parameter))
                return _startLine + address + command + _endLine;
            else
                return _startLine + address + command + parameter + _endLine;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            PfeifferPumpA603Message response = msg as PfeifferPumpA603Message;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (response.Address != Device.ADR)
                {
                    Device.NoteError("Invalid address");
                    msg.IsFormatError = true;
                }
                else if (response.Data.Contains("ERR"))
                {
                    Device.NoteError($"Command '{_command}' Error: {response.Data}");
                    msg.IsError = true;
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

    public class PfeifferPumpA603RawCommandHandler : PfeifferPumpA603Handler
    {
        public PfeifferPumpA603RawCommandHandler(PfeifferPumpA603 device, string command, string parameter)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as PfeifferPumpA603Message;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }


    }

    public class PfeifferPumpA603SimpleSwitchHandler : PfeifferPumpA603Handler
    {
        public PfeifferPumpA603SimpleSwitchHandler(PfeifferPumpA603 device, string command, string parameter)
            : base(device, command, parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted(_command, _parameter.Contains("ON"));
            }
            return true;
        }
    }

    public class PfeifferPumpSetPumpParameterHandler : PfeifferPumpA603Handler
    {
        public PfeifferPumpSetPumpParameterHandler(PfeifferPumpA603 device, string parameter)
            : base(device, "SET", parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParaCompleted();
            }
            return true;
        }
    }

    public class PfeifferPumpA603DisplayAlarmHandler : PfeifferPumpA603Handler
    {
        public PfeifferPumpA603DisplayAlarmHandler(PfeifferPumpA603 device, string parameter)
            : base(device, "DEF", parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteDisplayAlarmOrAlert(_parameter.Contains("0"));
            }
            return true;
        }
    }

    public class PfeifferPumpA603ReadPumpStatusHandler : PfeifferPumpA603Handler
    {
        public PfeifferPumpA603ReadPumpStatusHandler(PfeifferPumpA603 device)
            : base(device, "STA", null)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as PfeifferPumpA603Message;

                if (result.Data.Length != 68)
                {
                    Device.NoteError("Invalid 'RunParameter'response, the length is not 68");
                }
                else
                {
                    Device.NotePumpStatus(result.Data);
                }
            }
            return true;

        }

    }


}
