using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Mkss
{
    public class MksRfPowerMessage : AsciiMessage
    {
    }

    public class MksRfPowerConnection : SerialPortConnectionBase
    {
        public MksRfPowerConnection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        protected override MessageBase ParseResponse(string rawBuffer)
        {
            MksRfPowerMessage msg = new MksRfPowerMessage();
            msg.RawMessage = rawBuffer;
            msg.IsResponse = true;
            return msg;
        }

    }
}