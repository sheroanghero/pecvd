using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.EdwardsPump2205
{
    public abstract class EdwardsPump2205Handler : HandlerBase
    {
        public EdwardsPump2205 Device { get; }

        public byte _command;
        protected string _parameter;


        protected EdwardsPump2205Handler(EdwardsPump2205 device, EdwardsPump2205MessageType messageType, byte parameter)
            : base(BuildMessage(messageType, new byte[] { parameter }))
        {
            Device = device;
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] byteArray = new byte[] { parameter };
            _command = GetCommandByte(messageType);
            _parameter = asciiEncoding.GetString(byteArray);
            Name = messageType.ToString();
        }

        private byte GetCommandByte(EdwardsPump2205MessageType messageType)
        {
            if (messageType == EdwardsPump2205MessageType.Command)
            {
                return 0x23;
            }
            else if (messageType == EdwardsPump2205MessageType.SetSpeedSetPoint)
            {
                return 0x23;
            }
            else
                return 0x20;
        }

        protected EdwardsPump2205Handler(EdwardsPump2205 device, EdwardsPump2205MessageType messageType, byte[] parameter)
            : base(BuildMessage(messageType, parameter))
        {
            Device = device;
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] byteArray = parameter;
            _command = GetCommandByte(messageType);
            _parameter = asciiEncoding.GetString(byteArray);
            Name = messageType.ToString();
        }

        private static string _endLine = "\r";
        private static byte[] BuildMessage(EdwardsPump2205MessageType messageType, byte[] parameter)
        {
            byte[] cmdBytes = null;
            switch(messageType)
            {
                case EdwardsPump2205MessageType.Command:
                    cmdBytes = new byte[10];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x20; //Space
                    cmdBytes[5] = 0x45; //E

                    cmdBytes[6] = 0x30;

                    cmdBytes[7] = parameter[0];

                    cmdBytes[8] = 0x03; //Etx
                    cmdBytes[9] = 0xFF; //
                    cmdBytes[9] = CheckXor(cmdBytes); //
                    break;
                case EdwardsPump2205MessageType.ReadModFonct:
                    cmdBytes = new byte[8];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x3F; //?
                    cmdBytes[5] = 0x4D; //M

                    cmdBytes[6] = 0x03; //Etx
                    cmdBytes[7] = 0xFF; //
                    cmdBytes[7] = CheckXor(cmdBytes); //
                    break;
                case EdwardsPump2205MessageType.ReadMeasValue:
                    cmdBytes = new byte[8];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x3F; //?
                    cmdBytes[5] = 0x5B; //[

                    cmdBytes[6] = 0x03; //Etx
                    cmdBytes[7] = 0xFF; //
                    cmdBytes[7] = CheckXor(cmdBytes); //
                    break;
                case EdwardsPump2205MessageType.ReadEvents:
                    cmdBytes = new byte[8];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x3F; //?
                    cmdBytes[5] = 0x67; //g

                    cmdBytes[6] = 0x03; //Etx
                    cmdBytes[7] = 0xFF; //
                    cmdBytes[7] = CheckXor(cmdBytes); //
                    break;
                case EdwardsPump2205MessageType.SetSpeedSetPoint:
                    cmdBytes = new byte[8];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x20; //Sp
                    cmdBytes[5] = 0x68; //h

                    cmdBytes[6] = 0x03; //Etx
                    cmdBytes[7] = 0xFF; //
                    cmdBytes[7] = CheckXor(cmdBytes); //
                    break;
                case EdwardsPump2205MessageType.ReadSpeedSetPoint:
                    cmdBytes = new byte[8];
                    cmdBytes[0] = 0x02; //Stx
                    cmdBytes[1] = 0x30; //0
                    cmdBytes[2] = 0x30; //0
                    cmdBytes[3] = 0x31; //1

                    cmdBytes[4] = 0x3F; //?
                    cmdBytes[5] = 0x68; //h

                    cmdBytes[6] = 0x03; //Etx
                    cmdBytes[7] = 0xFF; //
                    cmdBytes[7] = CheckXor(cmdBytes); //
                    break;

            }
            return cmdBytes;
        }

        private static byte CheckXor(byte[] cmdBytes)
        {
            byte rt = 0;
            for(int i=0;i<cmdBytes.Length;i++)
            {
                rt ^= cmdBytes[i];
            }
            return rt;
        }
        protected bool MatchMessage(EdwardsPump2205Message msg)
        {
            if (msg.FunctionCode != this._command)
                return false;
            return true;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            handled = false;
            EdwardsPump2205Message response = msg as EdwardsPump2205Message;

            if (!MatchMessage(response))
                return false;

            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

                if (msg.IsError)
                {
                    Device.NoteError($"Command '{_command}' Error: {response.Datas}:{ErrorString(response.ErrorText)}");
                }
                else
                {
                    SetState(EnumHandlerState.Completed);
                    handled = true;
                    Device.NoteError(null);
                    return true;
                }
            }

            handled = false;
            return false;
        }

        private static Dictionary<string, string> ErrorDict = new Dictionary<string, string>()
        {
            {"1","Invalid message" },
            {"2","Number not found" },
            {"3","Number Invalid" },
            {"4","Parameter’s value not received" },
            {"5","Command not possible" }
        };
        private static string ErrorString(string errorCode)
        {
            if (ErrorDict.ContainsKey(errorCode))
                return ErrorDict[errorCode];
            else
                return "NotDefined error";
        }

    }

    public class EdwardsPump2205RawCommandHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205RawCommandHandler(EdwardsPump2205 device, byte parameter)
            : base(device, EdwardsPump2205MessageType.Command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted();
            }
            return true;
        }


    }
    public class EdwardsPump2205StartHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205StartHandler(EdwardsPump2205 device, byte parameter = 0x31)
            : base(device, EdwardsPump2205MessageType.Command,parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted();
            }
            return true;
        }
    }
    public class EdwardsPump2205StopHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205StopHandler(EdwardsPump2205 device,byte parameter = 0x32)
            : base(device, EdwardsPump2205MessageType.Command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted();
            }
            return true;
        }
    }
    public class EdwardsPump220RestHandler : EdwardsPump2205Handler
    {
        public EdwardsPump220RestHandler(EdwardsPump2205 device, byte parameter = 0x34)
            : base(device, EdwardsPump2205MessageType.Command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSwitchCompleted();
            }
            return true;
        }
    }


    public class EdwardsPump2205ReadMeasValueHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205ReadMeasValueHandler(EdwardsPump2205 device, byte parameter = 0x00)
            : base(device, EdwardsPump2205MessageType.ReadMeasValue, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPump2205Message;

                Device.Speed = Convert.ToInt32(result.AsciiMessage.Substring(54, 4), 16) * 60;  //55-58 转换成10进制后乘60得到转速rpm
                Device.Temperature = Convert.ToInt32(result.AsciiMessage.Substring(70, 4), 16); //71-74
                Device.MotorTemperature = Convert.ToInt32(result.AsciiMessage.Substring(40, 4),16); //41-44
                Device.MotorElectricity = Convert.ToInt32(result.AsciiMessage.Substring(46, 2), 16); //47-48
                Device.NoteRawCommandInfo(_command.ToString(), result.AsciiMessage);
            }
            return true;
        }
    }

    public class EdwardsPump2205ReadModFonctHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205ReadModFonctHandler(EdwardsPump2205 device, byte parameter = 0x00)
            : base(device, EdwardsPump2205MessageType.ReadModFonct, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPump2205Message;
                Device.OperationMode = Convert.ToInt32(result.AsciiMessage.Substring(6, 2), 16); //7、8位01悬浮02不悬浮03加速05减速
                Device.ErrorCount = Convert.ToInt32(result.AsciiMessage.Substring(8, 2), 16); //9、10位报警错误数 根据错误条数确定报警信息
                if (Device.ErrorCount > 0)
                {
                    Device.IsError = true;
                    //Device.Error = Convert.ToInt32(result.AsciiMessage.Substring(10, 2)).ToString();    //取第一个errorcode
                    Device.NoteError(Convert.ToInt32(result.AsciiMessage.Substring(10, 2), 16).ToString());
                }
                Device.NoteRawCommandInfo(_command.ToString(), result.AsciiMessage);
            }
            return true;
        }
    }

    public class EdwardsPump2205ReadMeasHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205ReadMeasHandler(EdwardsPump2205 device,byte parameter = 0x00)
            : base(device, EdwardsPump2205MessageType.ReadMeas, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPump2205Message;
                //Device.Speed = 
                Device.NoteRawCommandInfo(_command.ToString(), result.AsciiMessage);
            }
            return true;
        }
    }

    public class EdwardsPump2205ReadEventsHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205ReadEventsHandler(EdwardsPump2205 device, byte parameter = 0x00)
            : base(device, EdwardsPump2205MessageType.ReadEvents, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as EdwardsPump2205Message;
                Device.NoteError(result.ErrorText);
            }
            return true;
        }
    }

    public class EdwardsPump2205SetSpeedSetPointHandler : EdwardsPump2205Handler
    {
        public EdwardsPump2205SetSpeedSetPointHandler(EdwardsPump2205 device, byte[] parameters)
            : base(device, EdwardsPump2205MessageType.SetSpeedSetPoint, parameters)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
            }
            return true;
        }
    }
}

