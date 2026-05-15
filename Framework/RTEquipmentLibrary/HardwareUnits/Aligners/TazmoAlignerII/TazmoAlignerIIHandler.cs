using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAlignerIIs
{
    
    public class TazmoAlignerIIHandler : HandlerBase
    {
        public TazmoAlignerII Device { get; set; }

        public string Command;

        protected TazmoAlignerIIHandler(TazmoAlignerII device, string command,string para) : base(BuildMesage(command,para))
        {
            Device = device;
            Command = command;
            Name = command;
        }

        public static byte[] BuildMesage(string data, string para)
        {
            List<byte> ret = new List<byte>();
            foreach(char c in data)
            {
                ret.Add((byte)c);
            }
            if (!string.IsNullOrEmpty(para))
            {
                ret.Add((byte)0x2C);
                foreach (char b in para) ret.Add((byte)b);
            }

            ret.Add(0x0D);            
            return ret.ToArray();

        }     

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;
            transactionComplete = false;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
            }
            if(response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsEvent)
            {
                SendAck();
                if (response.CMD == Encoding.ASCII.GetBytes(Command))
                {
                    
                }
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;                
            }
            if(response.IsResponse)
            {
                SendAck();
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }


            
            return true;


        }
        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }
        public virtual void ParseStatus1(byte[] data)
        {
            try
            {
                if (data.Length < 3) return;

                int state1code = Convert.ToInt32(Encoding.ASCII.GetString(data), 16);

                Device.TaAlignerStatus1 = (TazmoState1)state1code;
                if (state1code >= 0x111) EV.PostAlarmLog("Aligner", $"Tazmo Aligner occurred error:{((TazmoState1)state1code).ToString()}");
            }
            catch(Exception ex)
            {
                LOG.Write("Parse Tazmo Status1 exception:" +ex);
            }            
        }

        public virtual void ParseStatus2(byte[] data)
        {
            if (data == null || data.Length < 10) return;
            try
            {
                Device.TaAlignerStatus2Status = (TazmoStatus)Convert.ToInt32(Encoding.ASCII.GetString(new byte[]{ data[0]}));
                Device.TaAlignerStatus2Lift =  (LiftStatus)Convert.ToInt32(Encoding.ASCII.GetString(new byte[] { data[2] }));
                Device.TaAlignerStatus2Notch = (NotchDetectionStatus)Convert.ToInt32(Encoding.ASCII.GetString(new byte[] { data[3] }));
                Device.TaAlignerStatus2DeviceStatus = Convert.ToInt32(Encoding.ASCII.GetString(new byte[] { data[7] }),16);
                Device.TaAlignerStatus2ErrorCode = Convert.ToInt32(Encoding.ASCII.GetString(new byte[] { data[8] }),16);
                Device.TaAlignerStatus2LastErrorCode = data[9];
            }
            catch (Exception ex)
            {
                LOG.Write($"Parse status2 exception:{ex}");
            }
        }

        public virtual bool PaserData(byte[] data)
        {
            return true;
        }



    }
    public class SingleTransactionHandler : TazmoAlignerIIHandler
    {
        public SingleTransactionHandler(TazmoAlignerII device, string command,string para) : base(device, BuildData(command),para)
        {

        }

        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;

            }
            if(response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;                
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if(response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                ParseStatus1(response.Data);
                Device.OnError(Command + "Execute Error");
            }
            if(response.IsResponse)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = true;
                transactionComplete = true;
                if (Encoding.Default.GetString(response.CMD) == "STS") ParseStatus1(response.Data);
                if (Encoding.Default.GetString(response.CMD) == "STU") ParseStatus2(response.Data);
            }

            return true;
        }
    }
    public class TwinTransactionHandler:TazmoAlignerIIHandler
    {
        public TwinTransactionHandler(TazmoAlignerII device,string command,string para): base(device, BuildData(command),para)
        {
        }
        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
            }
            if (response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                Device.TaExecuteSuccss = false;
                transactionComplete = true;
                ParseStatus1(response.Data);
                Device.OnError(Command + "Execute Error");
            }
            if (response.IsResponse)
            {
                string command = Encoding.Default.GetString(response.CMD);
                if (command == "RST" || command == "HOM")
                    Device.Initalized = true;
                SetState(EnumHandlerState.Completed);
                SendAck();
                Device.TaExecuteSuccss = true;
                transactionComplete = true;
            }            
            return true;
        }
    }
    public class MoveToPickHandler : TazmoAlignerIIHandler
    {
        public MoveToPickHandler(TazmoAlignerII device, string specPos) : base(device, BuildData(specPos),null)
        { }
        private static string BuildData(string data)
        {
            return TazmoCommand.MovetopickpositionMotion + "," + data;
        }
    }

    public class QueryStatus1 : TazmoAlignerIIHandler
    {
        public QueryStatus1(TazmoAlignerII device) : base(device, BuildData(),null)
        { }
        private static string BuildData()
        {
            return TazmoCommand.RequeststatusStatus;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;

            if (response.RawMessage == new byte[] { 0x06 })  //ACK
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = false; ;
                return true;
            }
 
            if (response.RawMessage == new byte[] { 0x11 })
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //Device.Busy = true;
                return true;

            }
            if (response.RawMessage.Length >= 8 && Encoding.ASCII.GetString(response.RawMessage.Take(3).ToArray()) == Command)
            {
                byte[] data = response.RawMessage.Skip(4).Take(3).ToArray();
                ParseStatus1(data);
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;  

                //Device.Busy = false;
                SendAck();
                return true;
            }
            transactionComplete = false;
            return false;

        }


    }

    public class QueryStatus2 : TazmoAlignerIIHandler
    {
        public QueryStatus2(TazmoAlignerII device) : base(device, BuildData(), null)
        { }
        private static string BuildData()
        {
            return TazmoCommand.Requeststatus2Status;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;

            if (response.RawMessage == new byte[] { 0x06 })  //ACK
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = false; ;
                return true;
            }

            if (response.RawMessage == new byte[] { 0x11 })
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //Device.Busy = true;
                return true;

            }
            if(response.RawMessage.Length >= 15 && Encoding.ASCII.GetString(response.RawMessage.Take(3).ToArray()) == Command)
            {
                byte[] data = response.RawMessage.Skip(4).Take(3).ToArray();
                int state1code = Convert.ToInt32(Encoding.ASCII.GetString(data),16);

                Device.TaAlignerStatus1 = (TazmoState1)state1code;


                SetState(EnumHandlerState.Completed);
                transactionComplete = true;

                //Device.Busy = false;
                SendAck();
                return true;
            }
            transactionComplete = false;
            return false;

        }


    }

    public class CheckWaferPresentHandler : TazmoAlignerIIHandler
    {
        public CheckWaferPresentHandler(TazmoAlignerII device,string command) : base(device, command, null)
        { }
       
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerIIMessageBIN response = msg as TazmoAlignerIIMessageBIN;
            ResponseMessage = msg;

            if (response.RawMessage == new byte[] { 0x06 })  //ACK
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = false; ;
                return true;
            }

            if (response.RawMessage == new byte[] { 0x11 })
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //Device.Busy = true;
                return true;

            }
            if (response.RawMessage.Length >= 5 && Encoding.ASCII.GetString(response.RawMessage.Take(3).ToArray()) == Command)
            {
                byte[] data = response.RawMessage.Skip(4).Take(1).ToArray();

                string strPresent = Encoding.ASCII.GetString(data);

                Device.IsWaferPresentByCheckResult = strPresent =="1";


                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;

        }


    }


}
