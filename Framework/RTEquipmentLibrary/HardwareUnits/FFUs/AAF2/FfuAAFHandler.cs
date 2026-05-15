using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF2
{
    public abstract class FfuAAFHandler : HandlerBase
    {
        public FfuAAF Device { get; }
        public byte DeviceAddress;
        public byte GroupAddress;

        public bool IsNeedFeedback;

        protected FfuAAFHandler(FfuAAF device, byte prefix, byte command, byte groupAddress, byte[] datas)
            : base(BuildMessage(prefix, command, groupAddress, datas))
        {
            Device = device;
            IsNeedFeedback = true;

        }
        protected FfuAAFHandler(FfuAAF device, byte prefix, byte command, byte groupAddress, byte[] datas, bool needfeedback)
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
    public class FfuAAFSetGroupAddressHandler : FfuAAFHandler
    {
        public FfuAAFSetGroupAddressHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] datas)
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
    public class FfuAAFGetGroupAddressHandler : FfuAAFHandler
    {
        public FfuAAFGetGroupAddressHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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
    public class FfuAAFSetAddressHandler : FfuAAFHandler
    {
        public FfuAAFSetAddressHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] datas)
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
    public class FfuAAFGetAddressHandler : FfuAAFHandler
    {
        public FfuAAFGetAddressHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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
    public class FfuAAFQueryMaxSpeed1Handler : FfuAAFHandler
    {
        public FfuAAFQueryMaxSpeed1Handler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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
    public class FfuAAFQueryMaxSpeed2Handler : FfuAAFHandler
    {
        public FfuAAFQueryMaxSpeed2Handler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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
    public class FfuAAFQueryMaxSpeed3Handler : FfuAAFHandler
    {
        public FfuAAFQueryMaxSpeed3Handler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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

    public class FfuAAFQuerySpeedFactorHandler : FfuAAFHandler
    {
        public FfuAAFQuerySpeedFactorHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] values)
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



    public class FfuAAFSetSpeedFactorHandler : FfuAAFHandler
    {
        public FfuAAFSetSpeedFactorHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] datas)
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

    public class FfuAAFQuerySpeedHandler : FfuAAFHandler
    {
        public FfuAAFQuerySpeedHandler(FfuAAF device, byte fanaddress, byte groupAddress)
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



    public class FfuAAFSetSpeedHandler : FfuAAFHandler
    {
        public FfuAAFSetSpeedHandler(FfuAAF device, byte fanaddress, byte groupAddress, byte[] datas)
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


    public class FfuAAFResetDeviceHandler : FfuAAFHandler
    {
        public FfuAAFResetDeviceHandler(FfuAAF device, byte fanaddress, byte groupAddress)
                : base(device, 0x15, (byte)(0x60 + fanaddress), groupAddress, null)
        {
            Name = "Reset Device";
        }

    }

    public class FfuAAFQueryStatusHandler : FfuAAFHandler
    {
        public FfuAAFQueryStatusHandler(FfuAAF device, byte fanaddress, byte groupAddress)
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
