using MECF.Framework.Common.Utilities;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Chillers
{
    public class SimChillerSmcHRS : SerialPortDeviceSimulator
    {
        Random _rd = new Random();

        private object _locker = new object();

        public bool IsNoResponse { get; internal set; }

        public bool IsRemote { get; internal set; }

        public bool IsRun { get; internal set; }

        public bool IsFault { get; internal set; }

        public string TempSetPoint { get; internal set; }

        public float TempFeedback { get; internal set; }

        public string Data_Alram1 { get; internal set; }
        public string Data_Alram2 { get; internal set; }
        public string Data_Alram3 { get; internal set; }

        public SimChillerSmcHRS(string port)
            : base(port, -1, "\r\n", ' ', true)
        {

        }

        static class Constant
        {
            public const string Start = ":";
            public const string End = "\r\n";
            public const string SlaveAddress = "01";
            public const string Function_R_M = "03";
            public const string Function_W_S = "06";
            public const string Function_W_M = "10";
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
            //if (message.Substring(9, 4) != "0000")
            //{
            //    string data = $"{Constant.SlaveAddress}{ Constant.Function_R_M}0E";
            //    for (int i = 0; i < 7; i++)
            //    {
            //        //data += $"0000";
            //        //data += i.ToString().PadLeft(4,'0');
            //        data += (i+256).ToString().PadLeft(4,'0');
            //    }
            //    OnWriteMessage(GetResponseText(data));
            //}
            if (message.Substring(3, 10) == "030000000D")
            {
                string data = $"{Constant.SlaveAddress}{ Constant.Function_R_M}1A";
                for (int i = 0; i < 13; i++)
                {
                    //data += $"0000";
                    //data += i.ToString().PadLeft(4,'0');
                    //data += (i + 256).ToString().PadLeft(4, '0');
                    if (i == 0)
                    {
                        int temp = (int)(TempFeedback * 10);
                        data += temp.ToString("X4");
                    }

                    else if (i == 4)
                    {
                        data += IsRun ? "1".PadLeft(4, '0') : "0".PadLeft(4, '0');
                    }
                    else if (i == 5)
                    {
                        data += string.IsNullOrEmpty(Data_Alram1) ? "0000" : Data_Alram1;
                    }

                    else if (i == 6)
                    {
                        data += string.IsNullOrEmpty(Data_Alram2) ? "0000" : Data_Alram2;
                    }
                    else if (i == 7)
                    {
                        data += string.IsNullOrEmpty(Data_Alram3) ? "0000" : Data_Alram3;
                    }
                    else if (i == 11)
                    {
                        if (TempSetPoint != null)
                        {
                            data += TempSetPoint;
                        }
                        else
                        {
                            data += "0000";
                        }
                    }
                    else
                    {
                        data += "0000";
                    }
                }
                OnWriteMessage(GetResponseText(data));
            }
            if (message.Substring(3, 6) == "06000B")
            {
                TempSetPoint = message.Substring(9, 4);

                SetTemp();


                string data = $"{Constant.SlaveAddress}{ Constant.Function_W_S}{message.Substring(5, 8)}";
                OnWriteMessage(GetResponseText(data));
            }
            if (message.Substring(3, 6) == "10000B")
            {
                //写入多个寄存器指令
                string registerQuantity = message.Substring(9, 4);

                int readQuantity = Convert.ToInt32(message.Substring(13, 2));

                string data1 = message.Substring(15, readQuantity * 2);

                if (readQuantity == 2)
                {
                    //Set Temprature
                    TempSetPoint = data1.Substring(0, 4);
                    SetTemp();

                }
                if (readQuantity == 4)
                {
                    //Set Temprature
                    TempSetPoint = data1.Substring(0, 4);

                    SetTemp();

                    //Set Run/Stop
                    IsRun = data1.Substring(4, 4) == "0001";
                }


                string data = $"{Constant.SlaveAddress}{ Constant.Function_W_M}{message.Substring(5, 8)}";

                OnWriteMessage(GetResponseText(data));
            }
            if (message.Substring(3, 6) == "10000C")
            {
                //写入多个寄存器指令
                string registerQuantity = message.Substring(9, 4);

                int readQuantity = Convert.ToInt32(message.Substring(13, 2));

                string data1 = message.Substring(15, readQuantity * 2);

                if (readQuantity == 2)
                {
                    //Set Run/Stop
                    IsRun = data1.Substring(0, 4) == "0001";
                }


                string data = $"{Constant.SlaveAddress}{ Constant.Function_W_M}{message.Substring(5, 8)}";

                OnWriteMessage(GetResponseText(data));
            }

            //写
        }

        public string GetResponseText(string str)
        {
            return ":" + str + ModbusUtility.CalculateLrc(ModbusUtility.HexToBytes(str)).ToString("X2") + "\r\n";
        }

        internal void SetTemp()
        {
            int iBuf = 0;
            if (!string.IsNullOrEmpty(TempSetPoint))
            {
                for (int i = 3; i > -1; i--)
                {
                    iBuf += Convert.ToInt32(TempSetPoint.Substring(i, 1), 16) * (int)Math.Pow(16, 3 - i);
                }
                if (iBuf > 1500)
                {
                    iBuf = (~iBuf ^ 0xFFFF) / 10;
                }
                else
                {
                    iBuf = iBuf / 10;
                }
                TempFeedback = iBuf;
            }

        }

        internal void SetAlarm(bool isAlarm)
        {
            if (isAlarm)
            {
                Data_Alram1 = "FFFF";
                Data_Alram2 = "FFFF";
                Data_Alram3 = "FFFF";
            }
            else
            {
                Data_Alram1 = "0000";
                Data_Alram2 = "0000";
                Data_Alram3 = "0000";
            }

        }



    }
}
