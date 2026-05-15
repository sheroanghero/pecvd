using System;
using System.Text;

using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Log;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.SR100
{
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
            return port.Write(string.Format("{0}{1}{2}", ProtocolTag.tag_cmd_start, package(), ProtocolTag.tag_end));
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
            //message = ">,1,EVNT,100,2018/11/16 17:01:06,2BA0,A1";
            //           >,1,EVNT,100,2019/10/11 17:27:32,1930,8B
            //message = " !,1,36,40,2BA0,MTRS,002200,-0011931,-0610171,00179995,00049818,00186336,57";
            //message = "!,1,16,40,0000,MTCH,000483,-0011862,00000000,00179516,00090369,00172882,0A";
            try
            {
                completed = false;

                string package = message;
                string[] words = Regex.Split(package, ProtocolTag.cmd_token);

                string type = words[0];
                int unit = int.Parse(words[1]);

                string sum = words[words.Length - 1];
                string check = Checksum(package);
                if (sum != check)
                {
                    throw (new InvalidPackageException(string.Format("check sum error{0}", package)));
                }

                if (type != ProtocolTag.resp_tag_event && type != ProtocolTag.resp_tag_error)
                {
                    int seq = int.Parse(words[2]);

                    if (seq != ID)
                        return false;

                    if (unit != Unit)
                    {
                        throw (new InvalidPackageException(string.Format("invalid unit {0}", package)));
                    }
                }

                if (type == ProtocolTag.resp_tag_error)
                {
                    string error = words[1];
                    //‘1’: Warning 1 (W1), ‘2’: Warning 2 (W2), ‘3’: Important alarm 1 (A1), ‘4’: Important alarm 2 (A2),
                    //‘5’: Serious alarm (F)

                    if (error[0] != '1' && error[0] != '2')
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
                    string evtType = words[3];
                    string evtInfo = words[5];

                    if (_imp.evt)
                        completed = _imp.unpackage(type, words);
                    return _imp.evt;
                }
                else
                {
                    completed = _imp.unpackage(type, words);
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
            data = string.Format(",{0:D1},{1:D2}{2}", Unit, ID, _imp.package(this._objs));

            string sum = Checksum(Encoding.ASCII.GetBytes(data));

            return data + sum;
        }

        private string ackn()
        {
            //$,<UNo>(,<SeqNo>),ACKN(,<Sum>)<CR>
            string data = string.Empty;
            data = string.Format(",{0:D1},{1:D2},ACKN,", Unit, ID);

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
            int value = Convert.ToInt32(items[3],16);
 
            _device.Status = value;
     
            int error = Convert.ToInt32(items[4], 16);
            
            _device.ErrorCode = error;
            if (error > 0)
                _device.LastErrorCode = error;

            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
                if (items.Length > 7)
                    _device.ElapseTime = int.Parse(items[6]);
                if (items.Length > 8)
                    _device.Rotation = int.Parse(items[7]);
                if (items.Length > 9)
                    _device.Extension = int.Parse(items[8]);
                if (items.Length > 10)
                    _device.Wrist1= int.Parse(items[9]);
                if (items.Length > 11)
                    _device.Wrist2= int.Parse(items[10]);
                if (items.Length > 12)
                    _device.Evevation = int.Parse(items[11]);

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
        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        public override string package(params object[] args)
        {
            return ",CCLR,E,";
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
             updateBefore();
             return ",INIT,1,1,G,";
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

    public class RbClearErrorHandler : RobotMotionHandler
    {       
        public RbClearErrorHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        public override string package(params object[] args)
        {
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
        //$,<UNo>(,<SeqNo>),CSOL,<Sol>,<Sw>,<Wait>(,<Sum>)<CR> 
        //sol Solenoid control specification (1 byte) • ‘1’ : Blade 1. • ‘2’ : Blade 2. • ‘F’ : Blade 1 + Blade 2. 
        // Solenoid command (1 byte)  • ‘0’ : Wafer release. / Lifter down. • ‘1’ : Wafer hold. / Lifter up. 
        public override string package(params object[] args)
        { 
            _hand = (Hand)args[0];
            bool bHold = (bool)args[1];
            if(bHold)
                return string.Format(",CSOL,{0},1,0,",RobotConvertor.hand2string(_hand));

            return string.Format(",CSOL,{0},0,0,", RobotConvertor.hand2string(_hand));
        }
    }

    public class RbStopHandler : RobotMotionHandler
    {
        public RbStopHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CSTP,<Sw>(,<Sum>)<CR>
        //• ‘H’ : Deceleration to a stop.
        //• ‘E’ : Emergency stop.
        public override string package(params object[] args)
        {
            bool isEmergency = (bool)args[0];
            if (isEmergency)
                return ",CSTP,E,";

            return ",CSTP,H,";
        }
    }
    public class RbResumeHandler : RobotMotionHandler
    {
        public RbResumeHandler()
        {
            background = true;
        }
        //[Command Message] $,<UNo>(,<SeqNo>),CRSM(,<Sum>)<CR> 
        //• UNo : Unit number(1 byte)
        //      • ‘1’ : Manipulator.
        //      • ‘2’ : Pre-aligner.
        //• SeqNo : Sequence number(None / 2 bytes)
        public override string package(params object[] args)
        {
            return ",CRSM,";
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
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTRS,G,{0},{1:D2},A,{2},Gb,",
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
            //$,<UNo>(,<SeqNo>),MPNT,<TrsPnt>(,<Sum>)<CR>
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
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
           // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();

            return string.Format(",MTRS,G,{0},{1:D2},A,{2},G4,",
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
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();
            return string.Format(",MTRS,P,{0},{1:D2},A,{2},P4,",
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
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            updateBefore();
            return string.Format(",MTRS,P,{0},{1:D2},A,{2},Pb,",
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
            //$,<UNo>(,<SeqNo>),MPNT,<TrsPnt>(,<Sum>)<CR>
            // $,<UNo>(,<SeqNo>),MPNT,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
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

    public class ExchangHandler : RobotMotionHandler
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
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            if (_hand == Hand.Blade1)
                _hand = Hand.Blade2;
            else
                _hand = Hand.Blade1;

            updateBefore();

            return string.Format(",MTRS,E,{0},{1:D2},A,{2},P4,",
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

    public class GotoHandler : RobotMotionHandler
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
            // $,<UNo>(,<SeqNo>),MTCH,<TrsSt>,<Slot>,<Posture>,<Hand>,<PMode>  (,< OfstX >,< OfstY >,< OfstZ >)(,< Sum >)< CR >
            //• ‘M’ : Intermediate position (position with XYZ direction offset value applied).
            //• ‘R’ : Ready position (position with XYZ direction offset value applied).
           // • ‘O’ : Offset position (position with XYZ direction offset values applied).
           //// • ‘S’ : Registered position.
           /// • ‘B’ : Mapping start position.
           // • ‘E’ : Mapping finish position.
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];

            updateBefore();
            return string.Format(",MTCH,{0},{1:D2},A,{2},R,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;
        }
    }

    public class MoveToHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;
        private bool _isPick;
        public MoveToHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,<UNo>(,<SeqNo>),MTCH,<TrsSt>,<Slot>,<Posture>,<Hand>,<PMode>  (,< OfstX >,< OfstY >,< OfstZ >)(,< Sum >)< CR >
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            _isPick = (bool)args[3];

            updateBefore();
            return string.Format(",MTRS,G,{0},{1:D2},A,{2},G4,",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.IsPick2Position(_isPick));
        }

        private void updateBefore()
        {
            _device.Blade1Target = _chamber;
            _device.Blade2Target = _chamber;
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
//            Move to ready position and get wafer with adjustment offset(MTRO+MGET)
//            $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt> (,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
//            UNo: Unit number (1 byte)
//                • ‘1’ : Manipulator.
//                • SeqNo : Sequence number (None / 2 bytes)
//                • Mtn : Motion mode (1 byte)
//                    • ‘G’ : Get motion.
//                    • ‘P’ : Put motion.
//                    • ‘E’ : Exchange motion.
//                • TrsSt : Transfer station (3 bytes)
//                    • “C01” - “C08” : Cassette stage.
//                    • “S01” - “S12” : Transfer stage.
//                    • “P01” : P/A stage.
//                • Slot : Slot number (2 bytes)
//                    <Cassette stage>
//                        • “01” - “30” : When cassette stage specified.
//                    <Transfer stage, Pre-aligner stage>
//                        • “00” : Fixed slot(because this type of station does not have multiple slots.).
//            Note) If value is less than 2 digits, fill the higher digit with ‘0’ so that the field always has 2 digits.
//            Note) If <Hand> is ‘F’(Blade 1 + Blade 2), specifies the slot accessed by Blade 1.
//            Note) If <TrsST> is “S01”-“S12” or “P01”, the slot no. is ignored.
//                • Posture : Arm Posture (1 byte)
//                    • ‘L’ : Left elbow.
//                    • ‘R’ : Right elbow.
//                    • ‘A’ : Automatic (Automatically selected posture with the proper path).
//                • Hand : Blade (1 byte)
//                    • ‘1’ : Blade 1.
//                    • ‘2’ : Blade 2.
//                    • ‘F’ : Blade 1 + Blade 2 (WGet/WPut operation).
//            Note) Except for <TrsPnt> is [C01-C08: Cassette stage], ‘F’(Blade 1 + Blade 2) cannot be
//            specified.
//            Note) If <Mtn> is ‘E’( Exchange motion), ‘F’(Blade 1 + Blade 2) cannot be specified.
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            int x = (int)args[3];
            int y = (int)args[4];
            int z = (int)args[5];
            if (x < -5000) x = -5000;
            if (x > 5000) x = 5000;
            if (y < -5000) y = -5000;
            if (y > 5000) y = 5000;
            if (z < -2500) z = -5000;
            if (z > 2500) z = 2500;
            updateBefore();

            return string.Format(",MTRS,G,{0},{1:D2},A,{2},G4,{3},{4},{5},",
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
        }

        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(_chamber, _slot + 1, ModuleName.Robot, (int)Hand.Blade2);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)_hand);
            }
            
            _device.Blade1Target = ModuleName.Robot;
            _device.Blade2Target = ModuleName.Robot;
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
            if (x < -5000) x = -5000;
            if (x > 5000) x = 5000;
            if (y < -5000) y = -5000;
            if (y > 5000) y = 5000;
            if (z < -2500) z = -5000;
            if (z > 2500) z = 2500;
            updateBefore();

            return string.Format(",MTRS,P,{0},{1:D2},A,{2},P4,{3},{4},{5},",
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
        }


        protected override void update(string[] data)
        {
            if (_hand == Hand.Both)
            {
                WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade1, _chamber, _slot);
                WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade2, _chamber, _slot + 1);
            }
            else
            {
                WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)_hand, _chamber, _slot);
            }


            _device.Blade1Target = ModuleName.Robot;
            _device.Blade2Target = ModuleName.Robot;
        }
    }
    
    public class ExchangeHandlerEx : RobotMotionHandler
    {
        private ModuleName _chamber;
        private int _slot;
        private Hand _hand;


        public ExchangeHandlerEx()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
            _chamber = (ModuleName)args[0];
            _slot = (int)args[1];
            _hand = (Hand)args[2];
            if (_hand == Hand.Blade1)
                _hand = Hand.Blade2;
            else
                _hand = Hand.Blade1;
            int x = (int)args[3];
            int y = (int)args[4];
            int z = (int)args[5];
            updateBefore();

            return string.Format(",MTRS,E,{0},{1:D2},A,{2},P4,{3},{4},{5},",
                RobotConvertor.chamber2staion(_chamber),
                RobotConvertor.chamberSlot2Slot(_chamber, _slot),
                RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x),
                RobotConvertor.Offset2String(y),
                RobotConvertor.Offset2String(z));
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
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)Hand.Blade2);
                WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade1, _chamber, _slot);
            }
            else
            {
                WaferManager.Instance.WaferMoved(_chamber, _slot, ModuleName.Robot, (int)Hand.Blade1);
                WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)Hand.Blade2, _chamber, _slot);
            }

            _device.Swap = false;
            _device.Blade1Target = ModuleName.Robot;
            _device.Blade2Target = ModuleName.Robot;
        }
    }
    
    public class RBWaferMappingHandler : RobotMotionHandler
    {
        private ModuleName _chamber;
        //private int _slot;
        //private Hand _hand;
        public RBWaferMappingHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            //$,<UNo>(,<SeqNo>),MMAP,<TrsSt>,<Slot>(,<Safe>)(,<Sum>)<CR>
            //TrsSt : Transfer station (3 bytes)
            //“C01” - “C08” : Cassette stage.
            //“S01” - “S12” : Transfer stage.
            //“P01” : P/A stage.
            //• Slot : Slot number (2 bytes)
            //<Cassette stage>
            //• “01” - “30” : When specifying individual cassette stage slot.
            //• “00” : When specifying all slots.
            //<Transfer stage, P/A stage>
            //• “00” : Fixed value t(because this type of station does not have multiple slots.)
            //Note) If value is less than 2 digits, fill the higher digit with ‘0’ so that the field always has 8 digits.
            //Note) If <TrsST> is “S01”-“S12” or “P01”, the slot no. is ignored.
            //• Safe : Specifies wafer protrusion detection operation yes/no (1 byte)
            //• ‘0’ : No wafer protrusion detection operation.
            //• ‘1’ : Wafer protrusion detection operation performed.
            _chamber = (ModuleName)args[0];
            //_slot = (int)args[1];
            //_hand = (Hand)args[2];

            updateBefore();
            return string.Format(",MMAP,{0},00,1,",
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

        //$,<UNo>(,<SeqNo>),RMAP,<TrsSt>,<Slot>(,<Sum>)<CR>
        public string package(params object[] args)
        {
            _chamber = (ModuleName)args[0];

            return string.Format(",RMAP,{0},00,",
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
            if (items.Length > 7)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 8; i < items.Length-1; i++)
                {
                    string value = items[i].Substring(3);
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

            return string.Format(",SSLV,{0},", _speed);
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
            /*
                (Positive Response Message)
                    $,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RSTS,<Errcd>,<Status>(,<Sum>)<CR>
                        • UNo : Unit number (1 byte)
                        • SeqNo : Sequence number (None / 2 bytes)
                        • Sts : Status (2 bytes)
                        • Ackcd : Response code (4 bytes)
                        • Errcd : Error code (4 bytes)
                        Note) Responds with the currently occurring error for the specified unit.
                        (When there is no error, responds “0000”)
                        • Status : Status information (4 bytes)
                        • Refer to the supplementary explanation.
                (Negative Response Message)
                    $,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RSTS(,<Sum>)<CR>
                        • UNo : Unit number (1 byte)
                        • SeqNo : Sequence number (None / 2 bytes)
                        • Sts : Status (2 bytes)
                        • Ackcd : Response code (4 bytes)
                */

            if (items.Length > 7)
            {
                int errorCode = Convert.ToInt32(items[6], 16);
                _device.ErrorCode = errorCode;
            }

            if (items.Length > 8)
            {
                int status = Convert.ToInt32(items[7], 16);
                _device.StateWaferOnBlade1 = (status & 0x01) == 0x01;
                _device.StateWaferOnBlade2 = (status & 0x02) == 0x02;
                _device.StateBlade1Gripped = (status & 0x04) == 0x04;
                _device.StateBlade2Gripped = (status & 0x08) == 0x08;

                _device.StateInterlock1 = (status & 0x0100) == 0x0100;
                _device.StateInterlock2 = (status & 0x0200) == 0x0200;
                _device.StateInterlock3 = (status & 0x0400) == 0x0400;
                _device.StateInterlock4 = (status & 0x0800) == 0x0800;

                _device.StateInterlock5 = (status & 0x010000) == 0x010000;
                _device.StateInterlock6 = (status & 0x020000) == 0x020000;
                _device.StateInterlock7 = (status & 0x040000) == 0x040000;
                _device.StateInterlock8 = (status & 0x080000) == 0x080000;
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

        //$,<UNo>(,<SeqNo>),RPOS,<PType>(,<Sum>)<CR>
        //• ‘R’ : Command position.• ‘F’ : Feedback position.
        public string package(params object[] args)
        {
            return ",RPOS,F,";
        }

        public bool unpackage(string type, string[] items)
        {
            //Positive $,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RPOS,<PType>,<Value1>…,<ValueN>(,<Sum>)<CR>
            //Negative $,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RPOS,<PType>(,<Sum>)<CR>

            if (items.Length > 11)
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

    public class PositionAdjustmentHandler : RobotMotionHandler
    {
        private Axis axis;
        //private int _slot;
        //private Motion _next;
        private Hand _hand;
        public PositionAdjustmentHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            // $,< UNo > (,< SeqNo >),MABS,< Axis >,< Hand >,< Mode >,< Value > (,< Sum >) < CR >
           // $，< UNo >（,< Saqno >),MREL,< Axis >，< mode >< Value >（< Sum >）< CR >
                         //  UNo : Unit number(1 byte)
                         //‘1’ : Manipulator.
                         //‘2’ : Edge grip type pre-alinger.
                         //SeqNo : Sequence number(None / 2 bytes)
                         //Axis: Axis(1 byte)
                         //< Manipulator >
                         //‘S’ : Rotation axis.
                         //‘A’ : Extension axis.
                         //‘H’ : Wrist axis 1.
                         //‘I’ : Wrist axis 2.
                         //‘Z’ : Elevation axis.
                         //< Edge grip type pre - alinger >
                         //‘S’ : Rotation axis.
                         //Hand: Blade(1 byte)
                         //‘1’ : Blade 1.
                         //‘2’ : Blade 2.
                         //Note) If the<Axis> specification is “A: Extension axis”, specify the access blade.
                         //If the<Axis> specification is not “A: Extension axis”, specify ‘1’
                         //Mode: Passive blade operation mode(1 byte)
                         //‘C’ : Maintain passive blade posture.
                         //‘H’ : Passive blade fixed to wafer center.
                         //Note) Valid if the < Axis > specification is “A: Extension axis”.
                         //If the<Axis> specification is not “A: Extension axis”, specify ‘C’.
                         //Value: Coordinate(8 bytes, Resolution: 0.001[mm] /[deg])
                         //Note) Specified in the range between “-9999999” and “99999999”.
                         //If value is less than 8 digits, fill the higher digit(s) with ‘0’ so that the field always has 8 digits.
                         //A sign is added to the highest digit.
                         //Note) If the operation range is exceeded a stroke limit error will be notified.

                         axis = (Axis)args[0];
            _hand = (Hand)args[1];
            int x = (int)args[2];
            return string.Format(",MREL,{0},{1},C,{2},",//MABS
                RobotConvertor.AxisToStr(axis),
                 RobotConvertor.hand2string(_hand),
                RobotConvertor.Offset2String(x)


                ); 
        }

       
    }

}
