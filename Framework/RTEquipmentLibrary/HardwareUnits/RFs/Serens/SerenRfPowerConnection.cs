using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Serens
{
    public class SerenRfPowerMessage : AsciiMessage
    {
    }

    public class SerenRfPowerConnection : SerialPortConnectionBase
    {
        public SerenRfPowerConnection(string portName, int baudRate=19200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        protected override MessageBase ParseResponse(string rawBuffer)
        {
            SerenRfPowerMessage msg = new SerenRfPowerMessage();
            msg.RawMessage = rawBuffer;
            msg.IsResponse = true;
            return msg;
        }

    }
}
