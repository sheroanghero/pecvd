using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public abstract class FfuAAFNewHandler : HandlerBase
    {
        public FfuAAFNew Device { get; }
        public byte DeviceAddress;
        public byte GroupAddress;

        public bool IsNeedFeedback;

        protected FfuAAFNewHandler(FfuAAFNew device, byte prefix, byte command, byte groupAddress, byte[] datas)
            : base(BuildMessage(prefix, command, groupAddress, datas))
        {
            Device = device;
            IsNeedFeedback = true;

        }
        protected FfuAAFNewHandler(FfuAAFNew device, byte prefix, byte command, byte groupAddress, byte[] datas, bool needfeedback)
            : base(BuildMessage(prefix, command, groupAddress, datas))
        {
            Device = device;
            IsNeedFeedback = needfeedback;
        }

        private static byte[] BuildMessage(byte prefix, byte command, byte groupAddress, byte[] datas)
        {
            List<byte> result = new List<byte>() { prefix, command, groupAddress };
            if (datas != null)
            {
                foreach (byte data in datas)
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

    /// <summary>
    /// 设置组地址
    /// </summary>
    public class FfuAAFNewSetGroupAddressHandler : FfuAAFNewHandler
    {
        public FfuAAFNewSetGroupAddressHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] datas)
            : base(device, 0x55, (byte)(0xC0 + fanaddress), groupAddress, datas)
        {
            Name = "Set Group Address";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }
    /// <summary>
    /// 查询组地址
    /// </summary>
    public class FfuAAFNewGetGroupAddressHandler : FfuAAFNewHandler
    {
        public FfuAAFNewGetGroupAddressHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
            : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Group Address";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            result.GroupAddress = result.Data1;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    /// <summary>
    /// 设置机地址
    /// </summary>
    public class FfuAAFNewSetAddressHandler : FfuAAFNewHandler
    {
        public FfuAAFNewSetAddressHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] datas)
            : base(device, 0x55, (byte)(0xC0 + fanaddress), groupAddress, datas)
        {
            Name = "Set Device  Address";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    /// <summary>
    /// 查询机地址
    /// </summary>
    public class FfuAAFNewGetAddressHandler : FfuAAFNewHandler
    {
        public FfuAAFNewGetAddressHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
               : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Device Address";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.DeviceAddress = result.Data1;
            ResponseMessage = msg;
            handled = true;
            return true;
        }

    }
    /// <summary>
    /// 查询最高转速 1
    /// </summary>
    public class FfuAAFNewQueryMaxSpeed1Handler : FfuAAFNewHandler
    {
        public FfuAAFNewQueryMaxSpeed1Handler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
           : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Max Speed1";
            DeviceAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.MaxSpeed1 = result.Data1;
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    /// <summary>
    /// 查询最高转速 2
    /// </summary>
    public class FfuAAFNewQueryMaxSpeed2Handler : FfuAAFNewHandler
    {
        public FfuAAFNewQueryMaxSpeed2Handler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
           : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Max Speed2";
            DeviceAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.MaxSpeed2 = result.Data1;
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    /// <summary>
    /// 查询最高转速 3
    /// </summary>
    public class FfuAAFNewQueryMaxSpeed3Handler : FfuAAFNewHandler
    {
        public FfuAAFNewQueryMaxSpeed3Handler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
           : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Max Speed3";
            DeviceAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.MaxSpeed3 = result.Data1;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class FfuAAFNewQuerySpeedFactorHandler : FfuAAFNewHandler
    {
        public FfuAAFNewQuerySpeedFactorHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] values)
            : base(device, 0x35, (byte)(0xE0 + fanaddress), groupAddress, values)
        {
            Name = "Query Speed Factor";
            DeviceAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.Speedfactor = result.Data1;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }



    public class FfuAAFNewSetSpeedFactorHandler : FfuAAFNewHandler
    {
        public FfuAAFNewSetSpeedFactorHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] datas)
            : base(device, 0x55, (byte)(0xC0 + fanaddress), groupAddress, datas)
        {
            Name = "Set Speed";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    public class FfuAAFNewQuerySpeedHandler : FfuAAFNewHandler
    {
        public FfuAAFNewQuerySpeedHandler(FfuAAFNew device, byte fanaddress, byte groupAddress)
            : base(device, 0x15, (byte)(0x20 + fanaddress), groupAddress, null)
        {
            Name = "Query Speed";
            DeviceAddress = fanaddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            Device.Speed = result.Data1 * Device.MaxSpeed2 / 250;//Device.NMaxSpeed 
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }



    public class FfuAAFNewSetSpeedHandler : FfuAAFNewHandler
    {
        public FfuAAFNewSetSpeedHandler(FfuAAFNew device, byte fanaddress, byte groupAddress, byte[] datas)
            : base(device, 0x35, (byte)(0x40 + fanaddress), groupAddress, datas)
        {
            Name = "Set Speed";
            DeviceAddress = fanaddress;
            GroupAddress = groupAddress;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FfuAAFMessage;
            handled = false;
            if (!result.IsResponse) return true;
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }


    public class FfuAAFNewResetDeviceHandler : FfuAAFNewHandler
    {
        public FfuAAFNewResetDeviceHandler(FfuAAFNew device, byte fanaddress, byte groupAddress)
                : base(device, 0x15, (byte)(0x60 + fanaddress), groupAddress, null)
        {
            Name = "Reset Device";
        }

    }

    public class FfuAAFNewQueryStatusHandler : FfuAAFNewHandler
    {
        public FfuAAFNewQueryStatusHandler(FfuAAFNew device, byte fanaddress, byte groupAddress)
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

    
}
