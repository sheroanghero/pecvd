using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Aitex.Core.UI.Control;
using MECF.Framework.Common.Utilities;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Chillers
{
    class SimChillerSmcHRZ : SerialPortDeviceSimulator
    {
        Random _rd = new Random();

        private object _locker = new object();

        public bool IsNoResponse { get; internal set; }

        public bool IsRemote { get; internal set; }

        public bool IsRun { get; internal set; }

        public bool IsFault { get; internal set; }

        public string TempSetPoint { get; internal set; }

        public float TempFeedback { get; internal set; }
 
        public SimChillerSmcHRZ(string port)
            : base(port, -1, "\r\n", ' ', true)
        {

        }

        static class Constant
        {
            public const string Start = ":";
            public const string End = "\r\n";
            public const string SlaveAddress = "01";
            public const string Function_R_M = "17";
            public const string Function_W_S = "17";
            public const string Function_W_M = "17";
            public const string Function_RW_M = "17";
            public const string Data_Address_GetTemp = "0000";
            public const string Data_Address_GetPress = "0002";
            public const string Data_Address_GetElec = "0003";
            public const string Data_Address_Status1 = "0004";
            public const string Data_Address_Alarm1 = "0005";
            public const string Data_Address_Alarm2 = "0006";
            public const string Data_Address_Alarm3 = "0007";
            public const string Data_Address_Status2 = "0009";
            public const string Data_Address_SetTemp = "000B";
            public const string Data_Address_SetRun = "000C";

            public const string SET_ON = SlaveAddress + Function_W_S + Data_Address_SetRun + "0001";
            public const string SET_OFF = SlaveAddress + Function_W_S + Data_Address_SetRun + "0000";
            public const string SET_TEMP = SlaveAddress + Function_W_S + Data_Address_SetTemp;
            public const string GET_ALL = SlaveAddress + Function_R_M + Data_Address_GetTemp + "000B";
        }

        protected override void ProcessUnsplitMessage(string message)
        {
            //if (IsFault)
            //{
            //    //OnWriteMessage($"{Constant.SlaveAddress}97");
            //    //return;
            //}

            //读，默认7个全返回
            if (message.Substring(9, 4) != "0000")
            {
                string data = $"{Constant.SlaveAddress}{ Constant.Function_R_M}0E";
                for (int i = 0; i < 7; i++)
                {
                    data += $"0000";
                }
                OnWriteMessage(GetResponseText(data));
            }
            

            //写
        }

        public string GetResponseText(string str)
        {
            return ":01171A00F900000000FFFF006600300040000000000000000000C8000039\r\n";

            //return ":" + str + ModbusUtility.CalculateLrc(ModbusUtility.HexToBytes(str)).ToString("X2") + "\r\n";
        }
 
        internal void SetTemp()
        {
            TempFeedback = float.Parse(TempSetPoint);
        }
 


    }
}
