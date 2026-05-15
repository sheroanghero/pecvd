using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using MECF.Framework.Common.Communications;
using static MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxLow.LowFrequencyRF;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AdTecTxLow
{
    public abstract class LowFrequencyRFHandler : HandlerBase
    {
        public LowFrequencyRF Device { get; }

        private byte[] _functionCode;
        private byte[] _executionCode;
        private byte[] _parameter;

        protected LowFrequencyRFHandler(LowFrequencyRF device, byte[] functionCode, byte[] executionCode, byte[] parameter)
            : base(BuildMessage(functionCode, executionCode, parameter))
        {
            Device = device;
            _functionCode = functionCode;
            _executionCode = executionCode;
            _parameter = parameter;
        }

        private static byte _header = 0x40;
        private static byte _command = 0x55;
        private static byte _stopCR = 0x0D;

        private static byte[] BuildMessage(byte[] functionCode, byte[] executionCode, byte[] parameter)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_header);
            buffer.Add(_command);

            if (functionCode != null && functionCode.Length > 0)
            {
                buffer.AddRange(functionCode);
            }

            if (executionCode != null && executionCode.Length > 0)
            {
                buffer.AddRange(executionCode);
            }

            if (parameter != null && parameter.Length > 0)
            {
                buffer.AddRange(parameter);
            }

            buffer.Add(_stopCR);

            return buffer.ToArray();
        }


        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            LowFrequencyRFMessage response = msg as LowFrequencyRFMessage;
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
        protected virtual void ParseData(LowFrequencyRFMessage msg)
        {
            if (msg.Data[0] != 0)
            {
                var reason = TranslateCsrCode(msg.Data[0]);
                //Device.NoteError(reason);
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
    public class LowFrequencyRFSetControlModeHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFSetControlModeHandler(LowFrequencyRF device, byte mode)
        : base(device, BuildPara('0', '0'), BuildPara('0', '0'), BuildData(mode))
        {
            Name = "Set control mode";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        private static byte[] BuildData(byte mode)
        {
            return mode == 0x01 ? BuildPara('0', '1') : BuildPara('0', '2');
        }
    }

    public class LowFrequencyRFGetControlModeHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFGetControlModeHandler(LowFrequencyRF device)
        : base(device, BuildPara('0', '0'), BuildPara('0', '1'), null)
        {
            Name = "Get control power";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        protected override void ParseData(LowFrequencyRFMessage response)
        {
            if (response.DataLength != 9)
            {
                Device.NoteError($"query alarm status, return data length {response.DataLength}");
            }
            else
            {
                EnumLowRfPowerCommunicationMode mode = EnumLowRfPowerCommunicationMode.Manual;

                if (response.Data[6] == (byte)'0' && response.Data[7] == (byte)'1')
                {
                    mode = EnumLowRfPowerCommunicationMode.Manual;
                }
                else if (response.Data[6] == (byte)'0' && response.Data[7] == (byte)'2')
                {
                    mode = EnumLowRfPowerCommunicationMode.RS232C;
                }

                Device.NoteCommMode(mode);
            }
        }
    } 


    //01 SetPoint Limit
    public class LowFrequencyRFSetPointLimitHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFSetPointLimitHandler(LowFrequencyRF device, int point)
        : base(device, BuildPara('0', '1'), BuildPara('0', '0'), BuildData(point))
        {
            Name = "SetPoint Limit";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        private static byte[] BuildData(int point)
        {
            string str = point.ToString("X4");

            return new byte[4] { (byte)str[0], (byte)str[1], (byte)str[2], (byte)str[3] };
        }
    }

    //02 Setpoint
    public class LowFrequencyRFSetPointHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFSetPointHandler(LowFrequencyRF device, int point)
        : base(device, BuildPara('0', '2'), BuildPara('0', '0'), BuildData(point))
        {
            Name = "Set Point";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        private static byte[] BuildData(int point)
        {
            string str = point.ToString("X4");

            return new byte[4] { (byte)str[0], (byte)str[1], (byte)str[2], (byte)str[3] };
        }
    }

    public class LowFrequencyRFGetSetPointHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFGetSetPointHandler(LowFrequencyRF device)
        : base(device, BuildPara('0', '2'), BuildPara('0', '1'), null)
        {
            Name = "Get SetPoint";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        protected override void ParseData(LowFrequencyRFMessage response)
        {
            int value = GetData(response.Data[6], response.Data[7], response.Data[8], response.Data[9]);

            Device.NotePowerSetPoint(value);
        }

        private static int GetData(byte b1, byte b2, byte b3, byte b4)
        {
            byte[] byteData = new byte[4] { b1, b2, b3, b4 };
            string str = Encoding.ASCII.GetString(byteData);

            int value = Convert.ToInt32(str, 16);
            return value;
        }
    }


    //03 00 off, AA on
    public class LowFrequencyRFSwitchOnOffHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFSwitchOnOffHandler(LowFrequencyRF device, bool isOn)
            : base(device, BuildPara('0', '3'), BuildPara('0', '0'), BuildData(isOn))
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        private static byte[] BuildData(bool isOn)
        {
            return isOn ? BuildPara('A', 'A') : BuildPara('0', '0');
        }
    }

    public class LowFrequencyRFGetSwitchOnOffHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFGetSwitchOnOffHandler(LowFrequencyRF device)
        : base(device, BuildPara('0', '3'), BuildPara('0', '1'), null)
        {
            Name = "Get Switch Status";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        protected override void ParseData(LowFrequencyRFMessage response)
        {
            if (response.DataLength != 9)
            {
                Device.NoteError($"query swith status, return data length {response.DataLength}");
            }
            else
            {
                bool isOn = false;

                if (response.Data[6] == (byte)'0' && response.Data[7] == (byte)'0')
                {
                    isOn = false;
                }
                else if (response.Data[6] == (byte)'A' && response.Data[7] == (byte)'A')
                {
                    isOn = true;
                }

                Device.NoteStatus(isOn);
            }
        }
    }

    //80 AlarmReset
    public class LowFrequencyRFAlarmResetHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFAlarmResetHandler(LowFrequencyRF device)
            : base(device, BuildPara('8', '0'), BuildPara('0', '0'), BuildPara('A', 'A'))
        {
            Name = "reset alarm" ;
        }
        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }
    }

    public class LowFrequencyRFGetAlarmStatusHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFGetAlarmStatusHandler(LowFrequencyRF device)
            : base(device, BuildPara('8', '0'), BuildPara('0', '1'), null)
        {
            Name = "get alarm status";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        protected override void ParseData(LowFrequencyRFMessage response)
        {
            if (response.DataLength != 15)
            {
                Device.NoteError($"query alarm status, return data length {response.DataLength}");
            }
            else
            {
                byte[] byteData = new byte[8] {response.Data[6], response.Data[7], response.Data[8], response.Data[9],
                response.Data[10], response.Data[11], response.Data[12], response.Data[13] };
                string errorCode = Encoding.ASCII.GetString(byteData);
                bool isError = false;

                if (errorCode != "00000000")
                    isError = true;

                Device.NoteErrorStatus(isError, errorCode);
            }
        }
    }

    //81 Waring Status
    public class LowFrequencyRFWaringStatusHandler : LowFrequencyRFHandler
    {
        public LowFrequencyRFWaringStatusHandler(LowFrequencyRF device)
            : base(device, BuildPara('8', '1'), BuildPara('0', '1'), null)
        {
            Name = "Waring Status";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }
    }

    //90 Actual Forward and Refected
    public class LowFrequencyGetForwardAndRefectedfHandler : LowFrequencyRFHandler
    {
        public LowFrequencyGetForwardAndRefectedfHandler(LowFrequencyRF device)
        : base(device, BuildPara('9', '0'), BuildPara('0', '3'), null)
        {
            Name = "Get Forward And Refected Power";
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        protected override void ParseData(LowFrequencyRFMessage response)
        {
            if (response.DataLength != 15)
            {
                Device.NoteError($"query forward and refected power, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteForwardPowerAndReflectPower(response.Data);
            }
        }
    }





}
