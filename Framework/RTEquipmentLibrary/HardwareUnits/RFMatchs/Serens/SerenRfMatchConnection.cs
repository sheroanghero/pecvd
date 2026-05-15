using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Serens
{
    public class SerenRfMatchMessage : AsciiMessage
    {
    }

    public class SerenRfMatchConnection : SerialPortConnectionBase
    {
        public SerenRfMatchConnection(string portName, int baudRate = 19200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        protected override MessageBase ParseResponse(string rawBuffer)
        {
            SerenRfMatchMessage msg = new SerenRfMatchMessage();
            msg.RawMessage = rawBuffer;
            msg.IsResponse = true;
            return msg;
        }
    }
}
