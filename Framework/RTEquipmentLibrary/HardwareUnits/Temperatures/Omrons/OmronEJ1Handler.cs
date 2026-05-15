using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Omrons
{
    public abstract class OmronEJ1Handler : HandlerBase
    {
        public OmronEJ1 Device { get; }

        protected OmronEJ1Handler(OmronEJ1 device, byte[] commandvalue)
            : base(BuildMessage(commandvalue))
        {
            Device = device;

        }

        private static byte[] BuildMessage(byte[] commandvalue)
        {
            byte[] crc = ModRTU_CRC(commandvalue);
            List<byte> result = commandvalue.ToList();
            foreach (byte b in crc)
            {
                result.Add(b);
            }
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as OmronEJ1Message;

            ResponseMessage = msg;
            handled = true;
            return true;
        }


        private static byte[] ModRTU_CRC(byte[] buffer)
        {
            ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;

            for (var pos = 0; pos < len; pos++)
            {
                crc ^= buf[pos]; // XOR byte into least sig. byte of crc

                for (var i = 8; i != 0; i--)
                    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {
                        // If the LSB is set
                        crc >>= 1; // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else // Else LSB is not set
                    {
                        crc >>= 1; // Just shift right
                    }
            }

            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return BitConverter.GetBytes(crc);

        }
    }

    public class OmronEJ1QueryHandler : OmronEJ1Handler
    {
        public OmronEJ1QueryHandler(OmronEJ1 device, string name, byte groupAddress, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 3, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;//"Query Temp";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as OmronEJ1Message;
            handled = false;
            if (!result.IsResponse) return true;

            short rvalue = 0;
            rvalue = (short)(rvalue ^ result.Data[0]);
            rvalue = (short)(rvalue << 8);
            rvalue = (short)(rvalue ^ result.Data[1]);

            //if(Name.Contains("Actual"))
            //Device.ActualTemp = rvalue;
            //else
            //Device.SettingTemp = rvalue;
            if (Name.Contains("Actual"))
            {
                var chel = Convert.ToInt16(Name.Split(' ')[1].Substring(7, 1));
                var count = Convert.ToInt16(Name.Split(' ')[2].Substring(6, 1));
                var index = (chel - 1) * 4 + count - 1;
                Device.ActualTemp[index] = Convert.ToSingle(rvalue) / 10;
            }
            else
            {
                var chel = Convert.ToInt16(Name.Split(' ')[1].Substring(7, 1));
                var count = Convert.ToInt16(Name.Split(' ')[2].Substring(7, 1));
                var index = (chel - 1) * 4 + count - 1;
                Device.SettingTemp[index] = Convert.ToSingle(rvalue) / 10;
            }
            Device._connecteTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(1000);
            return true;
        }
    }
    
    public class OmronEJ1WriteHandler : OmronEJ1Handler
    {
        public OmronEJ1WriteHandler(OmronEJ1 device, string name, byte groupAddress, byte offerHigh, byte offerLow, byte unitH, byte unitL, byte length, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 0x10, offerHigh, offerLow, unitH, unitL, length, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as OmronEJ1Message;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode == 0x90)
            {
                Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;
             
             
            return true;
        }
    }




    public class OmronEJ1ReadVariableHandler : OmronEJ1Handler
    {
        private int _slaveAddress;
        private int _startAddress;
        private int _length;

        public OmronEJ1ReadVariableHandler(OmronEJ1 device, int slaveAddress, int startAddress, int length)
            : base(device, BuildMessage(slaveAddress, startAddress, length) )
        {
            Name = "Read Variable";
            _slaveAddress = slaveAddress;
            _startAddress = startAddress;
            _length = length;
        }

        private static byte[] BuildMessage(int slaveAddress, int startAddress, int length)
        {
            List<byte> result = new List<byte>();
             
            result.Add((byte)slaveAddress);

            result.Add(0x03);

            byte addressH = Convert.ToByte((startAddress >> 8) & 0xff); 
            byte addressL = Convert.ToByte(startAddress & 0xff);
            result.Add(addressH);
            result.Add(addressL);

            byte lengthH = Convert.ToByte((length >> 8) & 0xff);
            byte lengthL = Convert.ToByte(length & 0xff);
            result.Add(lengthH);
            result.Add(lengthL);
 
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as OmronEJ1Message;
            handled = false;

            if (!result.IsResponse) 
                return true;

            short[] value = new  short[result.Data.Length/2];

            for (int i = 0; (i+1) < result.Data.Length; i+=2)
            {
                short rvalue = 0;
                rvalue = (short)(rvalue ^ result.Data[i]);
                rvalue = (short)(rvalue << 8);
                rvalue = (short)(rvalue ^ result.Data[i+1]);

                value[i / 2] = rvalue;
            }

            Device.NoteVariableReadout(_slaveAddress, _startAddress, value);

            ResponseMessage = msg;
            handled = true;
 
            return true;
        }
    }
    public class OmronEJ1WriteVariableHandler : OmronEJ1Handler
    {
        public OmronEJ1WriteVariableHandler(OmronEJ1 device,  int slave, int address, int data)
            : base(device, BuildMessage(slave, address, data))
        {
            Name = "Write Variable";
        }

        private static byte[] BuildMessage(int slaveAddress, int startAddress, int data)
        {
            List<byte> result = new List<byte>();

            result.Add((byte)slaveAddress);

            result.Add(0x06);

            byte addressH = Convert.ToByte((startAddress >> 8) & 0xff);
            byte addressL = Convert.ToByte(startAddress & 0xff);
            result.Add(addressH);
            result.Add(addressL);

            byte lengthH = Convert.ToByte((data >> 8) & 0xff);
            byte lengthL = Convert.ToByte(data & 0xff);
            result.Add(lengthH);
            result.Add(lengthL);

            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as OmronEJ1Message;
            handled = false;

            if (!result.IsResponse) 
                return true;

            if (result.FunctionCode == 0x86)
            {
                Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
 
}
