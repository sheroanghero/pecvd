using System;
using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.AE
{
    public abstract class AeRfPowerHandler : HandlerBase
    {
        public AeRfPower Device { get; }

        private byte _address;
        private byte _command;

        protected AeRfPowerHandler(AeRfPower device, byte address, byte command, byte[] data)
            : base(BuildMessage(address, command, data))
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
    public class AeRfPowerSwitchOnOffHandler : AeRfPowerHandler
    {
        public AeRfPowerSwitchOnOffHandler(AeRfPower device, byte address, bool isOn)
            : base(device, address, isOn ? (byte)0x02 : (byte)0x01, null)
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }


    //3 regulation mode
    public class AeRfPowerSetRegulationModeHandler : AeRfPowerHandler
    {
        public AeRfPowerSetRegulationModeHandler(AeRfPower device, byte address, EnumRfPowerRegulationMode mode)
            : base(device, address, 3, new byte[]{ GetMode(mode) })
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
    public class AeRfPowerSetPowerHandler : AeRfPowerHandler
    {
        public AeRfPowerSetPowerHandler(AeRfPower device, byte address, int power)
        : base(device, address, 8, BuildData(power))
        {
            Name = "set power";
        }

        private static byte[] BuildData(int power)
        {
            return new byte[]{(byte)power, (byte)(power >> 8)};
        }
    }

    //14 set communication mode
    public class AeRfPowerSetCommModeHandler : AeRfPowerHandler
    {
        public AeRfPowerSetCommModeHandler(AeRfPower device, byte address, EnumRfPowerCommunicationMode mode)
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
    public class AeRfPowerSetCapsCtrlModeHandler : AeRfPowerHandler
    {
        public AeRfPowerSetCapsCtrlModeHandler(AeRfPower device, byte address, byte mode)
            : base(device, address, 13, BuildData(mode))
        {
            Name = "set caps control mode";
        }

        private static byte[] BuildData(byte mode)
        {
            return new byte[] { mode == 0x01 ? (byte)0x01 : (byte)0x00 };
        }
    }
    //112 set load
    public class AeRfPowerSetLoadHandler : AeRfPowerHandler
    {
        public AeRfPowerSetLoadHandler(AeRfPower device, byte address, int data)
            : base(device, address, 112, BuildData(data))
        {
            Name = "set load";
        }

        private static byte[] BuildData(int data)
        {
            return new byte[] { (byte)data, (byte)(data >> 8) };
        }
    }
    //122 set tune
    public class AeRfPowerSetTuneHandler : AeRfPowerHandler
    {
        public AeRfPowerSetTuneHandler(AeRfPower device, byte address, int data)
            : base(device, address, 122, BuildData(data))
        {
            Name = "set tune";
        }

        private static byte[] BuildData(int data)
        {
            return new byte[] { (byte)data, (byte)(data >> 8) };
        }
    }

    //155 query comm mode 
    public class AeRfPowerQueryCommModeHandler : AeRfPowerHandler
    {
        public AeRfPowerQueryCommModeHandler(AeRfPower device, byte address)
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
    public class AeRfPowerQueryStatusHandler : AeRfPowerHandler
    {
        public AeRfPowerQueryStatusHandler(AeRfPower device, byte address)
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
            }
        }
    }
    //164 query setpoint 
    public class AeRfPowerQuerySetPointHandler : AeRfPowerHandler
    {
        public AeRfPowerQuerySetPointHandler(AeRfPower device, byte address)
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
                EnumRfPowerRegulationMode regMode = EnumRfPowerRegulationMode.Undefined;
                switch (response.Data[2])
                {
                    case 6:
                        regMode = EnumRfPowerRegulationMode.Forward;
                        break;
                    case 7:
                        regMode = EnumRfPowerRegulationMode.Load;
                        break;
                    case 8:
                        regMode = EnumRfPowerRegulationMode.DcBias;
                        break;
                    case 9:
                        regMode = EnumRfPowerRegulationMode.VALimit;
                        break;
                }
 
                Device.NoteRegulationModeSetPoint(regMode);
                Device.NotePowerSetPoint(response.Data[0] + (response.Data[1] << 8));
            }
        }
    }
    //165 forward power
    public class AeRfPowerQueryForwardPowerHandler : AeRfPowerHandler
    {
        public AeRfPowerQueryForwardPowerHandler(AeRfPower device, byte address)
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
                Device.NoteForwardPower( response.Data[0] + (response.Data[1] << 8));
            }
        }
    }

    //166 reflect power
    public class AeRfPowerQueryReflectPowerHandler : AeRfPowerHandler
    {
        public AeRfPowerQueryReflectPowerHandler(AeRfPower device, byte address)
            : base(device, address, 166, null)
        {
            Name = "Query reflect power";
        }

        protected override void ParseData(AeRfPowerMessage response)
        {
            if (response.DataLength != 2)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteReflectPower(response.Data[0] + (response.Data[1] << 8));
            }
        }
    }


    //221 query PIN number
    public class AeRfPowerQueryPinHandler : AeRfPowerHandler
    {
        public AeRfPowerQueryPinHandler(AeRfPower device, byte address)
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
                Device.NoteHaloInstalled(response.Data[20]==0x31);
            }
        }
    }
}
