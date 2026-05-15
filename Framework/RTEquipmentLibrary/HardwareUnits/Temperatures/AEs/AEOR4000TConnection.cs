using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.AEs
{
    public class AETempMessage : BinaryMessage
    {
        public byte DeviceAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; }

    }
    public class AETempAsciiMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
    }

    class AEOR4000TConnection : TCPPortConnectionBase
    {
        private List<byte> _lstCacheBuffer = new List<byte>();
        public AEOR4000TConnection(string address) : base(address, "\r\n", true)
        {

        }

        public override bool SendMessage(string message)
        {
            return base.SendMessage(message);
        }
        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }
        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();

            AETempMessage msg = new AETempMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();

            if (temps.Length < 9) return msg;

            msg.DeviceAddress = temps[6];
            msg.FunctionCode = temps[7];
            if (msg.FunctionCode == 0x03)
            {
                msg.Length = temps[8];
                if (temps.Length != 9)
                {
                    if (temps.Length < msg.Length + 9) return msg;
                }
                msg.Data = _lstCacheBuffer.Skip(9).Take(msg.Length).ToArray();
            }
            else
            {
                msg.Data = _lstCacheBuffer.ToArray();
            }
            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }
        protected override MessageBase ParseResponse(string rawText)
        {
            AETempAsciiMessage msg = new AETempAsciiMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response");
                msg.IsFormatError = true;
                return msg;
            }
            //if (rawText.Length <= 4)
            //{
            //    LOG.Error($"too short response");
            //    msg.IsFormatError = true;
            //    return msg;
            //}
            msg.Data = rawText.Replace("\r", "").Replace("\n", "");
            if (msg.Data.ToUpper().StartsWith("ERR"))
            {
                var errorCodeString = msg.Data.ToUpper().Replace("ERR", "").Replace(" ", "");
                int.TryParse(errorCodeString, out int errorCode);
                if (errorCode > 0)
                {
                    msg.ErrorText = errorCodeString;
                    msg.IsError = true;
                }
            }

            msg.IsResponse = true;
            msg.IsAck = true;

            return msg;
        }
    }
 }
