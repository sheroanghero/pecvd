using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MECF.Framework.Common.Communications;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TurboPumps.Pfeiffer
{

    public abstract class PumpMagpowerHandler : HandlerBase
    {
        protected const string QueryDataText = "=?";

        protected PumpMagpower _pump;

        protected PumpMagpowerHandler(PumpMagpower pump, string command, string parameter = null)
            : base(BuildMessage(pump.DeviceAddress, command, parameter))
        {
            _pump = pump;
        }


        private static string BuildMessage(string deviceAddress, string command, string parameter)
        {
            string msg = parameter == null ? $"#{deviceAddress}{command}" : $"#{deviceAddress}{command} {parameter}";

            return msg + "\r";
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

    public class MagpowerPumpStationHandler : PumpMagpowerHandler
    {
        public MagpowerPumpStationHandler(PumpMagpower pump, bool isOn)
            : base(pump, isOn ? "TMPON" : "TMPOFF")
        {
            Name = "Pump " + (isOn ? "On" : "Off");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
            }
            else
            {
                 _pump.NoteOnOff(Name == "Pump On");
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //023
    public class MagpowerSwitchSpeedHandler : PumpMagpowerHandler
    {
        public MagpowerSwitchSpeedHandler(PumpMagpower pump, bool isNormal)
            : base(pump, isNormal ? "NSP" : "SBY")
        {
            Name = "SwitchSpeed " + (isNormal ? "Normal" : "Standby");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
            }
            else
            {
                _pump.NoteNormalSpeed(Name.Contains("Normal"));
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //303
    public class MagpowerQueryErrorHandler : PumpMagpowerHandler
    {
        public MagpowerQueryErrorHandler(PumpMagpower pump)
            : base(pump, "DEF")
        {
            Name = "Query Error";
        }


        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.SetErrorCode(result.Data);
            }
            else
            {
                _pump.SetErrorCode("");
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //398
    public class MagpowerQuerySpeedHandler : PumpMagpowerHandler
    {
        public MagpowerQuerySpeedHandler(PumpMagpower pump)
            : base(pump, "SPD")
        {
            Name = "Query Speed";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
            }
            else
            {
                var dataArray = result.Data.Split(' ');
                if (dataArray.Count() == 2 && int.TryParse(dataArray[0], out int value))
                {
                    _pump.SetSpeed(value);
                }
                else
                {
                    _pump.NoteError(result.Data + "format error");
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //346
    public class MagpowerQueryStatusHandler : PumpMagpowerHandler
    {
        public MagpowerQueryStatusHandler(PumpMagpower pump)
            : base(pump, "STA")
        {
            Name = "Query Status";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //sss, rrrrr, vvv, www, xxx, yyy, zzz, aa, bbbbb, ccc, ddd, ggggggggggggggggggggggggg

            ResponseMessage = msg;
            handled = true;

            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
                return true;
            }
            var dataArray = result.Data.Split(',');
            bool isFormat = dataArray.Count() == 12;

            if (!isFormat)
            {
                _pump.NoteError(result.Data + "format error");
                return true;
            }

            string sss = dataArray[0].Trim();
            isFormat = sss.Length == 3;
            if (!isFormat)
            {
                _pump.NoteError(result.Data + "format error");
                return true;
            }
            int pumpStatus = sss.ElementAt(1);
            _pump.NoteStable((pumpStatus & 0b_0000_1000) > 0);
            _pump.NoteAccelerate((pumpStatus & 0b_0000_0100) > 0);


            int valveStatus = sss.ElementAt(2);
            _pump.SetAtSpeed((valveStatus & 0b_0100_0000) > 0);

            string ccc = dataArray[9].Trim();
            _pump.SetTemperature(int.Parse(ccc));

            return true;
        }
    }


    public class MagpowerQueryOptionHandler : PumpMagpowerHandler
    {
        public MagpowerQueryOptionHandler(PumpMagpower pump)
            : base(pump, "SEL10")
        {
            Name = "Query Status";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //a,u,1,b,r

            ResponseMessage = msg;
            handled = true;

            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
                return true;
            }
            var dataArray = result.Data.Split(',');
            bool isFormat = dataArray.Count() == 5;

            if (!isFormat)
            {
                _pump.NoteError(result.Data + "format error");
                return true;
            }

            string r = dataArray[4].Trim();
            if(r == "1")
            {
                _pump.NoteRemote(true);
            }
            else if(r == "0")
            {
                _pump.NoteRemote(false);
            }
            else
            {
                _pump.NoteError(result.Data + "format error");
            }

            return true;
        }
    }


    public class MagpowerEchoHandler : PumpMagpowerHandler
    {
        public MagpowerEchoHandler(PumpMagpower pump, bool isOn)
            : base(pump, isOn ? "ECHON" : "ECHOFF")
        {
            Name = "Echo " + (isOn ? "On" : "Off");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as MagpowerMessage;
            if (result.IsError)
            {
                _pump.NoteError(result.Data);
            }
            else
            {
                _pump.NoteInfo(Name);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }



}
