using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Wattsine
{

    public class WattsineRFMessage:BinaryMessage
    {
        public string Command { get; set; }

        public string ErrorCode { get; set; }

        public byte[] Data { get; set; }
    }
    public class WattsineRFConnection:SerialPortConnectionBase
    {
        public WattsineRFConnection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            :base(portName,baudRate,dataBits,parity,stopBits,"",false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage) 
        {
            WattsineRFMessage msg = new WattsineRFMessage();

            msg.RawMessage = rawMessage;

            var contentBuffer = rawMessage.Skip(0).Take(rawMessage.Length-2).ToArray();

            var recSum = (rawMessage[rawMessage.Length - 2] * 256 + rawMessage[rawMessage.Length - 1]);

            var checkSum = Crc16.Crc16Ccitt(contentBuffer);

            if(recSum!= checkSum) 
            {
                LOG.Error($"check sum failed");
                msg.IsFormatError = true;
                return msg;
            }


            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;
            return msg;
        }
    }
}
