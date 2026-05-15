using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.Hanbell
{
    public abstract class HanbellPumpHandler : HandlerBase
    {
        public HanbellPump Device { get; }

        private byte _address;
        protected byte _command;

        protected HanbellPumpHandler(HanbellPump device, byte address, byte command, byte[] data)
            : base(BuildMessage(address, command, data))
        {
            Device = device;
            _address = address;
            _command = command;
        }

        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            //MODBUS TCP
            buffer.Add(0x00);
            buffer.Add(0x00);
            buffer.Add(0x00);
            buffer.Add(0x00);

            int length = 2 + (data != null ? data.Length:0);
            buffer.Add((byte)((length >> 8) & 0xFF));
            buffer.Add((byte)(length & 0xFF));


            buffer.Add(address);
            buffer.Add(command);
            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            // CRC ??
            //buffer.Add(CalcSum(buffer, buffer.Count));

            return buffer.ToArray();
        }

        //host->unit, unit->host(ack), unit->host(csr), host->unit(ack)
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HanbellPumpMessage response = msg as HanbellPumpMessage;
            ResponseMessage = msg;

            Device.NoteError(null);
            if (response.Address != _address || response.FunctionCode != _command)
            {
                transactionComplete = false;
                return false;
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }


        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }


        private static byte ModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)~temp;
        }

    }

    public class HanbellPumpRequestRegistersHandler : HanbellPumpHandler
    {
        public HanbellPumpRequestRegistersHandler(HanbellPump device, byte address, int startAddress, int registerCount)
        : base(device, address, 0x03, BuildData(startAddress, registerCount))
        {
            Name = "RequestRegisters";
        }

        private static byte[] BuildData(int startAddress, int registerCount)
        {
            return new byte[] { (byte)(startAddress >> 8), (byte)startAddress, (byte)(registerCount >> 8), (byte)registerCount };
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as HanbellPumpMessage;
                if (result.Data.Length < 91)//91=data length + registerCount * 2
                {
                    Device.NoteError("invalid response for command RequestRegisters");
                }
                else
                {
                    //2031
                    var digitalOutputStatus1High = result.Data[61];
                    var digitalOutputStatus1Low = result.Data[62];
                    Device.IsOn = ((digitalOutputStatus1Low & 0x00000002) == 0) || ((digitalOutputStatus1Low & 0x00000001) == 0);
                    Device.NoteDPPower(result.Data[1] * 256 + result.Data[2]);
                    Device.NoteBPPower(result.Data[3] * 256 + result.Data[4]);
                    Device.NoteDPSpeed(result.Data[5] * 256 + result.Data[6]);
                    Device.NoteBPSpeed(result.Data[37] * 256 + result.Data[8]);

                    int waring0 = result.Data[45] * 256 + result.Data[46];
                    Device.NoteDPCurrentHigh(waring0 & (1 << 15));
                    Device.NoteBPCurrentHigh(waring0 & (1 << 14));
                    int waring1 = result.Data[47] * 256 + result.Data[48];
                    int waring2 = result.Data[49] * 256 + result.Data[50];
                    int alarm0 = result.Data[51] * 256 + result.Data[52];
                    int alarm1 = result.Data[53] * 256 + result.Data[54];
                    int alarm2 = result.Data[55] * 256 + result.Data[56];

                    Device.IsError = ((digitalOutputStatus1Low & 0x0000004) == 0) || ((digitalOutputStatus1Low & 0x000008) == 0) ||
                        waring0 + waring1 + waring2 != 0 || alarm0 + alarm1 + alarm2 != 0;

                    //if (waring0 + waring1 + waring2 != 0)
                    //{
                    //    Device.NoteError("pump is in warning status");
                    //}

                    //if (alarm0 + alarm1 + alarm2 != 0)
                    //{
                    //    Device.NoteError("pump is in alarm status");
                    //}
                }
            }
            return true;
        }
    }

    public class HanbellPumpStartVaccumPumpHandler : HanbellPumpHandler
    {
        public HanbellPumpStartVaccumPumpHandler(HanbellPump device, byte address)
        : base(device, address, 0x05, BuildData())
        {
            Name = "StartVaccumPump";
        }

        private static byte[] BuildData()
        {
            return new byte[] { 0x00,0x0A,0xFF,0x00 };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteVaccumPumpCompleted(true);
            }
            return true;
        }
    }
    public class HanbellPumpStopVaccumPumpHandler : HanbellPumpHandler
    {
        public HanbellPumpStopVaccumPumpHandler(HanbellPump device, byte address)
        : base(device, address, 0x05, BuildData())
        {
            Name = "StopVaccumPump";
        }

        private static byte[] BuildData()
        {
            return new byte[] { 0x00, 0x0A, 0x00, 0x00 };
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteVaccumPumpCompleted(false);
            }
            return true;
        }
    }
    public class HanbellPumpStartBPPumpHandler : HanbellPumpHandler
    {
        public HanbellPumpStartBPPumpHandler(HanbellPump device, byte address)
        : base(device, address, 0x05, BuildData())
        {
            Name = "StartBPPump";
        }

        private static byte[] BuildData()
        {
            return new byte[] { 0x00, 0x0B, 0xFF, 0x00 };
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteBPPumpOnOff(true);
            }
            return true;
        }
    }
    public class HanbellPumpStopBPPumpHandler : HanbellPumpHandler
    {
        public HanbellPumpStopBPPumpHandler(HanbellPump device, byte address)
        : base(device, address, 0x05, BuildData())
        {
            Name = "StopBPPump";
        }

        private static byte[] BuildData()
        {
            return new byte[] { 0x00, 0x0B, 0x00, 0x00 };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteBPPumpOnOff(false);
            }
            return true;
        }
    }

    public class HanbellPumpResetHandler : HanbellPumpHandler
    {
        public HanbellPumpResetHandler(HanbellPump device, byte address)
        : base(device, address, 0x05, BuildData())
        {
            Name = "Reset";
        }

        private static byte[] BuildData()
        {
            return new byte[] { 0x00, 0x0C, 0xFF, 0x00 };
        }
    }

    public class HanbellPumpRawCommandHandler : HanbellPumpHandler
    {
        public HanbellPumpRawCommandHandler(HanbellPump device, byte address, string command, string parameter = null)
            : base(device, address, byte.Parse(command), BuildData(parameter))
        {
        }

        private static byte[] BuildData(string parameter)
        {
            if (parameter == null) 
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para,16)).ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            base.HandleMessage(msg, out handled);

            var result = msg as HanbellPumpMessage;
            Device.NoteRawCommandInfo(_command.ToString("X2"), string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            return true;
        }


    }
}
