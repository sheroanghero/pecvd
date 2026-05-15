using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Device;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public abstract class RorzeEfemHandler : HandlerBase
    {
        public static Dictionary<string, ModuleName> ParameterModuleMap = new Dictionary<string, ModuleName>()
        {
            {"SYSTEM", ModuleName.System},
            {"ROB", ModuleName.EfemRobot},
            {"P1", ModuleName.LP1},
            {"P2", ModuleName.LP2},
            {"P3", ModuleName.LP3},
            {"P4", ModuleName.LP4},
            {"LLA", ModuleName.LL1},
            {"LLB", ModuleName.LL2},
            {"BF1", ModuleName.Buffer},
        };
        public RorzeEfem Device { get; }
        public ModuleName Module
        {
            get { return _module; }
        }
        public static string NewLine => "\r";
        private ModuleName _module;

        private RorzeEfemMessageType _type;
        private RorzeEfemBasicMessage _basicMessage;
        private string _parameter;
        private bool _waitInfo;

        protected RorzeEfemHandler(RorzeEfem device, ModuleName module,
            RorzeEfemMessageType type, RorzeEfemBasicMessage basicMessage, string parameter, bool waitInfo)
            : base(BuildMessage(type, basicMessage, parameter))
        {
            Device = device;
            _module = module;
            _waitInfo = waitInfo;
            _basicMessage = basicMessage;
            _parameter = parameter;
            _type = type;
        }

        public static string BuildMessage(RorzeEfemMessageType type, RorzeEfemBasicMessage basicMessage, string parameter)
        {
            return string.IsNullOrEmpty(parameter) ? $"{type}:{basicMessage};{NewLine}" : $"{type}:{basicMessage}/{parameter};{NewLine}";
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            transactionComplete = false;
            RorzeEfemMessage message = baseMessage as RorzeEfemMessage;
            if (!MatchMessage(message))
                return false;

            if (message.MessageType == RorzeEfemMessageType.ACK)
            {
                OnAcked();

                if (!_waitInfo)
                {
                    OnComplete();
                    transactionComplete = true;
                }
                return true;
            }

            if (message.MessageType == RorzeEfemMessageType.INF)
            {
                transactionComplete = true;

                ProceedInfo(message);

                OnComplete();
            }

            if (message.MessageType == RorzeEfemMessageType.ABS)
            {
                transactionComplete = true;

                ProceedAbs(message);

                OnComplete();
            }


            if (message.MessageType == RorzeEfemMessageType.NAK)
            {
                transactionComplete = true;

                ProceedNak(message);

                OnComplete();
            }

            if (message.MessageType == RorzeEfemMessageType.CAN)
            {
                transactionComplete = true;

                ProceedCancel(message);

                OnComplete();
            }

            if(message.MessageType == RorzeEfemMessageType.EVT)
            {
                transactionComplete = true;
                ProceedEvent(message);
            }

            return true;
        }

        protected virtual void ProceedNak(RorzeEfemMessage message)
        {
            Device.NoteCancel(Module, $"Can not {Name}, " + RorzeEfemAbsError.GetError(message.NakFactor));
        }

        protected virtual void ProceedCancel(RorzeEfemMessage message)
        {
            Device.NoteCancel(Module, $"Can not {Name}, " + RorzeEfemAbsError.GetError(message.CanError));
        }

        protected virtual void ProceedAbs(RorzeEfemMessage message)
        {
            Device.NoteFailed(Module, $"Failed {Name}, " + RorzeEfemAbsError.GetError(message.AbsError));
        }

        protected virtual bool ProceedInfo(RorzeEfemMessage msg)
        {
            Device.NoteComplete(Module);

            return true;
        }
        protected virtual bool ProceedEvent(RorzeEfemMessage msg)
        {
            if (msg.IsEvent && ConvertParameterToModule(msg.MessagePart[2], out ModuleName target))
            {
                if(msg.MessagePart[1] == "MAPDT")
                    Device.NoteWaferMapResult(target, msg.MessagePart[3]);
                else
                    Device.NoteStateEvent(target, msg.MessagePart[3], msg.MessagePart[4]);
            }

            return true;
        }

        protected bool MatchMessage(RorzeEfemMessage msg)
        {
            if (msg.BasicMessage != _basicMessage || !msg.Parameter.Contains(_parameter))
                return false;

            return true;
        }

        public static string ConvertModuleToParameter(ModuleName module)
        {
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
        public static bool ConvertParameterToModule(string parameter, out ModuleName module)
        {
            if (ParameterModuleMap.ContainsKey(parameter))
            {
                module = ParameterModuleMap[parameter];
                return true;
            }

            module = ModuleName.System;
            return false;
        }
    }

    //READY
    public class RorzeEfemHandlerReady : RorzeEfemHandler
    {
        public RorzeEfemHandlerReady(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.READY, ConvertModuleToParameter(module), true)
        {
            Name = $"Ready {module}";
            MutexId = -1;
        }
 
        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            Device.NoteCommunicationReady();

            transactionComplete = true;

            return true;
        }

    }

    //INIT
    public class RorzeEfemHandlerInit : RorzeEfemHandler
    {
        public RorzeEfemHandlerInit(RorzeEfem device, ModuleName module)
        : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.INIT, ConvertModuleToParameter(module), true)
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
    }

    //ORGSH
    public class RorzeEfemHandlerOrgsh : RorzeEfemHandler
    {
        public RorzeEfemHandlerOrgsh(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ORGSH, ConvertModuleToParameter(module), true)
        {
            Name = $"Home {module}";
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }

    }

    //LOCK
    public class RorzeEfemHandlerLock : RorzeEfemHandler
    {
        public RorzeEfemHandlerLock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.LOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.LOCK.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //UNLOCK
    public class RorzeEfemHandlerUnlock : RorzeEfemHandler
    {
        public RorzeEfemHandlerUnlock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.UNLOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.UNLOCK.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //DOCK
    public class RorzeEfemHandlerDock : RorzeEfemHandler
    {
        public RorzeEfemHandlerDock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.DOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.DOCK.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //UNDOCK
    public class RorzeEfemHandlerUndock : RorzeEfemHandler
    {
        public RorzeEfemHandlerUndock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.UNDOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.UNDOCK.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //OPEN
    public class RorzeEfemHandlerOpen : RorzeEfemHandler
    {
        public RorzeEfemHandlerOpen(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.OPEN, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.OPEN.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //CLOSE
    public class RorzeEfemHandlerClose : RorzeEfemHandler
    {
        public RorzeEfemHandlerClose(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.CLOSE, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.CLOSE.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //WAFSH
    public class RorzeEfemHandlerWafsh : RorzeEfemHandler
    {
        public RorzeEfemHandlerWafsh(RorzeEfem device, ModuleName target, bool mapByRobot)
            : base(device, mapByRobot ? ModuleName.EfemRobot : target, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.WAFSH, ConvertModuleToParameter(target), true)
        {
            Name = $"Map wafer of {target}";
            MutexId = mapByRobot ? (int)ModuleName.EfemRobot : (int)target;
        }

        public RorzeEfemHandlerWafsh(RorzeEfem device, ModuleName target, bool mapByRobot, int thicknessIndex)
            : base(device, mapByRobot ? ModuleName.EfemRobot : target, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.WAFSH, BuildParameter(target, thicknessIndex), true)
        {
            Name = $"Map wafer of {target}";
            MutexId = mapByRobot ? (int)ModuleName.EfemRobot : (int)target;
        }


        public static string BuildParameter(ModuleName target, int thicknessIndex)
        {
            string para1 = ConvertModuleToParameter(target);
            return $"{para1}/{thicknessIndex}";
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //MAPDT
    public class RorzeEfemHandlerMapdt : RorzeEfemHandler
    {
        private ModuleName _target;

        public RorzeEfemHandlerMapdt(RorzeEfem device, ModuleName target, bool mapByRobot)
            : base(device, mapByRobot ? ModuleName.EfemRobot : target, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.MAPDT, ConvertModuleToParameter(target), true)
        {
            Name = $"Get wafer map result of {target}";

            _target = target;
            MutexId = mapByRobot ? (int)ModuleName.EfemRobot : (int)target;
        }

        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            if (ConvertParameterToModule(msg.MessagePart[2], out ModuleName target))
            {
                Device.NoteWaferMapResult(target, msg.MessagePart[3]);
            }

            if (!msg.IsEvent)
            {
                Device.NoteComplete(Module);
            }

            return true;
        }


        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //GOTO
    public class RorzeEfemHandlerGoto : RorzeEfemHandler
    {
        public RorzeEfemHandlerGoto(RorzeEfem device, ModuleName target, int slot, Hand blade, bool isPick)
            : base(device, ModuleName.EfemRobot, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.GOTO, BuildParameter(target, slot, blade, isPick), true)
        {
            Name = $"Goto from {target} slot {slot + 1}";

            MutexId = (int)ModuleName.EfemRobot;
        }

        public static string BuildParameter(ModuleName target, int slot, Hand blade, bool isPick)
        {
            string para1 = ConvertModuleToParameter(target);
            string para2 = blade == Hand.Blade1 ? "ARM2" : (blade == Hand.Blade2 ? "ARM1" : "ARM3");
            string para3 = isPick ? "DOWN":"UP";
            return $"{para1}{slot + 1:D2}/{para2}/{para3}";
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //LOAD
    public class RorzeEfemHandlerLoad : RorzeEfemHandler
    {
        public RorzeEfemHandlerLoad(RorzeEfem device, ModuleName target, int slot, Hand blade, WaferSize waferSize)
            : base(device, ModuleName.EfemRobot, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.LOAD, BuildParameter(target, slot, blade, waferSize), true)
        {
            Name = $"Pick from {target} slot {slot + 1}";

            MutexId = (int)ModuleName.EfemRobot;
        }

        public static string BuildParameter(ModuleName target, int slot, Hand blade, WaferSize waferSize)
        {
            string para1 = ConvertModuleToParameter(target);
            string para2 = blade == Hand.Blade1 ? "ARM2" : (blade == Hand.Blade2 ? "ARM1" : "ARM3");
            string para3 = waferSize.ToString();
            //return $"{para1}{slot + 1:D2}/{para2}/{para3}";
            if (target == ModuleName.Aligner)
            {
                return $"{para1}1/{para2}";
            }
            else
            {
                return $"{para1}{slot + 1:D2}/{para2}";
            }
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //UNLOAD
    public class RorzeEfemHandlerUnload : RorzeEfemHandler
    {
        public RorzeEfemHandlerUnload(RorzeEfem device, ModuleName target, int slot, Hand blade, WaferSize waferSize)
            : base(device, ModuleName.EfemRobot, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.UNLOAD, BuildParameter(target, slot, blade, waferSize), true)
        {
            Name = $"Place to {target} slot {slot + 1}";
            MutexId = (int)ModuleName.EfemRobot;
        }

        public static string BuildParameter(ModuleName target, int slot, Hand blade, WaferSize waferSize)
        {
            string para1 = ConvertModuleToParameter(target);
            string para2 = blade == Hand.Blade1 ? "ARM2" : (blade == Hand.Blade2 ? "ARM1" : "ARM3");
            string para3 = waferSize.ToString();
            //return $"{para1}{slot + 1:D2}/{para2}/{para3}";
            if(target == ModuleName.Aligner)
            {
                return $"{para1}1/{para2}";
            }   
            else
            {
                return $"{para1}{slot + 1:D2}/{para2}";
            }
           
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //TRANS
    public class RorzeEfemHandlerTrans : RorzeEfemHandler
    {
        public RorzeEfemHandlerTrans(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.TRANS, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.TRANS.ToString();
            MutexId = (int)ModuleName.EfemRobot;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //CHANGE
    public class RorzeEfemHandlerChange : RorzeEfemHandler
    {
        public RorzeEfemHandlerChange(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.CHANGE, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.CHANGE.ToString();
            MutexId = (int)ModuleName.EfemRobot;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //ALIGN
    //public class RorzeEfemHandlerAlign : RorzeEfemHandler
    //{
    //    public RorzeEfemHandlerAlign(RorzeEfem device, ModuleName module, bool isMov)
    //        : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ALIGN, ConvertModuleToParameter(module), true)
    //    {
    //        Name = RorzeEfemBasicMessage.ALIGN.ToString();
    //        MutexId = (int)ModuleName.Aligner;
    //    }

    //    public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
    //    {
    //        return base.HandleMessage(baseMessage, out transactionComplete);
    //    }
    //}
    //ALIGN
    public class RorzeEfemHandlerAlign : RorzeEfemHandler
    {
        public RorzeEfemHandlerAlign(RorzeEfem device, ModuleName module, double angle)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ALIGN, $"A{angle.ToString("F1")}", true)
        {
            Name = RorzeEfemBasicMessage.ALIGN.ToString();
            MutexId = (int)ModuleName.Aligner;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //HOME
    public class RorzeEfemHandlerHome : RorzeEfemHandler
    {

        public RorzeEfemHandlerHome(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.HOME, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.HOME.ToString();
            MutexId = (int)module;
        }


    }

    //HOLD
    public class RorzeEfemHandlerHold : RorzeEfemHandler
    {
        public RorzeEfemHandlerHold(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.HOLD, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.HOLD.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //RESTR
    public class RorzeEfemHandlerRestr : RorzeEfemHandler
    {
        public RorzeEfemHandlerRestr(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.RESTR, ConvertModuleToParameter(module), false)
        {
            Name = RorzeEfemBasicMessage.RESTR.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //ABORT
    public class RorzeEfemHandlerAbort : RorzeEfemHandler
    {
        public RorzeEfemHandlerAbort(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ABORT, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.ABORT.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //EMS
    public class RorzeEfemHandlerEms : RorzeEfemHandler
    {
        public RorzeEfemHandlerEms(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.EMS, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.EMS.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //ERROR
    public class RorzeEfemHandlerError : RorzeEfemHandler
    {
        public RorzeEfemHandlerError(RorzeEfem device, bool isQuery)
            : base(device, ModuleName.System, isQuery ? RorzeEfemMessageType.GET : RorzeEfemMessageType.SET, RorzeEfemBasicMessage.ERROR, isQuery ? "" : "CLEAR", isQuery)
        {
            Name = RorzeEfemBasicMessage.ERROR.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

    //CLAMP
    public class RorzeEfemHandlerClamp : RorzeEfemHandler
    {
        public RorzeEfemHandlerClamp(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.CLAMP, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.CLAMP.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //STATE
    public class RorzeEfemHandlerState : RorzeEfemHandler
    {
        private RorzeEfemStateType _state;
        public RorzeEfemHandlerState(RorzeEfem device, RorzeEfemStateType state)
            : base(device, ModuleName.System, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.STATE, state.ToString(), true)
        {
            Name = "Query State";
            _state = state;
            MutexId = (int)ModuleName.System;
        }

        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            if (_state == RorzeEfemStateType.TRACK)
            {
                Device.NoteWaferTrack(msg.MessagePart[3]);
            }
            //Device.NoteComplete(Module);

            return true;
        }
    }
    //LED
    public class RorzeEfemHandlerLed : RorzeEfemHandler
    {
        public RorzeEfemHandlerLed(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.LED, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.LED.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //WORKCHK
    public class RorzeEfemHandlerWorkChk : RorzeEfemHandler
    {
        public RorzeEfemHandlerWorkChk(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.WORKCHK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.WORKCHK.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //FFU
    public class RorzeEfemHandlerFfu : RorzeEfemHandler
    {
        public RorzeEfemHandlerFfu(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.FFU, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.FFU.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //WTYPE
    public class RorzeEfemHandlerWType : RorzeEfemHandler
    {
        public RorzeEfemHandlerWType(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.WTYPE, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.WTYPE.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //PURGE
    public class RorzeEfemHandlerPurge : RorzeEfemHandler
    {
        public RorzeEfemHandlerPurge(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.PURGE, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.PURGE.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //ADPLOCK
    public class RorzeEfemHandlerAdpLock : RorzeEfemHandler
    {
        public RorzeEfemHandlerAdpLock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ADPLOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.ADPLOCK.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //ADPUNLOCK
    public class RorzeEfemHandlerAdpUnlock : RorzeEfemHandler
    {
        public RorzeEfemHandlerAdpUnlock(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.ADPUNLOCK, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.ADPUNLOCK.ToString();
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //MODE
    public class RorzeEfemHandlerMode : RorzeEfemHandler
    {
        public RorzeEfemHandlerMode(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.MODE, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.MODE.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //TRANSREQ
    public class RorzeEfemHandlerTransReq : RorzeEfemHandler
    {
        public RorzeEfemHandlerTransReq(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.TRANSREQ, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.TRANSREQ.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }
    //SIGOUT
    public class RorzeEfemHandlerSigout : RorzeEfemHandler
    {
        public RorzeEfemHandlerSigout(RorzeEfem device, ModuleName module, IndicatorType type, IndicatorState state)
            : base(device, module, RorzeEfemMessageType.SET, RorzeEfemBasicMessage.SIGOUT, BuildParameter(module, type, state), false)
        {
            Name = $"Set Signal Tower";
            MutexId = -1;
        }

        public RorzeEfemHandlerSigout(RorzeEfem device, ModuleName module, LightType type, TowerLightStatus state)
            : base(device, module, RorzeEfemMessageType.SET, RorzeEfemBasicMessage.SIGOUT, BuildParameter(module, type, state), false)
        {
            Name = $"Set Signal Tower";
            MutexId = -1;
        }

        public static string BuildParameter(ModuleName target, IndicatorType type, IndicatorState state)
        {
            Dictionary<IndicatorType, string> mapLightType = new Dictionary<IndicatorType, string>()
            {
                {IndicatorType.AccessAuto, "AUTO MODE"},
                {IndicatorType.AccessManual, "MANUAL MODE"},
                {IndicatorType.Load, "LOAD"},
                {IndicatorType.Unload, "UNLOAD"},
                {IndicatorType.Presence, "PRESENCE"},
                {IndicatorType.Placement, "PLACEMENT"},
                {IndicatorType.Error, "ERROR"},
                {IndicatorType.Busy, "BUSY"},
                {IndicatorType.Complete, "COMPLETE"},
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

    /// <summary>
    /// SIGSTAT
    /// EVT:/SIGSTAT/PARAMETER/DATA1/DATA2
    /// </summary>
    public class RorzeEfemHandlerSigStat : RorzeEfemHandler
    {
        public RorzeEfemHandlerSigStat(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.GET, RorzeEfemBasicMessage.SIGSTAT, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.SIGSTAT.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            if (baseMessage.IsEvent)
            {
                var msg = baseMessage as RorzeEfemMessage;

                if (!ConvertParameterToModule(msg.MessagePart[2], out ModuleName module))
                {
                    EV.PostWarningLog(Device.Module, $"Parameter unrecognized, {msg.RawMessage}");
                }
                else
                {
                    Device.NoteStateEvent(module, msg.MessagePart[3], msg.MessagePart[4]);
                }

                transactionComplete = true;
                return true;
            }

            return base.HandleMessage(baseMessage, out transactionComplete);
        }
        protected override bool ProceedInfo(RorzeEfemMessage msg)
        {
            if (ConvertParameterToModule(msg.MessagePart[2], out ModuleName target))
            {
                Device.NoteStateEvent(target, msg.MessagePart[3], msg.MessagePart[4]);
            }

            if (!msg.IsEvent && ModuleHelper.IsLoadPort(Module))
            {
                Device.NoteComplete(Module);
            }

            return true;
        }
    }

    //CSTID
    public class RorzeEfemHandlerCstid : RorzeEfemHandler
    {
        public RorzeEfemHandlerCstid(RorzeEfem device, ModuleName module)
            : base(device, module, RorzeEfemMessageType.MOV, RorzeEfemBasicMessage.CSTID, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.CSTID.ToString();
            MutexId = (int)module;
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


    //EVENT
    public class RorzeEfemHandlerEvent : RorzeEfemHandler
    {
        public RorzeEfemHandlerEvent(RorzeEfem device, ModuleName module, bool isSet = true)
            : base(device, module, isSet ? RorzeEfemMessageType.SET : RorzeEfemMessageType.GET, RorzeEfemBasicMessage.EVENT, ConvertModuleToParameter(module), true)
        {
            Name = RorzeEfemBasicMessage.EVENT.ToString();
            MutexId = (int)module;
        }

        public override bool HandleMessage(MessageBase baseMessage, out bool transactionComplete)
        {
            return base.HandleMessage(baseMessage, out transactionComplete);
        }
    }

}
