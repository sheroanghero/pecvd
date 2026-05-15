using System;
using System.Text;
using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Log;
using System.Collections.Generic;
using Aitex.Core.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100
{
    /* 
     * A command is transmitted from the host in the following format.
     * $ UNo (SeqNo) COMMAND Parameter Sum CR\
     * • $: Start mark (1 byte)
        Indicates the start of the message.
        • UNo: Unit No. (1 byte)
        Indicates the unit number
        • SeqNo: Sequence number (Non/1/2/3 bytes)
        Sequence number is used to avoid duplicate motion when sending of
        commands improperly. An integer parameter can specify the length of
        SeqNo. (Non/1/2/3 bytes). See parameter table for more details. (Default:
        Non), 设置 2bytes
        • Parameter: Parameter (Differs depending on the command.)
        Sets the operation axis, the moving amount, etc. following a command.
        • Sum: Checksum (2 bytes)
        This information is used for the communications error check.
        Calculate the sum of the ASCII characters (‘0’ to ‘9’ and ‘A’ to ‘F’), in “Uno”,
        “COMMAND” and “Parameter” section, and take the lowest tow digit.
    */
    public class handler<T> : IHandler where T : ITransferMsg, new()
    {
        public int ID { get; set; }
        public int Unit { get; set; }

        public bool IsBackground { get { return _imp.background; } }


        private static int retry_time = 3;
        private int retry_count = retry_time;

        private object[] _objs = null;



        private TokenGenerator _generator;

        private T _imp = new T();

        private List<string> _words = new List<string>();

        public handler(IDevice device)
        {
            _imp.Robot = device;
        }

        public handler(IDevice device, ref TokenGenerator gen, params object[] objs)
        {
            _imp.Robot = device;
            this._generator = gen;
            this._objs = objs;
        }


        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            retry_count = retry_time;
            ID = _generator.create();
            return port.Write(string.Format("{0}{1}{2}", ProtocolTag.tag_cmd_start, package().Replace(",", ""), ProtocolTag.tag_end));
        }
        /// <summary>
        /// return value: bhandle
        /// </summary>
        /// <typeparam name="TPort"></typeparam>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        /// 

        public bool OnMessage<TPort>(ref TPort port, string message, out bool completed) where TPort : ICommunication
        {
            //$1E000003AC0300010
            //message = ">,1,EVNT,100,2018/11/16 17:01:06,2BA0,A1";
            //message = " !,1,36,40,2BA0,MTRS,002200,-0011931,-0610171,00179995,00049818,00186336,57";
            //message = "!,1,16,40,0000,MTCH,000483,-0011862,00000000,00179516,00090369,00172882,0A";
            //message = "$156000000RMAPC2FF01OK02OK03OK04OK05OK06OK07OK08OK09--10--11--12--13--14--15--16--17--18--19--20--21--22--23--24--25--96";
            //message =!283000000MALNE5. 
            try
            {
                completed = false;

                string package = message;
                _words.Clear();


                string type = package.Substring(0,1);
                _words.Add(type);

                int unit = int.Parse(package.Substring(1, 1));
                _words.Add(package.Substring(1, 1));  

                string sum = package.Substring(package.Length - 2, 2);
                string check = Checksum(package);
                if (sum != check)
                {
                    throw (new InvalidPackageException(string.Format("check sum error{0}", package)));
                }

                if (type != ProtocolTag.resp_tag_event && type != ProtocolTag.resp_tag_error)
                {
                    _words.Add(package.Substring(2, 2));
                    int seq = int.Parse(package.Substring(2,2));

                    if (seq != ID)
                        return false;

                    if (unit != Unit)
                    {
                        throw (new InvalidPackageException(string.Format("invalid unit {0}", package)));
                    }
                }

                if (type == ProtocolTag.resp_tag_error)
                {
                    //? Ackcd Sum CR
                    string error = package.Substring(1,4);
                    //‘1’: Warning 1 (W1), ‘2’: Warning 2 (W2), ‘3’: Important alarm 1 (A1), ‘4’: Important alarm 2 (A2),
                    //‘5’: Serious alarm (F)

                    if (error[0] != '1' || error[0] != '2')
                    {
                        string warning = string.Format("can't execute retry, {0}", error);
                        LOG.Warning(warning);
                        throw (new ExcuteFailedException(warning));
                    }
                    if (retry_count-- <= 0)
                    {
                        string warning = string.Format("retry over {0} times", retry_time);
                        LOG.Warning(warning);
                        throw (new ExcuteFailedException(warning));
                    }

                    port.Write(string.Format("{0}{1}{2}", ProtocolTag.tag_cmd_start, this.package(), ProtocolTag.tag_end));
                    return true;
                }
                else if (type == ProtocolTag.resp_tag_event)
                {
                    //0 1   2-3    4-7    8-10 
                    //> UNo SeqNo COMMAND EvNo EvData Sum CR
                    //• COMMAND : Command (4 Byte) EVNT(Fixed length)
                    //• EvNo: Event number(

                    _words.Add(package.Substring(4, 4));   //stsN

                    string evtType = package.Substring(8, 10);
                    _words.Add(evtType);   //Errcd

                    string evtInfo = package.Substring(11, package.Length - 10 -2 );
                    _words.Add(evtInfo);   //Errcd

                    if (_imp.evt)
                        completed = _imp.unpackage(type, _words.ToArray());
                    return _imp.evt;
                }
                else
                {
                    //0 1   2,3     4,5  6-9
                    //! UNo (SeqNo) StsN Errcd COMMAND Sum 

                    _words.Add(package.Substring(4, 2));   //stsN
                    string error = package.Substring(6, 4);
                    //int intError = Convert.ToInt32(error, 16);
                    if (error !=  "0000")
                    {
                        string warning = string.Format("Robot execute failed, return error {0}", error);
                        throw (new ExcuteFailedException(warning));
                    }

                    _words.Add(error);   //Errcd


                    _words.Add(package.Substring(10, package.Length - 10 -2)) ;   //COMMAND

                    completed = _imp.unpackage(type, _words.ToArray());
                    if (completed)
                    {
                        _generator.release(ID);
                        if (_imp.background)
                        {
                            ID = this._generator.create();
                            port.Write(string.Format("{0}{1}{2}", ProtocolTag.tag_cmd_start, ackn(), ProtocolTag.tag_end));
                            _generator.release(ID);
                        }

                        //wait 2ms
                        return true;
                    }

                    return true;
                }

            }
            catch (ExcuteFailedException e)
            {
                throw (e);
            }
            catch (InvalidPackageException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                throw (new InvalidPackageException(message));
            }
        }

        private string Checksum(string package)
        {
            int start = package.IndexOf(ProtocolTag.cmd_token);
            int end = package.LastIndexOf(ProtocolTag.cmd_token);

            start = 1;
            end = package.Length - 2 - 1;


            int len = end - start + 1;
            if (len > 1)
            {
                string data = package.Substring(start, len);
                return Checksum(Encoding.Default.GetBytes(data));
            }

            return "";
        }

        private string Checksum(byte[] bytes)
        {
            int sum = 0;
            foreach (byte code in bytes)
            {
                sum += code;
            }
            string hex = String.Format("{0:X2}", sum % 256);
            return hex;
        }


        private string package()
        {
            //$,<UNo>(,<SeqNo>),<Command>,<Parameter>(,<Sum>)<CR>
            string data = string.Empty;
            data = string.Format("{0:D1}{1:D2}{2}", Unit, ID, _imp.package(this._objs).Replace(",",""));

            string sum = Checksum(Encoding.ASCII.GetBytes(data));

            return data + sum;
        }

        private string ackn()
        {
            //$<UNo>(<SeqNo>)ACKN(<Sum>)<CR>
            string data = string.Empty;
            data = string.Format("{0:D1}{1:D2}ACKN", Unit, ID);

            string sum = Checksum(Encoding.ASCII.GetBytes(data));

            return data + sum;
        }
    }

    public class RobotMotionHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }

        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;

        public RobotMotionHandler()
        {
            background = false;
        }

        public virtual string package(params object[] args)
        {
            return "";
        }

        public bool unpackage(string type, string[] items)
        {
            //! <UNo> (<SeqNo>) <StsN> <Errcd> MTRG <Sum> <CR>
            int value = Convert.ToInt32(items[3],16);
 
            _device.Status = value;
     
            int error = Convert.ToInt32(items[4], 16);
            _device.ErrorCode = error;
           if(error > 0)
                _device.LastErrorCode = error;
            

            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                //_device.ElapseTime = int.Parse(items[6]);

                //_device.Rotation = int.Parse(items[7]);
                //_device.Extension = int.Parse(items[8]);
                //_device.Wrist1= int.Parse(items[9]);
                //_device.Wrist2= int.Parse(items[10]);
                //_device.Evevation = int.Parse(items[11]);

                if (error == 0)
                {
                    update(items);    
                }

                return true;
            }

            return !background;
        }


        protected virtual void update(string[] data)
        { 
        
        }
    }

    public class RbInitHandler : RobotMotionHandler
    {
        public RbInitHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) INIT<IMode> <Sum> <CR>
            // IMode: Initialization mode(2 Byte)
            //• ”00”: Error clear, servo ON and all axes move to home position
            //• ”01”: Servo ON and all axes move to home position
            //• ”05”: Error clear, servo ON and arm moves to home position.
            //• ”06”: Servo ON and arm moves to home position
            //• ”10”: Error clear and servo ON
            updateBefore();
            return ",INIT,00,";
        }

        protected void updateBefore()
        {
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }

        protected override void update(string[] data)
        {
            _device.Initalized = true;
            _device.Swap = false;
        }
    }

    public class RbHomeHandler : RobotMotionHandler
    {
        public RbHomeHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
                //$ < UNo > > (< SeqNo >) MHOM<MMode> < Sum > < CR >
                //• UNo: Unit number(1 byte)
                //• ‘1’ to ‘4’: Unit specified
                //• SeqNo: Sequence number(Non/ 1 / 2 / 3 byte)
                //• MMode: Motion mode(1 byte)
                //• ‘F’: All axes
                //• ‘A’: Arm(s) only
              updateBefore();
             return ",MHOM,F,";
        }

        protected void updateBefore()
        {
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);

            _device.CmdBladeTarget = $"{_device.Name}.A";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }

        protected override void update(string[] data)
        {
            _device.Initalized = true;
            _device.Swap = false;
        }
    }
    
    public class RbArmHomeHandler : RobotMotionHandler
    {
        public RbArmHomeHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ < UNo > > (< SeqNo >) MHOM<MMode> < Sum > < CR >
            //• UNo: Unit number(1 byte)
            //• ‘1’ to ‘4’: Unit specified
            //• SeqNo: Sequence number(Non/ 1 / 2 / 3 byte)
            //• MMode: Motion mode(1 byte)
            //• ‘F’: All axes
            //• ‘A’: Arm(s) only
            updateBefore();
            return ",MHOM,A,";
        }

        protected void updateBefore()
        {
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);

            _device.CmdBladeTarget = $"{_device.Name}.A";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }

        protected override void update(string[] data)
        {
            _device.Initalized = true;
            _device.Swap = false;
        }
    }

    public class RbClearErrorHandler : RobotMotionHandler
    {
       
        public RbClearErrorHandler()
        {
            background = true;
        }
        //$ <UNo> (<SeqNo>) CCLR <CMode> <Sum> <CR>
        public override string package(params object[] args)
        {
            //• CMode: Clear mode(1 byte)
            //• ‘E’: Releases the error status.
            //• ‘H’: Clears the error history.
            return ",CCLR,E,";
        }
    }
    public class RbGripHandler : RobotMotionHandler
    {
        Hand _hand;
        public RbGripHandler()
        {
            background = true;
        }
        //Command wafer hold/release signal to the solenoid of the specified unit.
        //$ <UNo> (<SeqNo>) CSOL <Fork> <Sw> <Sum> <CR> 
        //• Fork: Fork specified(1 byte)
        //• ‘A’: Extension axis 1 (Blade 1), pre-aligner
        //• ‘B’: Extension axis 2 (Blade 2)
        //• Sw: Chucking command(1 byte)
        //• ‘0’: Chucking OFF
        //• ‘1’: Chucking ON
        public override string package(params object[] args)
        { 
            _hand = (Hand)args[0];
            bool bHold = (bool)args[1];
            if(bHold)
                return string.Format(",CSOL,{0},1,",RobotConvertor.hand2string(_hand));

            return string.Format(",CSOL,{0},0,", RobotConvertor.hand2string(_hand));
        }
    }

    public class RbStopHandler : RobotMotionHandler
    {
        public RbStopHandler()
        {
            background = true;
        }
        //$ <UNo> (<SeqNo>) CHLTs <Sum> <CR>
        //Pause (decelerate and stop) the motion of the specified unit during the drive command execution.

        public override string package(params object[] args)
        {
            bool isEmergency = (bool)args[0];
            if (isEmergency)
                return ",CEMG,";

            //$ < UNo > (< SeqNo >) CHLT<Sum> < CR >
            return ",CHLT,";
            //return ",MHOM,F,";
        }
    }

    public class RbResumeHandler : RobotMotionHandler
    {
        public RbResumeHandler()
        {
            background = true;
        }
        //$ <UNo> (<SeqNo>) CRSM <Sum> <CR> 
 
        public override string package(params object[] args)
        {
            return ",CRSM,";
        }
    }

    public class PickHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //Move To Ready position and wafer Get motion(MTRS+MGET)
            //$ <UNo> (<SeqNo>) MTRG <TrsSt> <SlotNo> <MtnMode> <Posture> <Sum> <CR>
            //            • TrsSt: Transfer station(2 bytes)
            //• "C1" to "C8": When cassette stage specified
            //• "S1" to "SC": When transfer stage specified
            //• "P1" to “P2”: When P/ A stage specified
            //Note: P2 station is effective only when two or more PA stations exist.
            //• SlotNo: Slot number(2 bytes)
            //< Manipulator >
            //• "01" to "XX": When cassette stage is specified by < TrsST >
            //(The maximum value of "XX" is in the ASCII code of the number of slots specified by parameter.)
            //• "00": When transfer stage or P / A stage specified by < TrsSt >
            //• MtnMode: Motion mode(2 bytes)
            //• "GA": Wafer Get motion at extension axis 1(Blade 1).
            //• "GB": Wafer Get motion at extension axis 2(Blade 2).
            //• "GW": Wafer Get motion with extension axis 1 and 2(Blade 1 and 2). < WGet motion >

            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            if (_hand == Hand.Both)
            {
                return string.Format(",MTRG,{0},{1:D2},G{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1) ,
                RobotConvertor.hand2string(_hand));
            }
            return string.Format(",MTRG,{0},{1:D2},G{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade1)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
            }
            else if (_hand == Hand.Blade2)
            {
                for (int i = 0; i < _device.Blade2Slots; i++)
                    WaferManager.Instance.WaferMoved(_chamber, _slot + i, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2 + i);
            }
            else
            {
                for (int i = 0; i < _device.Blade2Slots + 1; i++)
                    WaferManager.Instance.WaferMoved(_chamber, _slot + i, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1 + i);
            }


            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class PickExtendHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickExtendHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MTPT <TrsSt> <SlotNo> <NextMtn> <Posture> <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTPT,{0},{1:D2},G{2},A,Gb,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class PickRetractHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickRetractHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MPNT <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return ",MPNT,G4,";
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)_hand);
            }

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class PickReadyPositionHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickReadyPositionHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MTRS <TrsSt> <SlotNo> <NextMtn> <Posture> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTRS,{0},{1:D2},G{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }
    
    public class PlaceHandler : RobotMotionHandler
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
            // $ <UNo> (<SeqNo>) MTRP <TrsSt> <SlotNo> <MtnMode> <Posture> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();

            if (_hand == Hand.Both)
            {
                return string.Format(",MTRP,{0},{1:D2},P{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand));
            }


            return string.Format(",MTRP,{0},{1:D2},P{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }


        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade1)
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else if (_hand == Hand.Blade2)
            {
                for (int i = 0; i < _device.Blade2Slots; i++)
                    WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2 +i, _chamber, _slot + i);
            }
            else
            {
                for (int i = 0; i < _device.Blade2Slots + 1; i++)
                    WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1 + i, _chamber, _slot + i);
            }

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class PlaceExtendHandler : RobotMotionHandler
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
            //$ <UNo> (<SeqNo>) MTPT <TrsSt> <SlotNo> <NextMtn> <Posture> <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTPT,{0},{1:D2},P{2},A,Pb,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand));

        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class PlaceRetractHandler : RobotMotionHandler
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
            //$ <UNo> (<SeqNo>) MPNT <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();

            return ",MPNT,P4,"; 
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }


        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2, _chamber, _slot + 1);
            }
            else
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)_hand, _chamber, _slot);
            }
            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class PlaceReadyPositionHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PlaceReadyPositionHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MTRS <TrsSt> <SlotNo> <NextMtn> <Posture> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTRS,{0},{1:D2},P{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class ExchangeReadyPositionHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _pickHand;
        private Hand _placeHand;
        public ExchangeReadyPositionHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            ////$ <UNo> (<SeqNo>) MTRS <TrsSt> <SlotNo> <NextMtn> <Posture> <Sum> <CR>
            //_chamber = (ModuleName)args[0];
            //_slot = (int)args[1];
            //_pickHand = (Hand)args[2];
            //_placeHand = (Hand)args[3];

            //updateBefore();

            //return string.Format(",MTRS,{0},{1:D2},E{2},A,",
            //    RobotConvertor.chamber2staion(_chamber),
            //    RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
            //    RobotConvertor.hand2string(_pickHand));

            //$ <UNo> (<SeqNo>) MTPT <TrsSt> <SlotNo> <NextMtn> <Posture> <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _pickHand = (Hand)args[2];
            _placeHand = (Hand)args[3];

            updateBefore();

            return string.Format(",MTPT,{0},{1:D2},E{2},A,G1,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_pickHand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _pickHand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }
    }

    public class ExchangePickExtendHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;

        public ExchangePickExtendHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MPNT <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();

            return ",MPNT,Gb,";
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class ExchangePlaceExtendHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _pickHand;
        private Hand _placeHand;

        public ExchangePlaceExtendHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MPNT <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _pickHand = (Hand)args[2];
            _placeHand = (Hand)args[3];
            updateBefore();

            return ",MPNT,Pb,";
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _placeHand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _placeHand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _placeHand == Hand.Blade1 ? "0" : "1";
        }
    }

    public class ExchangePlaceRetractHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _pickHand;
        private Hand _placeHand;

        public ExchangePlaceRetractHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MPNT <TrsPnt> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _pickHand = (Hand)args[2];
            _placeHand = (Hand)args[3];
            updateBefore();

            return ",MPNT,P4,";
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _placeHand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }

        protected override void update(string[] data)
        {
            if (_placeHand == Hand.Blade1)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2, _chamber, _slot);
            }

            _device.Swap = false;
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class ExchangeHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;


        public ExchangeHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $ <UNo> (<SeqNo>) MTRE <TrsSt> <SlotNo> <MtnMode> <Posture> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            if (_hand == Hand.Blade1)
                _hand = Hand.Blade2;
            else
                _hand = Hand.Blade1;

            updateBefore();

            return string.Format(",MTRE,{0},{1:D2},E{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;
            _device.Swap = true;
            _device.PlaceBalde = _hand;
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade2)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2, _chamber, _slot);
            }

            _device.Swap = false;
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class ExchangeReadyExtHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _pickHand;
        private Hand _placeHand;
        public ExchangeReadyExtHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            ////$ <UNo> (<SeqNo>) MTRS <TrsSt> <SlotNo> <NextMtn> <Posture> <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _pickHand = (Hand)args[2];
            _placeHand = (Hand)args[3];

            updateBefore();

            return string.Format(",MTRS,{0},{1:D2},E{2},A,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_pickHand));

            //$ <UNo> (<SeqNo>) MTPT <TrsSt> <SlotNo> <NextMtn> <Posture> <TrsPnt> <Sum> <CR>
            //_chamber = (ModuleName)args[0];
            //_slot = (int)args[1];
            //_pickHand = (Hand)args[2];
            //_placeHand = (Hand)args[3];

            //updateBefore();

            //return string.Format(",MTPT,{0},{1:D2},E{2},A,G1,",
            //    RobotConvertor.chamber2staion(_chamber),
            //    RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
            //    RobotConvertor.hand2string(_pickHand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _pickHand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }
    }

    public class ExchangeAfterReadyHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;


        public ExchangeAfterReadyHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $ <UNo> (<SeqNo>) MEXG <Sum> <CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            //if (_hand == Hand.Blade1)
            //    _hand = Hand.Blade2;
            //else
            //    _hand = Hand.Blade1;

            updateBefore();

            return ",MEXG,";
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;
            _device.Swap = true;
            _device.PlaceBalde = _hand;
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade2)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2, _chamber, _slot);
            }

            _device.Swap = false;
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class GotoHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Motion _next;
        private Hand _hand;
        public GotoHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //Move to ready position with specified adjustment offset
            //$<UNo>(<SeqNo>) MTRO <TrsSt> <SlotNo> <NextMtn> <Posture> <OffsetX> <OffsetY> <OffsetZ> <Sum>

            //• OffsetN: Offset(5 bytes each)
            //• OffsetX: X direction offset
            //• OffsetY: Y direction offset
            //• OffsetZ: Z direction offset
            //• Specified in the range between "-9999" and "99999"(Resolution: 0.01[mm])
            //• If value is less than 5 digits, fill the higher digit with ‘0’ so that the field always has 5 digits.

            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _next = (Motion)args[2];
            _hand = (Hand)args[3];

            int x = (int)args[4];
            int y = (int)args[5];
            int z = (int)args[6];

            updateBefore();
            return string.Format(",MTRO,{0},{1:D2},{2},A,{3},{4},{5}",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.NextMotion2String(_next, _hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z)
                ); ;
        }

        private void updateBefore()
        {
            if(_hand == Hand.Blade1)
                _device.Blade1Target = _chamber;
            else if (_hand == Hand.Blade2)
                _device.Blade2Target = _chamber;
            else
            {
                _device.Blade1Target = _chamber;
                _device.Blade2Target = _chamber;
            }
        }
    }


    public class PickHandlerEx : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        public PickHandlerEx()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //Move to ready position and get wafer with adjustment offset(MTRO+MGET)
            //$ < UNo > (< SeqNo >) MTGO<TrsSt> < SlotNo > < NextMtn > < Posture > < OffsetX > < OffsetY > < OffsetZ > < Sum > < CR >
            //• UNo: Unit number(1 byte)
            //• ‘1’ to ‘4’: Unit specified
            //• SeqNo: Sequence number(Non/ 1 / 2 / 3 byte)
            //• TrsSt: Transfer station(2 bytes)
            //• "C1" to "C8": When cassette stage specified
            //• "S1" to "SC": When transfer stage specified
            //• "P1" to “P2”: When P/ A stage specified
            //Note: P2 station is effective only when two or more PA stations exist.
            //• SlotNo: Slot number(2 bytes)
            //• "01" to "XX": When cassette stage specified by<TrsST>
            //(The maximum value of "XX" is in the ASCII code of the number of slots specified by parameter.)
            //• "00": When transfer stage or P / A stage specified by < TrsSt >
            //• NextMtn: Next motion mode(2 bytes)
            //• "GA": Next motion is wafer Get motion with extension axis 1(Blade 1).
            //• "GB": Next motion is wafer Get motion with extension axis 2(Blade 2).
            //• "GW": Next motion is wafer Get motion with extension axis 1 and 2(Blade 1 and 2). < WGet motion >
            //Note 1.The motion of extension axis 2(or blade 2) is available only for a dual-arm manipulator and dual blade
            //linear motion manipulator.
            //2.The WGet motion is available for a dual-arm manipulator and dual blade linear motion manipulator.


            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            int x = (int)args[3];
            int y = (int)args[4];
            int z = (int)args[5];
            updateBefore();
            if (_hand == Hand.Both)
            {
                return string.Format(",MTGO,{0},{1:D2},G{2},A,{3},{4},{5}",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z)
                );
            }

            return string.Format(",MTGO,{0},{1:D2},G{2},A,{3},{4},{5}",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z)
);
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade1)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1);
            }
            else if (_hand == Hand.Blade2)
            {
                for (int i = 0; i < _device.Blade2Slots; i++)
                    WaferManager.Instance.WaferMoved(_chamber, _slot + i, ModuleHelper.Converter(_device.Name), (int)Hand.Blade2 + i);
            }
            else
            {
                for (int i = 0; i < _device.Blade2Slots + 1; i++)
                    WaferManager.Instance.WaferMoved(_chamber, _slot + i, ModuleHelper.Converter(_device.Name), (int)Hand.Blade1 + i);
            }


            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }


    public class PlaceHandlerEx : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;

        public PlaceHandlerEx()
        {
            background = true;
        }

        public override string package(params object[] args)
        {

            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            int x = (int)args[3];
            int y = (int)args[4];
            int z = (int)args[5];
            updateBefore();
            if (_hand == Hand.Both)
            {
                return string.Format(",MTPO,{0},{1:D2},P{2},A,{3},{4},{5}",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
                RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z)
                );
            }

            return string.Format(",MTPO,{0},{1:D2},P{2},A,{3},{4},{5}",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z)
);
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = _hand == Hand.Blade1 ? "1" : "0";
            _device.CmdBlade2Extend = _hand == Hand.Blade1 ? "0" : "1";
        }


        protected override void update(string[] data)
        {
            if (_hand == Hand.Blade1)
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1, _chamber, _slot);
            }
            else if (_hand == Hand.Blade2)
            {
                for (int i = 0; i < _device.Blade2Slots; i++)
                    WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade2 + i, _chamber, _slot + i);
            }
            else
            {
                for (int i = 0; i < _device.Blade2Slots + 1; i++)
                    WaferManager.Instance.WaferMoved(ModuleHelper.Converter(_device.Name), (int)Hand.Blade1 + i, _chamber, _slot + i);
            }

            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);

            string arm = _hand == Hand.Blade1 ? "A" : "B";
            _device.CmdBladeTarget = $"{_chamber}.{arm}";
            _device.CmdBlade1Extend = "0";
            _device.CmdBlade2Extend = "0";
        }
    }

    //NOT SUPPORT with OFFSET
    //public class ExchangeHandlerEx : RobotMotionHandler
    //{
    //    private ModuleName _chamber;
    //    private int _slot;
    //    private Hand _hand;


    //    public ExchangeHandlerEx()
    //    {
    //        background = true;
    //    }

    //    public override string package(params object[] args)
    //    {
    //        // $ <UNo> (<SeqNo>) MEXG <Sum> <CR>
    //        _chamber = (ModuleName)args[0];
    //        _slot = (int)args[1];
    //        _hand = (Hand)args[2];
    //        if (_hand == Hand.Blade1)
    //            _hand = Hand.Blade2;
    //        else
    //            _hand = Hand.Blade1;

    //        updateBefore();

    //        int x = (int)args[3];
    //        int y = (int)args[4];
    //        int z = (int)args[5];
    //        updateBefore();
    //        if (_hand == Hand.Both)
    //        {
    //            return string.Format(",MTPO,{0},{1:D2},G{2},A,{3},{4},{5}",
    //            RobotConvertor.chamber2staion(_chamber),
    //            RobotConvertor.chamberSlot2Slot(_chamber, _slot + 1),
    //            RobotConvertor.hand2string(_hand),
    //            RobotConvertor.Offset2String(x),
    //            RobotConvertor.Offset2String(y),
    //            RobotConvertor.Offset2String(z)
    //            );
    //        }

    //        return string.Format(",MTPO,{0},{1:D2},G{2},A,{3},{4},{5}",
    //            RobotConvertor.chamber2staion(_chamber),
    //            RobotConvertor.chamberSlot2Slot(_chamber, _slot),
    //            RobotConvertor.hand2string(_hand),
    //            RobotConvertor.Offset2String(x),
    //            RobotConvertor.Offset2String(y),
    //            RobotConvertor.Offset2String(z));

    //    }

    //    private void updateBefore()
    //    {
    //        _device.Blade1Target = _chamber;
    //        _device.Blade2Target = _chamber;
    //        _device.Swap = true;
    //        _device.PlaceBalde = _hand;
    //    }

    //    protected override void update(string[] data)
    //    {
    //        if (_hand == Hand.Blade2)
    //        {
    //            WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)Hand.Blade2);
    //            WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade1, _chamber, _slot);
    //        }
    //        else
    //        {
    //            WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)Hand.Blade1);
    //            WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade2, _chamber, _slot);
    //        }

    //        _device.Swap = false;
    //        _device.Blade1Target = ModuleName.Robot;
    //        _device.Blade2Target = ModuleName.Robot;
    //    }
    //}

    public class RBWaferMappingHandler : RobotMotionHandler
    {
        private ModuleName _chamber;

        public RBWaferMappingHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) MMAP <TrsSt> <SlotNo> <Sum> <CR>
            // SlotNo: Slot number(2 bytes)
            //• "FF": When all slots specified
            //• ”01” to ”XX”: When cassette stage is specified by < TrsSt >
            //(The maximum value of "XX" is in ASCII code of the number of slots specified by parameter.)
            //• ”00”: When transfer stage or P / A stage specified by<TrsSt>
            //  Note: Specific slot section definition cannot be performed.
            //• Reception rejected: Responds with the error code.
            _chamber = (ModuleName)args[0];
            //_slot = (int)args[1];
            //_hand = (Hand)args[2];

            updateBefore();
            return string.Format(",MMAP,{0},FF,",
                RobotConvertor.chamber2staion(_chamber));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;
        }

        protected override void update(string[] data)
        {
            _device.Blade1Target = ModuleHelper.Converter(_device.Name);
            _device.Blade2Target = ModuleHelper.Converter(_device.Name);
        }
    }

    public class RBQueryWaferMapHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;
        private ModuleName _chamber;
        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;

        public RBQueryWaferMapHandler()
        {
            background = false;
        }

        //$ <UNo> (<SeqNo>) RMAP <TrsSt> <SlotNo> <Sum> <CR>
        public string package(params object[] args)
        {
            _chamber = (ModuleName)args[0];

            return string.Format(",RMAP,{0},FF,",
                           RobotConvertor.chamber2staion(_chamber));
        }

        public bool unpackage(string type, string[] items)
        {
            //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RMAP,<TrsSt>,<Slot>,
            //01:<Result1>…,N:<ResultN>(,<Sum>)<CR>
            //• UNo : Unit number (1 byte)
            //• SeqNo : Sequence number (None / 2 bytes)
            //• Sts : Status (2 bytes)
            //• Ackcd : Response code (4 bytes)
            //• TrsSt : Transfer station (3 bytes)
            //• Slot : Slot number (2 bytes)
            //• Result* : Mapping result (2 bytes each)
            //• “--” : No wafer detected.
            //• “OK” : Wafer inserted correctly.
            //• “CW” : Wafer inserted incorrectly (inclined).
            //• “DW” : Wafer inserted incorrectly (duplicated).
            //Note) Responds with the number of slots of the specified transfer station.
            //$,1,00,0000,RMAP,C02,00,
            //01:OK,02:DW,03:OK,04:CW,05:CW,06:OK,07:OK,08:--,09:OK,10:OK    
            // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W" 
            if (items.Length > 5)
            {
                string data = items[5];
                StringBuilder sb = new StringBuilder();

           
                for (int i = 0; i < data.Length; i += 4)
                {
                    string value = data.Substring(i + 2, 2);
                    switch (value)
                    {
                        case "--":
                            sb.Append("0");
                            break;
                        case "OK":
                            sb.Append("1");
                            break;
                        case "CW":
                            sb.Append("2");
                            break;
                        case "DW":
                            sb.Append("W");
                            break;
                    }
                }

                _device.NotifySlotMapResult(_chamber, sb.ToString());
                return true;
            }

            return !background;
        }
    }


    //    Transfer speed level setting
    public class RbSetSpeedHandler : RobotMotionHandler
    {
        private int _speed = 0;
        public RbSetSpeedHandler()
        {
            background = false;
        }

        public override string package(params object[] args)
        {
            _speed = (int)args[0];
            //$ < UNo > (< SeqNo >) SSLV<Level> < Sum > < CR >
            ///
            ///            • ’H’ : High speed level
             //• ’M’ : Medium speed level
                //• ’L’ : Low speed level
                ///
                if(_speed == 1)
                    return string.Format(",SSLV,H,");
                else if (_speed == 2)
                    return string.Format(",SSLV,M,");

                return string.Format(",SSLV,L,");
        }
    }

    //    Transfer speed level setting
    public class RbSetOffsetHandler : RobotMotionHandler
    {
        public RbSetOffsetHandler()
        {
            background = false;
        }

        public override string package(params object[] args)
        {
            //$ <UNo> (<SeqNo>) SOFS <Mem> <TrsSt> <Offset1> <Offset2> … <Offset5> <Sum> <CR>
            ///Mem: Memory (1 byte) ‘V’: Volatile memory ‘N’: Non -volatile memory
            //• TrsSt: Transfer station(2 bytes)
            //• "C1" to "C8": When cassette stage specified
            //• "S1" to "SC": When transfer stage specified
            //• "P1" to “P2”: When P/ A stage specified
            //Note: P2 station is effective only when two or more PA stations exist.
            //• Offset: Transfer offset(4 bytes each)
            //• Offset1: Downward offset
            //• Offset2: Upward offset
            //• Offset3: Offset in the extending direction(with the edge - grip - type fork)
            //• Offset4: Offset in the retracting direction(with the edge - grip - type fork)
            //• Offset5: Put downward offset(with the edge - grip - type fork)
            //Note: Offset3 to Offset5 are omitted when the fork type is not the edge - grip type.
            //• About Offset3 to Offset5, it is omissible with a parameter setup.
            //• Specified in the range between "-999" and "9999"(resolution: 0.01[mm])
            //• If a value is less than specified digits, fill the higher digit with ‘0’ so that the field always has specfied digits.
            ModuleName _chamber = (ModuleName)args[0];
            int down = (int)args[1];
            int up = (int)args[2];
            int extend = (int)args[2];
            int retract = (int)args[2];
            int pdown = (int)args[2];

            return string.Format(",SOFS,V，{0},{1:D4},{2:D4},{3:D4},{4:D4},",
                RobotConvertor.chamber2staion(_chamber),
                down,
                up,
                extend,
                retract,
                pdown
               );
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

        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Errcd> <Status1> … <Status4> <Sum> <CR>

        public string package(params object[] args)
        {
            return ",RSTS,";
        }

        public bool unpackage(string type, string[] items)
        {
            //status1 bit 0 --blade 1 wafer status  0=wafer present, 1=wafer absent. Here balde 1 is upper
            //status1 bit 1 --blade 2 wafer status  0=wafer present, 1=wafer absent. Here balde 2 is lower
            //$,1,10,A0,0000,29D05000  status1 = 5 measn blade1 has wafer, blade2 no wafer
            int status1 = 0;
            if (items!=null && items.Length >= 6 && items[5].Length > 4)
            {
                var content = items[5];
                var status1Bit = content[4];
                if (status1Bit >= 'A' && status1Bit <= 'F')
                    status1 = status1Bit - 'A' + 10;
                else if (status1Bit >= 'a' && status1Bit <= 'f')
                    status1 = status1Bit - 'a' + 10;
                else
                    int.TryParse(status1Bit.ToString(), out status1);

                //Here balde 1 is upper,balde 2 is lower
                ((Robot)_device).NotifyWaferPresent(Hand.Blade2, (status1&0x0001) == 0 ? WaferStatus.Normal : WaferStatus.Empty);
                ((Robot)_device).NotifyWaferPresent(Hand.Blade1, (status1&0x0002) == 0 ? WaferStatus.Normal : WaferStatus.Empty);
            }
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
            string evtType = items[3];
            if (evtType.Equals(ProtocolTag.resp_evt_error))
            {
                int error = Convert.ToInt32(items[5], 16);
                _device.ErrorCode = error;
                if (error > 0)
                    _device.LastErrorCode = error;
                return true;
            }
            return false;
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

        //$ <UNo> (<SeqNo>) RPOS <TrsSt> <Fork> <Posture> <Sum> <CR>
        //     • TrsSt: Transfer station(2 bytes)
        //• "C1" to "C8": When the lowest-layer position of the cassette stage specified
        //• "H1" to "H8": When the highest-layer position of the cassette stage specified
        //• "S1" to "SC": When transfer stage specified
        //• "P1" to “P2”: When P/A stage specified
        //Note: P2 station is effective only when two or more PA stations exist.
        //• "FF": When current position specified
        //• "FE": When specify the feedback of present location
        //<Three-axis pre-aligner, Edge-grip pre-aligner>
        //• "FF": When current position specified
        //• "FE": When specify the feedback of present location
        //• Fork: Fork specified(1 byte)
        //• ‘A’: When extension axis 1 (Blade 1) specified
        //• ‘B’: When extension axis 2 (Blade 2) specified(for dual-arm manipulator and dual blade linear motion
        //manipulator only)
        //Note: Fixed to ‘A’ when the current position and feedback of present location are specified at<TrsSt>
        public string package(params object[] args)
        {
            return ",RPOS,FF,A,A,";
        }

        public bool unpackage(string type, string[] items)
        {
            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Value1> … <ValueN> <Sum> <CR>
            //            • ValueN: Coordinate(8 bytes each)
            //• Specified in the range as follows (The resolution is 0.01[mm] or[deg])
            //8 Byte: between "-9999999" ~"99999999"
            //• If a value is less than specified digits, fill the higher digit with ‘0’ so that the field always has specfied digits.
            //• The sign is added to the highest digit.
            //• The number of “ValueN” depends on the unit type.
            //Responds with as many axis numbers as the specified unit has.See the table bellow.
            //Unit Value1 Value2 Value3 Value4 Value5 Value6
            //Single - arm manipulator Rotation    //Elevation           //(Track            
            //Dual - arm manipulator Rotation      //Extension            //axis 1   //axis 2   //(Track
         
            if (items.Length > 7)
            {  
                _device.Rotation = int.Parse(items[7]);
                _device.Extension = int.Parse(items[8]);
                _device.Wrist1 = int.Parse(items[9]);
                _device.Wrist2 = int.Parse(items[10]);
                _device.Evevation = int.Parse(items[11]);
                
                return true;
            }

            return !background;
        }
    }


    public class RbSetWaferSizeHandler : RobotMotionHandler
    {
        public RbSetWaferSizeHandler()
        {
            background = false;
        }

        public override string package(params object[] args)
        {
            ////$ < UNo > (< SeqNo >) SPRM<Type> < PrmNo > < Value > < Sum > < CR >
            //// UNo: Unit No. (1 Byte)
            ////  ‘1’ : RB
            ////  ‘2’ : PA
            //// SeqNo: Sequence No. (None / 1 / 2 / 3 Byte)
            //// Type: Parameter type(3 Byte)
            ////  ”CIU” : Common integral number parameter
            ////  ”CRU” : Common real number parameter
            ////  ”CRS” : Common real number system parameter
            ////  ”UIU” : Unit integral number parameter
            ////  ”URU” : Unit real number parameter
            ////  ”URS” : Unit real number system parameter
            //// PrmNo: Parameter No. (4 Byte)
            ////  Designate in the range of ”0000” - ”9999”
            ////  In case of less than 4 - digit, put 0 in front of the numbers. Always designate numbers in 
            ////4 - digit format.
            //// Value: Parameter value(12 Byte)
            //// Designate in the range of ”-99999999999” - ”999999999999” (Resolving power of real
            ////number parameter is 0.0001)  In case of less than 12 - digit, put 0 in front of the numbers. Always designate numbers in 
            ////12 - digit format.
            //// Simbol is added on most significant bit
            ////[Response format](Response message) $ < UNo > (< SeqNo >) < StsN > < Ackcd > < Sum > < CR >
            //// UNo: Unit No.(1 Byte)
            ////  ‘1’ : RB
            ////  ‘2’ : PA
            //// SeqNo: Sequence No. (None / 1 / 2 / 3 Byte)
            //// StsN: Status(1 / 2 Byte)
            //// Ackcd: Response code(4 Byte)
            ////  In case of reception normally  Response ”0000”
            ////  Incase of reception denial  Response error code
            ///
            //SPRMCRS0000  000000625000 68
            int cmd = (int)args[0];
            WaferSize size = (WaferSize)args[1];

            string package = "";
            switch (cmd)
            {
                case 0x0000:
                    package = string.Format("SPRM,CRS,0000,{0:D12},",
                        RobotConvertor.WaferSize2Int(size)
                    );
                    break;

                case 0x0114:
                    package = string.Format("SPRM,URS,0114,{0:D12},",
                        RobotConvertor.MappingOffset2Int(size)
                    );
                    break;
            }

            return package;
        }
    }

    /*
    public class RBQueryWaferSizeHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Robot)value; } }
        protected Robot _device;
        public RBQueryWaferSizeHandler()
        {
            background = false;
        }

        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Errcd> <Status1> … <Status4> <Sum> <CR>

        public string package(params object[] args)
        {
            return ",RSTS,";
        }

        public bool unpackage(string type, string[] items)
        {
            //status1 bit 0 --blade 1 wafer status  0=wafer present, 1=wafer absent. Here balde 1 is upper
            //status1 bit 1 --blade 2 wafer status  0=wafer present, 1=wafer absent. Here balde 2 is lower
            //$,1,10,A0,0000,29D05000  status1 = 5 measn blade1 has wafer, blade2 no wafer
            int status1 = 0;
            if (items != null && items.Length >= 6 && items[5].Length > 4)
            {
                var content = items[5];
                var status1Bit = content[4];
                if (status1Bit >= 'A' && status1Bit <= 'F')
                    status1 = status1Bit - 'A' + 10;
                else if (status1Bit >= 'a' && status1Bit <= 'f')
                    status1 = status1Bit - 'a' + 10;
                else
                    int.TryParse(status1Bit.ToString(), out status1);

                //Here balde 1 is upper,balde 2 is lower
                ((Robot)_device).NotifyWaferPresent(Hand.Blade2, (status1 & 0x0001) == 0 ? WaferStatus.Normal : WaferStatus.Empty);
                ((Robot)_device).NotifyWaferPresent(Hand.Blade1, (status1 & 0x0002) == 0 ? WaferStatus.Normal : WaferStatus.Empty);
            }
            return !background;
        }
    }
*/
}
