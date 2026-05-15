using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.BaecChiller
{
    //Modbus RTU
    public abstract class BaecChillerHandler : HandlerBase
    {
        public BaecChiller Device { get; }

        public string _command;
        protected string _parameter;

        protected BaecChillerHandler(BaecChiller device, string command)
            : base(BuildMessage(ToByteArray(command)))
        {
            Device = device;
            _command = command;
            _address = device.SlaveAddress;
            Name = command;
        }


        protected BaecChillerHandler(BaecChiller device, string command, string parameter1, short parameter2)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter1), ShortToByteArray(parameter2)))
        {
            Device = device;
            _command = command;
            _parameter = $"{parameter1},{parameter2}";
            _address = device.SlaveAddress;
            Name = command;
        }
        protected BaecChillerHandler(BaecChiller device, string command, string parameter1, short parameter2, short parameter3)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter1), ShortToByteArray(parameter2), ShortToByteArray(parameter3)))
        {
            Device = device;
            _command = command;
            _parameter = $"{parameter1},{parameter2},{parameter3}";
            _address = device.SlaveAddress;
            Name = command;
        }

        protected BaecChillerHandler(BaecChiller device, string command, string parameter)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            _address = device.SlaveAddress;
            Name = command;
        }

        protected BaecChillerHandler(BaecChiller device, string command, byte[] parameter)
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
            RisshiChillerMessage response = msg as RisshiChillerMessage;
            if (!response.IsComplete)
                return false;
            return true;
        }

        protected static string Ushort2String(ushort num)
        {
            var bytes = UshortToByteArray(num);
            return string.Join(",", bytes, 0);
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

    public class BaecChillerSetOnOffHandler : BaecChillerHandler
    {
        public BaecChillerSetOnOffHandler(BaecChiller device, bool isCH1, bool isOn)
            : base(device, "06", $"{(isCH1 ? "00,00" : "00,01")},{(isOn ? "00,01" : "00,00")}")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as RisshiChillerMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class BaecChillerSetTemperatureHandler : BaecChillerHandler
    {
        public BaecChillerSetTemperatureHandler(BaecChiller device, bool isCH1, float temp)
            : base(device, "06", $"{(isCH1 ? "00,03" : "00,04")}", (short)(temp * 10))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as RisshiChillerMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class BaecChillerSetTemperatureWarningRangeHandler : BaecChillerHandler
    {
        public BaecChillerSetTemperatureWarningRangeHandler(BaecChiller device, bool isCH1, float tempHighLimit, float tempLowLimit)
            : base(device, "10", $"{(isCH1 ? "00,06" : "00,08")}", (short)(tempHighLimit * 10), (short)(tempLowLimit * 10))
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as RisshiChillerMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class BaecChillerGetStatusHandler : BaecChillerHandler
    {
        public BaecChillerGetStatusHandler(BaecChiller device)
            : base(device, "03", "00,0C,00,0E")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as RisshiChillerMessage;
                var datas = result.Data.ToList();
                if (datas.Count == 28)
                {
                    var reason = string.Empty;
                    var tempCH1 = datas.Skip(0).Take(2).Reverse().ToArray();
                    var tempCH2 = datas.Skip(2).Take(2).Reverse().ToArray();
                    var flowCH1 = datas.Skip(6).Take(2).Reverse().ToArray();
                    var flowCH2 = datas.Skip(8).Take(2).Reverse().ToArray();
                    var warningCH1 = datas.Skip(12).Take(2).Reverse().ToArray();
                    var warningCH2 = datas.Skip(14).Take(2).Reverse().ToArray();
                    var alarmCH1 = datas.Skip(18).Take(2).Reverse().ToArray();
                    var alarmCH2 = datas.Skip(20).Take(2).Reverse().ToArray();
                    var stateCH1 = datas.Skip(24).Take(2).Reverse().ToArray();
                    var stateCH2 = datas.Skip(26).Take(2).Reverse().ToArray();

                    Device.CH1TemperatureFeedback = BitConverter.ToInt16(tempCH1, 0) / 10.0f;
                    Device.CH2TemperatureFeedback = BitConverter.ToInt16(tempCH2, 0) / 10.0f;
                    Device.CH1WaterFlow = BitConverter.ToUInt16(flowCH1, 0) / 10.0f;
                    Device.CH2WaterFlow = BitConverter.ToUInt16(flowCH2, 0) / 10.0f;

                    var warningCH1Status = BitConverter.ToInt16(warningCH1, 0);
                    var warningCH2Status = BitConverter.ToInt16(warningCH2, 0);
                    Device.IsCH1Warning = (warningCH1Status & 0b_0000_0001) > 0;
                    Device.IsCH2Warning = (warningCH2Status & 0b_0000_0001) > 0;
                    if (Device.IsCH1Warning)
                    {
                        reason += (warningCH1Status & 0b_0000_0010) > 0 ? "CH1 water level low warning," : "";
                        reason += (warningCH1Status & 0b_0000_0100) > 0 ? "CH1 water temperature high warning," : "";
                        reason += (warningCH1Status & 0b_0000_1000) > 0 ? "CH1 water temperature low warning," : "";
                    }
                    if (Device.IsCH2Warning)
                    {
                        reason += (warningCH2Status & 0b_0000_0010) > 0 ? "CH2 water level low warning," : "";
                        reason += (warningCH2Status & 0b_0000_0100) > 0 ? "CH2 water temperature high warning," : "";
                        reason += (warningCH2Status & 0b_0000_1000) > 0 ? "CH2 water temperature low warning," : "";
                    }

                    var alarmCH1Status = BitConverter.ToInt16(alarmCH1, 0);
                    var alarmCH2Status = BitConverter.ToInt16(alarmCH2, 0);
                    Device.IsCH1Alarm = (alarmCH1Status & 0b_0000_0001) > 0;
                    Device.IsCH2Alarm = (alarmCH2Status & 0b_0000_0001) > 0;
                    if (Device.IsCH1Alarm)
                    {
                        reason += (alarmCH1Status & 0b_0000_0010) > 0 ? "CH1 water level low alarm," : "";
                        reason += (alarmCH1Status & 0b_0000_0100) > 0 ? "CH1 water temperature high alarm," : "";
                        reason += (alarmCH1Status & 0b_0000_1000) > 0 ? "CH1 water temperature low alarm," : "";
                        reason += (alarmCH1Status & 0b_0001_0000) > 0 ? "CH1 water flow low alarm," : "";
                    }
                    if (Device.IsCH2Alarm)
                    {
                        reason += (alarmCH2Status & 0b_0000_0010) > 0 ? "CH2 water level low alarm," : "";
                        reason += (alarmCH2Status & 0b_0000_0100) > 0 ? "CH2 water temperature high alarm," : "";
                        reason += (alarmCH2Status & 0b_0000_1000) > 0 ? "CH2 water temperature low alarm," : "";
                        reason += (alarmCH2Status & 0b_0001_0000) > 0 ? "CH2 water flow low alarm," : "";
                    }

                    Device.IsCH1On = (BitConverter.ToInt16(stateCH1, 0) & 0b_0000_0001) > 0;
                    Device.IsCH2On = (BitConverter.ToInt16(stateCH2, 0) & 0b_0000_0001) > 0;

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
