using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public abstract class FfuAAFHandler : HandlerBase
    {
        public FfuAAF Device { get; }
        public byte FanAddress;

        public bool IsNeedFeedback;

        protected FfuAAFHandler(FfuAAF device, byte prefix, byte command, byte groupAddress, byte[] datas)
            : base(BuildMessage(prefix, command, groupAddress, datas))
        {
            Device = device;
            IsNeedFeedback = true;
            AckTimeout = TimeSpan.FromSeconds(3);
        }
        protected FfuAAFHandler(FfuAAF device, byte prefix, byte command, byte groupAddress, byte[] datas,bool needfeedback)
            : base(BuildMessage(prefix, command, groupAddress, datas))
        {
            Device = device;
            IsNeedFeedback = needfeedback;
        }

        private static byte[] BuildMessage(byte prefix, byte command, byte groupAddress,byte[] datas)
        {
            List<byte> result = new List<byte>() { prefix, command , groupAddress };
            if (datas != null)
            {
                foreach(byte data in datas)
                result.Add(data);
            }
            result.Add(ModRTU_CRC(result.ToArray()));
            return result.ToArray();
        }        

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
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

    public class FfuAAFQuerySpeedHandler : FfuAAFHandler
    {
        public FfuAAFQuerySpeedHandler(FfuAAF device,byte fanaddress, byte groupAddress)
            : base(device, 0x15, (byte)(0x20 + fanaddress), groupAddress, null)
        {
            Name = "Query Speed";
            FanAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (FanAddress == 1) Device.FFU1Speed = result.Data1 * Device.NMaxSpeed / 250;
            if (FanAddress == 3) Device.FFU2Speed = result.Data1 * Device.NMaxSpeed / 250;

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class FfuAAFQueryStatusHandler : FfuAAFHandler
    {
        public FfuAAFQueryStatusHandler(FfuAAF device,byte fanaddress, byte groupAddress)
            : base(device, 0x35, fanaddress, groupAddress, new byte[] { 0x00 })
        {
            Name = "Query Status";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
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

    public class FfuAAFSetSpeedHandler : FfuAAFHandler
    {
        public FfuAAFSetSpeedHandler(FfuAAF device, int fanaddress,int groupAddress,int speed)
            : base(device, 0x35, (byte)(0x40 + fanaddress), (byte)groupAddress, new byte[] { (byte)(speed*250/device.NMaxSpeed) },false)
        {
            Name = "SetSpeed";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            if(result.IsAck)
            {
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }
}
