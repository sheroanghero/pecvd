using System;
using System.Collections.Generic;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.DCPowers.AE
{
    public abstract class DxkdpDcPowerHandler : HandlerBase
    {
        public DxkdpDcPower Device { get; }

        private byte _address;
        private byte _command;

        private static int ackTimeout = 2;

        protected DxkdpDcPowerHandler(DxkdpDcPower device, byte address, byte command, byte[] data)
            : base(BuildMessage(address, command, data), ackTimeout)
        {
            Device = device;
            _address = address;
            _command = command;
        }

        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(0xAA);
            buffer.Add(address);
            buffer.Add(command);
            buffer.Add((byte)(data == null ? 0 : data.Length));

            if (data != null)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer.GetRange(1,buffer.Count - 1)));
            return buffer.ToArray();


            //buffer.Add((byte)((address << 3) + (data == null ? 0 : data.Length)));

            //buffer.Add(command);

            //if (data != null && data.Length > 0)
            //{
            //    buffer.AddRange(data);
            //}

            //buffer.Add(CalcSum(buffer, buffer.Count));

            //return buffer.ToArray();
        }

        //host->unit, unit->host(ack), unit->host(csr), host->unit(ack)
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            DxkdpRfPowerMessage response = msg as DxkdpRfPowerMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if ((_command == 0x20 || _command == 0x21 || _command == 0x22 || _command == 0x23 || _command == 0x24 || _command == 0x27 || _command == 0x29 || _command == 0x30) && response.RawMessage[0] == 0x06)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
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

                //SendAck();

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected virtual void ParseData(DxkdpRfPowerMessage msg)
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

        private static byte CalcSum(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret += data[i];
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


    

    //2.启动电源    3.停止电源
    public class DxkdpRfPowerSwitchOnOffHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSwitchOnOffHandler(DxkdpDcPower device, byte address, bool isOn)
            : base(device, address,0x20, new byte[] { isOn ? (byte)0x01 : (byte)0x00 })
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }

    //4 set Voltage
    public class DxkdpRfPowerSetVoltageHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSetVoltageHandler(DxkdpDcPower device, byte address, UInt16 voltage)
        : base(device, address, 0x21, BuildData(voltage))
        {
            Name = "set Voltage";
        }

        private static byte[] BuildData(UInt16 power)
        {
            UInt16 powerUInt = power;
            byte[] data = BitConverter.GetBytes(powerUInt);

            return data;
        }
    }

    //5 set Electricity
    public class DxkdpRfPowerSetElectricityHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSetElectricityHandler(DxkdpDcPower device, byte address, UInt16 am)
        : base(device, address, 0x22, BuildData(am))
        {
            Name = "set Electricity";
        }

        private static byte[] BuildData(UInt16 am)
        {
            UInt16 amUInt = am ;
            byte[] data = BitConverter.GetBytes(amUInt);

            return data;
        }
    }

    //6 set Voltage and Electricity
    public class DxkdpRfPowerSetVEHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSetVEHandler(DxkdpDcPower device, byte address, int volt, int vmss, int ampere, int emss)
        : base(device, address, 0x23, BuildData(volt, vmss,ampere,emss))
        {
            Name = "set Voltage and Electricity";
        }

        private static byte[] BuildData(int volt, int vmss, int ampere, int emss)
        {
            byte[] data = new byte[4];
            UInt16 voltUInt = Convert.ToUInt16(volt * (int)Math.Pow(10, vmss));
            byte[] dataVolt = BitConverter.GetBytes(voltUInt);
            UInt16 ampereUInt = Convert.ToUInt16(ampere * (int)Math.Pow(10, emss));
            byte[] dataAm = BitConverter.GetBytes(ampereUInt);

            dataVolt.CopyTo(data, 0);
            dataAm.CopyTo(data, 4);
            return data;
        }
    }

    //8 set Polarity
    public class DxkdpRfPowerSetPolarityHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSetPolarityHandler(DxkdpDcPower device, byte address, int polarity)
        : base(device, address, 0x24, BuildData(polarity))
        {
            Name = "set Polarity";
        }

        private static byte[] BuildData(int polarity)
        {
            UInt16 polarityUInt = Convert.ToUInt16(polarity);
            byte[] data = BitConverter.GetBytes(polarityUInt);

            return data;
        }
    }

    // 7.读电源的实际电流值、实际电压值
    public class DxkdpRfPowerQueryPracticalHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerQueryPracticalHandler(DxkdpDcPower device, byte address)
            : base(device, address, 0x26, null)
        {
            Name = "Query Practical voltage, current";
        }

        protected override void ParseData(DxkdpRfPowerMessage response)
        {
            if (response.DataLength != 5)
            {
                Device.NoteError($"Query Practical voltage, current, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteVoltage((response.Data[1] << 8) + response.Data[0]);
                Device.NoteCurrent((response.Data[3] << 8) + response.Data[2]);
            }
        }
    }


    

    // 9.读设置的电压电流值及电源的状态
    public class DxkdpRfPowerQueryStateHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerQueryStateHandler(DxkdpDcPower device, byte address)
            : base(device, address, 0x28, null)
        {
            Name = "Query the voltage, current, and status";
        }

        protected override void ParseData(DxkdpRfPowerMessage response)
        {
            if (response.DataLength != 5)
            {
                Device.NoteError($"query forward State, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteStatus(response.Data[0]);
                //Device.NoteSetVoltage((response.Data[2] << 8) + response.Data[1]); 
                //Device.NoteSetCurrent((response.Data[4] << 8) + response.Data[3]); 不读电流设定值
            }
        }
    }

    // 1.读电源的系统信息
    public class DxkdpRfPowerQuerySysInformationHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerQuerySysInformationHandler(DxkdpDcPower device, byte address)
            : base(device, address, 0x2B, null)
        {
            Name = "Query forward Voltage,Electricity current";
        }

        protected override void ParseData(DxkdpRfPowerMessage response)
        {
            if (response.DataLength != 14)
            {
                Device.NoteError($"query forward system information, return data length {response.DataLength}");
            }
            else
            {
                Device.NoteMinVStepSize(response.Data[0]);
                Device.NoteMinCStepSize(response.Data[1]);
                Device.NoteSysVoltage((response.Data[7] << 8) + response.Data[6]);
                Device.NoteSysCurrent((response.Data[9] << 8) + response.Data[8]);
            }
        }
    }

    //8 set Mode=1
    public class DxkdpRfPowerSetModeHandler : DxkdpDcPowerHandler
    {
        public DxkdpRfPowerSetModeHandler(DxkdpDcPower device, byte address, EnumRfPowerCommunicationMode host)
        : base(device, address, 0x30, BuildData(host))
        {
            Name = "set Mode";
        }

        private static byte[] BuildData(EnumRfPowerCommunicationMode host)
        {
            byte dataByte =(byte) (host == EnumRfPowerCommunicationMode.Host ? 1 : 0);
            byte[] data = new byte[] { dataByte };

            return data;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            base.HandleMessage(msg, out transactionComplete);

            Device.NoteMode(true);
            return true;
        }
    }
}
