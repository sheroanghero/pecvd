using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using Aitex.Core.RT.Device;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.SmcHRZChillers
{
    static class SMCHRZChillerConstant
    {
        public const string Start = ":";
        public const string End = "\r\n";
        public const string SlaveAddress = "01";
        public const string Function_R_M = "17";
        public const string Function_W_S = "17";
        public const string Function_W_M = "17";
        public const string Function_RW_M = "17";
        public const string Data_Address_GetAll = "0000";
        public const string Data_Address_GetTemp = "0000";
        public const string Data_Address_GetFlow = "0001";
        public const string Data_Address_GetPress = "0002";
        public const string Data_Address_GetElec = "0003";
        public const string Data_Address_Status1 = "0004";
        public const string Data_Address_Alarm1 = "0005";
        public const string Data_Address_Alarm2 = "0006";
        public const string Data_Address_SetTemp = "000B";
        public const string Data_Address_SetRun = "000C";
        public const string Data_Address_SetFlow = "000D";

        public const string SET_ON = SlaveAddress + Function_W_S + "0000" + "0000" + Data_Address_SetRun + "0001" + "02" + "0001";
        public const string SET_OFF = SlaveAddress + Function_W_S + "0000" + "0000" + Data_Address_SetRun + "0001" + "02" + "0000";
        public const string SET_TEMP = SlaveAddress + Function_W_S + "0000" + "0000" + Data_Address_SetTemp + "0001" + "02";

        //全部13个寄存器
        public const string GET_ALL = SlaveAddress + Function_R_M + Data_Address_GetAll + "000D" + "0000"+"0000"+"00";
    }

    //Modbus RTU
    public abstract class SmcHRZChillerHandler : HandlerBase
    {
        public SmcHRZChiller Device { get; }

        public string _command;
        protected string _parameter;

        private static byte _address = 0x01;

        protected SmcHRZChillerHandler(SmcHRZChiller device, string command)
            : base(BuildMessage(command))
        {
            Device = device;
            _command = command;
            _address = device.SlaveAddress;
            Name = command;
        }

        protected static string BuildMessage(string command)
        {
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

    public class SmcHRZChillerSetOnOffHandler : SmcHRZChillerHandler
    {
        public SmcHRZChillerSetOnOffHandler(SmcHRZChiller device, bool isOn)
            : base(device, isOn ? SMCHRZChillerConstant.SET_ON : SMCHRZChillerConstant.SET_OFF)
        {
        }
    }

    public class SmcHRZChillerSetTemperatureHandler : SmcHRZChillerHandler
    {
        public SmcHRZChillerSetTemperatureHandler(SmcHRZChiller device, float temp)
            : base(device, BuildCommand(temp))
        {
        }

        private static string BuildCommand(float temp)
        {
            return SMCHRZChillerConstant.SET_TEMP + Convert.ToInt32(temp * 10).ToString("X4");
        }
    }

    //get all status register data
    public class SmcHRZChillerGetStatusHandler : SmcHRZChillerHandler
    {
        public SmcHRZChillerGetStatusHandler(SmcHRZChiller device)
            : base(device, SMCHRZChillerConstant.GET_ALL)
        {
        }

        string[] _statusFlag = new string[]
        {
            "Run",//(1) Run flag 0 = Stop 1 = Run
            //(2) Fault flag 0 = Not occurred 1 = Fault alarm given off
            //(3) Warning flag 0 = Not occurred 1 = Warning alarm given off
            //(4) Flow Unit flag 0 = LPM 1 = GPM
            //(5) Press Unit flag 0 = MPa 1 = PSI
            //(6) Remote flag 0 = Local 1 = SER REMOTE
            //(7) AUTO PURGE ready flag 0 = Condition isn’t formed 1 = Condition is formed
            //(8) AUTO PURGE running flag 0 = Stop 1 = running
            //(9) Time out 0 = Not occurred 1 = Occurred
            //(10)TEMP READY flag 0 = Condition isn’t formed 1 = Condition is formed
            //(11) to (16) reservation Fixed at 0
        };

        string[] _alarm1Flag = new string[]
        {
                "01 Water Leak Detect",// FLT 0=Not occurred, 1=occurred
                "02 Incorrect Phase Error",// FLT 0=Not occurred, 1=occurred
                "03 RFGT High Press FLT",// 0=Not occurred, 1=occurred
                "04 CPRSR Overheat FLT",// 0=Not occurred, 1=occurred
                "05 Reservoir Low Level FLT",// 0=Not occurred, 1=occurred
                "06 Reservoir Low Level WRN",// 0=Not occurred, 1=occurred
                "07 Reservoir High Level WRN",// 0=Not occurred, 1=occurred
                "08 Temp. Fuse Cutout FLT",// 0=Not occurred, 1=occurred
                "09 Reservoir High Temp.",// FLT 0=Not occurred, 1=occurred
                "10 Reservation",// 0=Fixed
                "11 Reservoir High Temp.WRN",//  0=Not occurred, 1=occurred
                "12 Return Low Flow FLT",// 0=Not occurred, 1=occurred
                "13 Return Low Flow WRN",// 0=Not occurred, 1=occurred
                "14 Heater Breaker Trip FLT",// 0=Not occurred, 1=occurred
                "15 Pump Breaker Trip FLT",// 0=Not occurred, 1=occurred
                "16 CPRSR Breaker Trip FLT",// 0=Not occurred, 1=occurred

        };

        string[] _alarm2Flag = new string[]
        {
                "17 Interlock Fuse Cutout FLT",// 0=Not occurred, 1=occurred
                "18 DC Power Fuse Cutout WRN",// 0=Not occurred, 1=occurred
                "19 FAN Motor Stop WRN",// 0=Not occurred, 1=occurred
                "20 Internal Pump Time Out WRN",// 0=Not occurred, 1=occurred
                "21 Controller Error FLT",// 0=Not occurred, 1=occurred
                "22 Memory Data Error FLT",// 0=Not occurred, 1=occurred
                "23 Communication Error WRN",// 0=Not occurred, 1=occurred
                "24* DI Low Level WRN",// 0=Not occurred, 1=occurred
                "25* Pump Inverter Error FLT",// 0=Not occurred, 1=occurred
                "26* DNET Comm. Error WRN",// 0=Not occurred, 1=occurred
                "27* DNET Comm. Error FLT",// 0=Not occurred, 1=occurred
                "28* CPRSR INV Error FLT",// 0=Not occurred, 1=occurred
                "",//29～32 Reservation Fixed at 0
                "",//29～32 Reservation Fixed at 0
                "",//29～32 Reservation Fixed at 0
                "",//29～32 Reservation Fixed at 0
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

            if (cmd.Substring(3, 2) != "17")
            {
                handled = true;
                Device.NoteError("Chiller device response error function code");
                return false;
            }

            string temp = cmd.Substring(7, 4);
            string flow = cmd.Substring(11, 4);
            string pressure = cmd.Substring(15, 4);
            string resistanceRate = cmd.Substring(19, 4);
            string status = cmd.Substring(23, 4);
            string alarm1 = cmd.Substring(27, 4);
            string alarm2 = cmd.Substring(31, 4);

            string tempSet = cmd.Substring(51, 4);
            string cmdSet = cmd.Substring(55, 4);

            int value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(temp.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
            }
            Device.NoteTemp((float)value/10);

            value = 0;
            for (int i = 3; i > -1; i--)
            {
                value += Convert.ToInt32(status.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
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
                if (Convert.ToBoolean((int) Math.Pow(2, i) & value))
                {
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
                    Device.NoteError($"Alarm {16 + i}: {_alarm2Flag[i]}");
                }
            }

            handled = true;
            return true;
        }
    }
}
