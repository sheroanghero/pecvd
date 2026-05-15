using System.Net;
using System.Text;
using System.Threading;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Communications.Tcp.Socket.Client.APM;
using MECF.Framework.Common.Communications.Tcp.Socket.Client.APM.EventArgs;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.HST
{
    public class HstMessage : AsciiMessage
    {     
        public string Data { get; set; }
       
    }
    public class HstConnection:HstConnectionBase
    {
        public HstConnection(string addr) : base(addr)
        {

        }
        public void EnableLog(bool enable)
        {
        }
        protected override void OnDataReceived(string oneLineMessage)
        {
            MessageBase msg = ParseResponse(oneLineMessage);
            if (msg.IsFormatError)
            {
                SetCommunicationError(true, "received invalid response message.");
            }

            lock (_lockerActiveHandler)
            {
                if (_activeHandler != null)
                {
                    if (msg.IsFormatError || (_activeHandler.HandleMessage(msg, out bool completed) && completed))
                    {
                        _activeHandler = null;
                    }
                }
            }
        }

        protected override MessageBase ParseResponse(string rawText)
        {
            HstMessage msg = new HstMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            string tempdata = rawText.Replace("\r", "").Replace("\n", "");
            msg.RawMessage = rawText;
            if(tempdata.Contains("Welcome")) msg.IsAck = true;
            if (tempdata == "1") msg.IsAck = true;
            else if (tempdata.Length == 1) msg.IsNak = true;
            else
            {
                msg.IsResponse = true;
                msg.Data = rawText;
            }
            return msg;
        }




    }
}
