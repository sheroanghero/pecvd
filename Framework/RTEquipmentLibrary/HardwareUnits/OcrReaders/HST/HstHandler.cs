using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.HST
{
    public abstract class HstHandler : HandlerBase 
    {
        protected ModuleName _target;
        protected int _slot;
        public HstOcrReader OCRDevice { get; set; }
        public string Command;
        protected HstHandler(HstOcrReader device,string command,string para): base(BuildMessage(command,para))
        {
            OCRDevice = device;
            Command = command;

            //_isSimulator = SC.GetValue<bool>("System.IsSimulatorMode");
        }

        public static byte[] BuildMessage(string command, string para)
        {
            List<byte> ret = new List<byte>();
            foreach (char c in command)
            {
                ret.Add((byte)c);
            }
            //cmd.Add((byte)(':'));    //3A
            if (!string.IsNullOrEmpty(para))
                foreach (char c in para)
                {
                    ret.Add((byte)c);
                }
            return ret.ToArray();       
        }

        public virtual void Update()
        {

        }

    }
    public class OnlineHandler: HstHandler
    {
        public OnlineHandler(HstOcrReader reader,bool online):base(reader,"SO",online?"1":"0")
        {
            OCRDevice = reader;
            Command = "SO";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HstMessage response = msg as HstMessage;
            ResponseMessage = msg;
            if(response.IsAck) SetState(EnumHandlerState.Acked);
            if(response.IsNak) SetState(EnumHandlerState.Completed);
            transactionComplete = true;
            return true;
        }
    }
    public class LoadJobHandler : HstHandler
    {
        public LoadJobHandler(HstOcrReader reader, string jobfile) : base(reader, "LF", jobfile)
        {
            OCRDevice = reader;
            Command = string.Format("LF{0}.job", jobfile);
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HstMessage response = msg as HstMessage;
            ResponseMessage = msg;
            if (response.IsAck) SetState(EnumHandlerState.Acked);
            if (response.IsNak) SetState(EnumHandlerState.Completed);
            transactionComplete = true;
            return true;
        }
    }

    public class ReadLMHandler : HstHandler
    {
        public ReadLMHandler(HstOcrReader reader) : base(reader, "SM\"READ\"0 ", null)
        {
            OCRDevice = reader;
            Command = "SM\"READ\"0 ";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HstMessage response = msg as HstMessage;
            ResponseMessage = msg;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
                return true;
            }
            if (response.IsNak) SetState(EnumHandlerState.Completed);
            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                
                string[] items = response.Data.TrimStart('[').TrimEnd(']').Split(',');
                if (OCRDevice.ReadLaserMaker)
                {
                    OCRDevice.LaserMaker = items[0];
                    OCRDevice.LaserMark1 = items[0];
                    if (items.Length > 1) OCRDevice.LaserMark1Score = items[1];
                    if (items.Length > 2) OCRDevice.LaserMark1ReadTime = items[2];
                    
                    LOG.Write($"{OCRDevice.Name} laser mark1 updated to {OCRDevice.LaserMaker}");
                }
                else
                {
                    OCRDevice.T7Code = items[0];
                    OCRDevice.LaserMark2 = items[0];
                    if (items.Length > 1) OCRDevice.LaserMark2Score = items[1];
                    if (items.Length > 2) OCRDevice.LaserMark2ReadTime = items[2];
                    LOG.Write($"{OCRDevice.Name} laser mark2 updated to {OCRDevice.T7Code}");
                }
                OCRDevice.ReadOK = double.Parse(items[1]) >= 0;
            }
            transactionComplete = true;
            return true;
        }
    }
}
