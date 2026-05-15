using System;
using System.Text;

using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Text.RegularExpressions;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.CommonData;
using Aitex.Core.RT.SCCore;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class Mag7RobotMotionHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }

        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _robot = (Robot)value; } }
        protected Robot _robot;

        public Mag7RobotMotionHandler()
        {
            background = false;
        }

        public virtual string package(params object[] args)
        {
            return "";
        }

        public bool unpackage(string type, string[] items)
        {
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                update(items);  
 
                return true;
            }

            if (type.Equals(ProtocolTag.resp_tag_error))
            {
                processError(items);

                return true;
            }

            return !background;
        }


        protected virtual void update(string[] data)
        {

        }

        protected virtual void processError(string[] data)
        {

        }
    }



    public class RbHomeHandler : Mag7RobotMotionHandler
    {
        public RbHomeHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            updateBefore();
            return "HOME ALL";
        }

        protected void updateBefore()
        {
            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

            _robot.CmdBladeTarget = $"{_robot.Name}.A";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };

        }

        protected override void update(string[] data)
        {
            _robot.Initalized = true;
            _robot.Swap = false;
        }
    }

    public class RbHomeAGLNHandler : Mag7RobotMotionHandler
    {
        public RbHomeAGLNHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            if (!SC.GetValue<bool>($"Aligner.BrooksAligner.IsConnectWithRobot"))
            {
                return "HOME";
            }
            else
            {
                return "ALIGNER HOME";
            }
  
        }

    }

    public class RbAlignerHandler : Mag7RobotMotionHandler
    {
        public RbAlignerHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            if (!SC.GetValue<bool>($"Aligner.BrooksAligner.IsConnectWithRobot"))
            {
                return "ALGN";
            }
            else
            {
                return "ALIGNER ALGN";
            }
           
        }
    }

    public class RRequestAlignerResultHandler : ITransferMsg
    {
        public RRequestAlignerResultHandler()
        {
            background = true;
        }

        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        /// <summary>
        /// ALGN RSLT [ALL]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string package(params object[] args)
        {
            return "ALGN RSLT";
        }

        public bool unpackage(string type, string[] items)
        {
            //DATA a1 a2 a3 a4 a5 a6
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }

            if (items.Length >= 6)
            {
                ((Robot)Robot).NotifyAlignerResultData(items[1], items[2], items[3], items[4]);
            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Aligner result data return error feedback");
                return false;
            }

            return false;
        }
    }

    public class RRequestAlignerOffsetHandler : ITransferMsg
    {
        public RRequestAlignerOffsetHandler()
        {
            background = true;
        }

        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        private Hand _hand;

        /// <summary>
        /// RQ ALGN alignerid OFFSETS [<ARM> (A|B)]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string package(params object[] args)
        {
            _hand = (Hand)args[0];

            return string.Format("RQ ALGN 00 OFFSETS ARM {0}", Mag7RobotConvertor.hand2string(_hand));
        }

        public bool unpackage(string type, string[] items)
        {
            //RO offset_value TO offset_value
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }

            if (items.Length == 4)
            {
                if (int.TryParse(items[1], out int rOffset) && int.TryParse(items[3], out int tOffset))
                    ((Robot)Robot).NotifyAlignerOffsetData(rOffset, tOffset);
            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Aligner offset data return error feedback");
                return false;
            }

            return false;
        }
    }

    public class RbStopHandler : Mag7RobotMotionHandler
    {
        public RbStopHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CSTP,<Sw>(,<Sum>)<CR>
        public override string package(params object[] args)
        {
            return "PAUSE";
        }
    }

    public class PickHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickHandler()
        {
            background = true;
        }
        /// <summary>
        /// PICK station [SLOT slot] <<(ARM)>[(A|B|AB)]> <PAN L|R> <CW|CCW>
        //[[RO r_offset] [TO t_offset] [ZO z_offset]]
        // [ALTWAFSPD(Y | N)]
        // [EX NO_VIA|RE NO_VIA]
        // [STRT(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // [ENRT [(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // In MAG6 Compatibility, entering the word ARM is optional and SLOT starts with 1; in VT5 Compatibility, ARM is required and SLOT starts with 0.


        public override string package(params object[] args)
        {
            // PICK<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format("PICK {0} ARM {1}",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
            }


            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);


            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }
		
		
		
        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 740: Error prior to a PICK: Wafer Sensed.
                    Error 741: Error after a PICK: No Wafer Sensed.
                    Error 742: EX wafer sensor error prior to a PICK: No Wafer Sensed.
                    Error 743: EX wafer sensor error after a PICK: Wafer Sensed.
                    Error 744: R_MT wafer sensor error on PICK: Wafer Sensed during Extend.
                    Error 745: R_MT wafer sensor error on PICK: No Wafer Sensed during Retract.


                Error 772: Broken wafer encountered.
                    Defect or breakage of a substrate is detected. If this error is reported on an unbroken wafer,increase the BREAK_THR parameter.
                    */

                if (error == 741 || error == 742 || error == 745)   // not picked the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }
                else //picked but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }

    public class PickExHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        private int _ro;
        private int _to;

        public PickExHandler()
        {
            background = true;
        }
        /// <summary>
        /// PICK station [SLOT slot] <<(ARM)>[(A|B|AB)]> <PAN L|R> <CW|CCW>
        //[[RO r_offset] [TO t_offset] [ZO z_offset]]
        // [ALTWAFSPD(Y | N)]
        // [EX NO_VIA|RE NO_VIA]
        // [STRT(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // [ENRT [(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // In MAG6 Compatibility, entering the word ARM is optional and SLOT starts with 1; in VT5 Compatibility, ARM is required and SLOT starts with 0.


        public override string package(params object[] args)
        {
            // PICK<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            _ro = (int)args[3];
            _to = (int)args[4];

            updateBefore();

            return string.Format("PICK {0} ARM {1} RO {2} TO {3}",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand),
                _ro, 
                _to);
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
            }


            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);


            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }


        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 740: Error prior to a PICK: Wafer Sensed.
                    Error 741: Error after a PICK: No Wafer Sensed.
                    Error 742: EX wafer sensor error prior to a PICK: No Wafer Sensed.
                    Error 743: EX wafer sensor error after a PICK: Wafer Sensed.
                    Error 744: R_MT wafer sensor error on PICK: Wafer Sensed during Extend.
                    Error 745: R_MT wafer sensor error on PICK: No Wafer Sensed during Retract.


                Error 772: Broken wafer encountered.
                    Defect or breakage of a substrate is detected. If this error is reported on an unbroken wafer,increase the BREAK_THR parameter.
                    */

                if (error == 741 || error == 742 || error == 745)   // not picked the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }
                else //picked but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }

    public class PickExtendHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickExtendHandler()
        {
            background = true;
        }
        /// <summary>
        /// PICK station [SLOT slot] <<(ARM)>[(A|B|AB)]> <PAN L|R> <CW|CCW>
        //[[RO r_offset] [TO t_offset] [ZO z_offset]]
        // [ALTWAFSPD(Y | N)]
        // [EX NO_VIA|RE NO_VIA]
        // [STRT(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // [ENRT [(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // In MAG6 Compatibility, entering the word ARM is optional and SLOT starts with 1; in VT5 Compatibility, ARM is required and SLOT starts with 0.


        public override string package(params object[] args)
        {
            // PICK<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format("PICK {0} ARM {1} ENRT NR",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";

            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }
		
		
        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 740: Error prior to a PICK: Wafer Sensed.
                    Error 741: Error after a PICK: No Wafer Sensed.
                    Error 742: EX wafer sensor error prior to a PICK: No Wafer Sensed.
                    Error 743: EX wafer sensor error after a PICK: Wafer Sensed.
                    Error 744: R_MT wafer sensor error on PICK: Wafer Sensed during Extend.
                    Error 745: R_MT wafer sensor error on PICK: No Wafer Sensed during Retract.
                    */

                if (error == 741 || error == 742 || error == 745)   // not picked the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }
                else //picked but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }

    public class PickRetractHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickRetractHandler()
        {
            background = true;
        }
        /// <summary>
        /// PICK station [SLOT slot] <<(ARM)>[(A|B|AB)]> <PAN L|R> <CW|CCW>
        //[[RO r_offset] [TO t_offset] [ZO z_offset]]
        // [ALTWAFSPD(Y | N)]
        // [EX NO_VIA|RE NO_VIA]
        // [STRT(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // [ENRT [(NR | R1 | R2 | VIA)]
        // [(GRIP(OFF | ON)]
        // In MAG6 Compatibility, entering the word ARM is optional and SLOT starts with 1; in VT5 Compatibility, ARM is required and SLOT starts with 0.


        public override string package(params object[] args)
        {
            // PICK<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format("PICK {0} ARM {1} STRT NR",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }

        protected override void update(string[] data)
        {
            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);
        }
		
		
        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 740: Error prior to a PICK: Wafer Sensed.
                    Error 741: Error after a PICK: No Wafer Sensed.
                    Error 742: EX wafer sensor error prior to a PICK: No Wafer Sensed.
                    Error 743: EX wafer sensor error after a PICK: Wafer Sensed.
                    Error 744: R_MT wafer sensor error on PICK: Wafer Sensed during Extend.
                    Error 745: R_MT wafer sensor error on PICK: No Wafer Sensed during Retract.
                    */

                if (error == 741 || error == 742 || error == 745)   // not picked the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }
                else //picked but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }

    public class PickAlignerHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickAlignerHandler()
        {
            background = true;
        }
        /// <summary>
        // PICK station [<ARM> (A|B|AB)] 
        // [ALGN<palgnId> <SLPS postPosNum>]
        // [PALGN<palgnId>]
        // [ADJ_STN]

        public override string package(params object[] args)
        {
            // PICK Aligner<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format("PICK {0} ARM {1} PALGN", 
                Mag7RobotConvertor.MapModule(_chamber),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Picking,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
            }


            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);


            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }



        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 740: Error prior to a PICK: Wafer Sensed.
                    Error 741: Error after a PICK: No Wafer Sensed.
                    Error 742: EX wafer sensor error prior to a PICK: No Wafer Sensed.
                    Error 743: EX wafer sensor error after a PICK: Wafer Sensed.
                    Error 744: R_MT wafer sensor error on PICK: Wafer Sensed during Extend.
                    Error 745: R_MT wafer sensor error on PICK: No Wafer Sensed during Retract.


                Error 772: Broken wafer encountered.
                    Defect or breakage of a substrate is detected. If this error is reported on an unbroken wafer,increase the BREAK_THR parameter.
                    */

                if (error == 741 || error == 742 || error == 745)   // not picked the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }
                else //picked but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                        WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)_hand);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }

    public class PlaceHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;

        public PlaceHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();
            return string.Format("PLACE {0} ARM {1}",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }


        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
            }
            else
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
            }
            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);


            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }
		
		
        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 730: Error prior to PLACE: No Wafer Sensed.
                    Error 731: Error after a PLACE: Wafer Sensed.
                    Error 732: EX wafer sensor error prior to a PLACE: Wafer Sensed.
                                     WAVE II 1.2P: Error after PLACE: Arm Extended and Wafer Sensed
                    Error 733: EX wafer sensor error prior to a PLACE: No Wafer Sensed.
                    Error 734: R_MT wafer sensor error on PLACE: No wafer sensed during Extend.
                    Error 735: R_MT wafer sensor failure.
                    Error 736: R_MT wafer sensor error on PLACE: Wafer sensed during Retract.
                    */

                if (error == 731 || error == 732 ||  error == 736)   // not placed the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }
                else //placed but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }
    public class PlaceExtendHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;

        public PlaceExtendHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();
            return string.Format("PLACE {0} ARM {1} ENRT NR",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";


            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Placing,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }


        protected override void update(string[] data)
        {
            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);
        }
		
		        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 730: Error prior to PLACE: No Wafer Sensed.
                    Error 731: Error after a PLACE: Wafer Sensed.
                    Error 732: EX wafer sensor error prior to a PLACE: Wafer Sensed.
                                     WAVE II 1.2P: Error after PLACE: Arm Extended and Wafer Sensed
                    Error 733: EX wafer sensor error prior to a PLACE: No Wafer Sensed.
                    Error 734: R_MT wafer sensor error on PLACE: No wafer sensed during Extend.
                    Error 735: R_MT wafer sensor failure.
                    Error 736: R_MT wafer sensor error on PLACE: Wafer sensed during Retract.
                    */

                if (error == 731 || error == 732 || error == 736)   // not placed the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }
                else //placed but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }
    public class PlaceRetractHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;

        public PlaceRetractHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();
            return string.Format("PLACE {0} ARM {1} STRT NR",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }


        protected override void update(string[] data)
        {
            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);
        }
		
		
		
        protected override void processError(string[] data)
        {
            try
            {
                int error = int.Parse(data[1]);
                string arm = _hand == Hand.Blade1 ? "A" : "B";
                _robot.CmdBladeTarget = $"{_chamber}.{arm}";
                _robot.CmdBlade1Extend = "0";
                _robot.CmdBlade2Extend = "0";

                _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
                _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);

                /*  Error 730: Error prior to PLACE: No Wafer Sensed.
                    Error 731: Error after a PLACE: Wafer Sensed.
                    Error 732: EX wafer sensor error prior to a PLACE: Wafer Sensed.
                                     WAVE II 1.2P: Error after PLACE: Arm Extended and Wafer Sensed
                    Error 733: EX wafer sensor error prior to a PLACE: No Wafer Sensed.
                    Error 734: R_MT wafer sensor error on PLACE: No wafer sensed during Extend.
                    Error 735: R_MT wafer sensor failure.
                    Error 736: R_MT wafer sensor error on PLACE: Wafer sensed during Retract.
                    */

                if (error == 731 || error == 732 || error == 736)   // not placed the wafer
                {

                }
                else if (error == 772)   // not picked the wafer
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferDuplicated(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }
                else //placed but error
                {
                    if (_hand == Hand.Both)
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot + 1);
                    }
                    else
                    {
                        WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)_hand, _chamber, _slot);
                    }
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
    }

    public class ExchangHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;


        public ExchangHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // Picks from designated arm and places using other arm.<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            if (_hand == Hand.Blade1)
                _hand = Hand.Blade2;
            else
                _hand = Hand.Blade1;

            updateBefore();


            return string.Format("SWAP {0} {1}",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;
            _robot.Swap = true;
            _robot.PlaceBalde = _hand;
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade2)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_robot.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_robot.Name), (int)Hand.Blade2, _chamber, _slot);
            }

            _robot.Swap = false;
            _robot.Blade1Target = ModuleHelper.Converter(_robot.Name);
            _robot.Blade2Target = ModuleHelper.Converter(_robot.Name);
        }
    }

    public class GotoHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public GotoHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // Moves to a specified station-referenced location.< CR >
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();
            return string.Format("GOTO N {0} ARM {1} R RE Z DN",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";

            string target = (_hand == Hand.Blade2 ? "ArmB" : "ArmA") + "." + _chamber;

            _robot.MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = _hand == Hand.Blade1 ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = target,
            };
        }
    }


    public class ExtendHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public ExtendHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // Moves to a specified station-referenced location.< CR >
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();
            return string.Format("GOTO N {0} ARM {1} R EX Z DN",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _robot.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class RetractHandler : Mag7RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public RetractHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // Moves to a specified station-referenced location.< CR >
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();
            return string.Format("GOTO N {0} ARM {1} R RE Z DN",
                Mag7RobotConvertor.MapModuleSlot(_chamber, _slot),
                Mag7RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _robot.Blade1Target = _chamber;
            _robot.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _robot.CmdBladeTarget = $"{_chamber}.{arm}";
            _robot.CmdBlade1Extend = "0";
            _robot.CmdBlade2Extend = "0";
        }
    }

    public class RbSetSpeedHandler : Mag7RobotMotionHandler
    {
        private int _speed = 0;
        public RbSetSpeedHandler()
        {
            background = false;
        }

        public override string package(params object[] args)
        {
            _speed = (int)args[0];

            string speed = "LOSPD";
            switch (_speed)
            {
                case 1:    //low
                    speed = "LOSPD";
                    break;
                case 2:    //Medium
                    speed = "MESPD";
                    break;
                case 3:    //high
                    speed = "HISPD";
                    break;
            }
            return string.Format(" SET LOSPD Y", speed);
        }
    }


    public class RbSetCommunicationHandler : Mag7RobotMotionHandler
    {
        public RbSetCommunicationHandler()
        {
            background = true;
        }
        //SET COMM [M/B (MON|PKT)] [FLOW (SEQ|BKG|BKG+|MULTI|MULTI_DEV)]
        //[LF(ON | OFF)]
        //[ECHO(ON | OFF)]
        //[CHECKSUM(ON | OFF)]
        //[ERRLVL err_level]
        //[DREP(AUT | REQ)]
        //[ERR_REP(AUT | REQ)]
        //[RR(ON | OFF)]
        public override string package(params object[] args)
        {
            return string.Format("SET COMM {0} {1} {2} {3} {4}",
                "M/B PKT",  // packet mode
                "FLOW SEQ", // sequential mode
                "ECHO OFF", // echo off
                "LF OFF",   //LF off
                "CHECKSUM OFF" // echo off

            );
        }
    }

    public class RbSetLoadHandler : Mag7RobotMotionHandler
    {
        public RbSetLoadHandler()
        {
            background = true;
        }
        //SET LOAD <<(ARM)>[(A | B)]> <PAN L|R> (?|ON|OFF)
        //RQ LOAD <<(ARM)>[(A | B)]> <PAN L|R>
        public override string package(params object[] args)
        {
            Hand hand = (Hand)args[0];

            bool isOn = (bool)args[1];
            return string.Format("SET LOAD {0} {1}", hand == Hand.Blade1 ? "A" : "B",isOn? "ON":"OFF");
        }
    }

    public class RbCheckLoadHandler : Mag7RobotMotionHandler
    {
        public RbCheckLoadHandler()
        {
            background = true;
        }
        //CHECK LOAD[station] [(A | B)] <PAN L|R> [[INTLCK ALL| (DIS|ENB)]| [EX_ENABLE
        //    (DIS | ENB)] |[SBIT_SLVL_SEN(DIS | ENB)] |[VLV_SEN(DIS | ENB)]
        public override string package(params object[] args)
        {
            ModuleName module = (ModuleName)args[0];
            int slot = (int)args[1];

            return string.Format("CHECK LOAD {0} INTLCK ALL DIS", Mag7RobotConvertor.MapModule(module));
        }
    }


    public class RbRequestWaferPresentHandler : ITransferMsg
    {
        public RbRequestWaferPresentHandler()
        {
            background = true;
        }
        //RQ WAFER PRESENT <<(ARM)>[(A|B|AB)]>
        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        public string package(params object[] args)
        {
            return string.Format("RQ WAFER PRESENT AB");
        }

        public bool unpackage(string type, string[] items)
        {
            //WAFER PRESENT Y N
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }


            if (items.Length == 4)
            {
                if (items[2] == "Y")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Normal);
                if (items[2] == "?")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Unknown);
                if (items[2] == "N")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Empty);

                if (items[3] == "Y")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Normal);
                if (items[3] == "?")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Unknown);
                if (items[3] == "N")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Empty);
            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Wafer Present return unexpected feedback");
                return false;
            }

            return false;
        }
    }

    public class RbRequestWaferPresentLoadAHandler : ITransferMsg
    {
        public RbRequestWaferPresentLoadAHandler()
        {
            background = true;
        }
        //RQ LOAD A
        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        public string package(params object[] args)
        {
            return string.Format("RQ LOAD A");
        }

        public bool unpackage(string type, string[] items)
        {
            //LOAD A ON
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }


            if (items.Length == 4)
            {
                if (items[2] == "ON")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Normal);
                if (items[2] == "?")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Unknown);
                if (items[2] == "OFF")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade1, WaferStatus.Empty);

            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Wafer Present RQ LOAD A return unexpected feedback {items.Length}");
                return false;
            }

            return false;
        }
    }

    public class RbRequestWaferPresentLoadBHandler : ITransferMsg
    {
        public RbRequestWaferPresentLoadBHandler()
        {
            background = true;
        }
        //RQ LOAD B
        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        public string package(params object[] args)
        {
            return string.Format("RQ LOAD B");
        }

        public bool unpackage(string type, string[] items)
        {
            //LOAD B ON
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }


            if (items.Length == 4)
            {
                if (items[2] == "ON")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Normal);
                if (items[2] == "?")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Unknown);
                if (items[2] == "OFF")
                    ((Robot)Robot).NotifyWaferPresent(Hand.Blade2, WaferStatus.Empty);

            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Wafer Present RQ LOAD B return unexpected feedback {items.Length} ");
                return false;
            }

            return false;
        }
    }

    public class RRequestAWCDataHandler : ITransferMsg
    {
        public RRequestAWCDataHandler()
        {
            background = true;
        }
        //RQ WAF_CEN DATA
        public IDevice Robot { get; set; }
        public bool background { get; }
        public bool evt { get; }

        public string package(params object[] args)
        {
            return string.Format("RQ WAF_CEN DATA");
        }

        public bool unpackage(string type, string[] items)
        {
            //WAF_CEN DATA SENS 1 r_pos_Striping_edge t_pos_Striping_edge r_pos_trailing_edge t_pos_trailing_edge OFFSET r_offset t_offset
            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                return true;
            }

            if (items.Length >= 15)
            {
                if (int.TryParse(items[13], out int rOffset) && int.TryParse(items[14], out int tOffset))
                    ((Robot)Robot).NotifyAWCData(rOffset, tOffset);
            }
            else
            {
                EV.PostWarningLog(Robot.Module, $"Request Wafer center find data return error feedback");
                return false;
            }

            return false;
        }
    }


    public class RBQueryStateHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;

        public RBQueryStateHandler()
        {
            background = false;
        }

        //$,<UNo>(,<SeqNo>),RSTS(,<Sum>)<CR> 

        public string package(params object[] args)
        {
            return ",RSTS,";
        }

        public bool unpackage(string type, string[] items)
        {

            return !background;
        }
    }

    public class RbEventHandler : ITransferMsg
    {
        public bool background { get { return false; } }
        public bool evt { get { return true; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;

        public RbEventHandler()
        {
        }

        //$,<UNo>(,<SeqNo>),RSTS(,<Sum>)<CR> 

        public string package(params object[] args)
        {
            return "";
        }

        public bool unpackage(string type, string[] items)
        {
            return true;
        }
    }

    public class RBQueryPositionHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;
        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;

        public RBQueryPositionHandler()
        {
            background = false;

        }

        //RQ POS ABS ALL
        //POS ABS(A|B) rVal tVal zVal sVal wVal waVal wbVal

        public string package(params object[] args)
        {
            return "RQ POS ABS ALL";
        }

        public bool unpackage(string type, string[] items)
        {
            //POS ABS (A|B) rVal tVal zVal sVal wVal waVal wbVal
            //Radial: rVal
            //Theta:tVal
            //Z:zVal
            //S:sVal
            //W:wVal
            //WA:waVal
            //WB:wbVal
            if (items.Length == 9)
            {
                _device.Rotation = int.Parse(items[1]);

                return true;
            }

            return !background;
        }
    }
}
