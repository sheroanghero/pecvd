using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FY8300
{
    public class FY8300Message : AsciiMessage
    {
        public int UNo { get; set; }
        public int SeqNo { get; set; }
        public string Status { get; set; }
        public string Ackcd { get; set; }
        public string Command { get; set; }
        public string[] Data { get; set; }
        public string ErrorCode { get; set; }
        public string EvNo { get; set; }
        public string EvDate { get; set; }
        public string EvData { get; set; }
    }

    public enum ChannelName
    {
        CH1,
        CH2,
        CH3,
    }
    public enum FuncName
    {
        WAVEFORM,
        FREQUENCY,
        RANGE,
        OUTPUT,
    }

    public class FY8300Connection: SerialPortConnectionBase
    {
        private string _cachedBuffer = string.Empty;

        public FY8300Connection(string portName, int baudRate = 115200, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\n", true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            FY8300Message msg = new FY8300Message();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }

            return msg;
        }
    }
}
