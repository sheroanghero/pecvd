using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.STeam
{
    public class STeamRFMatchMessage : BinaryMessage
    {
        public byte[] Data { get; set; }
        public string ErrorCode { get; set; }
    }
    public class STeamRFMatchConnection : SerialPortConnectionBase
    {
        private List<byte> _msgBuffer = new List<byte>();


        STeamRFMatch _device;
        public STeamRFMatchConnection(STeamRFMatch device, string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r\n", false)
        {
            _device = device;
        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            STeamRFMatchMessage msg = new STeamRFMatchMessage();
            if (rawMessage == null || rawMessage.Length < 1)
            {
                return null;
            }
            _msgBuffer.AddRange(rawMessage);

            if (_msgBuffer[0] == 0x80 && _msgBuffer[1] == 0x1C && _msgBuffer[2] == 0x11)
            {
                msg.Data = _msgBuffer.Skip(_msgBuffer.IndexOf(0x04) + 3).Take(30).ToArray();
            }

            _msgBuffer.RemoveRange(0, rawMessage.Length);


            msg.IsComplete = true;
            return msg;
        }
    }
}
