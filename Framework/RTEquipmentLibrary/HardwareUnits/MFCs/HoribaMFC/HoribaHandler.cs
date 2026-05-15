using Aitex.Core.RT.Event;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.HoribaMFC
{
    public abstract class HoribaHandler : HandlerBase
    {
        private const byte Header = 0x40;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        public MfcBase MFCDevice { get; }

        protected HoribaHandler(MfcBase mfc, string msdLsdAddress, string data)
            : base(BuildMessage(msdLsdAddress, data))
        {
            MFCDevice = mfc;
        }

        private static byte[] BuildMessage(string msdLsdAddress, string data)
        {
            List<byte> message = new List<byte>();
            message.Add(Header);
            PacketMessage(msdLsdAddress, message);
            message.Add(STX);
            PacketMessage(data, message);
            message.Add(ETX);
            message.Add((byte)CalcBBC(data));
            if(SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
                message.Add(0x0D);
            return message.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }

        private static int CalcBBC(string value)
        {
            int sum = 0;
            foreach (var item in value)
            {
                sum += (int)item;
            }
            sum += ETX;

            return sum % 128;
        }

        private static List<byte> PacketMessage(string msg, List<byte> msgList)
        {
            foreach (var item in msg)
            {
                msgList.Add((byte)item);
            }
            return msgList;
        }

        private static byte[] String2Ascii(string s)
        {
            byte[] array = System.Text.Encoding.ASCII.GetBytes(s);
            return array;
        }
    }

    public class HoribaMFCSetFlow : HoribaHandler
    {
        public HoribaMFCSetFlow(MfcBase mfc, string deviceAddress, float flow)
            : base(mfc, deviceAddress, $"AFC{flow},B")
        {
            Name = "SetFlow";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HoribaMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak)
            {
                Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
            }
            else
            {
                if (result.Datas[0] != 0x4f && result.Datas[0] != 0x4b)
                {
                    Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class HoribaMFCQueryFlow : HoribaHandler
    {
        public HoribaMFCQueryFlow(MfcBase mfc, string deviceAddress)
            : base(mfc, deviceAddress, $"RFV")
        {
            Name = "QueryFlow";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HoribaMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak)
            {
                Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
            }
            else
            {
                float.TryParse(System.Text.Encoding.ASCII.GetString(result.Datas), out float flow);
                MFCDevice.FeedBack = flow;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class HoribaMFCQueryScale : HoribaHandler
    {
        public HoribaMFCQueryScale(MfcBase mfc, string deviceAddress)
            : base(mfc, deviceAddress, $"RFS")
        {
            Name = "QueryScale";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HoribaMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak)
            {
                Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
            }
            else
            {
                var dataArray = System.Text.Encoding.ASCII.GetString(result.Datas).Split(',');

                float.TryParse(dataArray[0], out float range);
                //temporary comment to fix compile issue
                if(dataArray.Length > 1)
                {
                    if (MFCDevice.Unit != dataArray[1].ToUpper())
                        EV.PostInfoLog(MFCDevice.Module, $"{MFCDevice.Module} {MFCDevice.Name} change from {MFCDevice.Unit} to {dataArray[1].ToUpper()}");

                    MFCDevice.Unit = dataArray[1].ToUpper();
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class HoribaMFCSetDigitalMode : HoribaHandler
    {
        public HoribaMFCSetDigitalMode(MfcBase mfc, string deviceAddress)
            : base(mfc, deviceAddress, $"'!+REVERSE")
        {
            Name = "SetDigitalMode";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HoribaMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak)
            {
                Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
            }
            else
            {
                if (result.Datas[0] != 0x4f && result.Datas[0] != 0x4b)
                {
                    Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class HoribaMFCSetAnalogMode : HoribaHandler
    {
        public HoribaMFCSetAnalogMode(MfcBase mfc, string deviceAddress)
            : base(mfc, deviceAddress, $"'!+NORMAL")
        {
            Name = "SetAnalogMode";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as HoribaMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak)
            {
                Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
            }
            else
            {
                if (result.Datas[0] != 0x4f && result.Datas[0] != 0x4b)
                {
                    Singleton<HoribaConnection>.Instance.Horiba.SetError(result.RawMessage);
                }
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

}
