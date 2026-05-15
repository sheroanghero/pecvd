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
    public static class FuqiAlignerCommand
    {
        public const string Reset = "C01";
        public const string RequestPlace = "C02";
        public const string RequestFinishPlace = "C03";
        public const string RequestVacuumOn = "C04";
        public const string RequestVacuumOff = "C05";
        public const string SetVacuumOffAfterAlign = "C06";
        public const string SetVacuumOnAfterAlign = "C07";
        public const string SetWIDReaderOn = "C08";
        public const string SetWIDReaderOff = "C09";
        public const string SetWIDReadComplete = "C10";
        public const string SetNotchProduct = "C11";
        public const string SetLineProduct = "C12";
        public const string SetCenterAndNotch = "C13";
        public const string SetOnlyNotch = "C14";
        public const string SetWIDReadFail = "C15";
        public const string SetAdjustFirstTime = "C16";
        public const string SetAdjustTwice = "C17";
        public const string SetUseNewCommand = "C41";
        public const string SetUseOldCommand = "C40";
        public const string SetWaferSize = "T";

        /// <summary>
        /// A
        /// </summary>

    }
    public class FuqiAlignerHandler : HandlerBase
    {
        public FuqiAligner Device { get; set; }

        public string Command;

        protected FuqiAlignerHandler(FuqiAligner device, string command) : base(BuildMesage(command))
        {
            Device = device;
            Command = command;
            Name = command;
        }

        public static byte[] BuildMesage(string data)
        {
            List<byte> ret = new List<byte>();
            foreach (char c in data)
            {
                ret.Add((byte)c);
            }

            return ret.ToArray();

        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            FuqiAlignerMessageBIN response = msg as FuqiAlignerMessageBIN;
            ResponseMessage = msg;
            transactionComplete = false;
            if (response.IsResponse)
            {

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }

            Device.OnActionDone();

            return true;


        }

    }
    public enum FuqiMotionStatus
    {
        None,
        Complete,
        Fail,
    }
    public class FuqiResetHandler : FuqiAlignerHandler
    {
        public FuqiResetHandler(FuqiAligner device, string command) : base(device, BuildData(command))
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

                if (_xAxisStatus != MotionStatus.None &&
                    _yAxisStatus != MotionStatus.None &&
                    _zAxisStatus != MotionStatus.None) transactionComplete = true;
            }

            return true;
        }
    }
    public class FuqiRequestHandler : FuqiAlignerHandler
    {
        private string cmd;
        public FuqiRequestHandler(FuqiAligner device, string command) : base(device, BuildData(command))
        {
            cmd = command;
        }
        private static string BuildData(string command)
        {
            return command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            FuqiAlignerMessageBIN response = msg as FuqiAlignerMessageBIN;
            ResponseMessage = msg;
            Device.TaExecuteSuccss = false;
            transactionComplete = false;
            
            if (response.IsResponse)
            {
                string commandstr = Encoding.ASCII.GetString(response.CMD);
                switch (cmd)
                {
                    case FuqiAlignerCommand.RequestPlace:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M01"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnWaferPresent(false);
                            Device.OnActionDone();
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M02"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnWaferPresent(true);
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.RequestFinishPlace:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M03"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.IsAlignSuccess = true;
                            //Device.OnActionDone();
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M04"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                            Device.IsAlignSuccess = false;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M17"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                            Device.IsAlignSuccess = false;
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M18"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                            Device.IsAlignSuccess = false;
                        }
                        break;
                    case FuqiAlignerCommand.RequestVacuumOn:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M05"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M06"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                            Device.OnError(Encoding.ASCII.GetString(response.CMD));
                        }
                        break;
                    case FuqiAlignerCommand.RequestVacuumOff:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M07"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M08"))
                        {
                            Device.TaExecuteSuccss = false;
                            transactionComplete = true;
                            Device.OnError(Encoding.ASCII.GetString(response.CMD));
                        }
                        break;
                    case FuqiAlignerCommand.Reset:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M00"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.Initalized = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetVacuumOffAfterAlign:
                    case FuqiAlignerCommand.SetVacuumOnAfterAlign:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M09"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetWIDReaderOn:
                    case FuqiAlignerCommand.SetWIDReaderOff:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M10"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetWIDReadComplete:
                    case FuqiAlignerCommand.SetWIDReadFail:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M11"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetLineProduct:
                    case FuqiAlignerCommand.SetNotchProduct:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M12"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetCenterAndNotch:
                    case FuqiAlignerCommand.SetOnlyNotch:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M13"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetAdjustFirstTime:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M31"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetAdjustTwice:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M32"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetUseOldCommand:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M40"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();                            
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M41"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    case FuqiAlignerCommand.SetUseNewCommand:
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M41"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();        
                        }
                        if (Encoding.ASCII.GetString(response.CMD).Contains("M40"))
                        {
                            Device.TaExecuteSuccss = true;
                            transactionComplete = true;
                            Device.OnActionDone();
                        }
                        break;
                    default:                       
                        break;
                }
                if (cmd.Contains("A"))
                {
                    if (Encoding.ASCII.GetString(response.CMD).Contains("M14"))
                    {
                        Device.TaExecuteSuccss = true;
                        transactionComplete = true;
                        Device.OnActionDone();
                    }
                }
                if (cmd.Contains("B"))
                {
                    if (Encoding.ASCII.GetString(response.CMD).Contains("M15"))
                    {
                        Device.TaExecuteSuccss = true;
                        transactionComplete = true;
                        Device.OnActionDone();
                    }
                }
                if (cmd.Contains("T"))
                {
                    if (Encoding.ASCII.GetString(response.CMD).Contains("M16"))
                    {
                        Device.TaExecuteSuccss = true;
                        transactionComplete = true;
                        Device.OnActionDone();
                    }
                }
            }

            return true;
        }
    }







}
