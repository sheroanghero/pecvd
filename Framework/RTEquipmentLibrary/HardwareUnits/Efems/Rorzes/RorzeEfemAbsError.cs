using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public class RorzeEfemAbsError
    {
        public static Dictionary<string, string> AbsCode = new Dictionary<string, string>()
        {
            {"VAC",    "Lack of vacuum source"},
            {"AIR",    " Lack of positive source pressure"},
            {"STALL",    "   Stall"},
            {"LIMIT",    "   Limit"},
            {"EMS",    " Controller emergency stop"},
            {"COMM",    "    Internal communication error"},
            {"VACON",    "   Vacuum on error"},
            {"VACON2",    "  Vacuum pressure drops during transfer"},
            {"VACOF",    "   Vacuum OFF error"},
            {"CLAMPON",    " Clamp error (*)"},
            {"CLAMPOF",    " Clamp OFF error"},
            {"PRTWAF",    "  Wafer protrusion"},
            {"CRSWAF",    "  Cross wafer detection"},
            {"THICKWAF",    "    Wafer thickness error detection (Thick)"},
            {"THINWAF",    " Wafer thickness error detection (Thin)"},
            {"DBLWAF",    "  Double wafers detection"},
            {"BOWWAF",    "  Wafer front down detection"},
            {"COMMAND",    " Robot command error"},

            {"PODNG",    "   Carrier placement error"},
            {"VAC_S",    "   Vacuum sensor error"},
            {"UNIWIRE",    " Uniwire error"},
            {"SAFTY",    "   Pinch sensor reaction"},
            {"LOCKNG",    "  Carrier lock disabled"},
            {"UNLOCKNG",    "    Carrier unlock disabled"},
            {"L-KEY_OV",    "   Latch key over-rotation"},
            {"L-KEY_LK",    "    Latch key reverse disabled"},
            {"L-KEY_UL",    "    Latch key release disabled"},
            {"MAP_S1",    "  Mapping sensor ready disabled"},
            {"MAP_S2",    "  Mapping sensor return disabled"},
            {"NONWAF",    "  No wafer on arm"},
            {"ALIGNNG",    " Alignment failure"},
            {"ORG_NG",    "  Origin search uncompleted"},
            {"HARDWARE",    "    Hardware error"},
            {"UNSETUP",    " Setting data is not set"},
            {"SETUPDT_NG",    "  Setting data error"},
            {"CONTROLLER",    "  Controller malfunction"},
            {"SYSTEM",    "  System (unit)malfunction"},

            {"SYSTEM_FFxx",    " System (unit) malfunction"},
            {"N2",    "  Lack of N2 source pressure"},
            {"PURGE",    "   N2 purge error"},
            {"INTERNAL",    "    Internal error"},
            {"UNDEFINITION",    "    Undefined error"},
        };


        public static string GetError(string code)
        {
            if (AbsCode.ContainsKey(code))
                return AbsCode[code];

            return code;
        }
    }
 
}
