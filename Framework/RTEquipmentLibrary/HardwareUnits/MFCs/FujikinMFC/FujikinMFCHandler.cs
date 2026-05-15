using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FujikinMFC
{

    public abstract class FujikinMFCHandler : HandlerBase
    {
        public FujikinMFC Device { get; }

        public string _command;
        public string _commandType;

        private static byte _start = 0x02;
        private static byte _end = 0x00;
        //private static byte _ack = 0x06;
        //private static byte _nak = 0x16;

        private static byte _address = 0x21;


        public string DevicePropName;


        protected FujikinMFCHandler(FujikinMFC device, string commandType, string command, string parameter = null)
            : base(BuildMessage(commandType,command,parameter))
        {
            Device = device;
            _command = command;
            _commandType = commandType;
            _address = Convert.ToByte(Device.Address, 16);
            Name = command;
        }
        protected FujikinMFCHandler(FujikinMFC device, string commandType, string command, byte[] parameter)
        : base(BuildMessage(commandType, command, parameter))
        {
            Device = device;
            _command = command;
            _commandType = commandType;
            _address = Convert.ToByte(Device.Address, 16);
            Name = command;
        }
        private static byte[] BuildMessage(string commandType, string command, string parameter)
        {
            List<byte> buffer = new List<byte>();
            var parameterArray = parameter!=null? parameter.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray():null;
            buffer.Add(_address);
            buffer.Add(_start);
            buffer.Add(Convert.ToByte(commandType, 16));
            buffer.Add((byte)(3 + (parameterArray!=null ? parameterArray.Length:0)));
            buffer.AddRange(command.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            if(parameterArray != null)
                buffer.AddRange(parameterArray);
            buffer.Add(_end);

            byte checkSum = 0;
            for (int i = 1; i < buffer.Count - 1; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }

        protected static byte[] BuildMessage(string commandType, string command, byte[] parameterArray = null)
        {
            List<byte> buffer = new List<byte>();
            buffer.Add(_address);
            buffer.Add(_start);
            buffer.Add(Convert.ToByte(commandType, 16));
            buffer.Add((byte)(3 + (parameterArray != null ? parameterArray.Length : 0)));
            buffer.AddRange(command.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            if (parameterArray != null)
                buffer.AddRange(parameterArray);
            buffer.Add(_end);

            byte checkSum = 0;
            for (int i = 1; i < buffer.Count - 1; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            FujikinMFCMessage response = msg as FujikinMFCMessage;
            ResponseMessage = msg;

            if (response.IsNak)
            {
                Device.NoteNAK();
                transactionComplete = true;
                return true;
            }
            else if (response.IsAck)
            {
                if (this.IsAcked)
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    return true;
                }
                else
                {
                    SetState(EnumHandlerState.Acked);
                    transactionComplete = false;
                    return false;
                }
            }
            else if(response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }
            else
            {
                msg.IsFormatError = true;
                transactionComplete = false;
                return false;
            }
        }



        protected static byte[] StringToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
        protected static byte[] ShortToByteArray(short num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected static byte[] FloarToByteArray(float num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected int ByteArrayToInt(byte[] bytes)
        {
            int temp = BitConverter.ToInt32(bytes, 0);
            return temp;
        }
        protected short ByteArrayToShort(byte[] bytes)
        {
            short temp = BitConverter.ToInt16(bytes, 0);
            return temp;
        }
        protected float ByteArrayToFloat(byte[] bytes)
        {
            if(bytes.Length != 4)
            {
                return 0;
            }
            float temp = BitConverter.ToSingle(bytes, 0);
            return temp;
        }
        protected void SetDevicePropValue<T>(string propName, T value)
        {
            try
            {
                Type objType = Device.GetType();
                objType.GetProperty(propName).SetValue(Device, value, null);
            }
            catch
            {
            }
        }

    }

    public class FujikinMFCRawCommandHandler : FujikinMFCHandler
    {
        public FujikinMFCRawCommandHandler(FujikinMFC device, string commandType, string command, string parameter)
            : base(device, commandType, command, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            if(base.HandleMessage(msg, out transactionComplete))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteRawCommandInfo(_commandType, _command, string.Join(",", response.RawMessage.Select(bt => bt.ToString("X2")).ToArray()), msg.IsAck);
                return true;
            }
            return false;
        }
    }

    public class FujikinMFCResetHandler : FujikinMFCHandler
    {
        public FujikinMFCResetHandler(FujikinMFC device)
            : base(device, "81","01,01,C7")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteResetCompleted();
                return true;
            }
            return false;
        }
    }

    public class FujikinMFCSetEnableHandler : FujikinMFCHandler
    {
        public FujikinMFCSetEnableHandler(FujikinMFC device, string command, string enableValue)
            : base(device, "81", command, BoolToByte(enableValue))
        {
        }
        private static string BoolToByte(string boolStr)
        {
            return boolStr == "true" ? "01" : "00";
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteActionCompleted();
                return true;
            }
            return false;
        }
    }


    public class FujikinMFCSet8BitValueHandler : FujikinMFCHandler
    {
        public FujikinMFCSet8BitValueHandler(FujikinMFC device, string command, byte value)
            : base(device, "81", command, new byte[] { value })
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteActionCompleted();
                return true;
            }
            return false;
        }
    }
    public class FujikinMFCSet16BitValueHandler : FujikinMFCHandler
    {
        public FujikinMFCSet16BitValueHandler(FujikinMFC device, string command, short value)
            : base(device, "81", command, ShortToByteArray(value))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteActionCompleted();
                return true;
            }
            return false;
        }
    }
    public class FujikinMFCSet32BitValueHandler : FujikinMFCHandler
    {
        public FujikinMFCSet32BitValueHandler(FujikinMFC device, string command, int value)
            : base(device, "81", command, IntToByteArray(value))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                Device.NoteActionCompleted();
                return true;
            }
            return false;
        }
    }
    public class FujikinMFCReadExceptionStatusHandler : FujikinMFCHandler
    {
        public FujikinMFCReadExceptionStatusHandler(FujikinMFC device)
            : base(device, "80", "65,01,A0")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if(response.Data.Length != 1)
                {
                    Device.NoteError("invalid ExceptionStatus");
                }
                else
                {
                    Device.NoteExcetionInfo(response.Data[0]);
                    return true;
                }

            }
            return false;
        }
    }
    public class FujikinMFCReadAlarmDetailHandler : FujikinMFCHandler
    {
        public FujikinMFCReadAlarmDetailHandler(FujikinMFC device)
            : base(device, "80", "65,01,A1")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 2)
                {
                    Device.NoteError("invalid AlarmDetai");
                }
                else
                {
                    Device.NoteAlarmDetail(ByteArrayToShort(response.Data));
                    return true;
                }

            }
            return false;
        }
    }
    public class FujikinMFCReadWarningDetailHandler : FujikinMFCHandler
    {
        public FujikinMFCReadWarningDetailHandler(FujikinMFC device)
            : base(device, "80", "65,01,A2")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 1)
                {
                    Device.NoteError("invalid WarningDetail");
                }
                else
                {
                    Device.NoteWarningDetail(ByteArrayToShort(response.Data));
                    return true;
                }
            }
            return false;
        }
    }

    public class FujikinMFCReadEnableHandler : FujikinMFCHandler
    {
        
        public FujikinMFCReadEnableHandler(FujikinMFC device, string command, string propName)
            : base(device, "80", command)
        {
            DevicePropName = propName;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 1)
                {
                    Device.NoteError("invalid Enable response");
                }
                else
                {
                    SetDevicePropValue<bool>(DevicePropName, response.Data[0] == 1);
                    return true;
                }
            }
            return false;
        }
    }

    public class FujikinMFCRead8BitHandler : FujikinMFCHandler
    {
        public FujikinMFCRead8BitHandler(FujikinMFC device, string command, string propName)
            : base(device, "80", command)
        {
            DevicePropName = propName;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 1)
                {
                    Device.NoteError("invalid 8Bit response");
                }
                else
                {
                    SetDevicePropValue<short>(DevicePropName, (short)response.Data[0]);
                    return true;
                }
            }
            return false;
        }
    }

    public class FujikinMFCRead16BitHandler : FujikinMFCHandler
    {
        public FujikinMFCRead16BitHandler(FujikinMFC device, string command, string propName)
            : base(device, "80", command)
        {
            DevicePropName = propName;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 2)
                {
                    Device.NoteError("invalid 16Bit response");
                }
                else
                {
                    SetDevicePropValue<short>(DevicePropName, ByteArrayToShort(response.Data));
                    return true;
                }
            }
            return false;
        }
    }

    public class FujikinMFCRead32BitHandler : FujikinMFCHandler
    {
        public FujikinMFCRead32BitHandler(FujikinMFC device, string command, string propName)
            : base(device, "80", command)
        {
            DevicePropName = propName;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                FujikinMFCMessage response = msg as FujikinMFCMessage;
                if (response.Data.Length != 4)
                {
                    Device.NoteError("invalid 32Bit response");
                }
                else
                {
                    SetDevicePropValue<int>(DevicePropName, ByteArrayToInt(response.Data));
                    return true;
                }
            }
            return false;
        }
    }
}
