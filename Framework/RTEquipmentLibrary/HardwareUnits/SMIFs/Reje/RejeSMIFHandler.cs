using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Reje
{
    public class RejeSMIFHandler : HandlerBase
    {
        static string _Command;
        static string charStart = "s";
        static string charAdr = "00";
        static string charEnd = "\r";


        public RejeSMIFHandler(RejeSMIF device, string commandType, string command, string parameter) : base(BuilderMessage(commandType, command, parameter))
        {

        }

        public static string BuilderMessage(string commandType, string command, string parameter)
        {

            string commandStr = charStart + charAdr + commandType + ":";
            if (command != null)
            {
                _Command = command;
                commandStr += command;
            }
            if (parameter != null)
            {
                commandStr += parameter;
            }
            return commandStr + ";" + charEnd;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RejeSMIFMessage response = msg as RejeSMIFMessage;
            ResponseMessage = msg;
            transactionComplete = false;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;

            }
            if (response.IsEvent)
            {
                if (response.Command.Contains(_Command))
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                }
            }

            return true;
        }
    }

    public class RejeSMIFSetONMGVPModeHandler : RejeSMIFHandler
    {
        public RejeSMIFSetONMGVPModeHandler(RejeSMIF device)
            : base(device, "MOD", "ONMGV", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFSetMENTEHModeHandler : RejeSMIFHandler
    {
        public RejeSMIFSetMENTEHModeHandler(RejeSMIF device)
            : base(device, "MOD", "MENTE", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFStopHeandler : RejeSMIFHandler
    {
        public RejeSMIFStopHeandler(RejeSMIF device)
            : base(device, "MOV", "STOP_", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFORGJZHeandler : RejeSMIFHandler
    {
        public RejeSMIFORGJZHeandler(RejeSMIF device)
            : base(device, "MOV", "ORGJZ", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFPodOpenHeandler : RejeSMIFHandler
    {
        public RejeSMIFPodOpenHeandler(RejeSMIF device)
            : base(device, "MOV", "ORGJL", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFDorOpenHeandler : RejeSMIFHandler
    {
        public RejeSMIFDorOpenHeandler(RejeSMIF device)
            : base(device, "MOV", "DOROP", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFDorCloseHeandler : RejeSMIFHandler
    {
        public RejeSMIFDorCloseHeandler(RejeSMIF device)
            : base(device, "MOV", "DORCL", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFHomeHeandler : RejeSMIFHandler
    {
        public RejeSMIFHomeHeandler(RejeSMIF device)
            : base(device, "MOV", "HOME_", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFStageHeandler : RejeSMIFHandler
    {
        public RejeSMIFStageHeandler(RejeSMIF device)
            : base(device, "MOV", "STAGE", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFTeachHeandler : RejeSMIFHandler
    {
        public RejeSMIFTeachHeandler(RejeSMIF device)
            : base(device, "MOV", "TEACH", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFMapingHeandler : RejeSMIFHandler
    {
        public RejeSMIFMapingHeandler(RejeSMIF device)
            : base(device, "MOV", "MAPDO", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFORGSHHeandler : RejeSMIFHandler
    {
        public RejeSMIFORGSHHeandler(RejeSMIF device)
            : base(device, "MOV", "ORGSH", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFLoadCassetteHeandler : RejeSMIFHandler
    {
        public RejeSMIFLoadCassetteHeandler(RejeSMIF device)
            : base(device, "MOV", "CLDMP", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFUnLoadCassetteHeandler : RejeSMIFHandler
    {
        public RejeSMIFUnLoadCassetteHeandler(RejeSMIF device)
            : base(device, "MOV", "CUDMP", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFResetHeandler : RejeSMIFHandler
    {
        public RejeSMIFResetHeandler(RejeSMIF device)
            : base(device, "SET", "RESET", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFSetHomePositionHeandler : RejeSMIFHandler
    {
        public RejeSMIFSetHomePositionHeandler(RejeSMIF device, int position)
            : base(device, "SET", $"H{position}", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFSetWTYHeandler : RejeSMIFHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="mapNo">0-39</param>
        public RejeSMIFSetWTYHeandler(RejeSMIF device, int mapNo)
            : base(device, "SET", $"WTY{mapNo}", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            msg.IsAck = true;

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFGetStatusHandler : RejeSMIFHandler
    {
        RejeSMIF _device = null;
        private string sysStatus = "-1"; //位置1 状态0 正常，A可恢复报警
        private string mode = "-1"; //位置2 模式 0 online模式,1 维护模式，T 示教模式
        private int isRunOrigin = -1; //位置3 0未执行，1执行
        private int runStatus = -1; //位置4 0停止状态 1运行状态
        private string errorCode = "00";//5-6
        private int podStatus = -1;//7 0不在位,1 在位
        private int clampStatus = -1;//8 0松开状态,加紧状态，?异常状态
        private int doorSstatus = -1;//9 0门关闭状态，1夹紧状态，?异常状态
        private int waferStatus = -1;//12 0晶圆凸出,1无晶圆凸出
        private int zAixsPod = -1;//13 0 Home位置,1Stage位置,? 未知位置



        public RejeSMIFGetStatusHandler(RejeSMIF device)
            : base(device, "GET", $"STATE", "")
        {
            _device = device;
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;
            string raw = result.RawMessage;
            msg.IsAck = true;



            if (result.Data != null && result.Data.Length >= 20)
            {

                //获取状态信息
                sysStatus = result.Data.Substring(0, 1);
                mode = result.Data.Substring(1, 1);
                isRunOrigin = Convert.ToInt32(result.Data.Substring(2, 1));
                runStatus = Convert.ToInt32(result.Data.Substring(3, 1));
                errorCode = result.Data.Substring(4, 2);

                podStatus = Convert.ToInt32(result.Data.Substring(6, 1));
                clampStatus = Convert.ToInt32(result.Data.Substring(7, 1));
                doorSstatus = Convert.ToInt32(result.Data.Substring(8, 1));
                waferStatus = Convert.ToInt32(result.Data.Substring(11, 1));
                zAixsPod = Convert.ToInt32(result.Data.Substring(12, 1));

                if (errorCode != "00")
                {
                    _device.NoteError(errorCode, 3);
                    handled = false;
                    return false;
                }

            }
            else
            {
                handled = false;
                return false;
            }

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFGetVersnHandler : RejeSMIFHandler
    {
        public RejeSMIFGetVersnHandler(RejeSMIF device)
            : base(device, "GET", $"VERSN", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            if (result.Data != null && result.Data.Length > 0)
            {
                msg.IsAck = true;
                //获取状态信息
            }
            else
            {
                handled = false;
                return false;
            }

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFGetMapingStateHandler : RejeSMIFHandler
    {
        public RejeSMIFGetMapingStateHandler(RejeSMIF device)
            : base(device, "GET", $"MAPRD", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            if (result.Data != null && result.Data.Length > 0)
            {
                msg.IsAck = true;
                //获取状态信息
            }
            else
            {
                handled = false;
                return false;
            }

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }

    public class RejeSMIFGetWaferStateHandler : RejeSMIFHandler
    {
        public RejeSMIFGetWaferStateHandler(RejeSMIF device)
            : base(device, "GET", $"THICK", "")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RejeSMIFMessage;

            if (result.Data != null && result.Data.Length > 0)
            {
                msg.IsAck = true;
                //获取状态信息
            }
            else
            {
                handled = false;
                return false;
            }

            if (base.HandleMessage(msg, out handled))
            {
                //Device.NoteConstant(result.MessagePart[1]);
            }
            return true;
        }
    }
}
