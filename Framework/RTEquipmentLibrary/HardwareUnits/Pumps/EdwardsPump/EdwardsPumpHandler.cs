using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump
{
    public abstract class EdwardsPumpHandler : HandlerBase
    {
        public EdwardsPump Device { get; }

        public string _command;
        protected string _parameter;


        protected EdwardsPumpHandler(EdwardsPump device, string command,string parameter)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        private static string _endLine = "\r";
        private static string BuildMessage(string command, string parameter)
        {
            if(string.IsNullOrEmpty(parameter))
                return command + _endLine;
            else
                return command + parameter + _endLine;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            EdwardsPumpMessage response = msg as EdwardsPumpMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (msg.IsError)
                {
                    Device.NoteError($"Command '{_command}' Error: {response.Data}:{ErrorString(response.ErrorText)}");
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    handled = true;
                    Device.NoteError(null);
                    return true;
                }
            }

            handled = false;
            return false;
        }

        private static Dictionary<string, string> ErrorDict = new Dictionary<string, string>()
        {
            {"1","Invalid message" },
            {"2","Number not found" },
            {"3","Number Invalid" },
            {"4","Parameter’s value not received" },
            {"5","Command not possible" }
        };
        private static string ErrorString(string errorCode)
        {
            if (ErrorDict.ContainsKey(errorCode))
                return ErrorDict[errorCode];
            else
                return "NotDefined error";
        }

    }

    public class EdwardsPumpRawCommandHandler : EdwardsPumpHandler
    {
        public EdwardsPumpRawCommandHandler(EdwardsPump device, string command, string parameter)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPumpMessage;
                Device.NoteRawCommandInfo(_command, result.RawMessage);
            }
            return true;
        }


    }

    public class EdwardsPumpSimpleSwitchHandler : EdwardsPumpHandler
    {
        public EdwardsPumpSimpleSwitchHandler(EdwardsPump device, string command, string parameter)
            : base(device, command, parameter)
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted();
            }
            return true;
        }
    }

    public class PfeifferPumpSetPumpParameterHandler : EdwardsPumpHandler
    {
        public PfeifferPumpSetPumpParameterHandler(EdwardsPump device, string parameter)
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

    public class EdwardsPumpReadAlarmStatusHandler : EdwardsPumpHandler
    {
        public EdwardsPumpReadAlarmStatusHandler(EdwardsPump device, string parameter)
            : base(device, "?A", parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPumpMessage;

                Device.NoteAlarmStatus(_parameter, result.Data);
            }
            return true;

        }

    }

    public class EdwardsQueryPumpStatusHandler : EdwardsPumpHandler
    {
        public EdwardsQueryPumpStatusHandler(EdwardsPump pump)
            : base(pump, "?P","")
        {
            Name = "Query Pump Status";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                EdwardsPumpMessage response = msg as EdwardsPumpMessage;
                var msgArry = response.Data.Split(',');
                if(msgArry.Length == 7)
                {
                    //0=Switched off
                    //1=Off, switching on
                    //2=On, switching off (shut down after fault)
                    //3=On, switching off (normal shut down)
                    //4=on
                    var statusLevel = msgArry[0];
                    if (statusLevel == "4")
                        Device.IsOn = true;
                    else
                        Device.IsOn = false;

                    //0=Indication only; no warning or alarm.
                    //1=Warning condition exists
                    //2=Alarm condition exists: shut down the pump unless run til crash is set.
                    //3=Alarm condition exists: shut down the pump.
                    var priorityLevel = msgArry[1];
                    //if (priorityLevel == "0")
                    //    Device.IsError = false;
                    //else
                    //    Device.IsError = true;

                    //0=No alarm
                    //1= Digital alarm
                    //9= Low warning
                    //10=Low alarm
                    //11=High warning
                    //12=High alarm
                    //13=Device error
                    //14=Device not present
                    var alarmType = msgArry[2];
                    if (alarmType == "0" && priorityLevel == "0")
                        Device.IsError = false;
                    else
                        Device.IsError = true;

                    //0=Module missing
                    //1=Sensor present at switch-on, but now disconnected
                    //2=Wrong gas module fitted
                    //3=Voltage above valid maximum voltage
                    //4=Voltage below valid minimum voltage
                    //5=ADC (analogue to digital convertor) not operating
                    //6=Electrical supply has been interrupted
                    //7=Watchdog reset has occurred
                    //8=Sensor missing at switch on
                    //9=Module switching on
                    //10=No current consumption at pump switch-on
                    //11=Wrong phase input to pump
                    //12=EMS (emergency stop) has been activated
                    //13=Flow sensor zero out of range
                    //14=Cannot zero sensors
                    //15=Configuration set read error
                    var bitfieldStatus = msgArry[3];

                    //1=run til crash is selected
                    //0=run til crash is not selected
                    var runTilCrashStatus = msgArry[4];

                    //1=On process flag is set
                    //0=On process flag is not set
                    var onProcessStatus = msgArry[5];

                    //0=No module has control
                    //91=Single Pumpset Monitor has control
                    //101=Pump Display Module has control
                    //102=Remote Display has control
                    //121=Parallel (tool) interface has control
                    //181=Serial interface has control
                    var controlObject = msgArry[6];
                }
                if(msgArry.Length == 1)
                {
                    var statusLevel = msgArry[0];
                }
                Device.NoteSetParaCompleted();
            }
            return true;
        }
    }

    public class EdwardsExitSimulationModeHandler : EdwardsPumpHandler
    {
        public EdwardsExitSimulationModeHandler(EdwardsPump pump)
               : base(pump, "!M0", "")
        {
            Name = "Command Simulation Mode";
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
    public class EdwardsEnterSimulationModeHandler : EdwardsPumpHandler
    {
        public EdwardsEnterSimulationModeHandler(EdwardsPump pump)
               : base(pump, "!M1", "")
        {
            Name = "Command Simulation Mode";
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

    public class EdwardsFormatModeHandler : EdwardsPumpHandler
    {
        public EdwardsFormatModeHandler(EdwardsPump pump, bool isLongReplay)
               : base(pump, $"!F{(isLongReplay ? "1" : "0")}", "")
        {
            Name = "Format mode";
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

    public class EdwardsGetControlHandler : EdwardsPumpHandler
    {
        public EdwardsGetControlHandler(EdwardsPump pump)
               : base(pump, "!C1", "")
        {
            Name = "Get control";
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

    public class EdwardsSwitchOnPumpHandler : EdwardsPumpHandler
    {
        public EdwardsSwitchOnPumpHandler(EdwardsPump pump)
               : base(pump, "!P1", "")
        {
            Name = "Switch On Pump";
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

    public class EdwardsSwitchOffPumpHandler : EdwardsPumpHandler
    {
        public EdwardsSwitchOffPumpHandler(EdwardsPump pump)
               : base(pump, "!P0", "")
        {
            Name = "Switch Off Pump";
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

    public class EdwardsFastSwitchOffPumpHandler : EdwardsPumpHandler
    {
        public EdwardsFastSwitchOffPumpHandler(EdwardsPump pump)
               : base(pump, "!P2", "")
        {
            Name = "Switch Off Pump";
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

}

