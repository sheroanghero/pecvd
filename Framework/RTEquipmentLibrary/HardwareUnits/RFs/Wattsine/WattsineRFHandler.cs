using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Wattsine
{
    public class WattsineRFHandler : HandlerBase
    {
        public WattsineRF Device { get; set; }

        public string _command;

        protected string _parameter;

        private static byte _cId = 0x01;

        public WattsineRFHandler(WattsineRF device,string command,string parameter = null) : base(BuildMessage(ToByteArray(command), ToByteArray(parameter))) 
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            WattsineRFMessage response = msg as WattsineRFMessage;
            ResponseMessage = msg;


            transactionComplete = true;
            return true;
        }

        private static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_cId);

            if (commandArray != null && commandArray.Length > 0)
            {
                if (!(commandArray.Length == 1 && commandArray[0] == 0))
                {
                    buffer.AddRange(commandArray);
                }
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray);
            }

            var checkSum = Crc16.Crc16Ccitt(buffer.ToArray());

            //buffer.AddRange(BitConverter.GetBytes(checkSum));
            buffer.Add((byte)(checkSum >> 8));
            buffer.Add((byte)(checkSum & 0xFF));

            return buffer.ToArray();
        }


        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
    }

    public class WattsineRFReadRegisterHandler : WattsineRFHandler
    {
        public WattsineRFReadRegisterHandler(WattsineRF device,string adress,string readLength)
            : base(device,"03", $"{ adress},{ readLength}")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as WattsineRFMessage;


                //Device.NoteInterfaceActived(true);
            }
            return true;
        }
    }

    public class WattsineRFWriteRegisterHandler : WattsineRFHandler
    {
        public WattsineRFWriteRegisterHandler(WattsineRF device, string adress,string commnad)
            : base(device, "06", $"{ adress},{commnad}")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as WattsineRFMessage;


                //Device.NoteInterfaceActived(true);
            }
            return true;
        }
    }

    public class WattsineRFWriteMultiRegisterHandler : WattsineRFHandler
    {
        public WattsineRFWriteMultiRegisterHandler(WattsineRF device, string adress,string registerNumber,string writeLength,string multiValue)
            : base(device, "10", $"{adress},{registerNumber},{writeLength},{multiValue}")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as WattsineRFMessage;


                //Device.NoteInterfaceActived(true);
            }
            return true;
        }
    }


}
