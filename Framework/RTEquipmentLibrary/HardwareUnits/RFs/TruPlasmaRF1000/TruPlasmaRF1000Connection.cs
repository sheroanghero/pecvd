using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.TruPlasmaRF
{

    public class TruPlasmaRF1000Message : BinaryMessage
    {
        public string Command { get; set; }
        public string ErrorCode { get; set; }
        public byte[] Data { get; set; }
    }

    public class TruPlasmaRF1000Connection : TCPPortConnectionBase
    {
        //private static byte _GS = 0x00;
        private List<byte> _msgBuffer = new List<byte>();

        public TruPlasmaRF1000Connection(string address)
            : base(address, "", false)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            if(rawMessage == null || rawMessage.Length == 0)
            {
                return null;
            }
            _msgBuffer.AddRange(rawMessage);
            var length = _msgBuffer[0];
            TruPlasmaRF1000Message msg = new TruPlasmaRF1000Message();
            
            if(_msgBuffer.Count != length + 1)
                return msg;

            msg.RawMessage = _msgBuffer.ToArray();
            _msgBuffer.Clear();          

            if (msg.RawMessage.Length < 7)
            {
                msg.IsFormatError = true;
                LOG.Error($"invalid response");
                return msg;
            }

            //if ((msg.RawMessage[1] & 0b_1000_0000) > 0 || (msg.RawMessage[1] & 0b_0100_0000) > 0)
            //{
            //    msg.IsError = true;
            //    msg.ErrorCode = $"Alarms or error messages are pending";
            //    LOG.Error($"Alarms or error messages are pending");
            //    return msg;
            //}

            if ((msg.RawMessage[1] & 0x08) == 0)
            {
                msg.IsFormatError = true;
                LOG.Error($"interface is not active");
                return msg;
            }

            if (msg.RawMessage[2] == 0xFE)
            {
                msg.IsFormatError = true;
                LOG.Error($"telegram error");
                return msg;
            }

            if (msg.RawMessage[6] != 0xFF && msg.RawMessage[6] != 0x00)
            {
                msg.IsError = true;
                msg.ErrorCode = msg.RawMessage[6].ToString("X2");
            }

            var msgArray = msg.RawMessage.Select(bt => bt.ToString("X2")).ToArray();
            string msgCommand = string.Join(",", msgArray, 2, 4);
            msg.Command = msgCommand;

            if(msg.RawMessage.Length > 7 && msg.RawMessage[6] == 0x00)
                msg.Data = msg.RawMessage.Skip(8).Take(msg.RawMessage.Length - 8).ToArray();
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
    }
}
