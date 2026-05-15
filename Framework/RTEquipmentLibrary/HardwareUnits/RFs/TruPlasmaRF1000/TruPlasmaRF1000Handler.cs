using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{
    public abstract class TruPlasmaRF1000Handler : HandlerBase
    {
        public TruPlasmaRF1000 Device { get; }

        public string _command;
        protected string _parameter;


        protected TruPlasmaRF1000Handler(TruPlasmaRF1000 device, string command, string parameter = null)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        protected TruPlasmaRF1000Handler(TruPlasmaRF1000 device, string command, byte[] parameter)
        : base(BuildMessage(ToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _GS = 0x00;
        //private static byte _stop = 0x55;

        private static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray)
        {
            List<byte> buffer = new List<byte>();

            int length = 1 + (commandArray != null ? commandArray.Length : 0) + (argumentArray != null ? argumentArray.Length : 0);
            buffer.Add((byte)length);
            buffer.Add(_GS);

            if (commandArray != null && commandArray.Length > 0)
            {
                //skip ManualExecuteCommand
                if (!(commandArray.Length == 1 && commandArray[0] == 0))
                {
                    buffer.AddRange(commandArray);
                }
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray);
            }

            return buffer.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TruPlasmaRF1000Message response = msg as TruPlasmaRF1000Message;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.ErrorCode);
            }
            else
            {
                Device.NoteError(null);
            }

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }


            if (this.IsAcked && response.IsComplete)
            {
                if(response.Command == _command)
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    return true;
                }
                else
                    Device.NoteError($"command mismatch set command={_command} response command={response.Command}");
            }

            transactionComplete = false;
            return false;
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }

    }

    public class TruPlasmaRF1000RawCommandHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000RawCommandHandler(TruPlasmaRF1000 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if(base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //public class TruPlasmaRF1000ReleaseControlHandler : TruPlasmaRF1000Handler
    //{
    //    public TruPlasmaRF1000ReleaseControlHandler(TruPlasmaRF1000 device)
    //        : base(device, "05,02,00,00", "FF")
    //    {
    //    }

    //    public override bool HandleMessage(MessageBase msg, out bool handled)
    //    {
    //        if (base.HandleMessage(msg, out handled))
    //        {
    //            var result = msg as TruPlasmaRF1000Message;
    //            Device.NoteInterfaceActived(false);
    //        }
    //        return true;
    //    }
    //}

    //Pi=forward power
    public class TruPlasmaRF1000PreSetPiValueHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x06;//UINT16
        public TruPlasmaRF1000PreSetPiValueHandler(TruPlasmaRF1000 device, int piValue)
            : base(device, "02,09,00,01", IntToByteArray(piValue))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((ushort)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //Pi=forward power
    public class TruPlasmaRF1000ReadPiValueHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadPiValueHandler(TruPlasmaRF1000 device)
            : base(device, "01,0C,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if(result.Data != null && result.Data.Length == 4)
                {
                    Device.ForwardPower = BitConverter.ToUInt32(result.Data, 0);
                }
            }
            return true;
        }
    }

    //Pr=Reflected power
    public class TruPlasmaRF1000ReadPrValueHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadPrValueHandler(TruPlasmaRF1000 device)
            : base(device, "01,0D,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.ReflectPower = BitConverter.ToUInt32(result.Data, 0);
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ReadProcessStatusHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadProcessStatusHandler(TruPlasmaRF1000 device)
            : base(device, "01,1E,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length > 0)
                {
                    //Bit 0 = Alarm is pending.
                    var status = result.Data[0];
                    Device.IsError = (status & 0x01) > 0;
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1000SetPowerOnOffHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x07;//UINT32
        public TruPlasmaRF1000SetPowerOnOffHandler(TruPlasmaRF1000 device, bool isOn)
            : base(device, "02,6F,00,01", IntToByteArray(isOn ? 1: 0))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((uint)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ReadPowerOnOffHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadPowerOnOffHandler(TruPlasmaRF1000 device)
            : base(device, "01,6F,00,01", "FF")
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.IsPowerOn = BitConverter.ToUInt32(result.Data, 0) > 0;
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1000SetPulseModeHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32
        public TruPlasmaRF1000SetPulseModeHandler(TruPlasmaRF1000 device, EnumRfPowerWorkMode pulseMode)
            : base(device, "02,6A,01,01", IntToByteArray(pulseMode == EnumRfPowerWorkMode.PulsingMode ? 1 : 0))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes(num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ReadPulseModeHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadPulseModeHandler(TruPlasmaRF1000 device)
            : base(device, "01,6A,01,01", "FF")
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.WorkMode = BitConverter.ToInt32(result.Data, 0) == 1 ? EnumRfPowerWorkMode.PulsingMode : EnumRfPowerWorkMode.ContinuousWaveMode;
                }
            }
            return true;
        }
    }

    //820
    public class TruPlasmaRF1000SetFreqHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32
        //unit is Hz
        //the colck offset ode must be set to FrequencyOffset
        public TruPlasmaRF1000SetFreqHandler(TruPlasmaRF1000 device, int freq)
            : base(device, "02,34,03,01", IntToByteArray(freq))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes(num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ReadFreqHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadFreqHandler(TruPlasmaRF1000 device)
            : base(device, "01,63,00,01", "FF")
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.Frequency = BitConverter.ToUInt32(result.Data, 0)/1000.0f;//unit=Hz to kHz
                }
            }
            return true;
        }
    }

    //866
    public class TruPlasmaRF1000SetClockModeHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x05;//UINT8
        //0=manual;1=auto
        public TruPlasmaRF1000SetClockModeHandler(TruPlasmaRF1000 device, EnumRfPowerClockMode clockMode)
            : base(device, "02,62,03,01", IntToByteArray(GetValue(clockMode)))
        {
        }
        private static int GetValue(EnumRfPowerClockMode clockMode)
        {
            if (clockMode == EnumRfPowerClockMode.FreqAuto)
                return 1;
            if (clockMode == EnumRfPowerClockMode.FreqManual)
                return 0;

            return 0;
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.Add((byte)num);
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ReadClockModeHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000ReadClockModeHandler(TruPlasmaRF1000 device)
            : base(device, "01,62,03,01", "FF")
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length > 0)
                {
                    Device.ClockMode = GetMode(result.Data[0]);
                }
            }
            return true;
        }

        private EnumRfPowerClockMode GetMode(int mode)
        {
            if (mode == 1)
                return EnumRfPowerClockMode.FreqAuto;
            if (mode == 0)
                return EnumRfPowerClockMode.FreqManual;

            return EnumRfPowerClockMode.FreqAuto;
        }
    }

    public class TruPlasmaRF1000SetGainHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x06;//UINT16
        public TruPlasmaRF1000SetGainHandler(TruPlasmaRF1000 device, int gain)
            : base(device, "02,5C,03,01", IntToByteArray(gain))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((ushort)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000SetRelativeGainHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x06;//UINT16
        public TruPlasmaRF1000SetRelativeGainHandler(TruPlasmaRF1000 device, int gain)
            : base(device, "02,5D,03,01", IntToByteArray(gain))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((ushort)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //858
    public class TruPlasmaRF1000SetModulationDeviationHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x06;//UINT16
        public TruPlasmaRF1000SetModulationDeviationHandler(TruPlasmaRF1000 device, int deviation)
            : base(device, "02,5A,03,01", IntToByteArray(deviation))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((ushort)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //857
    public class TruPlasmaRF1000SetRelativeModulationDeviationHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x06;//UINT16
        public TruPlasmaRF1000SetRelativeModulationDeviationHandler(TruPlasmaRF1000 device, int deviation)
            : base(device, "02,59,03,01", IntToByteArray(deviation))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes((ushort)num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1000Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1000GetControlHandler : TruPlasmaRF1000Handler
    {
        public TruPlasmaRF1000GetControlHandler(TruPlasmaRF1000 device)
            : base(device, "05,01,00,00", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteInterfaceActived(true);
            }
            return true;
        }
    }

    public class TruPlasmaRF1000ResetHandler : TruPlasmaRF1000Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32
        public TruPlasmaRF1000ResetHandler(TruPlasmaRF1000 device)
            : base(device, "02,4D,00,01", IntToByteArray(0xAA))
        {
        }

        private static byte[] IntToByteArray(int num)
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes(num));
            return tmp.ToArray();
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }
}
