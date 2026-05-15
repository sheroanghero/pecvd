using System;
using System.Collections.Generic; 
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.MayAir
{
    public abstract class FfuMayAirHandler : HandlerBase
    {
        public FfuMayAir Device { get; }

        protected FfuMayAirHandler(FfuMayAir device, byte prefix, byte command, byte deviceAddress, byte groupAddress, byte data1, byte data2)
            : base(BuildMessage(prefix, command, deviceAddress, groupAddress, data1, data2))
        {
            Device = device;
        }

        protected FfuMayAirHandler(FfuMayAir device, byte prefix, byte command, byte deviceAddress, byte groupAddress, byte data1)
            : base(BuildMessage(prefix, command, deviceAddress, groupAddress, data1))
        {
            Device = device;
        }

        protected FfuMayAirHandler(FfuMayAir device, byte prefix, byte command, byte deviceAddress, byte groupAddress)
            : base(BuildMessage(prefix, command, deviceAddress, groupAddress))
        {
            Device = device;
        }

        private static byte[] BuildMessage(byte prefix, byte command, byte deviceAddress, byte groupAddress)
        {
            List<byte> result = new List<byte>() { prefix, (byte)(command | deviceAddress), groupAddress };
            result.Add(CalcSum(result));
            return result.ToArray();
        }

        private static byte[] BuildMessage(byte prefix, byte command, byte deviceAddress, byte groupAddress, byte data1)
        {
            List<byte> result = new List<byte>(){prefix, (byte)(command | deviceAddress), groupAddress, data1};
            result.Add(CalcSum(result));
            return result.ToArray();
        }

        private static byte[] BuildMessage(byte prefix, byte command, byte deviceAddress, byte groupAddress, byte data1, byte data2)
        {
            List<byte> result = new List<byte>() { prefix, (byte)(command | deviceAddress), groupAddress, data1, data2 };
            result.Add(CalcSum(result));
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }
 
        private static byte CalcSum(List<byte> value)
        {
            int sum = 0;
            foreach (var item in value)
            {
                sum += (int)item;
            }

            return 0;
        }
    }
 
    public class FfuMayAirQuerySpeedHandler : FfuMayAirHandler
    {
        public FfuMayAirQuerySpeedHandler(FfuMayAir device, byte deviceAddress, byte groupAddress)
            : base(device, 0x15, 0x20, deviceAddress, groupAddress)
        {
            Name = "Query Speed";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuMayAirMessage;
 

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
 
    public class FfuMayAirQueryStatusHandler : FfuMayAirHandler
    {
        public FfuMayAirQueryStatusHandler(FfuMayAir device, byte deviceAddress, byte groupAddress)
            : base(device, 0x35, 0x00, deviceAddress, groupAddress, 0x00)
        {
            Name = "Query Status";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuMayAirMessage;
            //if (result.IsError)
            //{
            //    Device.SetError(result.Data);
            //}
            //else
            //{
            //    if (int.TryParse(result.Data, out int code))
            //    {
            //        Device.SetErrorCode(code);
            //    }
            //    else
            //    {
            //        Device.SetError(result.Data + "format error");
            //    }
            //}

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
 
 
}
