﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Common;
using Aitex.Core.RT.Log;
 using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100
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
            {ModuleName.LP1, "C1"},
            {ModuleName.LP2, "C2"},
            {ModuleName.LP3, "C3"},
            {ModuleName.LP4, "C4"},
            {ModuleName.LP5, "C5"},
            {ModuleName.LP6, "C6"},
            {ModuleName.LP7, "C7"},
            {ModuleName.LP8, "C8"},
            {ModuleName.LP9, "C9"},
            {ModuleName.LP10, "CA"},

            {ModuleName.Aligner, "P1"},

            {ModuleName.Buffer, "C5"},

            {ModuleName.TurnOverStation, "S1"},

            {ModuleName.PM1, "S1"},
            {ModuleName.PM2, "S2"},
            {ModuleName.PM3, "S3"},
            {ModuleName.PM4, "S4"},
            {ModuleName.PM5, "S5"},
            {ModuleName.PM6, "S6"},
            {ModuleName.PM7, "S7"},
            {ModuleName.PM8, "S8"},

            {ModuleName.Spin1L, "S1"},
            {ModuleName.Spin1H, "S2"},
            {ModuleName.Spin2L, "S3"},
            {ModuleName.Spin2H, "S4"},
            {ModuleName.Spin3L, "S5"},
            {ModuleName.Spin3H, "S6"},
            {ModuleName.Spin4L, "S7"},
            {ModuleName.Spin4H, "S8"},

            {ModuleName.LL1, "S1"},
            {ModuleName.LL2, "SA"},
            {ModuleName.LL3, "SB"},
            {ModuleName.LL4, "SC"},
        };

 
        public static string chamber2staion(ModuleName chamber)
        {
            if (!TeachStation.ContainsKey(chamber))
            {
                LOG.Error($"not define teach position {chamber}");
                return "";
            }
            if (chamber == ModuleName.Aligner && SC.ContainsItem("Aligner.AlignerStation"))
                return SC.GetStringValue("Aligner.AlignerStation");
            int index = SC.ContainsItem($"CarrierInfo.{chamber.ToString()}CarrierIndex") ?
                SC.GetValue<int>($"CarrierInfo.{chamber.ToString()}CarrierIndex") : -1; 
            if (SC.ContainsItem($"CarrierInfo.{chamber.ToString()}Station{index}"))
                return SC.GetStringValue($"CarrierInfo.{chamber.ToString()}Station{index}");
            return TeachStation[chamber];


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
            if (sslot == "SlotPlusOne")
            {
                sslot = string.Format("{0:D2}", slot + 1);
            }
            return sslot;
        }

        public static string hand2string(Hand hand)
        {
            string st = "";
            if (SC.ContainsItem("Robot.BladeUpDownReverse") && SC.GetValue<bool>("Robot.BladeUpDownReverse"))
            {
                switch (hand)
                {
                    case Hand.Blade1:
                        st = "A";      //单手臂 下
                        break;
                    case Hand.Blade2:
                        st = "B";     //4手臂  上
                        break;
                    case Hand.Both:
                        st = "W";
                        break;
                }
            }
            else
            {
                switch (hand)
                {
                    case Hand.Blade1:
                        st = "B";      //单手臂 下
                        break;
                    case Hand.Blade2:
                        st = "A";     //4手臂  上
                        break;
                    case Hand.Both:
                        st = "W";
                        break;
                }
            }
            return st;
        }


        public static string NextMotion2String(Motion motion,Hand hand)
        {
            string st = "";
            switch (motion)
            {
                case Motion.Pick:
                    st = string.Format("G{0}", hand2string(hand));
                    break;
                case Motion.Place:
                    st = string.Format("P{0}", hand2string(hand));
                    break;
                case Motion.Exchange:
                    st = string.Format("E{0}", hand2string(hand));
                    break;
                default:
                    st = "AL";
                    break;
            }

            return st;
        }

        public static string Offset2String(int offset)
        {
            string st = "";

            if (offset >= 0)
            {
                st = string.Format("{0:D5}", offset);
            }
            else
            {
                st = string.Format("{0:D4}", offset);
            }

            return st;
        }



        public static int  WaferSize2Int(WaferSize size)
        {
            int ret = 0;
            switch (size)
            {
                case WaferSize.WS3:
                    ret = 625000;  //
                    break;
                case WaferSize.WS4:
                    ret = 750000;  //
                    break;
                case WaferSize.WS6:
                    ret = 1000000;  //
                    break;
            }

            return ret;
        }

        public static int MappingOffset2Int(WaferSize size)
        {
            int ret = 0;
            switch (size)
            {
                case WaferSize.WS3:
                    ret = 2357000;  //
                    break;
                case WaferSize.WS4:
                    ret = 2452000;  //
                    break;
                case WaferSize.WS6:
                    ret = 2732000;  //
                    break;
            }

            return ret;
        }
    }
}
