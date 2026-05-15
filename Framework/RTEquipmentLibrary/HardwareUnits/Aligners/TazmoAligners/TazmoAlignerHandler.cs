using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners
{
    public static class TazmoCommand
    {
        public static readonly string CPUresetMotion = "CPI";
        public static readonly string InitializeMotion = "RST";
        public static readonly string PauseMotion = "PAU";
        public static readonly string CancelthepauseMotion = "CNT";
        public static readonly string MovealignertohomepositionMotion = "HOM";
        public static readonly string MovetopickpositionMotion = "PRP";
        public static readonly string SeriesofalignmentMotion = "ALG";
        public static readonly string Alignmentpartialmovement1Motion = "SRH";
        public static readonly string Alignmentpartialmovement2Motion = "OCH";
        public static readonly string Alignmentpartialmovement1_2Motion = "ALN";
        public static readonly string Alignmentpartialmovement3, movetodeliverypositionMotion = "ULD";
        public static readonly string Readrobotoffsetamountafteralignmentunit1_100Status = "POS";
        public static readonly string Readrobotoffsetamountafteralignmentunit1_1000Status = "RPS";
        public static readonly string ClearrobotoffsetamountafteralignmentSet = "CPS";
        public static readonly string RequeststatusStatus = "STS";
        public static readonly string Requeststatus2Status = "STU";
        public static readonly string CheckwaferpresenceMotion = "WCH";
        public static readonly string CheckwaferpresenceMotionNoEValve = "VCH";

        public static readonly string ReadwafertypeStatus = "RWF";
        public static readonly string CancelerrorSet = "ERF";
        public static readonly string ReaderrorlogStatus = "RER";
        public static readonly string Readerrorlog2Status = "DER";
        public static readonly string ClearerrorlogSet = "CER";
        public static readonly string SetdefaultvalueMotion = "DEF";
        public static readonly string SetalignmentoffsetvalueSet = "REV";
        public static readonly string ReadalignmentoffsetvalueStatus = "RRE";
        public static readonly string SetalignmentangleetcSet = "DWL";
        public static readonly string ReadalignmentangleetcStatus = "UPL";
        public static readonly string ReadsoftwareproductNoStatus = "PNO";
        public static readonly string ReadmodelnameStatus = "RMN";
        public static readonly string ReadsoftwareversionStatus = "VER";
        public static readonly string Readalignmenttype_chuckpositionStatus = "TYP";
        public static readonly string ReadmotorcontrolIC_readregisterStatus = "RRG";
        public static readonly string ReadalignmentcountStatus = "ADM";
        public static readonly string ReadIOsignalStatus = "RIO";
        public static readonly string TurnwaferonetimeatslowspeedMotion = "TRN";
        public static readonly string ClosealignerchuckMotion = "VVN";
        public static readonly string OpenalignerchuckMotion = "VVF";
        public static readonly string AscenddeliverychuckMotion = "ZUP";
        public static readonly string DescenddeliverychuckMotion = "ZDN";
        public static readonly string Movetodeliveryposition, withoutlinesensorcheckMotion = "PRR";
        public static readonly string Movetohomeposition_withoutlinesensorcheckMotion = "HOE";
        public static readonly string TurnwaferforthespecifiednumberoftimeatthespecifiedspeedMotion = "TUR";

        public static readonly string MoveTheAlignerChuckToSpecifiedPosition = "NYG";
        /// <summary>
        /// A
        /// </summary>

    }
    public class TazmoAlignerHandler : HandlerBase
    {
        public TazmoAligner Device { get; set; }

        public string Command;

        protected TazmoAlignerHandler(TazmoAligner device, string command,string para) : base(BuildMesage(command,para))
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
            TazmoAlignerMessageBIN response = msg as TazmoAlignerMessageBIN;
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
    public class SingleTransactionHandler : TazmoAlignerHandler
    {
        public SingleTransactionHandler(TazmoAligner device, string command,string para) : base(device, BuildData(command),para)
        {

        }

        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerMessageBIN response = msg as TazmoAlignerMessageBIN;
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
    public class TwinTransactionHandler:TazmoAlignerHandler
    {
        public TwinTransactionHandler(TazmoAligner device,string command,string para): base(device, BuildData(command),para)
        {
        }
        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerMessageBIN response = msg as TazmoAlignerMessageBIN;
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
    public class MoveToPickHandler : TazmoAlignerHandler
    {
        public MoveToPickHandler(TazmoAligner device, string specPos) : base(device, BuildData(specPos),null)
        { }
        private static string BuildData(string data)
        {
            return TazmoCommand.MovetopickpositionMotion + "," + data;
        }
    }

    public class QueryStatus1 : TazmoAlignerHandler
    {
        public QueryStatus1(TazmoAligner device) : base(device, BuildData(),null)
        { }
        private static string BuildData()
        {
            return TazmoCommand.RequeststatusStatus;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerMessageBIN response = msg as TazmoAlignerMessageBIN;
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

    public class QueryStatus2 : TazmoAlignerHandler
    {
        public QueryStatus2(TazmoAligner device) : base(device, BuildData(), null)
        { }
        private static string BuildData()
        {
            return TazmoCommand.Requeststatus2Status;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TazmoAlignerMessageBIN response = msg as TazmoAlignerMessageBIN;
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




}
