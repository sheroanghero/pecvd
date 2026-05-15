using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Siasuns
{
    public enum SiasunsType
    {
        GET,
        MOV,
        SET,
    }

    public enum SiasunsMovCmd
    {
        INIT,
        ORGSH,
        LOCK,
        UNLOCK,
        DOCK,
        UNDOCK,
        OPEN,
        CLOSE,
        WAFSH,
        MAPDT,
        GOTO,
        LOAD,
        UNLOAD,
        ALIGN,
        HOME,
        TRANSREQ,
        ADPLOCK,
        ADPUNLOCK,
    }

    public enum SiasunsGetCmd
    {
        MAPDT,
        ERROR,
        CLAMP,
        STATE,
        MODE,
        TRANSREQ,
        SIGSTAT,
        EVENT,
        CSTID,
        SIZE,
    }

    public enum SiasunsSetCmd
    {
        ALIGN,
        ERROR,
        CLAMP,
        MODE,
        SIGOUT,
        EVENT,
        SIZE,
        FFU,
    }
    public abstract class SiasunsEfemHandler : HandlerBase
    {
        public SiasunsEfem Device { get; }

        public ModuleName Module
        {
            get { return _module; }
        }

        private ModuleName _module;

        protected string _command;
        protected string _parameter;
        protected SiasunsEfemHandler(SiasunsEfem device, ModuleName module, SiasunsType type, string command, string parameter = null)
            : base(BuildMessage(type, command, parameter))
        {
            Device = device;
            _module = module;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        protected SiasunsEfemHandler(string command)
            : base(command)
        {
            _command = command;
        }
        private static string BuildMessage(SiasunsType type, string command, string parameter)
        {
            if(string.IsNullOrEmpty(parameter))
                return $"{type}:{command};\r";
            else
                return $"{type}:{command}/{parameter};\r";

        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunsEfemMessage response = msg as SiasunsEfemMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class SiasunsEfemMOVHandler : SiasunsEfemHandler
    {
        SiasunsMovCmd cmd;
        public SiasunsEfemMOVHandler(SiasunsEfem device, ModuleName module, SiasunsMovCmd command, string parameter = null)
            : base(device,module, SiasunsType.MOV, command.ToString(), parameter)
        {
            cmd = command;
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunsEfemMessage response = msg as SiasunsEfemMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

            }
            if (response.IsResponse)
            {
                if(cmd == SiasunsMovCmd.INIT)
                {
                    Device.NoteInitComplete(response.RawMessage);
                }
                else if(cmd == SiasunsMovCmd.HOME)
                {
                    Device.NoteHomeComplete(response.RawMessage.Replace("\r", ""));
                }
                else if (cmd == SiasunsMovCmd.ALIGN)
                {
                    Device.NoteAlign(response.RawMessage.Replace("\r", ""));
                }
                SetState(EnumHandlerState.Completed);
                Device.NoteComplete(Module);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class SiasunsEfemSetHandler : SiasunsEfemHandler
    {
        SiasunsSetCmd cmd;
        public SiasunsEfemSetHandler(SiasunsEfem device, ModuleName module, SiasunsSetCmd command, string parameter = null)
            : base(device,module, SiasunsType.SET, command.ToString(), parameter)
        {
            cmd = command;
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunsEfemMessage response = msg as SiasunsEfemMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                Device.ParseData(_command, _parameter, response.Data);
                SetState(EnumHandlerState.Completed);
                Device.NoteComplete(Module);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class SiasunsEfemGetHandler : SiasunsEfemHandler
    {
        SiasunsGetCmd cmd;
        public SiasunsEfemGetHandler(SiasunsEfem device, ModuleName module, SiasunsGetCmd command, string parameter = null)
            : base(device,module, SiasunsType.GET, command.ToString(), parameter)
        {
            cmd = command;
            string temp = string.IsNullOrEmpty(parameter) ? parameter : "";
            LOG.Write($"{device.Name} execute move command {command} {temp} in byte.");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SiasunsEfemMessage response = msg as SiasunsEfemMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);

            }
            if (response.IsResponse)
            {
                string[] param = response.MessagePart[1].Split('/');
                if (cmd == SiasunsGetCmd.SIGSTAT)
                {
                    Device.ParseData(cmd.ToString(), param[0], param[1]);
                }
                
                SetState(EnumHandlerState.Completed);
                Device.NoteComplete(Module);
                transactionComplete = true;
                return true;
            }
            //if (response.IsAck)
            //{
            //    Device.ParseData(_command, _parameter, response.Data);
            //    SetState(EnumHandlerState.Completed);
            //    Device.NoteComplete(Module);
            //    transactionComplete = true;
            //    return true;
            //}

            transactionComplete = false;
            return false;
        }
    }

    public class SiasunEfemHandlerCstid : RorzeEfemHandler
    {
        public SiasunEfemHandlerCstid(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.CSTID, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.CSTID.ToString();
            MutexId = -1;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }

        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            if (ConvertParameterToModule(msg.MessagePart[2], out ModuleName target))
            {
                Device.NoteCarrierIDReadResult(target, msg.MessagePart[3]);
            }

            if (!msg.IsEvent)
            {
                Device.NoteComplete(Module);
            }

            return true;
        }
    }

    public class SiasunEfemHandlerInit : RorzeEfemHandler
    {
        public SiasunEfemHandlerInit(RorzeEfem device, ModuleName module)
        : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.INIT, ConvertSiasunModuleToParameter(module), true)
        {
            Name = $"Initialize {module}";
            MutexId = (int)module;
        }

        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            if (Module == ModuleName.System)
                Device.NoteInitialized();

            Device.NoteComplete(Module);

            return true;
        }
        public static string ConvertSiasunModuleToParameter(ModuleName module)
        {
            if (module == ModuleName.System)
                return "ALL";

            if (ParameterModuleMap.Values.Contains(module))
            {
                foreach (var moduleName in ParameterModuleMap)
                {
                    if (moduleName.Value == module)
                        return moduleName.Key;
                }
            }

            
            return "";
        }
    }


    public class SiasunEfemHandlerAlign : RorzeEfemHandler
    {
        public SiasunEfemHandlerAlign(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ALIGN, "ALIGN1", true)
        {
            Name = RorzeEfemBasicMessage.ALIGN.ToString();
            MutexId = (int)ModuleName.Aligner;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //ERROR
    public class SiasunsEfemHandlerSetAlign : RorzeEfemHandler
    {
        public SiasunsEfemHandlerSetAlign(RorzeEfem device, double angle)
            : base(device, ModuleName.System, RorzeEfemMessageType.SET, RorzeEfemBasicMessage.ALIGN, $"ALIGN1/D{angle.ToString("F2").PadLeft(6,'0')}", false)
        {
            Name = "Set Align";
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }


    //SIGOUT
    public class SiasunsEfemHandlerSigout : RorzeEfemHandler
    {
        public SiasunsEfemHandlerSigout(RorzeEfem device, ModuleName module, IndicatorType type, IndicatorState state)
            : base(device, module, RorzeEfemMessageType.SET, RorzeEfemBasicMessage.SIGOUT, BuildParameter(module, type, state), false)
        {
            Name = $"Set Signal Tower";
            MutexId = -1;
        }

        public SiasunsEfemHandlerSigout(RorzeEfem device, ModuleName module, LightType type, TowerLightStatus state)
            : base(device, module, RorzeEfemMessageType.SET, RorzeEfemBasicMessage.SIGOUT, BuildParameter(module, type, state), false)
        {
            Name = $"Set Signal Tower";
            MutexId = -1;
        }

        public static string BuildParameter(ModuleName target, IndicatorType type, IndicatorState state)
        {
            Dictionary<IndicatorType, string> mapLightType = new Dictionary<IndicatorType, string>()
            {
                {IndicatorType.AccessAuto, "AUTO"},
                {IndicatorType.AccessManual, "MANUAL"},
                {IndicatorType.Load, "LOAD"},
                {IndicatorType.Unload, "UNLOAD"},
                {IndicatorType.Access, "ACCESS"},
            };

            Dictionary<IndicatorState, string> mapLightState = new Dictionary<IndicatorState, string>()
            {
                {IndicatorState.ON, "ON"},
                {IndicatorState.OFF, "OFF"},
                {IndicatorState.BLINK, "BLINK"},
            };

            string par1 = ConvertModuleToParameter(target);
            string par2 = mapLightType[type];
            string par3 = mapLightState[state];
            return $"{par1}/{par2}/{par3}";
        }

        public static string BuildParameter(ModuleName target, LightType type, TowerLightStatus state)
        {
            if (type == LightType.Buzzer)
            {
                type = state == TowerLightStatus.Blinking ? LightType.Buzzer2 : LightType.Buzzer1;
                state = state == TowerLightStatus.Off ? TowerLightStatus.Off : TowerLightStatus.On;
            }

            Dictionary<LightType, string> mapLightType = new Dictionary<LightType, string>()
            {
                {LightType.Red, "RED"},
                {LightType.Yellow, "YELLOW"},
                {LightType.Green, "GREEN"},
                {LightType.Blue, "BLUE"},
                {LightType.White, "WHITE"},
                {LightType.Buzzer1, "BUZZER1"},
                {LightType.Buzzer2, "BUZZER2"},
            };

            Dictionary<TowerLightStatus, string> mapLightState = new Dictionary<TowerLightStatus, string>()
            {
                {TowerLightStatus.On, "ON"},
                {TowerLightStatus.Off, "OFF"},
                {TowerLightStatus.Blinking, "BLINK"},
            };

            string par1 = "STOWER";
            string par2 = mapLightType[type];
            string par3 = mapLightState[state];
            return $"{par1}/{par2}/{par3}";
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }

        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            return true;
        }
    }
}
