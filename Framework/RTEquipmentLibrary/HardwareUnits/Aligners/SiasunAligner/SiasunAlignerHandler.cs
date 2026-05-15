using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.SiasunAligner
{
    public abstract class SiasunAlignerHandler : HandlerBase
    {
        public SiasunAligner Device { get; }

        public string _command;
        protected string _parameter;


        protected SiasunAlignerHandler(SiasunAligner device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string BuildMessage(string command, string parameter)
        {
            string msg = string.IsNullOrEmpty(parameter) ? $"{command}" : $"{command} {parameter}";

            return msg + "\r";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunAlignerMessage response = msg as SiasunAlignerMessage;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class SiasunAlignerRawCommandHandler : SiasunAlignerHandler
    {
        public SiasunAlignerRawCommandHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            Device.NoteRawCommandInfo(_command, result.RawMessage);

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerXferHandler : SiasunAlignerHandler
    {
        public SiasunAlignerXferHandler(SiasunAligner device, string jsonParameter)
            : base(device, "XFER", BuildCommandParameter(jsonParameter))
        {

        }
        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (item.Key.Contains("Slot") || item.Key == "Arm")
                    paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                else if (item.Value != null && item.Value.ToString().Length > 0)
                    paraBuilder.Append(" " + item.Value);
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteActionCompleted(_command);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }
    public class SiasunAlignerPickHandler : SiasunAlignerHandler
    {
        public SiasunAlignerPickHandler(SiasunAligner device, string jsonParameter)
            : base(device, "PICK", BuildCommandParameter(jsonParameter))
        {
        }

        //N 2 R EX Z DOWN SLOT 1 ARM A
        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (paraBuilder.Length == 0)
                {
                    paraBuilder.Append(item.Value);
                }
                else
                {
                    if (item.Key == "Slot" || item.Key == "Arm")
                        paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                    else
                        paraBuilder.Append(" " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteLastPickInfo(SendText);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class SiasunAlignerPlaceHandler : SiasunAlignerHandler
    {
        public SiasunAlignerPlaceHandler(SiasunAligner device, string jsonParameter)
            : base(device, "PLACE", BuildCommandParameter(jsonParameter))
        {
        }

        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                if (paraBuilder.Length == 0)
                {
                    paraBuilder.Append(item.Value);
                }
                else
                {
                    if (item.Key == "Slot" || item.Key == "Arm")
                        paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                    else
                        paraBuilder.Append(" " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteLastPlaceInfo(SendText);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class SiasunAlignerSimpleActionHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleActionHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteActionCompleted(_command);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
    public class SiasunAlignerSimpleSetHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleSetHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteSetCompleted(_command, _parameter);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerAlignHandler : SiasunAlignerHandler
    {
        public SiasunAlignerAlignHandler(SiasunAligner device)
            : base(device, "ALIGN", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.ResetError();
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerSetAngleHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSetAngleHandler(SiasunAligner device, float angle)
            : base(device, "SWAA", angle.ToString("f1"))
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }


    public class SiasunAlignerRotateHandler : SiasunAlignerHandler
    {
        public SiasunAlignerRotateHandler(SiasunAligner device, float angle)
            : base(device, "ROTATE", angle.ToString("f1"))
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerSevoOnOffHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSevoOnOffHandler(SiasunAligner device, bool isOn)
            : base(device, isOn ? "SVON" : "SVOFF")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteSevoOnOff(_command == "SVON");
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class SiasunAlignerQueryBiasHandler : SiasunAlignerHandler
    {
        public SiasunAlignerQueryBiasHandler(SiasunAligner device)
            : base(device, "RBIASRT", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if (resultArray.Count() >= 2 && resultArray[0] == _command)
                {
                    var ar1 = resultArray[1].Split(',');
                    if (ar1.Length == 2)
                    {
                        Device.RadialOffset = Convert.ToSingle(ar1[0]);
                        Device.ThetaOffset = Convert.ToSingle(ar1[1]);
                    }
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerQueryErrorHandler : SiasunAlignerHandler
    {
        public SiasunAlignerQueryErrorHandler(SiasunAligner device)
            : base(device, "REER", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if (resultArray.Count() >= 2 && resultArray[0] == "_ERR")
                {
                    Device.NoteError(resultArray[1]);
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerQueryWaferPresentHandler : SiasunAlignerHandler
    {
        public SiasunAlignerQueryWaferPresentHandler(SiasunAligner device)
            : base(device, "RWK", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if (resultArray.Count() >= 2 && resultArray[0] == _command)
                {
                    Device.IsWaferPresence = resultArray[1].Contains("1");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerQueryReadyHandler : SiasunAlignerHandler
    {
        public SiasunAlignerQueryReadyHandler(SiasunAligner device)
            : base(device, "RAR", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if (resultArray.Count() >= 2 && resultArray[0] == _command)
                {
                    var isReady = resultArray[0].Contains("1");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SiasunAlignerSimpleQueryHandler : SiasunAlignerHandler
    {
        public SiasunAlignerSimpleQueryHandler(SiasunAligner device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as SiasunAlignerMessage;

            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                var resultArray = result.Data.Split(' ');
                if (resultArray.Count() >= 2 && resultArray[0] == _command)
                {
                    Device.NoteError(null);
                    Device.NoteQueryResult(_command, _parameter, string.Join(",", resultArray, 1, resultArray.Count() - 1));
                }
                else
                {
                    Device.NoteError($"Invalid Response '{result.Data}' for command: {_command}");
                }

            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }


}
