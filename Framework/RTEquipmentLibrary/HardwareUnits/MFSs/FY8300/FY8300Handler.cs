using Aitex.Core.RT.Event;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.FY8300
{
    public abstract class FY8300Handler : HandlerBase
    {
        private const byte Header = 0x40;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        public MfcBase MFCDevice { get; }

        protected FY8300Handler(MfcBase mfc, string cmd, float data = -1)
            : base(BuildMessage(cmd, data))
        {
            MFCDevice = mfc;
        }

        private static string BuildMessage(string cmd, float data)
        {
            string sendValue = "";
            if (data == -1)
                return sendValue = $"{cmd}";
            else
                sendValue = $"{cmd}{data}";
            return sendValue;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }

    }

    public class FY8300MFCWrite : FY8300Handler
    {
        public FY8300MFCWrite(MfcBase mfc, string cmd, float value)
            : base(mfc, cmd, value)
        {
            Name = $"Write{cmd}";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FY8300Message;
            handled = false;
            if (!result.IsResponse) return true;

            
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class FY8300MFCRead : FY8300Handler
    {
        public FY8300MFCRead(MfcBase mfc, string cmd)
            : base(mfc, cmd, -1)
        {
            Name = $"Read{cmd}";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as FY8300Message;
            handled = false;
            if (!result.IsResponse) return true;

           
            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

}
