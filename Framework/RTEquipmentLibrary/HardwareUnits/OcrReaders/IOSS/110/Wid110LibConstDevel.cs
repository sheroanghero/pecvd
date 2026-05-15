using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.IOSS
{
    static class Wid110LibConstDev
    {
        // ====================================================================
        /// <summary>
        /// Constants that should be used overall WID119 library software.
        /// </summary>
        // ====================================================================


        // local system resources
        //
#if SOUNDCHECK
        public static string sndChk = "C:\\windows\\system32\\ALSNDMGR.WAV";
#endif


#if LOAD_DLL
#if DEBUG
        public static string dllPath  = ".\\lib_dbg";
#else
        public static string dllPath  = ".\\lib_rel";
#endif
        public static string dllName  = "wid110Lib_clr.dll";
        public static string dllClass = "CWID110Dll_clr";
#endif


        // connect and log button text
        //
        public static string cbtnConn = "Connect";
        public static string cbtnCong = "Connecting";
        public static string cbtnDisc = "Disconnect";

        public static string lbtnLog = "Log";
        public static string lbtnNoLog = "no Log";


        // code names
        //
        public static string codeOCR = "OCR";
        public static string codeBCR = "BCR";
        public static string codeDMR = "DMR";
        public static string codeLAST = "LAST";


        // color names
        //
        public static string colRED = "RED";
        public static string colGREEN = "GREEN";
        public static string colBLUE = "BLUE";


        // channel names
        //
        public static string chanBRIGHT = "BRIGHTFIELD";
        public static string chanFOCUS = "FOCUSED";
        public static string chanINNER = "INNER ROW";
        public static string chanOUTER = "OUTER ROW";
        public static string chanALL = "ALL ROWS";


        // image type selectors
        //
        public static int imgBEST = 0;
        public static int imgALL = 1;
    }
}
