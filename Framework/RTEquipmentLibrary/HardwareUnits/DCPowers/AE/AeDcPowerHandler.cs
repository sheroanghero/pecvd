using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.DCPowers.AE
{
    public abstract class AeDcPowerHandler : HandlerBase
    {
        public AeDcPower Device { get; }

        private byte _address;
        private byte _command;

        private static int ackTimeout = 2;

        protected AeDcPowerHandler(AeDcPower device, byte address, byte command, byte[] data)
            : base(BuildMessage(address, command, data), ackTimeout)
        {
            Device = device;
            _address = address;
            _command = command;
        }

        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add((byte)((address << 3) + (data == null ? 0 : data.Length)));

            buffer.Add(command);

            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer, buffer.Count));

            return buffer.ToArray();
        }

        //host->unit, unit->host(ack), unit->host(csr), host->unit(ack)
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            AeRfPowerMessage response = msg as AeRfPowerMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (response.Address != _address || response.CommandNumber != _command)
            {
                transactionComplete = false;
                return false;
            }

            if (response.IsResponse)
            {
                if (response.DataLength >= 1)
                {
                    ParseData(response);
                }

                SendAck();

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected virtual void ParseData(AeRfPowerMessage msg)
        {
            if (msg.Data[0] != 0)
            {
                var reason = TranslateCsrCode(msg.Data[0]);
                Device.NoteError(reason);
            }
        }

        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }

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


    //1 off, 2 on
    public class AeRfPowerSwitchOnOffHandler : AeDcPowerHandler
    {
        public AeRfPowerSwitchOnOffHandler(AeDcPower device, byte address, bool isOn)
            : base(device, address, isOn ? (byte)0x02 : (byte)0x01, null)
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }


    //3 regulation mode
    public class AeRfPowerSetRegulationModeHandler : AeDcPowerHandler
    {
        public AeRfPowerSetRegulationModeHandler(AeDcPower device, byte address, EnumRfPowerRegulationMode mode)
            : base(device, address, 3, new byte[] { GetMode(mode) })
        {
            Name = "set regulation mode";
        }

        private static byte GetMode(EnumRfPowerRegulationMode mode)
        {
            switch (mode)
            {             
                case EnumRfPowerRegulationMode.Power:
                    return 6;
                case EnumRfPowerRegulationMode.Voltage:
                    return 7;
                case EnumRfPowerRegulationMode.Current:
                    return 8;
            }

            return 0;
        }
    }

    //6 set power
    public class AeRfPowerSetPowerHandler : AeDcPowerHandler
    {
        public AeRfPowerSetPowerHandler(AeDcPower device, byte address, int power)
        : base(device, address, 6, BuildData(power))
        {
            Name = "set power";
        }

        private static byte[] BuildData(int power)
        {
            return new byte[] { (byte)power, (byte)(power >> 8) };
        }
    }

    //14 set communication mode
    public class AeRfPowerSetCommModeHandler : AeDcPowerHandler
    {
        public AeRfPowerSetCommModeHandler(AeDcPower device, byte address, EnumRfPowerCommunicationMode mode)
            : base(device, address, 14, BuildData(mode))
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
    public class AeRfPowerSetCapsCtrlModeHandler : AeDcPowerHandler
    {
        public AeRfPowerSetCapsCtrlModeHandler(AeDcPower device, byte address, byte mode)
            : base(device, address, 13, BuildData(mode))
        {
            Name = "set caps control mode";
        }

        private static byte[] BuildData(byte mode)
        {
            return new byte[] { mode == 0x01 ? (byte)0x01 : (byte)0x00 };
        }
    }

    //65 set pulse type
    public class AeRfPowerSetPulseTypeHandler : AeDcPowerHandler
    {
        public AeRfPowerSetPulseTypeHandler(AeDcPower device, byte address, EnumDcPowerPulseType type)
            : base(device, address, 65, BuildData(type))
        {
            Name = "set pulse type";
        }

        private static byte[] BuildData(EnumDcPowerPulseType type)
        {
            byte value = 0;
            switch (type)
            {
                case EnumDcPowerPulseType.Current:
                    value = 0;
                    break;
                case EnumDcPowerPulseType.Always:
                    value = 1;
                    break;
                case EnumDcPowerPulseType.Voltage:
                    value = 2;
                    break;
            }
            return new byte[] { value };
        }
    }

    //92 set pulse frequency index
    public class AeRfPowerSetPulseFrequencyHandler : AeDcPowerHandler
    {
        public AeRfPowerSetPulseFrequencyHandler(AeDcPower device, byte address, byte frequency)
        : base(device, address, 92, BuildData(frequency))
        {
            Name = "set pulse frequency";
        }

        private static byte[] BuildData(byte frequency)
        {
            return new byte[] { frequency };
        }
    }

    //93 set pulse reverse time
    public class AeRfPowerSetPulseReverseTimeHandler : AeDcPowerHandler
    {
        public AeRfPowerSetPulseReverseTimeHandler(AeDcPower device, byte address, byte time)
        : base(device, address, 93, BuildData(time))
        {
            Name = "set pulse reverse time";
        }

        private static byte[] BuildData(byte time)
        {
            return new byte[] { time };
        }
    }

    //146 query pulse frequency index
    public class AeRfPowerQueryPulseFrequencyHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryPulseFrequencyHandler(AeDcPower device, byte address)
            : base(device, address, 146, null)
        {
            Name = "Query pulse frequency index";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 1)
            {
                Device.NoteError($"query pulse frequency index, return data length {response.DataLength}");
            }
            else
            {
                
                Device.NotePulseFrequency(response.Data[0]);
            }
        }
    }

    //147 query pulse reverse time
    public class AeRfPowerQueryPulseReverseTimeHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryPulseReverseTimeHandler(AeDcPower device, byte address)
            : base(device, address, 147, null)
        {
            Name = "Query pulse reverse time";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 1)
            {
                Device.NoteError($"query pulse reverse time, return data length {response.DataLength}");
            }
            else
            {
                Device.NotePulseReverseTime(response.Data[0]);
            }
        }
    }

    //154 query regulation mode 
    public class AeRfPowerQueryRegulationModeHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryRegulationModeHandler(AeDcPower device, byte address)
            : base(device, address, 154, null)
        {
            Name = "query regulation mode";
        }
        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 1)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                EnumRfPowerRegulationMode mode = EnumRfPowerRegulationMode.Undefined;
                switch (response.Data[0])
                {
                    case 6:
                        mode = EnumRfPowerRegulationMode.Power;
                        break;
                    case 7:
                        mode = EnumRfPowerRegulationMode.Voltage;
                        break;
                    case 8:
                        mode = EnumRfPowerRegulationMode.Current;
                        break;
                }

                Device.NoteRegulationModeSetPoint(mode);
            }
        }
    }

    //155 query comm mode 
    public class AeRfPowerQueryCommModeHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryCommModeHandler(AeDcPower device, byte address)
            : base(device, address, 155, null)
        {
            Name = "query comm mode";
        }
        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 1)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                EnumRfPowerCommunicationMode mode = EnumRfPowerCommunicationMode.Undefined;
                switch (response.Data[0])
                {
                    case 2:
                        mode = EnumRfPowerCommunicationMode.Host;
                        break;
                    case 4:
                        mode = EnumRfPowerCommunicationMode.UserPort;
                        break;
                    case 8:
                        mode = EnumRfPowerCommunicationMode.Diagnostic;
                        break;
                    case 16:
                        mode = EnumRfPowerCommunicationMode.DeviceNet;
                        break;
                    case 32:
                        mode = EnumRfPowerCommunicationMode.EtherCat32;
                        break;
                }

                Device.NoteCommMode(mode);
            }
        }
    }

    //162 query status 
    public class AeRfPowerQueryStatusHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryStatusHandler(AeDcPower device, byte address)
            : base(device, address, 162, null)
        {
            Name = "query status";
        }
        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 4)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteStatus(response.Data);

                if ((response.Data[1] & 0x80) == 0x80)
                {
                    Device.NoteErrorStatus(true, "Interlock open");
                }
                else
                {
                    Device.NoteErrorStatus(false, "");
                }
            }
        }
    }
    //164 query setpoint 
    public class AeRfPowerQuerySetPointHandler : AeDcPowerHandler
    {
        public AeRfPowerQuerySetPointHandler(AeDcPower device, byte address)
            : base(device, address, 164, null)
        {
            Name = "query setpoint";
        }
        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 3)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                EnumRfPowerRegulationMode mode = EnumRfPowerRegulationMode.Undefined;
                switch (response.Data[2])
                {
                    case 6:
                        mode = EnumRfPowerRegulationMode.Power;
                        break;
                    case 7:
                        mode = EnumRfPowerRegulationMode.Voltage;
                        break;
                    case 8:
                        mode = EnumRfPowerRegulationMode.Current;
                        break;
                }

                Device.NoteRegulationModeSetPoint(mode);
                Device.NotePowerSetPoint(response.Data[0] + (response.Data[1] << 8));
            }
        }
    }
    //165 query forward power
    public class AeRfPowerQueryForwardPowerHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryForwardPowerHandler(AeDcPower device, byte address)
            : base(device, address, 165, null)
        {
            Name = "Query forward power";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 2)
            {
                Device.NoteError($"query forward power, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteForwardPower(response.Data[0] + (response.Data[1] << 8));
            }
        }
    }

    //168 query forward power, voltage, current
    public class AeRfPowerQueryPowerVoltageCurrentHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryPowerVoltageCurrentHandler(AeDcPower device, byte address)
            : base(device, address, 168, null)
        {
            Name = "Query forward power, voltage, current";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 6)
            {
                Device.NoteError($"query forward power, voltage, current, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteForwardPower(response.Data[0] + (response.Data[1] << 8));
                Device.NoteVoltage(response.Data[2] + (response.Data[3] << 8));
                Device.NoteCurrent(response.Data[4] + (response.Data[5] << 8));
            }
        }
    }

    //221 query PIN number
    public class AeRfPowerQueryPinHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryPinHandler(AeDcPower device, byte address)
            : base(device, address, 221, null)
        {
            Name = "Query PIN number";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 32)
            {
                Device.NoteError($"query pin number, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteHaloInstalled(response.Data[20] == 0x31);
            }
        }
    }

    //223 query fault or warning code
    public class AeRfPowerQueryFaultorWarningHandler : AeDcPowerHandler
    {
        public AeRfPowerQueryFaultorWarningHandler(AeDcPower device, byte address)
            : base(device, address, 223, null)
        {
            Name = "Query fault or warning code";
        }

        //protected override void ParseData(AeRfPowerMessage response)
        //{
        //    if (response.DataLength == 1)
        //    {
        //        Device.NoteErrorStatus(false, string.Empty);
        //    }
        //    else if (response.DataLength >= 2 && (response.Data[0] == 1 || response.Data[0] == 3))
        //    {
        //        Device.NoteErrorStatus(true, string.Empty);
        //    }
        //}
    }
}
