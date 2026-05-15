using System;
using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100.AL
{
    public class AlignerHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Aligner)value; } }
        protected Aligner _device;

        public AlignerHandler()
        {
            background = false;
        }

        public virtual string package(params object[] args)
        {
            return "";
        }

        public bool unpackage(string type, string[] items)
        {
            int value = Convert.ToInt32(items[3], 16);

            _device.Status = value;

            int error = Convert.ToInt32(items[4], 16);
            _device.ErrorCode = error;
            if (error > 0)
                _device.LastErrorCode = error;

            if (type.Equals(ProtocolTag.resp_tag_excute))
            {
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
    public class AlInitHandler : AlignerHandler
    {
        public AlInitHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            return ",INIT,00,";
        }

        protected override void update(string[] data)
        {
            _device.Initalized = true;
        }
    }

    public class AlHomeHandler : AlignerHandler
    {
        public AlHomeHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {

            return ",MHOM,F,";
        }

        protected override void update(string[] data)
        {
            _device.Initalized = true;
        }
    }

    public class AlClearErrorHandler : AlignerHandler
    {

        public AlClearErrorHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        public override string package(params object[] args)
        {
            return ",CCLR,E,";
        }
    }
    public class AlGripHandler : AlignerHandler
    {
        public AlGripHandler()
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
            bool bHold = (bool)args[1];
            // bool bHold = (bool)args[0];
            if (bHold)
                return ",CSOL,A,1,";

            return ",CSOL,A,0,";
        }
    }

    
    public class ALMoveToReadyPositionHandler : AlignerHandler
    {
        public ALMoveToReadyPositionHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
 
 
            return ",MTRS,GP,00,AL,A,";
        }
    }
    public class ALLiftHandler : AlignerHandler
    {
        public ALLiftHandler()
        {
            background = true;
        }
        //$ <UNo> (<SeqNo>) CLFT<Sw> <Sum> <CR>
        //• UNo: Unit number(1 byte)
        //• ‘1’ to ‘4’: Unit specified
        //• SeqNo: Sequence number(Non/1/2/3 byte)
        //• Sw: Lifter command(1 byte)
        //• ‘0’: Lifter down
        //• ‘1’: Lifter up
        public override string package(params object[] args)
        {

            bool bUp = (bool)args[0];
            if (bUp)
                return ",CLFT,1,";

            return ",CLFT,0,";
        }
    }

    public class AlStopHandler : AlignerHandler
    {
        public AlStopHandler()
        {
            background = true;
        }

        public override string package(params object[] args)
        {
            return ",CHLT,";
        }
    }

    public class ALQueryStateHandler : ITransferMsg
    {
        public bool background { get; protected set; }
        public bool evt { get { return false; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Aligner)value; } }
        protected Aligner _device;

        public ALQueryStateHandler()
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

    public class AlAlignHandler : AlignerHandler
    {
        private int _angle = 0;
        public AlAlignHandler()
        {
            background = true;
        }
        // $ <UNo> (<SeqNo>) MALN <TUNo> <TrsSt> <Angle> <Sum> <CR>
        //Mode : Motion mode (1 byte) If the case of edge grip type pre-Aligner, specify ‘0’.
        public override string package(params object[] args)
        {
            //            • TUNo: Unit number to be compensated for (1 byte)
            //• ‘1’ to ‘4’: Unit specified
            //• TrsSt: Target station(2 Byte)
            //• ”P1” - ”P2”: At the time of PA stage specification
            //Note: P1 and P2 station are effective only when two or more PA stations exist.
            //When it does not exist in addition to P1 station, specification of an target station is nothing.
            //• Angle: Positioning angle(6 bytes)
            //• Relative angle from the position set by alignment calibration as the reference point.
            //• Specified in the range between "000000" and "035999"(0 to 359.99 degree.Resolution: 0.01[deg])
            //• If a value is less than specified digits, fill the higher digit with ‘0’ so that the field always has specfied digits

            //_angle is 0.001 pecent
            _angle =(int)Math.Round((double)args[0]* 100.0,2);
            return string.Format(",MALN,1,{0:D6},",_angle);
            //return string.Format(",MALN,2,{0:D6},", _angle);
        }

        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MALN,<ExeTime>,<PosData1>…,<PosDataN>,<Value1>…,<Value10>(,<Sum>)<CR>
        protected override void update(string[] data)
        {
            /*
                $ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            */

            _device.Notch = _angle;
        }
    }

    public class ALEventHandler : ITransferMsg
    {
        public bool background { get { return false; } }
        public bool evt { get { return true; } }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;

        public IDevice Robot { set { _device = (Aligner)value; } }
        protected Aligner _device;

        public ALEventHandler()
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
   

}
