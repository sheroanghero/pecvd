using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public class DWPressureMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }        
        public byte[] Data { get; set; }
        
    }

    public class DWPressureConnection : SerialPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public DWPressureConnection(string portName) : base(portName,9600,8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One,"\n",false)
        {

        }
        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }


        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();

            DWPressureMessage msg = new DWPressureMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 4) return msg;

            msg.DeviceAddress = temps[0];
            msg.FunctionCode = temps[1];
            msg.Length = temps[2];

            if (temps.Length < msg.Length + 5) return msg;

            msg.Data = _lstCacheBuffer.Skip(3).Take(msg.Length).ToArray();  
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
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
    public abstract class DWPressureHandler : HandlerBase
    {
        public DWPressure Device { get; }        

        protected DWPressureHandler(DWPressure device, byte[] commandvalue)
            : base(BuildMessage(commandvalue))
        {
            Device = device;
            
        }       

        private static byte[] BuildMessage(byte[] commandvalue)
        {
            byte[] crc = ModRTU_CRC(commandvalue);
            List<byte> result = commandvalue.ToList();
            foreach(byte b in crc)
            {
                result.Add(b);
            }
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as DWPressureMessage;
            
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

    public class DWPressureQueryPressureHandler : DWPressureHandler
    {
        public DWPressureQueryPressureHandler(DWPressure device, byte groupAddress)
            : base(device, new byte[] { groupAddress, 3,0,0,0,1 })
        {
            Name = "Query pressure";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as DWPressureMessage;
            handled = false;
            if (!result.IsResponse) return true;

            short rvalue = 0;
            if (result.Data != null && result.Data.Length >= 2)
            {
                rvalue = (short)(rvalue ^ result.Data[0]);
                rvalue = (short)(rvalue << 8);
                rvalue = (short)(rvalue ^ result.Data[1]);
            }
            if (result.DeviceAddress == 1)
            {
                Device.PressureValue1 = rvalue;
            }
            else
            {
                Device.PressureValue2 = rvalue;
            }

            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(2000);
            return true;
        }
    }
}
