using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FujikinMFC
{

    public class FujikinMFCMessage : BinaryMessage
    {
        public string CommandType { get; set; }
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class FujikinMFCConnection : SerialPortConnectionBase
    {

        private static byte _start = 0x02;
        //private static byte _end = 0x00;
        private static byte _ack = 0x06;
        private static byte _nak = 0x16;

        private static byte _address = 0x00;



        public FujikinMFCConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            FujikinMFCMessage msg = new FujikinMFCMessage();
            msg.RawMessage = rawMessage;
            //msg.Data = rawMessage;

            if (rawMessage.Length == 1)
            {
                if(rawMessage[0] == _ack)
                {
                    msg.IsAck = true;
                }
                else if(rawMessage[0] == _nak)
                {
                    msg.IsNak = true;
                }
                else
                {
                    msg.IsFormatError = true;
                    LOG.Error($"invalid response");
                }
                return msg;
            }


            if (rawMessage.Length < 7)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }


            if (rawMessage[0] != _address)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid address byte");
                return msg;
            }
            if (rawMessage[1] != _start)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid start byte");
                return msg;
            }

            byte checkSum = 0;
            for (int i = 1; i < rawMessage.Length-1; i++)
            {
                checkSum += rawMessage[i];
            }
            var recSum = rawMessage[rawMessage.Length - 1];
            if (recSum != checkSum)
            {
                LOG.Error($"check sum failed,");
                msg.IsFormatError = true;
                return msg;
            }
            msg.CommandType = rawMessage[2].ToString("X2");
            var commandArray = rawMessage.Skip(4).Take(3).ToArray();
            msg.Command = string.Join(",", commandArray.Select(bt => bt.ToString("X2")).ToArray());
            msg.Data = rawMessage.Skip(7).Take(rawMessage.Length - 9).ToArray();
            msg.IsResponse = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}
