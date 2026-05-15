using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Serens
{
    public abstract class SerenRfPowerHandler : HandlerBase
    {
        public SerenRfPower Device { get; }

        private string _command;

        protected SerenRfPowerHandler(SerenRfPower device, string command)
            : base($"{command}\r")
        {
            Device = device;
            _command = command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SerenRfPowerMessage response = msg as SerenRfPowerMessage;
            ResponseMessage = msg;

            if (response.RawMessage.Length >= 1)
            {
                ParseData(response);
            }

            SetState(EnumHandlerState.Acked);
            SetState(EnumHandlerState.Completed);

            transactionComplete = true;
            return true;
        }

        protected virtual void ParseData(SerenRfPowerMessage msg)
        {
 
        }
    }


    public class SerenRfPowerSetEchoHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetEchoHandler(SerenRfPower device, bool isOn)
            : base(device, isOn ? "ECHO" : "NOECHO")
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }

    public class SerenRfPowerSwitchOnOffHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSwitchOnOffHandler(SerenRfPower device, bool isOn)
            : base(device, isOn ? "G":"S")
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }


    //3 regulation mode
    public class SerenRfPowerSetRegulationModeHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetRegulationModeHandler(SerenRfPower device, byte address, EnumRfPowerRegulationMode mode)
            : base(device, "")
        {
            Name = "set regulation mode";
        }

        private static byte GetMode(EnumRfPowerRegulationMode mode)
        {
            switch (mode)
            {
                case EnumRfPowerRegulationMode.DcBias:
                    return 8;
                case EnumRfPowerRegulationMode.Forward:
                    return 6;
                case EnumRfPowerRegulationMode.Load:
                    return 7;
                case EnumRfPowerRegulationMode.VALimit:
                    return 9;
            }

            return 0;
        }
    }

    //8 set power
    public class SerenRfPowerSetPowerHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetPowerHandler(SerenRfPower device, int power)
            : base(device, power==0 ? $"WS": $"{power} WG")
        {
            Name = "set power";
        }
    }

    //14 set communication mode
    public class SerenRfPowerSetCommModeHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetCommModeHandler(SerenRfPower device, byte address, EnumRfPowerCommunicationMode mode)
            : base(device, "")
        {
            Name = "set communication mode";
        }

        private static byte[] BuildData(EnumRfPowerCommunicationMode mode)
        {
            byte value = 0;
            switch (mode)
            {
                case EnumRfPowerCommunicationMode.DeviceNet:
                    value = 16;
                    break;
                case EnumRfPowerCommunicationMode.Diagnostic:
                    value = 8;
                    break;
                case EnumRfPowerCommunicationMode.EtherCat32:
                    value = 32;
                    break;
                case EnumRfPowerCommunicationMode.Host:
                    value = 2;
                    break;
                case EnumRfPowerCommunicationMode.UserPort:
                    value = 4;
                    break;
            }
            return new byte[] { value };
        }
    }

    //13 set caps control mode
    public class SerenRfPowerSetCapsCtrlModeHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetCapsCtrlModeHandler(SerenRfPower device, byte address, byte mode)
            : base(device, "")
        {
            Name = "set caps control mode";
        }

        private static byte[] BuildData(byte mode)
        {
            return new byte[] { mode == 0x01 ? (byte)0x01 : (byte)0x00 };
        }
    }
    //112 set load
    public class SerenRfPowerSetLoadHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetLoadHandler(SerenRfPower device, byte address, int data)
            : base(device, "")
        {
            Name = "set load";
        }

        private static byte[] BuildData(int data)
        {
            return new byte[] { (byte)data, (byte)(data >> 8) };
        }
    }
    //122 set tune
    public class SerenRfPowerSetTuneHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetTuneHandler(SerenRfPower device, byte address, int data)
            : base(device, "")
        {
            Name = "set tune";
        }

        private static byte[] BuildData(int data)
        {
            return new byte[] { (byte)data, (byte)(data >> 8) };
        }
    }

    //155 query comm mode 
    public class SerenRfPowerQueryCommModeHandler : SerenRfPowerHandler
    {
        public SerenRfPowerQueryCommModeHandler(SerenRfPower device, byte address)
            : base(device, "")
        {
            Name = "query comm mode";
        }
        protected override void ParseData(SerenRfPowerMessage response)
        {
 
            {
                EnumRfPowerCommunicationMode mode = EnumRfPowerCommunicationMode.Undefined;
                //switch (response.Data[0])
                //{
                //    case 2:
                //        mode = EnumRfPowerCommunicationMode.Host;
                //        break;
                //    case 4:
                //        mode = EnumRfPowerCommunicationMode.UserPort;
                //        break;
                //    case 8:
                //        mode = EnumRfPowerCommunicationMode.Diagnostic;
                //        break;
                //    case 16:
                //        mode = EnumRfPowerCommunicationMode.DeviceNet;
                //        break;
                //    case 32:
                //        mode = EnumRfPowerCommunicationMode.EtherCat32;
                //        break;
                //}

                Device.NoteCommMode(mode);
            }
        }
    }

    //162 query status 
    public class SerenRfPowerQueryStatusHandler : SerenRfPowerHandler
    {
        public SerenRfPowerQueryStatusHandler(SerenRfPower device)
            : base(device, "Q")
        {
            Name = "query status";
        }

        //Response: XXXXXXX_aaaa_bbbbb_cccc_ddddd<cr>
        //2320000 0 0 0 1000
        protected override void ParseData(SerenRfPowerMessage response)
        {
            string[] splitData = response.RawMessage.TrimEnd('\r').Split(' ');
            if (splitData.Length != 5)
            {
                Device.NoteError($"{Name}, return data {response.RawMessage} is not valid");
            }
            else
            {
                //Device.ControlSource = splitData[0];
                if(splitData[0].Length == 7)
                {
                    int status0 = (int)splitData[0][0];
                    int status1 = (int)splitData[0][1];
                    int status2 = (int)splitData[0][2];
                    int status3 = (int)splitData[0][3];
                    int status4 = (int)splitData[0][4];
                    int status5 = (int)splitData[0][5];
                    int status6 = (int)splitData[0][6];
                    Device.NoteStatus((status3 & 0x08) == 0x08, 
                        (status5 & 0x08) == 0x08 || (status5 & 0x04) == 0x04 || (status5 & 0x01) == 0x01,
                        (status5 & 0x02) == 0x02);
                }

                Device.NotePowerSetPoint(int.Parse(splitData[1]));

                Device.NoteForwardPower(int.Parse(splitData[2]));

                Device.NoteReflectPower( int.Parse(splitData[3]));

            }
        }
    }
    //164 query setpoint 
    public class SerenRfPowerQuerySetPointHandler : SerenRfPowerHandler
    {
        public SerenRfPowerQuerySetPointHandler(SerenRfPower device, byte address)
            : base(device, "")
        {
            Name = "query setpoint";
        }
        protected override void ParseData(SerenRfPowerMessage response)
        {
            //if (response.DataLength != 3)
            //{
            //    Device.NoteError($"{Name}, return data length {response.DataLength}");
            //}
            //else
            //{
            //    EnumRfPowerRegulationMode regMode = EnumRfPowerRegulationMode.Undefined;
            //    switch (response.Data[2])
            //    {
            //        case 6:
            //            regMode = EnumRfPowerRegulationMode.Forward;
            //            break;
            //        case 7:
            //            regMode = EnumRfPowerRegulationMode.Load;
            //            break;
            //        case 8:
            //            regMode = EnumRfPowerRegulationMode.DcBias;
            //            break;
            //        case 9:
            //            regMode = EnumRfPowerRegulationMode.VALimit;
            //            break;
            //    }

            //    Device.NoteRegulationModeSetPoint(regMode);
            //    Device.NotePowerSetPoint(response.Data[0] + (response.Data[1] << 8));
            //}
        }
    }
    //165 forward power
    public class SerenRfPowerQueryForwardPowerHandler : SerenRfPowerHandler
    {
        public SerenRfPowerQueryForwardPowerHandler(SerenRfPower device, byte address)
            : base(device, "")
        {
            Name = "Query forward power";
        }

        protected override void ParseData(SerenRfPowerMessage response)
        {
 
        }
    }

    //166 reflect power
    public class SerenRfPowerQueryReflectPowerHandler : SerenRfPowerHandler
    {
        public SerenRfPowerQueryReflectPowerHandler(SerenRfPower device, byte address)
            : base(device, "")
        {
            Name = "Query reflect power";
        }

        protected override void ParseData(SerenRfPowerMessage response)
        {
 
        }
    }


    //set serial control mode
    public class SerenRfPowerSetSerialModeHandler : SerenRfPowerHandler
    {
        public SerenRfPowerSetSerialModeHandler(SerenRfPower device)
            : base(device, "***")
        {
            Name = "Set Serial Control Mode";
        }

 
    }
}
