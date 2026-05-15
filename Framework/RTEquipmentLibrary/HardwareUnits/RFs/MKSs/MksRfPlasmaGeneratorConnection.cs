using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Mkss
{
    public class MksRfPlasmaGeneratorMessage : AsciiMessage
    {
    }

    public class MksRfPlasmaGeneratorConnection : SerialPortConnectionBase
    {
        public MksRfPlasmaGeneratorConnection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "", true)
        {

        }

        protected override MessageBase ParseResponse(string rawBuffer)
        {
            MksRfPlasmaGeneratorMessage msg = new MksRfPlasmaGeneratorMessage();
            msg.RawMessage = rawBuffer;
            msg.IsResponse = true;
            return msg;
        }

    }
}