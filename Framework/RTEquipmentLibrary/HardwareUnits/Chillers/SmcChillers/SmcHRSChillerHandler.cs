using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.SmcHRSChillers
{
    static class SMCHRSChillerConstant
    {
        public const string Start = ":";
        public const string End = "\r\n";
        public const string SlaveAddress = "01";
        public const string Function_R_M = "03";
        public const string Function_W_S = "06";
        public const string Function_W_M = "10";
        public const string Function_RW_M = "17";
        public const string Data_Address_GetAll = "0000";
        public const string Data_Address_GetTemp = "0000";
        public const string Data_Address_GetFlow = "0001";
        public const string Data_Address_GetPress = "0002";
        public const string Data_Address_GetElec = "0003";
        public const string Data_Address_Status1 = "0004";
        public const string Data_Address_Status2 = "0009";
        public const string Data_Address_Alarm1 = "0005";
        public const string Data_Address_Alarm2 = "0006";
        public const string Data_Address_Alarm3 = "0007";
        public const string Data_Address_SetTemp = "000B";
        public const string Data_Address_SetRun = "000C";
        public const string Data_Address_SetFlow = "000D";

        public const string SET_ON = SlaveAddress + Function_W_M + Data_Address_SetRun + "0001" + "02" + "0001";
        public const string SET_OFF = SlaveAddress + Function_W_M + Data_Address_SetRun + "0001" + "02" + "0000";
        public const string SET_TEMP = SlaveAddress + Function_W_M + Data_Address_SetTemp + "0001" + "02";

        //全部13个寄存器
        public const string GET_ALL = SlaveAddress + Function_R_M + Data_Address_GetAll + "000D";
    }

    //Modbus RTU
    public abstract class SmcHRSChillerHandler : HandlerBase
    {
        public SmcHRSChiller Device { get; }

        public string _command;
        protected string _parameter;

        private static byte _address = 0x01;

        protected SmcHRSChillerHandler(SmcHRSChiller device, string command)
            : base(BuildMessage(command))
        {
            Device = device;
            _command = command;
            _address = device.SlaveAddress;
            Name = command;
        }

        protected static string BuildMessage(string command)
        {
            var temp = ":" + command + ModbusUtility.CalculateLrc(ModbusUtility.HexToBytes(command)).ToString("X2") + "\r\n";
            return ":" + command + ModbusUtility.CalculateLrc(ModbusUtility.HexToBytes(command)).ToString("X2") + "\r\n";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            transactionComplete = true;

            AsciiMessage response = msg as AsciiMessage;
            if (!response.IsComplete)
                return false;
            return true;
        }


    }

    public class SmcHRSChillerSetOnOffHandler : SmcHRSChillerHandler
    {
        public SmcHRSChillerSetOnOffHandler(SmcHRSChiller device, bool isOn)
            : base(device, isOn ? SMCHRSChillerConstant.SET_ON : SMCHRSChillerConstant.SET_OFF)
        {
        }
    }

    public class SmcHRSChillerSetTemperatureHandler : SmcHRSChillerHandler
    {
        public SmcHRSChillerSetTemperatureHandler(SmcHRSChiller device, float temp)
            : base(device, BuildCommand(temp))
        {
        }

        private static string BuildCommand(float temp)
        {
            return SMCHRSChillerConstant.SET_TEMP + Convert.ToInt32(temp * 10).ToString("X4");
        }
    }

    //get all status register data
    public class SmcHRSChillerGetStatusHandler : SmcHRSChillerHandler
    {
        public SmcHRSChillerGetStatusHandler(SmcHRSChiller device)
            : base(device, SMCHRSChillerConstant.GET_ALL)
        {
        }

        string[] _status1Flag = new string[]
        {
            "Run",//(1) Run flag 0 = Stop 1 = Run
            //(2) Fault flag 0 = Not occurred 1 = Fault alarm given off
            //(3) Warning flag 0 = Not occurred 1 = Warning alarm given off
            //(4) Unused
            //(5) Press Unit flag 0 = MPa 1 = PSI
            //(6) Remote flag 0 = Local 1 = SER REMOTE
            //(7) Unused
            //(8) Unused
            //(9) Unused
            //(10)TEMP READY flag 0 = Condition isn’t formed 1 = Condition is formed
            //(11) Temp Unit flag 0 = °C 1 = °F
            //(12) Run timer set status 0= Not set 1= Set
            //(13) Stop timer set status 0= Not set 1= Set
            //(14) Reset after power failure set status 0= Not set 1= Set
            //(15) Anti-freezing set status 0= Not set 1= Set
            //(16) Automatic fluid filling condition 0= Not set 1= Set
        };

        string[] _alarm1Flag = new string[]
        {
                "Low level in tank",// 0=Not occurred, 1=occurred
                "High circulating fluid discharge temp",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge temp. rise",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge temp.",// 0=Not occurred, 1=occurred
                "High circulating fluid return temp.",// 0=Not occurred, 1=occurred
                "High circulating fluid discharge pressure",// 0=Not occurred, 1=occurred
                "Abnormal pump operation",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge pressure rise",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge pressure drop",// 0=Not occurred, 1=occurred
                "High compressor intake temp.",// 0=Fixed
                "Low compressor intake temp.",//  0=Not occurred, 1=occurred
                "Low super heat temperature",// 0=Not occurred, 1=occurred
                "High compressor discharge pressure",// 0=Not occurred, 1=occurred
                "Unused",// 0=Not occurred, 1=occurred
                "Refrigerant circuit pressure (high pressure side) drop",// 0=Not occurred, 1=occurred
                "Refrigerant circuit pressure (low pressure side) rise",// 0=Not occurred, 1=occurred

        };

        string[] _alarm2Flag = new string[]
        {
                "Refrigerant circuit pressure (low pressure side) drop",// 0=Not occurred, 1=occurred
                "Compressor overload",// 0=Not occurred, 1=occurred
                "Communication error",// 0=Not occurred, 1=occurred
                "Memory error",// 0=Not occurred, 1=occurred
                "DC line fuse cut",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge temp. sensor failure",// 0=Not occurred, 1=occurred
                "Circulating fluid return temp. sensor failure",// 0=Not occurred, 1=occurred
                "Compressor intake temp. sensor failure",// 0=Not occurred, 1=occurred
                "Circulating fluid discharge pressure sensor failure",// 0=Not occurred, 1=occurred
                "Compressor discharge pressure sensor failure",// 0=Not occurred, 1=occurred
                "Compressor intake pressure sensor failure",// 0=Not occurred, 1=occurred
                "Maintenance of pump",// 0=Not occurred, 1=occurred
                "Maintenance of fan motor",// 0=Not occurred, 1=occurred
                "Maintenance of compressor",// 0=Not occurred, 1=occurred
                "Contact input 1 signal detection alarm",// 0=Not occurred, 1=occurred
                "Contact input 2 signal detection alarm",// 0=Not occurred, 1=occurred
        };
        string[] _alarm3Flag = new string[]
        {
                "Water leakage",// 0=Not occurred, 1=occurred
                "Electric resistivity/conductivity level rise",// 0=Not occurred, 1=occurred
                "Electric resistivity/conductivity level drop",// 0=Not occurred, 1=occurred
                "Electric resistivity/conductivity sensor error",// 0=Not occurred, 1=occurred
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
                "",//Reservation Fixed at 0
        };

        //: 01 17 1A 00F9 0000 0000 FFFF 0066 0030 0040 0000 0000 0000 0000 00C8 0000 39
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            AsciiMessage response = msg as AsciiMessage;
            string cmd = response.RawMessage;

            if (cmd.Length < 61)
            {
                handled = true;
                Device.NoteError("Chiller device response error format message, should at least 60 return data length");
                return false;
            }

            if (cmd.Substring(5, 2) != "1A")
            {
                handled = true;
                Device.NoteError("Chiller device response error format message, register number should be 1A (26)");
                return false;
            }

            if (cmd.Substring(3, 2) != "03")
            {
                handled = true;
                Device.NoteError("Chiller device response error function code");
                return false;
            }

            string temp = cmd.Substring(7, 4);
            //string flow = cmd.Substring(11, 4);
            string pressure = cmd.Substring(15, 4);
            string resistanceRate = cmd.Substring(19, 4);
            string status1 = cmd.Substring(23, 4);
            string alarm1 = cmd.Substring(27, 4);
            string alarm2 = cmd.Substring(31, 4);
            string alarm3 = cmd.Substring(35, 4);
            string status2 = cmd.Substring(43, 4);

            string tempSet = cmd.Substring(51, 4);
            string cmdSet = cmd.Substring(55, 4);

            int value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(temp.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            Device.NoteTemp((float)value / 10);

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(tempSet.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            Device.NoteTempSetpoint((float)value / 10);

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(status1.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            bool IsRun = Convert.ToBoolean((int)Math.Pow(2, 0) & value);
            bool IsFault = Convert.ToBoolean((int)Math.Pow(2, 1) & value);
            bool IsWarning = Convert.ToBoolean((int)Math.Pow(2, 2) & value);
            bool FlowUnit = Convert.ToBoolean((int)Math.Pow(2, 3) & value);
            bool PressUnit = Convert.ToBoolean((int)Math.Pow(2, 4) & value);
            bool IsRemote = Convert.ToBoolean((int)Math.Pow(2, 5) & value);
            bool IsTimeout = Convert.ToBoolean((int)Math.Pow(2, 8) & value);
            bool IsTempReady = Convert.ToBoolean((int)Math.Pow(2, 9) & value);

            Device.NoteIsRun(IsRun);
            Device.NoteIsFault(IsFault);
            Device.NoteIsWarning(IsWarning);
            Device.NoteFlowUnit(FlowUnit);
            Device.NotePressUnit(PressUnit);
            Device.NoteIsRemote(IsRemote);
            Device.NoteIsTimeout(IsTimeout);
            Device.NoteIsTempReady(IsTempReady);

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(alarm1.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            for (int i = 0; i < 16; i++)
            {
                if (Convert.ToBoolean((int)Math.Pow(2, i) & value))
                {
                    Device.IsCH1Alarm = true;
                    Device.NoteError($"Alarm {_alarm1Flag[i]}");
                }
            }

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(alarm2.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            for (int i = 0; i < 16; i++)
            {
                if (Convert.ToBoolean((int)Math.Pow(2, i) & value))
                {
                    Device.IsCH1Alarm = true;
                    Device.NoteError($"Alarm {16 + i}: {_alarm2Flag[i]}");
                }
            }

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(alarm3.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            for (int i = 0; i < 16; i++)
            {
                if (Convert.ToBoolean((int)Math.Pow(2, i) & value))
                {
                    Device.IsCH1Alarm = true;
                    Device.NoteError($"Alarm {16 + i}: {_alarm3Flag[i]}");
                }
            }

            handled = true;
            return true;
        }
    }
}
