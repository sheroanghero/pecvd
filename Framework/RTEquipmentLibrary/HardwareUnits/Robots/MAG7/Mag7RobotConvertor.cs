using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class ProtocolTag
    {
        public const string tag_end = "\r";

        public const string cmd_token = " ";

        public const string resp_tag_error = "_ERR";
        public const string resp_tag_excute = "_RDY";
        public const string resp_tag_event = "_EVENT";

        public const string resp_tage_alignerError = "ALGN";

        public const string resp_evt_error = "100";
    }

    public interface IMag7RobotConverter
    {
        string MapModuleSlot(ModuleName chamber, int slot);
        string MapModule(ModuleName chamber);
        string hand2string(Hand hand);
    }

    public class DefaultMag7RobotConverter : IMag7RobotConverter
    {
        public string MapModuleSlot(ModuleName chamber, int slot)
        {
            string st = "";
            switch (chamber)
            {

                case ModuleName.LL1:
                case ModuleName.LLA:
                    st = "2 SLOT 1";
                    break;
                case ModuleName.LL2:
                case ModuleName.LLB:
                    st = "1 SLOT 1";
                    break;

                case ModuleName.LL1IN:
                    st = "2 SLOT 1";
                    break;
                case ModuleName.LL1OUT:
                    st = "2 SLOT 1";
                    break;
                case ModuleName.LL2IN:
                    st = "1 SLOT 1";
                    break;
                case ModuleName.LL2OUT:
                    st = "1 SLOT 1";
                    break;
                case ModuleName.PM1:
                case ModuleName.PMA:
                    st = slot == 0 ? "3 SLOT 1" : "3 SLOT 2";
                    break;
                case ModuleName.PM2:
                case ModuleName.PMB:
                    st = "4 SLOT 1";
                    break;
                case ModuleName.PM3:
                case ModuleName.PMC:
                    st = "5 SLOT 1";
                    break;
                case ModuleName.PM4:
                case ModuleName.PMD:
                    st = "6 SLOT 1";
                    break;
                case ModuleName.PM5:
                    st = "7 SLOT 1";
                    break;
                case ModuleName.PM6:
                    st = "8 SLOT 1";
                    break;
                case ModuleName.VCEA:
                    st = "2 SLOT 1";
                    break;
                case ModuleName.VCEB:
                    st = "1 SLOT 1";
                    break;

            }

            return st;
        }

        public string MapModule(ModuleName chamber)
        {
            string st = "";
            switch (chamber)
            {

                case ModuleName.LL1:
                case ModuleName.LLA:
                    st = "2";
                    break;
                case ModuleName.LL2:
                case ModuleName.LLB:
                    st = "1";
                    break;

                case ModuleName.LL1IN:
                    st = "2";
                    break;
                case ModuleName.LL1OUT:
                    st = "2";
                    break;
                case ModuleName.LL2IN:
                    st = "1";
                    break;
                case ModuleName.LL2OUT:
                    st = "1";
                    break;
                case ModuleName.PM1:
                case ModuleName.PMA:
                    st = "3";
                    break;
                case ModuleName.PM2:
                case ModuleName.PMB:
                    st = "4";
                    break;
                case ModuleName.PM3:
                case ModuleName.PMC:
                    st = "5";
                    break;
                case ModuleName.PM4:
                case ModuleName.PMD:
                    st = "6";
                    break;
                case ModuleName.PM5:
                    st = "7";
                    break;
                case ModuleName.PM6:
                    st = "8";
                    break;
                case ModuleName.VCEA:
                    st = "2";
                    break;
                case ModuleName.VCEB:
                    st = "1";
                    break;
            }

            return st;
        }

        public string hand2string(Hand hand)
        {
            string st = "";
            switch (hand)
            {
                case Hand.Blade1:
                    st = "A";
                    break;
                case Hand.Blade2:
                    st = "B";
                    break;
                case Hand.Both:
                    st = "AB";
                    break;
            }

            return st;
        }
    }

    public class Mag7RobotConvertor
    {
        private static IMag7RobotConverter _converter = new DefaultMag7RobotConverter();
        public static IMag7RobotConverter Converter
        {
            get { return _converter; }
            set { _converter = value; }
        }

        public static string MapModuleSlot(ModuleName chamber, int slot)
        {
            return Converter.MapModuleSlot(chamber, slot);
        }

        public static string MapModule(ModuleName chamber)
        {
            return Converter.MapModule(chamber);
        }

        public static string hand2string(Hand hand)
        {
            return Converter.hand2string(hand);
        }

    }
}
