using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using static MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxHigh.HighFrequencyRF;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxHigh
{
    public abstract class HighFrequencyRFHandler : HandlerBase
    {
        public HighFrequencyRF Device { get; }

        private string _command;

        protected HighFrequencyRFHandler(HighFrequencyRF device, string command)
            : base(BuildMessage(command))
        {
            Device = device;
            _command = command;
        }

        private static byte[] BuildMessage(string command)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(command);

            return buffer;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HighFrequencyRFMessage response = msg as HighFrequencyRFMessage;
            ResponseMessage = msg;

            SetState(EnumHandlerState.Acked);

            if (response.IsResponse)
            {
                if (response.DataLength >= 1)
                {
                    ParseData(response);
                }

                //SendAck();

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
        protected virtual void ParseData(HighFrequencyRFMessage msg)
        {
            if (msg.Data[0] != 0)
            {
                var reason = TranslateCsrCode(msg.Data[0]);
                Device.NoteError(reason);
            }
        }

        //public void SendAck()
        //{
        //    Device.Connection.SendMessage(new byte[] { 0x06 });
        //}

        private static byte CalcSum(List<byte> data, int length)
        {
            byte ret = 0x00;
            for (var i = 0; i < length; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

        protected string TranslateCsrCode(int csrCode)
        {
            string ret = csrCode.ToString();
            switch (csrCode)
            {
                case 0:
                    ret = null;//"Command accepted";
                    break;
                case 1:
                    ret = "Control Code Is Incorrect";
                    break;
                case 2:
                    ret = "Output Is On(Change Not Allowed)";
                    break;
                case 4:
                    ret = "Data Is Out Of Range";
                    break;
                case 7:
                    ret = "Active Fault(s) Exist";
                    break;
                case 9:
                    ret = "Data Byte Count Is Incorrect";
                    break;
                case 19:
                    ret = "Recipe Is Active(Change Not Allowed)";
                    break;
                case 50:
                    ret = "The Frequency Is Out Of Range";
                    break;
                case 51:
                    ret = "The Duty Cycle Is Out Of Range";
                    break;
                case 53:
                    ret = "The Device Controlled By The Command Is Not Detected";
                    break;
                case 99:
                    ret = "Command Not Accepted(There Is No Such Command)";
                    break;
                default:
                    break;
            }
            return ret;
        }
    }

    //00 ControlMode
    public class HighFrequencyRFSetControlModeHandler : HighFrequencyRFHandler
    {
        public HighFrequencyRFSetControlModeHandler(HighFrequencyRF device, string command)
        : base(device, command)
        {
            Name = "Set control mode";
        }

        protected override void ParseData(HighFrequencyRFMessage response)
        {
            if (response.Data[0] == (byte)'\r')
            {
                //EV.PostInfoLog("PM1", "High RF Power set RS232C mode succeed");
            }
        }
    }


    //02 Setpoint
    public class HighFrequencyRFSetPointHandler : HighFrequencyRFHandler
    {
        public HighFrequencyRFSetPointHandler(HighFrequencyRF device, string command)
        : base(device, command)
        {
            Name = "Set Point";
        }

        protected override void ParseData(HighFrequencyRFMessage response)
        {
            if (response.Data[0] == (byte)'\r')
            {
                //EV.PostInfoLog("PM1", "High RF Power set RS232C mode succeed");
            }
        }
    }

    //public class HighFrequencyRFGetSetPointHandler : HighFrequencyRFHandler
    //{
    //    public HighFrequencyRFGetSetPointHandler(HighFrequencyRF device, string command)
    //    : base(device, command)
    //    {
    //        Name = "Get SetPoint";
    //    }

    //    protected override void ParseData(HighFrequencyRFMessage response)
    //    {
    //        Device.NotePowerSetPoint(response.Data[response.Data.Length - 2]);
    //    }


    //}


    //03 00 off, AA on
    public class HighFrequencyRFSwitchOnOffHandler : HighFrequencyRFHandler
    {
        public HighFrequencyRFSwitchOnOffHandler(HighFrequencyRF device, string command)
            : base(device, command)
        {
            if (command.Contains("ACON"))
                Name = "Switch " + "On";
            else if (command.Contains("ACOFF"))
                Name = "Switch " + "Off";
        }

        protected override void ParseData(HighFrequencyRFMessage response)
        {
            if (response.Data[0] == (byte)'\r')
            {
                //EV.PostInfoLog("PM1", "High RF Power set RS232C mode succeed");
            }
        }
    }

    public class HighFrequencyRFQueryHandler : HighFrequencyRFHandler
    {
        public HighFrequencyRFQueryHandler(HighFrequencyRF device, string command)
        : base(device, command)
        {
            Name = "Qurey Status";
        }

        protected override void ParseData(HighFrequencyRFMessage response)
        {
            string str = Encoding.ASCII.GetString(response.Data);

            if (string.IsNullOrEmpty(str))
            {
                Device.NoteError($"High RF Power no data feedback");
                return;
            }

            string str2 = str.Trim('\r');
            if (str2 == Cmd.ERR_RES)
            {
                Device.NoteError($"High RF Power receive [{str2}]");
                return;
            }

            try
            {
                string pattern = @"(\d{7})\s(\d{5})\s(\d{5})\s(\d{5})\s(\d{5})";

                Match match1 = Regex.Match(str2, pattern);
                if (!match1.Success)
                {
                    if (!SC.GetValue<bool>("System.IsSimulatorMode"))
                    {
                        Device.NoteError($"High RF Power data format incorrect [{str2}]");
                    }

                    return;
                }

                string[] str1 =
                {
                    match1.Groups[1].Value,
                    match1.Groups[2].Value,
                    match1.Groups[3].Value,
                    match1.Groups[4].Value,
                    match1.Groups[5].Value
                };

                this.ParseQueryData(str1);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        private void ParseQueryData(string[] strInfo)
        {
            // Control mode
            string s2 = strInfo[0].Substring(0, 1);
            Device.NoteCommMode((EnumHighRfPowerCommunicationMode)Convert.ToUInt16(s2));

            // output mode
            string s0 = strInfo[0].Substring(1, 1);
            var WorkMode = (EnumRfPowerWorkMode)Convert.ToUInt16(s0);

            // ON/OFF
            char s1 = strInfo[0][2];
            if (s1 == '1')
            {
                Device.NoteStatus(true);
            }
            else if (s1 == '0')
            {
                Device.NoteStatus(false);
            }

            // error code
            string alarm = strInfo[0].Substring(5, 2);
            byte errCode = Convert.ToByte(alarm);
            bool isError = false;

            string code = errCode == 1 ? "Ref Over" :
                errCode == 2 ? "Ref Limit" :
                errCode == 3 ? "Cur Over" :
                errCode == 4 ? "Cur Limit" :
                errCode == 5 ? "Temp Over" :
                errCode == 6 ? "Temp Sensor Short" :
                errCode == 7 ? "Temp Sensor Open" :
                errCode == 8 ? "Sensor Error" :
                errCode == 9 ? "Fwd Power Over" :
                errCode == 10 ? "RF ON Timer" :
                errCode == 11 ? "RS232C error" :
                errCode == 12 ? "Amp Unbalance" :
                errCode == 14 ? "Fan error" :
                errCode == 15 ? "Coolant Error" :
                errCode == 16 ? "Voltage Error" :
                errCode == 17 ? "Fwd Power Down" :
                errCode == 22 ? "PD Over" :
                errCode == 23 ? "PD Limit" :
                errCode == 26 ? "Dew Condensation" :
                errCode == 29 ? "SW Failure" :
                errCode == 99 ? "Safety Lock" : string.Empty;

                if (!string.IsNullOrEmpty(code))
                {
                    //if (errCode == 99)
                    //    EV.PostInfoLog("PM1", "High RF Power: " + code);
                    //else
                    //    EV.PostAlarmLog("PM1", "High RF Power: " + code);

                    isError = true; 
                }

                Device.NoteErrorStatus(isError, code);

            // setpoint power
            //Device.NotePowerSetPoint(Convert.ToUInt64(strInfo[1]));

            // forward power
            Device.NoteForwardPower(Convert.ToUInt64(strInfo[2]));

            // reflect power
            Device.NotedReflectPower(Convert.ToUInt64(strInfo[3]));
        }
    }


}
