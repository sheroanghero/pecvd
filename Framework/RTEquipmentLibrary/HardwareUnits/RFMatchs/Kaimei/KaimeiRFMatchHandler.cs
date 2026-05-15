using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Kaimei
{
    public abstract class KaimeiRFMatchHandler : HandlerBase
    {
        public KaimeiRFMatch Device { get; }

        public string _command;
        protected string _parameter;

        protected KaimeiRFMatchHandler(KaimeiRFMatch device, string command)
            : base(BuildMessage(ToByteArray(command)))
        {
            Device = device;
            _command = command;
            _address = device.SlaveAddress;
            Name = command;
        }


        protected KaimeiRFMatchHandler(KaimeiRFMatch device, string command, ushort parameter)
            : base(BuildMessage(ToByteArray(command), UshortToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter.ToString();
            _address = device.SlaveAddress;
            Name = command;
        }
        protected KaimeiRFMatchHandler(KaimeiRFMatch device, string command, string parameter1, ushort parameter2, ushort parameter3)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter1), UshortToByteArray(parameter2), UshortToByteArray(parameter3)))
        {
            Device = device;
            _command = command;
            _parameter = $"{parameter1},{parameter2},{parameter3}";
            _address = device.SlaveAddress;
            Name = command;
        }

        protected KaimeiRFMatchHandler(KaimeiRFMatch device, string command, string parameter)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            _address = device.SlaveAddress;
            Name = command;
        }

        protected KaimeiRFMatchHandler(KaimeiRFMatch device, string command, byte[] parameter)
        : base(BuildMessage(ToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _address = 0x01;

        protected static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray1 = null, byte[] argumentArray2 = null, byte[] argumentArray3 = null)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_address);

            if (commandArray != null && commandArray.Length > 0)
            {
                buffer.AddRange(commandArray);
            }
            if (argumentArray1 != null && argumentArray1.Length > 0)
            {
                buffer.AddRange(argumentArray1);
            }
            if (argumentArray2 != null && argumentArray2.Length > 0)
            {
                buffer.AddRange(argumentArray2.Reverse());
            }
            if (argumentArray3 != null && argumentArray3.Length > 0)
            {
                buffer.AddRange(argumentArray3.Reverse());
            }
            var checkSum = Crc16.CRC16_ModbusRTU(buffer.ToArray());
            var ret = BitConverter.GetBytes(checkSum);
            buffer.AddRange(ret);

            return buffer.ToArray();
        }
        protected bool HandleMessage(MessageBase msg)
        {
            KaimeiRFMatchMessage response = msg as KaimeiRFMatchMessage;
            if (!response.IsComplete)
                return false;
            return true;
        }

        protected static string Ushort2String(ushort num)
        {
            var bytes = UshortToByteArray(num);
            return string.Join(",", bytes,0);
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
        protected static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected static byte[] UshortToByteArray(ushort num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected static byte[] ShortToByteArray(short num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected int ByteArrayToInt(byte[] bytes)
        {
            if (bytes.Length == 4)
            {
                int temp = BitConverter.ToInt32(bytes, 0);
                return temp;
            }
            return 0;
        }
        protected short ByteArrayToShort(byte[] bytes)
        {
            if (bytes.Length == 2)
            {
                short temp = BitConverter.ToInt16(bytes, 0);
                return temp;
            }
            return 0;
        }

        protected ushort ByteArrayToUshort(byte[] bytes)
        {
            if (bytes.Length == 2)
            {
                ushort temp = BitConverter.ToUInt16(bytes, 0);
                return temp;
            }
            return 0;
        }
    }

    public class KaimeiRFMatchSetCurrentValveHandler : KaimeiRFMatchHandler
    {
        public KaimeiRFMatchSetCurrentValveHandler(KaimeiRFMatch device)
            : base(device, "10", $"00,08,00,02,04,FF,FF,FF,FF")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as KaimeiRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class KaimeiRFMatchPresetValveHandler : KaimeiRFMatchHandler
    {
        public KaimeiRFMatchPresetValveHandler(KaimeiRFMatch device, EnumRfMatchTuneMode mode, float tune, float load)
            : base(device, "10", $"00,05,00,03,06,{(mode == EnumRfMatchTuneMode.Auto ? "00,00":"00,01")}", (ushort)((0x1 << 12) + (((int)tune * 10) & 0x0FFF)), (ushort)((0x1 << 12) + (((int)load * 10) & 0x0FFF)))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as KaimeiRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class KaimeiRFMatchGetStatusHandler : KaimeiRFMatchHandler
    {
        public KaimeiRFMatchGetStatusHandler(KaimeiRFMatch device)
            : base(device, "03", "00,00,00,06")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as KaimeiRFMatchMessage;
                var datas = result.Data.ToList();
                if (datas.Count == 12)//ox00 no error
                {
                    var tuneArray = datas.Skip(6).Take(2).Reverse().ToArray();
                    var loadArray = datas.Skip(8).Take(2).Reverse().ToArray();
                    var modeArray = datas.Skip(10).Take(2).Reverse().ToArray();

                    Device.TunePosition1 = BitConverter.ToInt16(tuneArray,0)/10.0f;
                    Device.LoadPosition1 = BitConverter.ToInt16(loadArray, 0)/10.0f;
                    Device.TuneMode1 = modeArray[1] == 0x00 ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;

                    var reason = string.Empty;
                    Device.NoteError(reason);
                }

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }
}
