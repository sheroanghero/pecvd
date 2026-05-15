using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{
    public enum Unit
    {
        Robot = 1,
        Aligner = 2,
    }

    public enum EventType
    {
        Error = 100,
        Aligner = 140,
    }

    public enum SpeedType
    {
        H,             //No-wafer transfer speed,
        M,             //With-wafer transfer speed
        L,             //Low speed
        O,             //Home speed
        B,             //Speed in low-speed-area
    }

    public enum StateBit
    {
        LowBattery = 0x10,          //1: Low battery voltage, 0: Normal state
        Ready  = 0x20,              //1: Ready, 0: Busy
        ServorOff = 0x40,           //1: Servo OFF, 0: Servo ON
        Error = 0x80,               //Error occurrence, 0: No error occurrence
        CtrlLowBattery =0x01,       //1: Low battery voltage, 0: Normal state

        WaferOnBlade1 = 0x02,       //(1: Has wafer, 0: No wafer)
        WaferOnBlade2 = 0x04,       //(1: Has wafer, 0: No wafer)
        WaferOnBlade3 = 0x08,       //(1: Has wafer, 0: No wafer)
        WaferOnBlade4 = 0x10,       //(1: Has wafer, 0: No wafer)

        WaferOnGrip = 0x02,      //
        WaferOnCCD  = 0x04,      //
    }



    public class ProtocolTag
    {
        public const string tag_end = "\r";
        public const string tag_cmd_start = "$";
        public const string cmd_token = ",";

        public const string resp_tag_normal = "$";
        public const string resp_tag_error = "?";
        public const string resp_tag_excute = "!";
        public const string resp_tag_event = ">";


        public const string resp_evt_error = "100";
    }


}
