using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners
{
    public static class HonghuAlignerCommand
    {
        public  const string Reset = "C01";
        public const string RequestPlace = "C02";
        public const string RequestFinishPlace = "C03";
        public const string RequestVacuumOn = "C04";
        public const string RequestVacuumOff = "C05";
        public const string SetWafer200mm = "C06";
        public const string SetWafer300mm = "C07";
        public const string SetVacuumOffAfterAln = "C08";
        public const string SetVacuumOnAfterAln = "C09";
        public const string SetReadLM = "C10";
        public const string SetNotReadLM = "C11";
        public const string SetAlnAngleTo180 = "C12";
        public const string SetAlnAngleTo270 = "C13";
        
        /// <summary>
        /// A
        /// </summary>

    }
    public class HonghuAlignerHandler : HandlerBase
    {
        public HonghuAligner Device { get; set; }

        public string Command;

        protected HonghuAlignerHandler(HonghuAligner device, string command) : base(BuildMesage(command))
        {
            Device = device;
            Command = command;
            Name = command;
        }

        public static byte[] BuildMesage(string data)
        {
            List<byte> ret = new List<byte>();
            foreach(char c in data)
            {
                ret.Add((byte)c);
            }
          
            return ret.ToArray();

        }     

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HonghuAlignerMessageBIN response = msg as HonghuAlignerMessageBIN;
            ResponseMessage = msg;
            transactionComplete = false;
            if(response.IsResponse)
            {
               
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }


            
            return true;


        }

    }
    public enum MotionStatus
    {
        None,
        Complete,
        Fail,
    }
    public class ResetHandler : HonghuAlignerHandler
    {
        public ResetHandler(HonghuAligner device, string command) : base(device, BuildData(command))
        {
            _xAxisStatus = MotionStatus.None;
            _yAxisStatus = MotionStatus.None;
            _zAxisStatus = MotionStatus.None;
        }
        private MotionStatus _xAxisStatus;
        private MotionStatus _yAxisStatus;
        private MotionStatus _zAxisStatus;

        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HonghuAlignerMessageBIN response = msg as HonghuAlignerMessageBIN;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = false;
            transactionComplete = false;
            if (response.IsAck)
            {
                if (Encoding.ASCII.GetString(response.CMD).Contains("X01"))
                    _xAxisStatus = MotionStatus.Complete;
                if (Encoding.ASCII.GetString(response.CMD).Contains("X02"))
                    _xAxisStatus = MotionStatus.Fail;

                if (Encoding.ASCII.GetString(response.CMD).Contains("Y01"))
                    _yAxisStatus = MotionStatus.Complete;
                if (Encoding.ASCII.GetString(response.CMD).Contains("Y02"))
                    _yAxisStatus = MotionStatus.Fail;

                if (Encoding.ASCII.GetString(response.CMD).Contains("Z01"))
                    _zAxisStatus = MotionStatus.Complete;
                if (Encoding.ASCII.GetString(response.CMD).Contains("Z02"))
                    _zAxisStatus = MotionStatus.Fail;

                SetState(EnumHandlerState.Completed);


                if (_xAxisStatus == MotionStatus.Complete &&
                    _yAxisStatus == MotionStatus.Complete &&
                    _zAxisStatus == MotionStatus.Complete) Device.TaExecuteSuccss = true;

                if(_xAxisStatus != MotionStatus.None &&
                    _yAxisStatus != MotionStatus.None &&
                    _zAxisStatus != MotionStatus.None) transactionComplete = true;
            }            

            return true;
        }
    }
    public class RequestHandler: HonghuAlignerHandler
    {
        private string cmd;
        public RequestHandler(HonghuAligner device,string command): base(device, BuildData(command))
        {
            cmd = command;
        }
        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HonghuAlignerMessageBIN response = msg as HonghuAlignerMessageBIN;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = false;
            transactionComplete = false;
            if (response.IsResponse)
            {
                
                switch (cmd)
                {
                    case HonghuAlignerCommand.RequestPlace:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M06"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M07"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.RequestFinishPlace:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M08"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M09"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.RequestVacuumOn:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M01"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M02"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.RequestVacuumOff:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M03"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M04"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.Reset :
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M05"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.Initalized = true;
                        }                       
                        break;
                    case HonghuAlignerCommand.SetWafer200mm:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M10"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetWafer300mm:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M10"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetVacuumOffAfterAln:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M11"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetVacuumOnAfterAln:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M11"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetNotReadLM:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M13"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetReadLM:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M13"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetAlnAngleTo180:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M12"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    case HonghuAlignerCommand.SetAlnAngleTo270:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M12"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                        }
                        break;
                    default:
                        break;
                }
                if(cmd.Contains("A"))
                {
                    if (Encoding.ASCII.GetString(response.CMD).Contains("M14"))
                    {
                        Device.TaExecuteSuccss = true;
                        transactionComplete = true;
                    }
                }
                if (cmd.Contains("B"))
                {
                    if (Encoding.ASCII.GetString(response.CMD).Contains("M15"))
                    {
                        Device.TaExecuteSuccss = true;
                        transactionComplete = true;
                    }
                }



            }
            
            return true;
        }
    }
    

   

    


}
