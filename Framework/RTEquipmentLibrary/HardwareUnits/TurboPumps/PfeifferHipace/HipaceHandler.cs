using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.PfeifferHipace
{
    public abstract class HipaceHandler : HandlerBase
    {
        protected const string QueryDataText = "=?";

        public HipaceTurboPump PumpDevice { get; }

        protected HipaceHandler(HipaceTurboPump pump, string deviceAddress, bool isQuery, string parameter, string data)
            : base(BuildMessage(deviceAddress, isQuery, parameter, data))
        {
            PumpDevice = pump;
        }

        private static string BuildMessage(string deviceAddress, bool isQuery, string parameter, string data)
        {
            string msg = deviceAddress + (isQuery ? "00" : "10") + parameter + data.Length.ToString("D2") + data;

            return msg + CalcSum(msg).ToString("D3") + "\r";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }
 
        private static int CalcSum(string value)
        {
            int sum = 0;
            foreach (var item in value)
            {
                sum += (int)item;
            }

            return sum % 256;
        }
    }

    //010
    public class HipacePumpStationHandler : HipaceHandler
    {
        public HipacePumpStationHandler(HipaceTurboPump pump, string deviceAddress, bool isOn, bool isQuery)
            : base(pump, deviceAddress, isQuery, "010", isQuery ? QueryDataText : (isOn ? "111111" : "000000"))
        {
            Name = "Pump " + (isOn ? "On" : "Off");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (result.Data == "111111")
                {
                    PumpDevice.NoteOnOff(true);
                }
                else if (result.Data == "000000")
                {
                    PumpDevice.NoteOnOff(false);
                }
                else
                {
                    PumpDevice.SetError(result.Data + " format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
 
    //023
    public class HipaceMotorPumpHandler : HipaceHandler
    {
        public HipaceMotorPumpHandler(HipaceTurboPump pump, string deviceAddress, bool isOn)
            : base(pump, deviceAddress, false, "023", isOn ? "111111" : "000000")
        {

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (result.Data == "111111")
                {
                    PumpDevice.NoteOnOff(true);
                }
                else if (result.Data == "000000")
                {
                    PumpDevice.NoteOnOff(false);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //303
    public class HipaceQueryErrorHandler : HipaceHandler
    {
        public HipaceQueryErrorHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "303", QueryDataText)
        {
            Name = "Query Error";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (int.TryParse(result.Data, out int code))
                {
                    PumpDevice.SetErrorCode(code);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //398
    public class HipaceQuerySpeedHandler : HipaceHandler
    {
        public HipaceQuerySpeedHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "398", QueryDataText)
        {
            Name = "Query Speed";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (int.TryParse(result.Data, out int value))
                {
                    PumpDevice.SetSpeed(value);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //346
    public class HipaceQueryTemperatureHandler : HipaceHandler
    {
        public HipaceQueryTemperatureHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "346", QueryDataText)
        {
            Name = "Query Temperature";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (int.TryParse(result.Data, out int value))
                {
                    PumpDevice.SetTemperature(value);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }


    //307
    public class HipaceAccelerateHandler : HipaceHandler
    {
        public HipaceAccelerateHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "307", QueryDataText)
        {
            Name = "Query Accelerate";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (result.Data == "111111")
                {
                    PumpDevice.SetIsAccelerate(true);
                }
                else if (result.Data == "000000")
                {
                    PumpDevice.SetIsAccelerate(false);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //306
    public class HipaceAtSpeedHandler : HipaceHandler
    {
        public HipaceAtSpeedHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "306", QueryDataText)
        {
            Name = "Query At Speed";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (result.Data == "111111")
                {
                    PumpDevice.SetAtSpeed(true);
                }
                else if (result.Data == "000000")
                {
                    PumpDevice.SetAtSpeed(false);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //305
    public class HipaceOverTempHandler : HipaceHandler
    {
        public HipaceOverTempHandler(HipaceTurboPump pump, string deviceAddress)
            : base(pump, deviceAddress, true, "305", QueryDataText)
        {
            Name = "Query over temp";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HipaceMessage;
            if (result.IsError)
            {
                PumpDevice.SetError(result.Data);
            }
            else
            {
                if (result.Data == "111111")
                {
                    PumpDevice.SetOverTemp(true);
                }
                else if (result.Data == "000000")
                {
                    PumpDevice.SetOverTemp(false);
                }
                else
                {
                    PumpDevice.SetError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
}
