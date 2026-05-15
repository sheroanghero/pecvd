using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.SR100.AL
{
    public class AlignerHandler : ITransferMsg
    {
        public int LastErrorCode { get; set; }
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
                _device.ElapseTime = int.Parse(items[6]);

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
        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        public override string package(params object[] args)
        {
            return ",CCLR,E,";
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
            if(SC.ContainsItem("Aligner.AlignerType") && SC.GetValue<int>("Aligner.AlignerType") == 1 )
            {
                return ",INIT,1,1,N,";
            }
            else return ",INIT,1,1,G,";
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
        //$,<UNo>(,<SeqNo>),CSOL,<Sol>,<Sw>,<Wait>(,<Sum>)<CR> 
        //sol Solenoid control specification (1 byte) • ‘1’ : Blade 1. • ‘2’ : Blade 2. • ‘F’ : Blade 1 + Blade 2. 
        // Solenoid command (1 byte)  • ‘0’ : Wafer release. / Lifter down. • ‘1’ : Wafer hold. / Lifter up. 
        public override string package(params object[] args)
        {

            // bool bHold = (bool)args[0];
            bool bHold = (bool)args[1];
            if (bHold)
                return ",CSOL,1,1,0,";

            return ",CSOL,1,0,0,";
        }
    }

    public class ALLiftHandler : AlignerHandler
    {
        public ALLiftHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CSOL,<Sol>,<Sw>,<Wait>(,<Sum>)<CR> 
        //sol Solenoid control specification (1 byte) • ‘1’ : Blade 1. • ‘2’ : Blade 2. • ‘F’ : Blade 1 + Blade 2. 
        // Solenoid command (1 byte)  • ‘0’ : Wafer release. / Lifter down. • ‘1’ : Wafer hold. / Lifter up. 
        public override string package(params object[] args)
        {

            bool bUp = (bool)args[0];
            if (bUp)
                return ",CSOL,2,1,0,";

            return ",CSOL,2,0,0,";
        }
    }

    public class AlStopHandler : AlignerHandler
    {
        public AlStopHandler()
        {
            background = true;
        }
        //$,<UNo>(,<SeqNo>),CSTP,<Sw>(,<Sum>)<CR>
        //• ‘H’ : Deceleration to a stop.
        //• ‘E’ : Emergency stop.
        public override string package(params object[] args)
        {
            return ",CSTP,H,";
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
        public AlAlignHandler()
        {
            background = true;
        }
        // $,<UNo>(,<SeqNo>),MALN,<Mode>,<Angle>(,<Sum>)<CR>
        //Mode : Motion mode (1 byte) If the case of edge grip type pre-Aligner, specify ‘0’.
        public override string package(params object[] args)
        {
            int angle = (int)Math.Round((double)args[0] * 1000.0, 2);
            return string.Format(",MALN,0,{0:D8},", angle);
        }

        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MALN,<ExeTime>,<PosData1>…,<PosDataN>,<Value1>…,<Value10>(,<Sum>)<CR>
        protected override void update(string[] data)
        {
            /*
            • Value1 : Wafer eccentric amount before alignment operation (8 bytes, Resolution: 0.001 [mm])
            • Value2 : Wafer eccentric angle direction before alignment operation (8 bytes, Resolution: 0.001 [deg])
            • Value3 : Notch/Orientation Flat direction before alignment operation (8 bytes, Resolution: 0.001 [deg])
            • Value4 : X direction offset amount before alignment operation (8 bytes, Resolution: 0.001 [mm])
            • Value5 : Y direction offset amount before alignment operation (8 bytes, Resolution: 0.001 [mm])
            • Value6 : Pre-aligner adjustment angle (8 bytes, Resolution: 0.001 [deg])
            • Value7 : Manipulator adjustment amount (8 bytes, Resolution: 0.001 [mm])
            • Value8 : Manipulator adjustment angle (8 bytes, Resolution: 0.001 [deg])
            • Value9 : X direction offset amount after alignment operation (8 bytes, Resolution: 0.001 [mm])
            • Value10 : Y direction offset amount after alignment operation (8 bytes, Resolution: 0.001 [mm])
             * //value index is 9
            */

            _device.Notch = int.Parse(data[11]);
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
