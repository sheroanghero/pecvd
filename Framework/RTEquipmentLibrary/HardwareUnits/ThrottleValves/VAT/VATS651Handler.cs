using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.VAT
{
    public abstract class VATS651Handler : HandlerBase
    {
        protected VATS651 Device { get; }

        public string _command;
        protected string _parameter;


        protected VATS651Handler(VATS651 device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string _endLine = "\r\n";
        private static string BuildMessage(string command, string parameter)
        {
            string commandContent = string.IsNullOrEmpty(parameter) ? $"{command}" : $"{command}{parameter}";
            return commandContent + _endLine.ToString();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            VATS651Message response = msg as VATS651Message;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
                transactionComplete = true;
                return false;
            }
            Device.NoteError(null);

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                if (response.Data.IndexOf(_command) == 0)
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

    public class VATS651RawCommandHandler : VATS651Handler
    {
        public VATS651RawCommandHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as VATS651Message;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }
    }

    public class VATS651SimpleActionHandler : VATS651Handler
    {
        public VATS651SimpleActionHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteActionCompleted(_command);
            }
            return true;
        }
    }
    public class VATS651SimpleSetHandler : VATS651Handler
    {
        public VATS651SimpleSetHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetCompleted(_command, _parameter);
            }
            return true;
        }
    }
    public class VATS651SimpleQueryHandler : VATS651Handler
    {
        public VATS651SimpleQueryHandler(VATS651 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651CloseValveHandler : VATS651Handler
    {
        public VATS651CloseValveHandler(VATS651 device)
            : base(device, "C:", "")
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

    public class VATS651OpenValveHandler : VATS651Handler
    {
        public VATS651OpenValveHandler(VATS651 device)
            : base(device, "O:", "")
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

    public class VATS651HoldValveHandler : VATS651Handler
    {
        public VATS651HoldValveHandler(VATS651 device)
            : base(device, "H:", "")
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

    public class VATS651SetPositionHandler : VATS651Handler
    {
        public VATS651SetPositionHandler(VATS651 device, int position)
            : base(device, "R:", position.ToString("D6"))
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

    public class VATS651SetPressureHandler : VATS651Handler
    {
        public VATS651SetPressureHandler(VATS651 device, int position)
            : base(device, "S:", position.ToString("D6"))
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

    public class VATS651SetAccessModeHandler : VATS651Handler
    {
        //c:01aa
        //c:01
        //aa 01 = remote operation, change to local enabled
        public VATS651SetAccessModeHandler(VATS651 device)
            : base(device, "c:01", "01")
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

    public class VATS651SetValveSpeedHandler : VATS651Handler
    {
        //V:00aaaa
        //V:
        //aaaa valve speed, 1 ... 1000 (1 = min. speed, 1000 = max. speed)
        public VATS651SetValveSpeedHandler(VATS651 device, int speed)
            : base(device, "V:", "00" + speed.ToString("D4"))
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

    public class VATS651SetSensorDelayHandler : VATS651Handler
    {
        //s:02A00c
        //s:02
        //c = 0.00…1.00
        public VATS651SetSensorDelayHandler(VATS651 device, float delay)
            : base(device, "s:02", "A00" + delay.ToString())
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

    public class VATS651SetRampTimeHandler : VATS651Handler
    {
        //s:02A01c
        //s:02
        //c = 0.00…1’000’000.0
        public VATS651SetRampTimeHandler(VATS651 device, float delay)
            : base(device, "s:02", "A01" + delay.ToString())
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

    public class VATS651SetGainFactorHandler : VATS651Handler
    {
        //s:02A04c
        //s:02
        //c = 0.0001…7.5
        public VATS651SetGainFactorHandler(VATS651 device, float delay)
            : base(device, "s:02", "A04" + delay.ToString())
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

    public class VATS651ResetHandler : VATS651Handler
    {
        //c:82aa
        //c:82
        //aa 00 = reset service request bit from WARNINGS   01 = reset FATAL ERROR (restart control unit)
        public VATS651ResetHandler(VATS651 device)
            : base(device, "c:82", "01")
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

    public class VATS651QueryPositionHandler : VATS651Handler
    {
        //A:
        //A:aaaaaa
        public VATS651QueryPositionHandler(VATS651 device)
            : base(device, "A:", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651QueryPressureHandler : VATS651Handler
    {
        //P:
        //P:saaaaaaa
        //s sign, 0 for positive readings, - for negative readings
        public VATS651QueryPressureHandler(VATS651 device)
            : base(device, "P:", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651QuerySetPressureHandler : VATS651Handler
    {
        //P:
        //P:saaaaaaa
        //s sign, 0 for positive readings, - for negative readings
        public VATS651QuerySetPressureHandler(VATS651 device)
            : base(device, "W:", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651QueryModeHandler : VATS651Handler
    {
        //P:
        //P:saaaaaaa
        //s sign, 0 for positive readings, - for negative readings
        public VATS651QueryModeHandler(VATS651 device)
            : base(device, "M:", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651QueryStatusHandler : VATS651Handler
    {
        //i:76
        //i:76xxxxxxsyyyyyyyabc
        //xxxxxx  position
        //s sign, 0 for positive readings, - for negative readings
        //yyyyyyy  pressure
        //a Access Mode
        //b Control Mode
        //c Warning
        public VATS651QueryStatusHandler(VATS651 device)
            : base(device, "i:76", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                if (result.Length == 17)
                {
                    var pos = result.Substring(0, 6);
                    int.TryParse(pos, out int posValue);
                    Device.SetPositionFeedback(posValue);

                    var sign = result.Substring(6, 1);

                    var press = result.Substring(7, 7);
                    int.TryParse(press, out int pressValue);
                    Device.SetPressureFeedback(pressValue);

                    var remote = result.Substring(14, 1);

                    var controlMode = result.Substring(15, 1);
                    if (controlMode == "2")
                        Device.Mode = PressureCtrlMode.TVPositionCtrl;
                    else if (controlMode == "3")
                        Device.Mode = PressureCtrlMode.TVClose;
                    if (controlMode == "4")
                        Device.Mode = PressureCtrlMode.TVOpen;
                    if (controlMode == "5")
                        Device.Mode = PressureCtrlMode.TVPressureCtrl;

                    var warning = result.Substring(16, 1);
                }
            }
            return true;
        }
    }

    public class VATS651QueryErrorStatusHandler : VATS651Handler
    {
        //i:50
        //i:50abc
        //abc error code
        public VATS651QueryErrorStatusHandler(VATS651 device)
            : base(device, "i:50", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                Device.NoteQueryResult(_command, result);
            }
            return true;
        }
    }

    public class VATS651QueryOpenStatusHandler : VATS651Handler
    {
        //i:05
        //i:05V1:aV2-
        //a = 0(valve open) a=C(valve close) a=N(valve in intermediate position)
        public VATS651QueryOpenStatusHandler(VATS651 device)
            : base(device, "i:05", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                if (vatMsg.Data.Substring(_command.Length).Split(':').Length > 0)
                {
                    string result = vatMsg.Data.Substring(_command.Length).Split(':')[1].Substring(0, 1);
                    Device.NoteQueryResult(_command, result);
                }
            }
            return true;
        }
    }

    public class VATS651QuerySensorDelayHandler : VATS651Handler
    {
        //i:02A00
        //i:02A00c
        //c = 0.00…1.00
        public VATS651QuerySensorDelayHandler(VATS651 device)
            : base(device, "i:02A00", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                float.TryParse(result, out float sensorDelay);
            }
            return true;
        }
    }

    public class VATS651QueryRampTimeHandler : VATS651Handler
    {
        //i:02A01
        //i:02A01c
        //c = 0.00…1’000’000.0
        public VATS651QueryRampTimeHandler(VATS651 device)
            : base(device, "i:02A01", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                float.TryParse(result, out float rampTime);
            }
            return true;
        }
    }

    public class VATS651QueryRampModeHandler : VATS651Handler
    {
        //i:02A02
        //i:02A02c
        //c = 0 or 1
        public VATS651QueryRampModeHandler(VATS651 device)
            : base(device, "i:02A02", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                int.TryParse(result, out int rampMode);
            }
            return true;
        }
    }

    public class VATS651QueryGainFactorHandler : VATS651Handler
    {
        //i:02A04
        //i:02A04c
        //c = 0.0001…7.5
        public VATS651QueryGainFactorHandler(VATS651 device)
            : base(device, "i:02A04", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);
                float.TryParse(result, out float gainFactor);
            }
            return true;
        }
    }

    public class VATS651QueryLearnStatusHandler : VATS651Handler
    {
        //i:32
        //i:32abcdefgh
        //a Running 0 = No;  1 = Yes
        //b Data set present; 0 = Ok 1 = No (Learn necessary)
        //c Abortion 0 = Ok, Learn completed;  1 = Abort by user; 2 = Abort by control unit
        //d Open pressure 0 = Ok; 1 = > 50% learn pressure limit (gas flow too high);  2 = < 0 (no gas flow or zero done with gas flow)
        //e Close pressure 0 = OK; 1 = < 10% learn pressure limit (gas flow too low)
        //f Pressure raising 0 = Ok; 1 = pressure not raising during LEARN (gasflow missing)
        //g Pressure stability 0 = OK; 1 = sensor unstable during LEARN
        //h Reserved do not use
        public VATS651QueryLearnStatusHandler(VATS651 device)
            : base(device, "i:32", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var vatMsg = msg as VATS651Message;
                string result = vatMsg.Data.Substring(_command.Length);

                if (result.Length == 8)
                {
                    string info = "";
                    bool isLearning = result[0] == '1';

                    if (result[1] == '0')
                        info += "Data set present;";
                    else
                        info += "Data set not present;";

                    if (result[2] == '0')
                        info += "Learn completed automaticly;";
                    else if (result[2] == '1')
                        info += "Learn abort by user;";
                    else
                        info += "Learn abort control unit;";

                    if (result[3] == '0')
                        info += "Open pressure ok;";
                    else if (result[3] == '1')
                        info += "Open pressure gas flow too high;";
                    else
                        info += "Open pressure no gas flow or zero done with gas flow;";

                    if (result[4] == '0')
                        info += "Close pressure ok;";
                    else
                        info += "Close pressure gas flow too low);";

                    if (result[5] == '0')
                        info += "Pressure raising ok;";
                    else
                        info += "Pressure not raising during LEARN ;";

                    if (result[6] == '0')
                        info += "Pressure stability ok;";
                    else
                        info += "sensor unstable during LEARN;";

                    Device.SetLearnStatus(isLearning, info);
                }

            }
            return true;
        }
    }

    public class VATS651ZeroHandler : VATS651Handler
    {
        //Z:
        //Z:
        public VATS651ZeroHandler(VATS651 device)
            : base(device, "Z:", "")
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

    public class VATS651LearnHandler : VATS651Handler
    {
        //L:0aaaaaaa
        //L:
        public VATS651LearnHandler(VATS651 device, int pressureLimit)
            : base(device, "L:", "0" + pressureLimit.ToString("D7"))
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
