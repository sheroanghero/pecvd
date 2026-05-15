using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotHirataR4
{
    public interface IParseData
    {
        void ParseData(string data);
    }
    public abstract class HirataR4RobotHandler : HandlerBase
    {
        public HirataR4Robot Device { get; }

        protected string _command;
        public string _parameter;

        //protected static char _stx = '\u0002';
        //protected static char _etx = '\u0003';
        //protected static char _space = '\u0020';

        protected static string _stx = "\x02"; // Start of Text
        protected static string _etx = "\x03"; // End of Text
        protected static string _sp = "\x20";
        //byte[] myBytes = System.Text.Encoding.ASCII.GetBytes("\x02Hello, world!");
        //socket.Send(myBytes);

        protected HirataR4RobotHandler(HirataR4Robot device, string command, string parameter = null)
            : base(BuildMessage(device.Address,command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage(string address, string command, string parameter)
        {
            string msg1 = parameter!= null ? $"{address}{_sp}{command}{_sp}{parameter}{_etx}": $"{address}{_sp}{command}{_etx}";

            byte lrc = CalcLRC(Encoding.ASCII.GetBytes(msg1).ToList());
            string msg = $"{_stx}{msg1}{(char)(lrc)}";
            return msg;// +"\0";
        }
        private static byte CalcLRC(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }
        protected static string F2S(float value)
        {
            return value < 0 ? value.ToString() : " " + value.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;


            if (msg.IsError)
            {
                Device.NoteError(response.Data);                
            }
            else
            {
                Device.NoteRobotStatus(response.Data);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected virtual void ParseData(HirataR4RobotMessage msg)
        {
            if (msg.Data[0] != 0)
            {
                var reason = TranslateCsrCode(msg.Data[0]);
                Device.NoteError(reason);
            }
        }

        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }

        protected string TranslateCsrCode(int csrCode)
        {
            string ret = csrCode.ToString();
            switch (csrCode)
            {
                case 0:
                    ret = null;//"Command accepted";
                    break;
                case 1:
                    ret = "Control Code Is Incorrect";
                    break;
                case 2:
                    ret = "Output Is On(Change Not Allowed)";
                    break;
                case 4:
                    ret = "Data Is Out Of Range";
                    break;
                case 7:
                    ret = "Active Fault(s) Exist";
                    break;
                case 9:
                    ret = "Data Byte Count Is Incorrect";
                    break;
                case 19:
                    ret = "Recipe Is Active(Change Not Allowed)";
                    break;
                case 50:
                    ret = "The Frequency Is Out Of Range";
                    break;
                case 51:
                    ret = "The Duty Cycle Is Out Of Range";
                    break;
                case 53:
                    ret = "The Device Controlled By The Command Is Not Detected";
                    break;
                case 99:
                    ret = "Command Not Accepted(There Is No Such Command)";
                    break;
                default:
                    break;
            }
            return ret;
        }
    }

    public class HirataR4RobotRawCommandHandler : HirataR4RobotHandler
    {
        public HirataR4RobotRawCommandHandler(HirataR4Robot device, string command, string parameter = null)
            : base(device, command, parameter)
        {
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute raw command {command} {temp} in byte.");
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as HirataR4RobotMessage;
                Device.NoteRawCommandInfo(_command, result.RawMessage);

                ResponseMessage = msg;
                handled = true;
            }
            return true;
        }


    }
    public class HirataR4RobotWritePositionHandler : HirataR4RobotHandler
    {
        public HirataR4RobotWritePositionHandler(HirataR4Robot device, int address, float x, float y, float z, float w, 
            float r, float c, string posture,string Mdata,string Fcode,string Scode)
            : base(device, "SD", BuildParameter(address, x, y, z, w, r, c, posture, device.Axis,Mdata,Fcode,Scode))
        {

        }

        private static string BuildParameter(int address, float x, float y, float z, float w, float r, float c, 
            string posture, int axis, string Mdata, string Fcode, string Scode)
        {
            if(axis == 6)
                return F2S(address) + F2S(x) + F2S(y) + F2S(z) + F2S(w) + F2S(r) + F2S(c) + posture +
                    $" 0 {Mdata} {Fcode} {Scode}";
            else
                return F2S(address) + F2S(x) + F2S(y) + F2S(z) + F2S(w) + posture +
                    $" 0 {Mdata} {Fcode} {Scode}";
        }

    }
    public class HirataR4RobotMoveToPositionHandler : HirataR4RobotHandler
    {
        public HirataR4RobotMoveToPositionHandler(HirataR4Robot device, int address, string motion = null)
            : base(device, BuildCommand(motion), address.ToString())
        {
        }

        private static string BuildCommand(string motion)
        {
            if (motion == null)
            {
                return "GP";
            }
            else
            {
                return "GP" + motion;
            }
        }
    }
    public class HirataR4RobotMoveToPositionWithDeviationHandler : HirataR4RobotHandler
    {
        public HirataR4RobotMoveToPositionWithDeviationHandler(HirataR4Robot device, string motion, int address, float xoffset, float yoffset, float zoffset, float woffset)
            : base(device, BuildCommand(motion), BuildParameter(address,xoffset, yoffset, zoffset, woffset))
        {
            LOG.Write($"{device.Name} move to postion:{address} on motion {motion} with deviation x:{xoffset},y:{yoffset},z:{zoffset},w:{woffset}"); 
      
        }

        private static string BuildCommand(string motion)
        {
            if (motion == null)
            {
                return "GP";
            }
            else
            {
                return "GP" + motion;
            }
        }

        private static string BuildParameter(int address,float x, float y, float z, float w)
        {
            return address.ToString()+ _sp+ "("+ F2S(x)+ F2S(y)+ F2S(z)+ F2S(w)+ ")";
        }
    }
    public class HirataR4RobotMoveDeviationHandler : HirataR4RobotHandler
    {
        public HirataR4RobotMoveDeviationHandler(HirataR4Robot device, string motion,float xoffset, float yoffset, float zoffset, float woffset)
            : base(device, BuildCommand(motion), BuildParameter(xoffset, yoffset, zoffset, woffset))
        {
            LOG.Write($"{device.Name} move deviation on motion {motion} with deviation x:{xoffset},y:{yoffset},z:{zoffset},w:{woffset}"); 
        }

        private static string BuildCommand(string motion)
        {
            if (motion == null)
            {
                return "GP";
            }
            else
            {
                return "GP" + motion;
            }
        }

        private static string BuildParameter(float x, float y, float z, float w)
        {
            return "*" + _sp + "("+ F2S(x)+ F2S(y)+ F2S(z)+ F2S(w)+ ")";
        }
    }
    public class HirataR4RobotSimpleActionHandler : HirataR4RobotHandler
    {
        public HirataR4RobotSimpleActionHandler(HirataR4Robot device, string command)
            : base(device, command)
        {
            LOG.Write($"{device.Name} execute the command:{command}"); 
      
        }
    }
    public class HirataR4RobotMonitorRobotStatusHandler : HirataR4RobotHandler
    {
        public HirataR4RobotMonitorRobotStatusHandler(HirataR4Robot device)
            : base(device, "LS", null)
        {

        }
    }
    public class HirataR4RobotReadRobotPositionHandler : HirataR4RobotHandler
    {
        public HirataR4RobotReadRobotPositionHandler(HirataR4Robot device, int address)
            : base(device, "LD", F2S(address))
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotPositon(response.Data);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }
    public class HirataR4RobotMonitorRobotConnectedHandler : HirataR4RobotHandler
    {
        public HirataR4RobotMonitorRobotConnectedHandler(HirataR4Robot device)
            : base(device, "CO", null)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotWriteDOHandler : HirataR4RobotHandler
    {
        public HirataR4RobotWriteDOHandler(HirataR4Robot device,int offset,bool value)
            : base(device, "SOB", BuildParamter(offset,value))
        {
            LOG.Write($"{device.Name} write DO {offset} in byte.");
        }
        
        private static string BuildParamter(int offset,bool value)
        {
            return $"{offset}{_sp}{(value ? 1 : 0)}";
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotReadSingleDIHandler : HirataR4RobotHandler
    {
        public HirataR4RobotReadSingleDIHandler(HirataR4Robot device, int offset)
            : base(device, "LIB", BuildParamter(offset))
        {
            LOG.Write($"{device.Name} read single DI {offset} in byte.");
        }
        private static string BuildParamter(int offset)
        {
            return offset.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                if (response.Data == "0") Device.DIReadValue = false;
                if (response.Data == "1") Device.DIReadValue = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotReadDIByteHandler : HirataR4RobotHandler
    {
        private int _offset;
        public HirataR4RobotReadDIByteHandler(HirataR4Robot device, int offset)
            : base(device, "LID", BuildParamter(offset))
        {
            LOG.Write($"{device.Name} read DI {offset} in byte.");
            _offset = offset;
        }
        private static string BuildParamter(int offset)
        {
            return offset.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteRobotDIByte(response.Data, _offset);
                return true;
            }
            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotReadHandler : HirataR4RobotHandler
    {
        public HirataR4RobotReadHandler(HirataR4Robot device, string command,string parameter)
            : base(device, command, parameter)
        {
            LOG.Write($"{device.Name} read command {command} {parameter}"); 
      
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);                
                Device.ParseReadData(_command, _parameter, response.Data);
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotWriteHandler : HirataR4RobotHandler
    {
        public HirataR4RobotWriteHandler(HirataR4Robot device, string command, string parameter)
            : base(device, command, parameter)
        {
            LOG.Write($"{device.Name} Write command {command} {parameter}");

        }        

    }
    public class HirataR4RobotOperateVacDependsOnWaferHandler : HirataR4RobotHandler
    {
        public HirataR4RobotOperateVacDependsOnWaferHandler(HirataR4Robot device, int offset)
            : base(device, "SOB", BuildParamter(offset, device))
        {
            LOG.Write($"{device.Name} write DO {offset} in byte.");
        }

        private static string BuildParamter(int offset, HirataR4Robot device)
        {
            bool value = false;
            if(offset == 0)
            {
                value = device.GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present;

            }
            if(offset == 1)
            {
                value = device.GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent;
            }
            if (offset == 2)
            {
                value = device.GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present;
            }
            if (offset == 3)
            {
                value = device.GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent;
            }
            return $"{offset}{_sp}{(value ? 1 : 0)}";
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotWaferInforMoveHandler : HirataR4RobotHandler
    {
        private int _offset;
        private bool _isPick;
        public HirataR4RobotWaferInforMoveHandler(HirataR4Robot device,int offset,bool isPickNoPlace)
            : base(device, "LID", BuildParamter(offset))
        {
            _isPick = isPickNoPlace;
            LOG.Write($"{device.Name} read DI {offset} and move wafer infor.");
            _offset = 0;
        }
        private static string BuildParamter(int offset)
        {
            Thread.Sleep(300);
            return offset.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteRobotDIByte(response.Data, _offset);
                if (_isPick)
                    Device.MoveWaferInforFromStation();
                else
                    Device.MoveWaferInforToStation();
                return true;
            }
            transactionComplete = false;
            return false;
        }

    }
    public class HirataR4RobotReadDIByteForInitHandler : HirataR4RobotHandler
    {
        private int _offset;
        public HirataR4RobotReadDIByteForInitHandler(HirataR4Robot device, int offset)
            : base(device, "LID", BuildParamter(offset))
        {
            LOG.Write($"{device.Name} read DI {offset} in byte.");
            _offset = offset;
        }
        private static string BuildParamter(int offset)
        {
            Thread.Sleep(1000);
            return offset.ToString();
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataR4RobotMessage response = msg as HirataR4RobotMessage;
            ResponseMessage = msg;

            if (msg.IsError)
            {
                Device.NoteError(response.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteRobotConnected(true);
            }
            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteRobotDIByte(response.Data, _offset);
                return true;
            }
            transactionComplete = false;
            return false;
        }

    }

    public class HirataR4RobotWriteMdataOnCurrenPositionHandler : HirataR4RobotHandler
    {
        public HirataR4RobotWriteMdataOnCurrenPositionHandler(HirataR4Robot device, int address, string Mdata)
            : base(device, "SD", BuildParameter(device,address,Mdata))
        {

        }

        private static string BuildParameter(HirataR4Robot device, int address, string Mdata)
        {
           
                return F2S(address) + F2S(device.ReadXPosition) + F2S(device.ReadYPosition) + F2S(device.ReadZPosition) +
                F2S(device.ReadWPosition) + device.RobotPosture + $" 0 {Mdata} {device.FCode} {device.SCode}";
        }

    }
}
