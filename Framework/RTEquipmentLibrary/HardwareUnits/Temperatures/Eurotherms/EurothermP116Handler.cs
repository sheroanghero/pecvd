using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Eurotherms
{    
    public abstract class EurothermP116Handler : HandlerBase
    {
        public Temperatures.Eurotherms.EurothermP116 Device { get; }

        protected EurothermP116Handler(Temperatures.Eurotherms.EurothermP116 device, byte[] commandvalue)
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
            var result = msg as P116PIDTCMessage;

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

    public class P116PIDTCQueryHandler : EurothermP116Handler
    {
        public P116PIDTCQueryHandler(Temperatures.Eurotherms.EurothermP116 device,string name, byte groupAddress,byte offerHigh,byte offerLow,byte dataHigh,byte dataLow)
            : base(device, new byte[] { groupAddress, 3, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;//"Query Temp";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as P116PIDTCMessage;
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
            if (Name.Contains("QueryPVInValue"))
            {   
                Device.fPVInValue =Convert.ToSingle(rvalue)/10;
            }
            else if(Name.Contains("QueryTargetSP"))
            {                
                Device.fTargetSP = Convert.ToSingle(rvalue)/10;
            }
            else if (Name.Contains("QueryAM"))
            {
                Device.iAM = Convert.ToInt32(rvalue);
            }
            else
            {

            }
            Device._connecteTimes = 0;
            ResponseMessage = msg;
            handled = true;
            Thread.Sleep(500);
            return true;
        }
    }
    public class P116PIDTCWriteSingleHandler : EurothermP116Handler
    {
        public P116PIDTCWriteSingleHandler(Temperatures.Eurotherms.EurothermP116 device, string name, byte groupAddress, byte offerHigh, byte offerLow, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 0x06, offerHigh, offerLow, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as P116PIDTCMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode == 0x86)
            {
               Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;
            Device.IsConfig = false;
            Thread.Sleep(1000);
            return true;
        }
    }
    public class P116PIDTCWriteHandler : EurothermP116Handler
    {
        public P116PIDTCWriteHandler(Temperatures.Eurotherms.EurothermP116 device, string name,byte groupAddress, byte offerHigh, byte offerLow,byte unitH,byte unitL,byte length, byte dataHigh, byte dataLow)
            : base(device, new byte[] { groupAddress, 0x10, offerHigh, offerLow, unitH, unitL, length, dataHigh, dataLow })
        {
            Name = name;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as P116PIDTCMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.FunctionCode == 0x90)
            {
                Device.RespondAbnormal(result.FunctionCode, result.Data[2]);
            }
            ResponseMessage = msg;
            handled = true;
            Device.IsConfig = false;
            Thread.Sleep(1000);
            return true;
        }
    }
}
