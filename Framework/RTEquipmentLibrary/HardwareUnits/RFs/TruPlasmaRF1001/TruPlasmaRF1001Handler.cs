using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{
    //TruPlasma RF 1001 to 1003
    public abstract class TruPlasmaRF1001Handler : HandlerBase
    {
        public TruPlasmaRF1001 Device { get; }
        public string _command;
        protected string _parameter;

        protected TruPlasmaRF1001Handler(TruPlasmaRF1001 device, string cmd):base("")
        {
            Device = device;
            Name = _command = cmd;
        }

        protected TruPlasmaRF1001Handler(TruPlasmaRF1001 device, string command, string parameter = null)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }

        protected TruPlasmaRF1001Handler(TruPlasmaRF1001 device, string command, byte[] parameter)
        : base(BuildMessage(ToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _start = 0xAA;
        private static byte _address = 0x02;
        private static byte _GS = 0x00;
        private static byte _stop = 0x55;

        protected static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);
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
            //var checkSum = Crc16.CRC16_CCITT(contentBuffer);
            var checkSum = Crc16.Crc16Ccitt(buffer.ToArray());

            buffer.AddRange(BitConverter.GetBytes(checkSum));

            buffer.Add(_stop);

            return buffer.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            transactionComplete = false;
            TruPlasmaRF1001Message response = msg as TruPlasmaRF1001Message;
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
                if (response == null || response.RawMessage == null) return false;
                ushort checkSum_calc = Crc16.Crc16Ccitt(response.RawMessage.Take(response.RawMessage.Length - 3).ToArray());
                ushort checkSum = BitConverter.ToUInt16(response.RawMessage, response.RawMessage.Length - 3);
                if (checkSum_calc != checkSum)
                {
                    LOG.Warning($"{this.Device.Address} received wrong checksum");
                    return false;
                }

                if (response.Command == _command)
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    return true;
                }
                else
                    Device.NoteError($"command mismatch set command={_command} response command={response.Command}");
            }

            return false;
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
    }

    public class TruPlasmaRF1001RawCommandHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001RawCommandHandler(TruPlasmaRF1001 device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001GetControlHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001GetControlHandler(TruPlasmaRF1001 device)
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

    public class TruPlasmaRF1001ReleaseControlHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReleaseControlHandler(TruPlasmaRF1001 device)
            : base(device, "05,02,00,00", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteInterfaceActived(false);
            }
            return true;
        }
    }

    //active RS232
    public class TruPlasmaRF1001SetActiveInterfaceHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//SINT32

        public TruPlasmaRF1001SetActiveInterfaceHandler(TruPlasmaRF1001 device)
            : base(device, "02,52,01,01", IntToByteArray(1))
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

    //Pi=forward power
    public class TruPlasmaRF1001PreSetPiValueHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//SINT32

        public TruPlasmaRF1001PreSetPiValueHandler(TruPlasmaRF1001 device, int piValue)
            : base(device, "02,06,00,01", IntToByteArray(piValue))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //Pi=forward power
    public class TruPlasmaRF1001ReadPiValueHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadPiValueHandler(TruPlasmaRF1001 device)
            : base(device, "01,12,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.ForwardPower = BitConverter.ToInt32(result.Data, 0);
                }
            }
            return true;
        }
    }

    //Pr=Reflected power
    public class TruPlasmaRF1001ReadPrValueHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadPrValueHandler(TruPlasmaRF1001 device)
            : base(device, "01,14,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.ReflectPower = BitConverter.ToInt32(result.Data, 0);
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1001ReadProcessStatusHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadProcessStatusHandler(TruPlasmaRF1001 device)
            : base(device, "01,75,00,01", "FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    //0x00000010 = Power output on
                    //0x00000080 = Alarm pending
                    //0x00008000 = Temperature alarm pending
                    var status = BitConverter.ToUInt32(result.Data, 0);
                    Device.IsPowerOn = (status & 0x00000010) > 0;
                    Device.IsError = (status & 0x00000080) > 0 || (status & 0x00008000) > 0;
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetPowerOnOffHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x07;//UINT32

        public TruPlasmaRF1001SetPowerOnOffHandler(TruPlasmaRF1001 device, bool isOn)
            : base(device, "02,6F,00,01", IntToByteArray(isOn ? 1 : 0))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001ReadPowerOnOffHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadPowerOnOffHandler(TruPlasmaRF1001 device)
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.IsPowerOn = BitConverter.ToUInt32(result.Data, 0) > 0;
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetPulseModeHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetPulseModeHandler(TruPlasmaRF1001 device, EnumRfPowerWorkMode pulseMode)
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001ReadPulseModeHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadPulseModeHandler(TruPlasmaRF1001 device)
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.WorkMode = BitConverter.ToInt32(result.Data, 0) == 1 ? EnumRfPowerWorkMode.PulsingMode : EnumRfPowerWorkMode.ContinuousWaveMode;
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetClockOffsetModeHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetClockOffsetModeHandler(TruPlasmaRF1001 device, bool isFrequencyOffset)
            : base(device, "02,04,02,01", IntToByteArray(isFrequencyOffset ? 1 : 0))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    // Tuning start offset
    public class TruPlasmaRF1001TuningOffsetHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001TuningOffsetHandler(TruPlasmaRF1001 device, int tuningOffset)
            : base(device, "02,46,02,01")
        {
            List<byte> offset = new List<byte>() { _STAT, _TYP };
            offset.AddRange(BitConverter.GetBytes(tuningOffset));

            SendBinary = BuildMessage(ToByteArray(_command), offset.ToArray());
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    // Tuning delay
    public class TruPlasmaRF1001TuningDelayHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001TuningDelayHandler(TruPlasmaRF1001 device, int delay)
            : base(device, "02,49,02,01")
        {
            List<byte> tmp = new List<byte>() { _STAT, _TYP };
            tmp.AddRange(BitConverter.GetBytes(delay));

            SendBinary = BuildMessage(ToByteArray(_command), tmp.ToArray());
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetFreqHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        //unit is Hz
        //the colck offset ode must be set to FrequencyOffset
        public TruPlasmaRF1001SetFreqHandler(TruPlasmaRF1001 device, int freq)
            : base(device, "02,C9,01,01", IntToByteArray(freq))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    //457
    public class TruPlasmaRF1001ReadFreqOffsetHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadFreqOffsetHandler(TruPlasmaRF1001 device)
            : base(device, "01,C9,01,01", "FF")
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.Frequency = BitConverter.ToInt32(result.Data, 0) / 1000;//unit kHz
                }
            }
            return true;
        }
    }

    //519
    public class TruPlasmaRF1001ReadActualFreqHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadActualFreqHandler(TruPlasmaRF1001 device)
            : base(device, "01,07,02,01", "FF")
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.Frequency = BitConverter.ToInt32(result.Data, 0);//unit kHz
                }
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetClockModeHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        //0=Phase offset;1=Frequency offset;2=Frequency agility
        public TruPlasmaRF1001SetClockModeHandler(TruPlasmaRF1001 device, EnumRfPowerClockMode clockMode)
            : base(device, "02,04,02,01", IntToByteArray(GetValue(clockMode)))
        {
        }

        private static int GetValue(EnumRfPowerClockMode clockMode)
        {
            if (clockMode == EnumRfPowerClockMode.FreqAuto)
                return 2;
            if (clockMode == EnumRfPowerClockMode.FreqManual)
                return 1;

            return 0;
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001ReadClockModeHandler : TruPlasmaRF1001Handler
    {
        public TruPlasmaRF1001ReadClockModeHandler(TruPlasmaRF1001 device)
            : base(device, "01,04,02,01", "FF")
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                if (result.Data != null && result.Data.Length == 4)
                {
                    Device.ClockMode = GetMode(BitConverter.ToInt32(result.Data, 0));
                }
            }
            return true;
        }

        private EnumRfPowerClockMode GetMode(int mode)
        {
            if (mode == 2)
                return EnumRfPowerClockMode.FreqAuto;
            if (mode == 1)
                return EnumRfPowerClockMode.FreqManual;

            return EnumRfPowerClockMode.Phase;
        }
    }

    public class TruPlasmaRF1001SetGainHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetGainHandler(TruPlasmaRF1001 device, int gain)
            : base(device, "02,4C,02,01", IntToByteArray(gain))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetRelativeGainHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetRelativeGainHandler(TruPlasmaRF1001 device, int gain)
            : base(device, "02,4D,02,01", IntToByteArray(gain))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetModulationDeviationHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetModulationDeviationHandler(TruPlasmaRF1001 device, int deviation)
            : base(device, "02,4E,02,01", IntToByteArray(deviation))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001SetRelativeModulationDeviationHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001SetRelativeModulationDeviationHandler(TruPlasmaRF1001 device, int deviation)
            : base(device, "02,4F,02,01", IntToByteArray(deviation))
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
                var result = msg as TruPlasmaRF1001Message;
                Device.NoteRawCommandInfo(_command, string.Join(",", result.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
            }
            return true;
        }
    }

    public class TruPlasmaRF1001ResetHandler : TruPlasmaRF1001Handler
    {
        private static byte _STAT = 0x00;//No error, data follows.
        private static byte _TYP = 0x04;//INT32

        public TruPlasmaRF1001ResetHandler(TruPlasmaRF1001 device)
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