using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Siasun1500C800C
{
    public abstract class RobotSiasun1500C800CHandler : HandlerBase
    {
        public RobotSiasun1500C800C Device { get; }

        protected string _command;
        protected string _parameter;

        protected string _requestResponse = "";

        protected RobotSiasun1500C800CHandler(RobotSiasun1500C800C device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage( string command, string parameter)
        {
            string msg = parameter == null ? $"{command}" : $"{command} {parameter}";

            return msg + "\r";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RobotSiasun1500C800CMessage response = msg as RobotSiasun1500C800CMessage;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }
            if(response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class RobotSiasun1500C800CRawCommandHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CRawCommandHandler(RobotSiasun1500C800C device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as RobotSiasun1500C800CMessage;
                var rawMsg = _requestResponse != null ? _requestResponse + "$" + result.RawMessage : result.RawMessage;
                Device.NoteRawCommandInfo(_command, rawMsg);
            }

            return true;
        }


    }

    public class RobotSiasun1500C800CGotoHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CGotoHandler(RobotSiasun1500C800C device, string jsonParameter)
            : base(device, "GOTO", BuildCommandParameter(jsonParameter))
        {

        }

        static string BuildCommandParameter(string jsonParameter)
        {
            JObject jsonObject = JObject.Parse(jsonParameter);
            StringBuilder paraBuilder = new StringBuilder();
            foreach (var item in jsonObject)
            {
                string key = item.Key;
                if (key == "station")
                    key = "N";

                if (paraBuilder.Length == 0)
                {
                    paraBuilder.Append(key.ToUpper() + " " + item.Value);
                }
                else
                {
                    paraBuilder.Append(" " + key.ToUpper() + " " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteIsHalted(true);

                if (_parameter.Contains("ARM B"))
                {
                    string position = _parameter.Replace(" ARM B", "");
                    Device.NoteArmBPosition(position);
                }
                else
                {
                    string position = _parameter;
                    if (_parameter.Contains("ARM A"))
                    {
                        position = _parameter.Replace(" ARM A", "");
                    }
                    Device.NoteArmAPosition(position);
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }
    public class RobotSiasun1500C800CPickHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CPickHandler(RobotSiasun1500C800C device, string jsonParameter)
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
                    if(item.Key == "Slot" || item.Key == "Arm")
                        paraBuilder.Append(" " + item.Key.ToUpper() + " " + item.Value);
                    else
                        paraBuilder.Append(" " + item.Value);
                }
            }

            return paraBuilder.Length > 0 ? paraBuilder.ToString() : null;

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
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

    public class RobotSiasun1500C800CPlaceHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CPlaceHandler(RobotSiasun1500C800C device, string jsonParameter)
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
            var result = msg as RobotSiasun1500C800CMessage;
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
    public class RobotSiasun1500C800CTransferHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CTransferHandler(RobotSiasun1500C800C device, string fromStation, string toStation, string arm = null)
            : base(device, "XFER", arm == null? $"{fromStation} {toStation}" : $"ARM {arm} {fromStation} {toStation}")
        {
        }
    }

    public class RobotSiasun1500C800CRetractHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CRetractHandler(RobotSiasun1500C800C device)
            : base(device, "RETRACT")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRetracted(true);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }
    public class RobotSiasun1500C800CHaltHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CHaltHandler(RobotSiasun1500C800C device)
            : base(device, "HALT")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
            if (result.IsError)
            {
                Device.NoteIsHalted(false);
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteIsHalted(true);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class RobotSiasun1500C800CSetCommunicationEchoHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CSetCommunicationEchoHandler(RobotSiasun1500C800C device, string echoStaus)
            : base(device, "SET COMM ECHO", echoStaus)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetCommEchoCompleted(true);
            }

            return true;
        }

    }

    public class RobotSiasun1500C800CSetLoadHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CSetLoadHandler(RobotSiasun1500C800C device, string arm, string status)
            : base(device, "SET LOAD", arm + " " + status)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetLoadCompleted(true);
            }

            return true;
        }

    }

    public class RobotSiasun1500C800CSevoOnOffHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CSevoOnOffHandler(RobotSiasun1500C800C device, bool isOn)
            : base(device, isOn ? "SVON" : "SVOFF")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
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

    public class RobotSiasun1500C800CHomeAxisHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CHomeAxisHandler(RobotSiasun1500C800C device, string parameter)
            : base(device, "HOME", parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasun1500C800CMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteAxisHomed(_parameter);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class RobotSiasun1500C800CQueryWaferOnOffHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CQueryWaferOnOffHandler(RobotSiasun1500C800C device, string jsonParameter)
            : base(device, "RQ LOAD", jsonParameter != null ? jsonParameter : "A")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as RobotSiasun1500C800CMessage;

                if (_parameter != null && _parameter.Contains('B'))
                {
                    Device.NoteWafeOnOff("B", _requestResponse.Contains("ON"));
                }
                else
                {
                    Device.NoteWafeOnOff("A", _requestResponse.Contains("ON"));
                }
            }

            return true;
        }
    }
    public class RobotSiasun1500C800CRequestCommunicationEchoHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CRequestCommunicationEchoHandler(RobotSiasun1500C800C device)
            : base(device, "RQ COMM ECHO")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as RobotSiasun1500C800CMessage;

                Device.NoteCommEchoStatus(_requestResponse.Contains("ON"));
            }

            return true;
        }



    }

    public class RobotSiasun1500C800CRequestWaferCentDataHandler : RobotSiasun1500C800CHandler
    {
        public RobotSiasun1500C800CRequestWaferCentDataHandler(RobotSiasun1500C800C device)
            : base(device, "RQ WAF_CEN DATA")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as RobotSiasun1500C800CMessage;
                //WAF_CEN RT value1x value1y value4x value4y LFT value2x value2y value3x value3y offset_r offset_t
                Device.NoteWafeCenData(_requestResponse);
            }

            return true;
        }



    }

}
