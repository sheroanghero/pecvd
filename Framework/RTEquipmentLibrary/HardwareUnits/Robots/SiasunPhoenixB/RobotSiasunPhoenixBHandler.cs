using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.SiasunPhoenixB
{
    public abstract class RobotSiasunPhoenixBHandler : HandlerBase
    {
        public RobotSiasunPhoenixB Device { get; }
        public bool HasResponse { get; set; } = true;

        protected string _command;
        protected string _parameter;
        protected string _target;
        protected RobotArmEnum _blade;


        protected string _requestResponse = "";

        protected RobotSiasunPhoenixBHandler(RobotSiasunPhoenixB device, string command, string parameter = null)
            : base(BuildMessage(command, parameter))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            Name = command;
        }
        private static string BuildMessage(string command, string parameter)
        {
            string msg = parameter == null ? $"{command}" : $"{command} {parameter}";

            return msg + "\r";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RobotSiasunPhoenixBMessage response = msg as RobotSiasunPhoenixBMessage;
            ResponseMessage = msg;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }
            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class RobotSiasunPhoenixBRawCommandHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBRawCommandHandler(RobotSiasunPhoenixB device, string command, string parameter = null)
            : base(device, command, parameter)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as RobotSiasunPhoenixBMessage;
                var rawMsg = _requestResponse != null ? _requestResponse + "$" + result.RawMessage : result.RawMessage;
                Device.NoteRawCommandInfo(_command, rawMsg);
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBGotoHandler : RobotSiasunPhoenixBHandler
    {
        //GOTO N station [R (EX|RE)] [Z (UP|DN)] [SLOT num] [[ARM] arm]
        public RobotSiasunPhoenixBGotoHandler(RobotSiasunPhoenixB device, ModuleName module, int slot, RobotArmEnum blade,
            bool isRetract = true, bool isZaxisDown = true, int timeout = 60)
            : base(device, "GOTO", $"N {device.ModuleAssociateStationDic[module.ToString()]} R {(isRetract ? "RE" : "EX")}" +
                  $" Z {(isZaxisDown ? "DN" : "UP")} SLOT {slot} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + module;
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = isRetract ? RobotAction.Moving : RobotAction.Picking,
                ArmTarget = blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }

    }
    public class RobotSiasunPhoenixBPickHandler : RobotSiasunPhoenixBHandler
    {
        //PICK station [SLOT slot] [[ARM] arm][ENRT|STRT NR][RO x TO x]
        public RobotSiasunPhoenixBPickHandler(RobotSiasunPhoenixB device, string module, int slot, RobotArmEnum blade,
            int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PICK", $"{device.ModuleAssociateStationDic[module]} SLOT {slot}" +
                  $" ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")}{(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            var moduleArr = module.Split('.');
            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + moduleArr[0];
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = blade == RobotArmEnum.Blade2 ? RobotArm.ArmB : RobotArm.ArmA, //blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = _blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = _target,
                };
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBPickExtendHandler : RobotSiasunPhoenixBHandler
    {
        //PICK station [SLOT slot] [[ARM] arm][ENRT|STRT NR][RO x TO x]
        public RobotSiasunPhoenixBPickExtendHandler(RobotSiasunPhoenixB device, ModuleName module, int slot, RobotArmEnum blade,
            int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PICK", $"{device.ModuleAssociateStationDic[module.ToString()]} SLOT {slot}" +
                  $" ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} ENRT NR {(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + module;
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = blade == RobotArmEnum.Blade2 ? RobotArm.ArmB : RobotArm.ArmA, //blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBPickRetractHandler : RobotSiasunPhoenixBHandler
    {
        //PICK station [SLOT slot] [[ARM] arm][ENRT|STRT NR][RO x TO x]
        public RobotSiasunPhoenixBPickRetractHandler(RobotSiasunPhoenixB device, ModuleName module, int slot, RobotArmEnum blade, int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PICK", $"{device.ModuleAssociateStationDic[module.ToString()]} SLOT {slot} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} STRT NR {(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + module;
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBPlaceHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBPlaceHandler(RobotSiasunPhoenixB device, string module, int slot, RobotArmEnum blade,
            int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PLACE", $"{device.ModuleAssociateStationDic[module]} SLOT {slot}" +
                  $" ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")}{(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            var moduleArr = module.Split('.');
            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + moduleArr[0];
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = blade == RobotArmEnum.Blade2 ? RobotArm.ArmB : RobotArm.ArmA,//RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = _blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = _target,
                };
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBPlaceExtendHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBPlaceExtendHandler(RobotSiasunPhoenixB device, ModuleName module, int slot, RobotArmEnum blade, int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PLACE", $"{device.ModuleAssociateStationDic[module.ToString()]} SLOT {slot} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} ENRT NR {(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + module;
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBPlaceRetractHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBPlaceRetractHandler(RobotSiasunPhoenixB device, ModuleName module, int slot, RobotArmEnum blade, int ro = 0, int to = 0, int timeout = 60, bool isDisableInterlock = false)
            : base(device, "PLACE", $"{device.ModuleAssociateStationDic[module.ToString()]} SLOT {slot} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} STRT NR {(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")}{(isDisableInterlock ? " INTLCK DIS" : "")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);

            _target = (blade == RobotArmEnum.Blade2 ? "ArmB" : "ArmA") + "." + module;
            _blade = blade;

            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = blade == RobotArmEnum.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = _target,
            };
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBTransferHandler : RobotSiasunPhoenixBHandler
    {
        //XFER [[ARM]arm] station-a station-b
        public RobotSiasunPhoenixBTransferHandler(RobotSiasunPhoenixB device, int fromStation, int toStation, RobotArmEnum blade, int timeout)
            : base(device, "XFER", $"ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} {fromStation} {toStation}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }
    }

    public class RobotSiasunPhoenixBRetractHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBRetractHandler(RobotSiasunPhoenixB device, int timeout)
            : base(device, "RETRACT")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;
            Device.NoteActionCompleted();

            return true;
        }

    }

    public class RobotSiasunPhoenixBResetHandler : RobotSiasunPhoenixBHandler
    {
        //RESET
        public RobotSiasunPhoenixBResetHandler(RobotSiasunPhoenixB device)
            : base(device, "RESET")
        {
            HasResponse = false;
        }

        //no return message
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            handled = true;

            return true;
        }
    }

    public class RobotSiasunPhoenixBHaltHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBHaltHandler(RobotSiasunPhoenixB device)
            : base(device, "HALT")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class RobotSiasunPhoenixBSetCommunicationEchoHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBSetCommunicationEchoHandler(RobotSiasunPhoenixB device, bool isEchoOn)
            : base(device, "SET COMM ECHO", isEchoOn ? "ON" : "OFF")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetCommEchoCompleted(true);
            }

            return true;
        }

    }

    public class RobotSiasunPhoenixBSetLoadHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBSetLoadHandler(RobotSiasunPhoenixB device, RobotArmEnum blade, bool isWaferPresent)
            : base(device, "SET LOAD", $"{(blade == RobotArmEnum.Blade1 ? "A" : "B")} {(isWaferPresent ? "ON" : "OFF")}")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBStoreLoadHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBStoreLoadHandler(RobotSiasunPhoenixB device, RobotArmEnum blade)
            : base(device, "STORE LOAD", $"{(blade == RobotArmEnum.Blade1 ? "A" : "B")}")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBSevoOnOffHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBSevoOnOffHandler(RobotSiasunPhoenixB device, bool isOn)
            : base(device, isOn ? "SVON" : "SVOFF")
        {

        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteSevoOnOff(_command == "SVON");
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class RobotSiasunPhoenixBHomeAxisHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBHomeAxisHandler(RobotSiasunPhoenixB device, int timeout)
            : base(device, "HOME", "ALL")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
            device.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as RobotSiasunPhoenixBMessage;
            if (result.IsError)
            {
                Device.NoteError(result.Data);
            }
            else
            {
                Device.NoteError(null);
                Device.NoteAxisHomed();
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }

    }

    public class RobotSiasunPhoenixBCheckLoadHandler : RobotSiasunPhoenixBHandler
    {
        //CHECK LOAD station [[ARM] arm]
        //_RDY
        public RobotSiasunPhoenixBCheckLoadHandler(RobotSiasunPhoenixB device, int station, RobotArmEnum blade, int timeout)
            : base(device, "CHECK LOAD", $"{station} {(blade == RobotArmEnum.Blade1 ? "A" : "B")}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            RobotSiasunPhoenixBMessage response = msg as RobotSiasunPhoenixBMessage;
            ResponseMessage = msg;
            handled = false;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                handled = true;
                Device.NoteReadDataComplete();
                return true;
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBCheckLoadABHandler : RobotSiasunPhoenixBHandler
    {
        //CHECK LOAD station 
        //_RDY
        public RobotSiasunPhoenixBCheckLoadABHandler(RobotSiasunPhoenixB device, int station, int timeout)
            : base(device, "CHECK LOAD", $"{station}")
        {
            AckTimeout = TimeSpan.FromSeconds(timeout);
            CompleteTimeout = TimeSpan.FromSeconds(timeout);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            RobotSiasunPhoenixBMessage response = msg as RobotSiasunPhoenixBMessage;
            ResponseMessage = msg;
            handled = false;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                handled = true;
                Device.NoteReadDataComplete();
                return true;
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBQueryWaferPresentHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBQueryWaferPresentHandler(RobotSiasunPhoenixB device, RobotArmEnum blade)
            : base(device, "RQ LOAD", blade == RobotArmEnum.Blade1 ? "A" : "B")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            RobotSiasunPhoenixBMessage response = msg as RobotSiasunPhoenixBMessage;
            ResponseMessage = msg;
            handled = false;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }

            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }

            if (_requestResponse != null && _requestResponse.Contains(" B"))
            {
                Device.NoteWafeOnOff("B", _requestResponse.ToUpper().Contains("ON"), _requestResponse.ToUpper().Contains("UNKNOW"));
            }
            else if (_requestResponse != null && _requestResponse.Contains(" A"))
            {
                Device.NoteWafeOnOff("A", _requestResponse.ToUpper().Contains("ON"), _requestResponse.ToUpper().Contains("UNKNOW"));
            }

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                handled = true;
                Device.NoteReadDataComplete();
                return true;
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBRequestWaferCentDataHandler : RobotSiasunPhoenixBHandler
    {
        public RobotSiasunPhoenixBRequestWaferCentDataHandler(RobotSiasunPhoenixB device)
            : base(device, "RQ WAF_CEN DATA")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            RobotSiasunPhoenixBMessage response = msg as RobotSiasunPhoenixBMessage;
            ResponseMessage = msg;
            handled = false;

            if (response.IsError)
            {
                Device.NoteError(response.Data);
            }


            if (response.IsResponse)
            {
                _requestResponse = response.Data;
            }
            //WAF_CEN RT value1x value1y value4x value4y LFT value2x value2y value3x value3y offset_r offset_t
            Device.NoteWafeCenData(_requestResponse);

            if (response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                handled = true;
                Device.NoteReadDataComplete();
                return true;
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBSetWaferCentModeHandler : RobotSiasunPhoenixBHandler
    {
        //SET WAF_CEN MODE SINGLE_SENS|RL_SENS
        public RobotSiasunPhoenixBSetWaferCentModeHandler(RobotSiasunPhoenixB device, bool isSingleSensorMode)
            : base(device, "SET WAF_CEN MODE", isSingleSensorMode ? "SINGLE_SENS" : "RL_SENS")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBSetWaferCentEnableHandler : RobotSiasunPhoenixBHandler
    {
        //SET STN station [[ARM]arm] WAF_CEN ENABLE Y|N
        public RobotSiasunPhoenixBSetWaferCentEnableHandler(RobotSiasunPhoenixB device, int station, RobotArmEnum blade, bool isEnable)
            : base(device, "SET STN", $"{station} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} WAF_CEN ENABLE {(isEnable ? "Y" : "N")}")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBSetWaferCentWaferSizeHandler : RobotSiasunPhoenixBHandler
    {
        //SET STN station [[ARM]arm] WAF_CEN WAF_SIZE value
        //SET STN 1 ARM A WAF_CEN WAF_SIZE 300000
        public RobotSiasunPhoenixBSetWaferCentWaferSizeHandler(RobotSiasunPhoenixB device, int station, RobotArmEnum blade, int waferSize)
            : base(device, "SET STN", $"{station} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} WAF_CEN WAF_SIZE {waferSize}")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    public class RobotSiasunPhoenixBSetWaferCentAutoNotchHandler : RobotSiasunPhoenixBHandler
    {
        //SET STN station [[ARM]arm] WAF_CEN AUTO_NOTCH Y|N
        //SET STN 1 ARM A WAF_CEN WAF_SIZE 300000
        public RobotSiasunPhoenixBSetWaferCentAutoNotchHandler(RobotSiasunPhoenixB device, int station, RobotArmEnum blade, bool isAutoNotch)
            : base(device, "SET STN", $"{station} ARM {(blade == RobotArmEnum.Blade1 ? "A" : "B")} WAF_CEN AUTO_NOTCH {(isAutoNotch ? "Y" : "N")}")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }

    #region Aligner
    public class RobotSiasunPhoenixBTransferAndAlignHandler : RobotSiasunPhoenixBHandler
    {
        //XFER [[ARM] arm] station [SLOT num] station [SLOT num] ALGN
        public RobotSiasunPhoenixBTransferAndAlignHandler(RobotSiasunPhoenixB device, int fromStation, int formSlot, int toStation, int toSlot, Hand blade)
            : base(device, "XFER", $"ARM {(blade == Hand.Blade1 ? "A" : "B")} {fromStation} SLOT {formSlot} {toStation} SLOT {toSlot} ALGN")
        {
        }
    }

    public class RobotSiasunPhoenixBAlignAndPickHandler : RobotSiasunPhoenixBHandler
    {
        //PICK station [SLOT num] [[ARM] arm][ENRT|STRT NR][RO x TO x][ALGN][PALGN]
        //align and then pick it
        public RobotSiasunPhoenixBAlignAndPickHandler(RobotSiasunPhoenixB device, int alignerStation, int alignerSlot, Hand blade, int ro = 0, int to = 0)
            : base(device, "PICK", $" {alignerStation} SLOT {alignerSlot} ARM {(blade == Hand.Blade1 ? "A" : "B")}{(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")} ALGN")
        {
        }
    }

    public class RobotSiasunPhoenixBPlaceAndAlignHandler : RobotSiasunPhoenixBHandler
    {
        //PLACEstation [SLOT num] [[ARM] arm][ENRT|STRT NR][RO x TO x][ALGN]
        //place to aligner and align
        public RobotSiasunPhoenixBPlaceAndAlignHandler(RobotSiasunPhoenixB device, int alignerStation, int alignerSlot, Hand blade, int ro = 0, int to = 0)
            : base(device, "PLACE", $" {alignerStation} SLOT {alignerSlot} ARM {(blade == Hand.Blade1 ? "A" : "B")}{(ro == 0 ? "" : $" RO {ro}")}{(to == 0 ? "" : $" TO {to}")} ALGN")
        {
        }
    }

    //两个aligner怎么区分
    public class RobotSiasunPhoenixBRequestAlignerOffsetHandler : RobotSiasunPhoenixBHandler
    {
        //RQ ALGN 00 OFFSETS
        //RO 0.000 TO 0.000
        //_RDY
        public RobotSiasunPhoenixBRequestAlignerOffsetHandler(RobotSiasunPhoenixB device)
            : base(device, "RQ ALGN 00 OFFSETS")
        {
        }
    }

    //两个aligner怎么区分
    public class RobotSiasunPhoenixBRequestAlignerWaferPresentHandler : RobotSiasunPhoenixBHandler
    {
        //RQ ALIGNER RQWPRS
        //WFR PRS YES/NO
        //_RDY
        public RobotSiasunPhoenixBRequestAlignerWaferPresentHandler(RobotSiasunPhoenixB device)
            : base(device, "RQ ALIGNER RQWPRS")
        {
        }
    }

    //默认选用哪个index对应的angle？
    public class RobotSiasunPhoenixBSetAlignerAlignAngleHandler : RobotSiasunPhoenixBHandler
    {
        //SET ALIGNER 1 POSTPOS %i POS %i
        //_RDY
        //eg. SET ALIGNER 1 POSTPOS 1 POS 27.1
        public RobotSiasunPhoenixBSetAlignerAlignAngleHandler(RobotSiasunPhoenixB device, int alignerID, int index, int angle)
            : base(device, "SET ALIGNER", $"{alignerID} POSTPOS {index} POS {angle}")
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                Device.NoteSetParametersComplete();
            }

            return true;
        }
    }
    #endregion

}
