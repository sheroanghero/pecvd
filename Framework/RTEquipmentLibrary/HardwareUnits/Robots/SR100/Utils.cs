using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.SR100
{
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

    public class RobotConvertor
    {
        public static Dictionary<ModuleName, string> TeachStation = new Dictionary<ModuleName, string>()
        {
            {ModuleName.LP1, "C01"},
            {ModuleName.LP2, "C02"},
            {ModuleName.LP3, "C03"},
            {ModuleName.LP4, "C04"},
            {ModuleName.LP5, "C05"},
            {ModuleName.LP6, "C06"},
            {ModuleName.LP7, "C07"},
            {ModuleName.LP8, "C08"},

            {ModuleName.Aligner, "P01"},

            {ModuleName.Buffer, "C05"},

            {ModuleName.TurnOverStation, "S01"},

            {ModuleName.PM1, "S01"},
            {ModuleName.PM2, "S02"},
            {ModuleName.PM3, "S03"},
            {ModuleName.PM4, "S04"},
            {ModuleName.PM5, "S05"},
            {ModuleName.PM6, "S06"},
            {ModuleName.PM7, "S07"},
            {ModuleName.PM8, "S08"},

            {ModuleName.Spin1L, "S01"},
            {ModuleName.Spin1H, "S02"},
            {ModuleName.Spin2L, "S03"},
            {ModuleName.Spin2H, "S04"},
            {ModuleName.Spin3L, "S05"},
            {ModuleName.Spin3H, "S06"},
            {ModuleName.Spin4L, "S07"},
            {ModuleName.Spin4H, "S08"},

            {ModuleName.LL1, "S01"},
            {ModuleName.LL2, "S02"},
            {ModuleName.LL3, "S03"},
            {ModuleName.LL4, "S04"},
        };

        public static string chamber2staion(ModuleName chamber)
        {
            
            if (!TeachStation.ContainsKey(chamber))
            {
                LOG.Error($"not define teach position {chamber}");
                return "";
            }
            string ret =TeachStation[chamber];
            if(chamber == ModuleName.Aligner && SC.ContainsItem("Aligner.AlignerStation"))
                return  SC.GetStringValue("Aligner.AlignerStation");            

            if (SC.ContainsItem($"CarrierInfo.{chamber}CarrierIndex"))
            {
                int idex = SC.GetValue<int>($"CarrierInfo.{chamber}CarrierIndex");
                if(SC.ContainsItem($"CarrierInfo.{chamber}Station{idex}"))
                {
                    ret = SC.GetStringValue($"CarrierInfo.{chamber}Station{idex}");
                }
            }

            return ret;
        }

        public static Dictionary<ModuleName, string> Slot2Slot = new Dictionary<ModuleName, string>()
        {
            {ModuleName.LP1, "SlotPlusOne"},
            {ModuleName.LP2, "SlotPlusOne"},
            {ModuleName.LP3, "SlotPlusOne"},
            {ModuleName.LP4, "SlotPlusOne"},
            {ModuleName.LP5, "SlotPlusOne"},
            {ModuleName.LP6, "SlotPlusOne"},
            {ModuleName.LP7, "SlotPlusOne"},
            {ModuleName.LP8, "SlotPlusOne"},
            {ModuleName.LP9, "SlotPlusOne"},
            {ModuleName.LP10, "SlotPlusOne"},
            {ModuleName.Buffer, "SlotPlusOne"},

            {ModuleName.Aligner, "00"},
            {ModuleName.Robot, "00"},
            {ModuleName.PM1, "00"},
            {ModuleName.PM2, "00"},
            {ModuleName.PM3, "00"},
            {ModuleName.PM4, "00"},
            {ModuleName.PM5, "00"},
            {ModuleName.PM6, "00"},
            {ModuleName.PM7, "00"},
            {ModuleName.PM8, "00"},
            {ModuleName.Spin1L, "00"},
            {ModuleName.Spin1H, "00"},
            {ModuleName.Spin2L, "00"},
            {ModuleName.Spin2H, "00"},
            {ModuleName.Spin3L, "00"},
            {ModuleName.Spin3H, "00"},
            {ModuleName.Spin4L, "00"},
            {ModuleName.Spin4H, "00"},
            {ModuleName.LL1, "00"},
            {ModuleName.LL2, "00"},
            {ModuleName.LL3, "00"},
            {ModuleName.LL4, "00"},
            {ModuleName.LL5, "00"},
            {ModuleName.LL6, "00"},
            {ModuleName.LL7, "00"},
            {ModuleName.LL8, "00"},
            {ModuleName.LLA, "00"},
            {ModuleName.LLB, "00"},
            {ModuleName.LLC, "00"},
            {ModuleName.LLD, "00"},
            {ModuleName.TurnOverStation, "00"},
        };

        public static string chamberSlot2Slot(ModuleName chamber, int slot)
        {
            if (!Slot2Slot.ContainsKey(chamber))
            {
                LOG.Error($"not define slot2slot config {chamber}");
                return "";
            }

            string sslot = Slot2Slot[chamber];
            if(sslot == "SlotPlusOne")
            {
                sslot = string.Format("{0:D2}", slot + 1);
            }
            return sslot;            
        }

        public static string hand2string(Hand hand)
        {
            string st = "";
            switch (hand)
            {
                case Hand.Blade1:
                    st = "1";
                    break;
                case Hand.Blade2:
                    st = "2";
                    break;
                case Hand.Both:
                    st = "F";
                    break;
            }

            return st;
        }

        public static string AxisToStr(Axis axis)
        {
            string st = "";
            switch (axis)
            {
                case Axis.S:
                    st = "S";
                    break;
                case Axis.A:
                    st = "A";
                    break;
                case Axis.H:
                    st = "H";
                    break;
                case Axis.I:
                    st = "I";
                    break;
                case Axis.Z:
                    st = "Z";
                    break;
                default:
                    break;
            }
            return st;
        }

        public static string IsPick2Position(bool isPick)
        {
            var pos = isPick ? "P1" : "G1";
            return pos;
        }
        
        public static string Offset2String(int offset)
        {
            string st = "";

            if (offset >= 0)
            {
                st = string.Format("{0:D8}", offset);
            }
            else
            {
                st = string.Format("{0:D7}", offset);
            }

            return st;
        }

    }
}
