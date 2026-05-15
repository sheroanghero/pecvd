using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Equipment
{
    /// <summary>
    /// 在框架中，Module指具备放置Wafer的单元
    /// </summary>
    [DataContract]
    [Serializable]
    public enum ModuleName
    {
        [EnumMember]
        System = 0,

        [EnumMember]
        LP1,

        [EnumMember]
        LP2,

        [EnumMember]
        LP3,

        [EnumMember]
        LP4,

        [EnumMember]
        LP5,

        [EnumMember]
        LP6,

        [EnumMember]
        LP7,

        [EnumMember]
        LP8,

        [EnumMember]
        LP9,

        [EnumMember]
        LP10,

        //robot modules
        //[EnumMember]
        //Robot,

        [EnumMember]
        VaccumRobot,

        [EnumMember]
        LoadRobot,

        [EnumMember]
        EfemRobot,

        [EnumMember]
        TMRobot,

        //upenders
        [EnumMember]
        Upender,

        //efem
        [EnumMember]
        EFEM,

        //aligners
        [EnumMember]
        Aligner,

        [EnumMember]
        Aligner1,

        [EnumMember]
        Aligner2,

        [EnumMember]
        AlignerA,

        [EnumMember]
        AlignerB,

        //load locks
        [EnumMember]
        LL1,

        [EnumMember]
        LL2,

        [EnumMember]
        LL3,

        [EnumMember]
        LL4,

        [EnumMember]
        LL5,

        [EnumMember]
        LL6,

        [EnumMember]
        LL7,

        [EnumMember]
        LL8,

        //load locks
        [EnumMember]
        LLA,

        [EnumMember]
        LLB,

        [EnumMember]
        LLC,

        [EnumMember]
        LLD,

        [EnumMember]
        LLE,

        [EnumMember]
        LLF,

        [EnumMember]
        LLG,

        [EnumMember]
        LLH,

        [EnumMember]
        VCE1,

        [EnumMember]
        VCE2,

        [EnumMember]
        VCEA,

        [EnumMember]
        VCEB,

        //transfer modules
        [EnumMember]
        TM,

        [EnumMember]
        Cooling1,

        [EnumMember]
        Cooling2,
        //buffers
        [EnumMember]
        Buffer,

        [EnumMember]
        Buffer1,

        [EnumMember]
        Buffer2,

        [EnumMember]
        Buffer3,

        [EnumMember]
        Buffer4,

        [EnumMember]
        Buffer5,

        [EnumMember]
        Buffer6,
        
        //PMs

        [EnumMember]
        PM,

        [EnumMember]
        PM1,

        [EnumMember]
        PM2,

        [EnumMember]
        PM3,

        [EnumMember]
        PM4,

        [EnumMember]
        PM5,

        [EnumMember]
        PM6,

        [EnumMember]
        PM7,

        [EnumMember]
        PM8,

        [EnumMember]
        PMA,

        [EnumMember]
        PMB,

        [EnumMember]
        PMC,

        [EnumMember]
        PMD,

        //PMs
        [EnumMember]
        Spin1L,

        [EnumMember]
        Spin1H,

        [EnumMember]
        Spin2L,

        [EnumMember]
        Spin2H,

        [EnumMember]
        Spin3L,

        [EnumMember]
        Spin3H,

        [EnumMember]
        Spin4L,

        [EnumMember]
        Spin4H,

        [EnumMember]
        Spin5L,

        [EnumMember]
        Spin5H,

        [EnumMember]
        Spin6L,

        [EnumMember]
        Spin6H,

        [EnumMember]
        LDULD,
        [EnumMember]
        BufferOut,
        [EnumMember]
        BufferIn,
        [EnumMember]
        Dryer,
        [EnumMember]
        QDR,
        [EnumMember]
        Robot,
        [EnumMember]
        Handler,
        [EnumMember]
        WIDReader1,
        [EnumMember]
        WIDReader2,

        [EnumMember]
        LL1IN,

        [EnumMember]
        LL1OUT,

        [EnumMember]
        LL2IN,

        [EnumMember]
        LL2OUT,

        [EnumMember]
        TurnOverStation,

        [EnumMember]
        Host,

        [EnumMember]
        PTR,

        [EnumMember]
        Cooling,

        [EnumMember]
        Cooler,

        [EnumMember]
        Cassette,

        [EnumMember]
        CassInnerL,
        [EnumMember]
        CassInnerR,
        [EnumMember]
        CassOuterL,
        [EnumMember]
        CassOuterR,

        [EnumMember]
        CassAL,
        [EnumMember]
        CassAR,
        [EnumMember]
        CassBL,
        [EnumMember]
        CassBR,

        [EnumMember]
        Shuttle,

        [EnumMember]
        CoolingBuffer1,

        [EnumMember]
        CoolingBuffer2,

        [EnumMember]
        LoadLock,

        [EnumMember]
        SMIFLeft,

        [EnumMember]
        SMIFRight,


        [EnumMember]
        SMIFA,

        [EnumMember]
        SMIFB,

        [EnumMember]
        SPM1,

        [EnumMember]
        SPM2,

        [EnumMember]
        BRM1,

        [EnumMember]
        BRM2,

        [EnumMember]
        CCU,

        [EnumMember]
        Buffer7,

        [EnumMember]
        Buffer8,
        [EnumMember]
        Robot1,
        [EnumMember]
        Robot2,
        [EnumMember]
        CassetteRobot,
        [EnumMember]
        SignalTower,
        [EnumMember]
        TurnStation,
        [EnumMember]
        InternalCassette1,
        [EnumMember]
        InternalCassette2,
        [EnumMember]
        InternalCassette3,
        [EnumMember]
        InternalCassette4,
        [EnumMember]
        InternalCassette5,
        [EnumMember]
        InternalCassette6,
        [EnumMember]
        InternalCassette7,
        [EnumMember]
        InternalCassette8,
        [EnumMember]
        InternalCassette9,
        [EnumMember]
        InternalCassette10,

        [EnumMember]
        Prs1,

        [EnumMember]
        Prs2,

        [EnumMember]
        Prs3,

        [EnumMember]
        Prs4,
       
        [EnumMember]
        PerHeat,

        [EnumMember]
        StageA,
        [EnumMember]
        StageB,

        [EnumMember]
        Power,

        [EnumMember]
        ABD,

        [EnumMember]
        VBD,

        [EnumMember]
        Degas,
    }

    public enum ChamberType
    {
        [EnumMember]
        Clean = 0,

        [EnumMember]
        PVD = 1,

        [EnumMember]
        Degas = 2,
    }

    public static class ModuleHelper
    {
        public static bool IsTurnOverStation(ModuleName unit)
        {
            return unit == ModuleName.TurnOverStation;
        }
        public static bool IsLoadPort(ModuleName unit)
        {
            return unit == ModuleName.LP1
                   || unit == ModuleName.LP2
                   || unit == ModuleName.LP3
                   || unit == ModuleName.LP4
                   || unit == ModuleName.LP5
                   || unit == ModuleName.LP6
                   || unit == ModuleName.LP7
                   || unit == ModuleName.LP8
                   || unit == ModuleName.LP9
                   || unit == ModuleName.LP10;
        }
        public static bool IsCoolingBuffer(ModuleName unit)
        {
            return unit == ModuleName.CoolingBuffer1
                   || unit == ModuleName.CoolingBuffer2;
        }
        public static bool IsCassette(ModuleName unit)
        {
            return unit == ModuleName.CassAL
                   || unit == ModuleName.CassAR
                   || unit == ModuleName.CassBL
                   || unit == ModuleName.CassBR
                   || unit == ModuleName.Cassette;
        }

        public static bool IsBuffer(ModuleName unit)
        {
            return unit == ModuleName.Buffer
                   || unit == ModuleName.Buffer1
                   || unit == ModuleName.Buffer2
                   || unit == ModuleName.Buffer3
                   || unit == ModuleName.Buffer4
                   || unit == ModuleName.Buffer5
                   || unit == ModuleName.Buffer6
                   || unit == ModuleName.Buffer7
                   || unit == ModuleName.Buffer8
                   || unit == ModuleName.BufferIn
                   || unit == ModuleName.BufferOut;
        }
        public static bool IsPm(string unit)
        {
            return IsPm(ModuleHelper.Converter(unit));
        }
        public static bool IsPm(ModuleName unit)
        {
            return unit == ModuleName.PM1
                   || unit == ModuleName.PM2
                   || unit == ModuleName.PM3
                   || unit == ModuleName.PM4
                   || unit == ModuleName.PM5
                   || unit == ModuleName.PM6
                   || unit == ModuleName.PM7
                   || unit == ModuleName.PM8
                   || unit == ModuleName.Spin1L
                   || unit == ModuleName.Spin1H
                   || unit == ModuleName.Spin2L
                   || unit == ModuleName.Spin2H
                   || unit == ModuleName.Spin3L
                   || unit == ModuleName.Spin3H
                   || unit == ModuleName.Spin4L
                   || unit == ModuleName.Spin4H
                   || unit == ModuleName.PM
                   || unit == ModuleName.PMA
                   || unit == ModuleName.PMB
                   || unit == ModuleName.PMC
                   || unit == ModuleName.PMD
                   || unit == ModuleName.SPM1
                   || unit == ModuleName.SPM2
                   || unit == ModuleName.BRM1
                   || unit == ModuleName.BRM2
                    ;
        }
 
        public static bool IsLoadLock(string unit)
        {
            return IsLoadLock(ModuleHelper.Converter(unit));
        }

        public static bool IsLoadLock(ModuleName unit)
        {
            return unit == ModuleName.LLA
                   || unit == ModuleName.LLB
                   || unit == ModuleName.LL1
                   || unit == ModuleName.LL1IN
                   || unit == ModuleName.LL1OUT
                   || unit == ModuleName.LL2
                   || unit == ModuleName.LL2IN
                   || unit == ModuleName.LL2OUT
                   || unit == ModuleName.LL3
                   || unit == ModuleName.LL4
                   || unit == ModuleName.LL5
                   || unit == ModuleName.LL6
                   || unit == ModuleName.LL7
                   || unit == ModuleName.LL8
                   || unit == ModuleName.LLC
                   || unit == ModuleName.LLD
                   || unit == ModuleName.LoadLock;
        }

        public static bool IsCooling(string unit)
        {
            return IsCooling(ModuleHelper.Converter(unit));
        }
        public static bool IsCooling(ModuleName unit)
        {
            return unit == ModuleName.Cooling
                   || unit == ModuleName.CoolingBuffer1
                   || unit == ModuleName.CoolingBuffer2
                   || unit == ModuleName.Cooler;
        }

        public static bool IsAligner(string unit)
        {
            return IsAligner(ModuleHelper.Converter(unit));
        }
        

        public static bool IsAligner(ModuleName unit)
        {
            return unit == ModuleName.Aligner
                   || unit == ModuleName.Aligner1
                   || unit == ModuleName.Aligner2
                   || unit == ModuleName.AlignerA
                   || unit == ModuleName.AlignerB;
        }
        public static bool IsRobot(string unit)
        {
            return IsRobot(ModuleHelper.Converter(unit));
        }
        public static bool IsRobot(ModuleName unit)
        {
            return unit.ToString().Contains("Robot");
        }

        public static bool IsEfemRobot(string unit)
        {
            return IsEfemRobot(ModuleHelper.Converter(unit));
        }

        public static bool IsEfemRobot(ModuleName unit)
        {
            return unit == ModuleName.EfemRobot;
        }

        public static bool IsTMRobot(string unit)
        {
            return IsTMRobot(ModuleHelper.Converter(unit));
        }

        public static bool IsTMRobot(ModuleName unit)
        {
            return unit == ModuleName.TMRobot;
        }
        public static bool IsTurnStation(ModuleName unit)
        {
            return unit == ModuleName.TurnStation;
        }

        public static bool IsVCE(string unit)
        {
            return IsVCE(ModuleHelper.Converter(unit));
        }

        public static bool IsVCE(ModuleName unit)
        {
            return unit == ModuleName.VCE1|| unit == ModuleName.VCE2|| unit == ModuleName.VCEA|| unit == ModuleName.VCEB;
        }
        public static bool IsWIDReader(ModuleName unit)
        {
            switch(unit)
            {
                case ModuleName.WIDReader1:
                case ModuleName.WIDReader2:
                    return true;
                default:
                    return false;
            }
        }

        public static string GetAbbr(ModuleName module)
        {
            switch (module)
            {
                case ModuleName.Aligner: return "PA";
                case ModuleName.Robot: return "RB";
                default: return module.ToString();
            }
        }


        public static string GetE84LpName(string device)
        {
            string e84 = string.Empty;
            switch(device)
            {
                case "P1":
                case "LP1":
                    e84 = "Loadport1E84";
                    break;
                case "P2":
                case "LP2":
                    e84 = "Loadport2E84";
                    break;
                case "P3":
                case "LP3":
                    e84 = "Loadport3E84";
                    break;
                case "P4":
                case "LP4":
                    e84 = "Loadport4E84";
                    break;
                case "P5":
                case "LP5":
                    e84 = "Loadport5E84";
                    break;
                case "P6":
                case "LP6":
                    e84 = "Loadport6E84";
                    break;
                case "P7":
                case "LP7":
                    e84 = "Loadport7E84";
                    break;
                case "P8":
                case "LP8":
                    e84 = "Loadport8E84";
                    break;
                case "P9":
                case "LP9":
                    e84 = "Loadport9E84";
                    break;
                case "P10":
                case "LP10":
                    e84 = "Loadport10E84";
                    break;
            }

            return e84;
        }
        
        public static ModuleName Converter(string module)
        {
            return (ModuleName) Enum.Parse(typeof(ModuleName), module);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">0 based</param>
        /// <returns></returns>
        public static ModuleName GetLoadPort(int index)
        {
            ModuleName[] lps = new ModuleName[]
            {
                ModuleName.LP1, ModuleName.LP2, ModuleName.LP3, ModuleName.LP4, ModuleName.LP5,
                ModuleName.LP6, ModuleName.LP7, ModuleName.LP8, ModuleName.LP9, ModuleName.LP10,
            };
            return lps[index];
        }
    }

    public class ModuleNameString
    {
        public const string System = "System";

        public const string LDULD = "LDULD";
        public const string BufferOut = "BufferOut";
        public const string BufferIn = "BufferIn";
        public const string Dryer = "Dryer";
        public const string QDR = "QDR";
        public const string Robot = "Robot";
        public const string Handler = "Handler";

    }
}
