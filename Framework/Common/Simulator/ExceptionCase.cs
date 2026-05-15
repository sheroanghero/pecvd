using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Simulator
{
    [Serializable]
    public static  class ExceptionCase
    {
        ///<summary>
        /// 
        /// 系统相关
        /// 
        /// 
        /// </summary>
        //
        [Display(Description = "Maintenance", GroupName = "System")]
        public static bool ExMaintenance{ get; set; }

        [Display(Description = " MaintenanceDoorOpen", GroupName = "System")]
        public static bool ExMaintenanceDoorOpen{ get; set; }

        [Display(Description = "DI_RobotFork1WaferOn", GroupName = "System")]
        public static bool ExRobotFork1WaferOn{ get; set; }

        [Display(Description = "DI_RobotFork2WaferOn", GroupName = "System")]
        public static bool ExRobotFork2WaferOn{ get; set; }

        [Display(Description = "DI_PreAlignerWaferOn", GroupName = "System")]
        public static bool ExPreAlignerWaferOn{ get; set; }

        [Display(Description = "DI_RobotReady", GroupName = "System")]
        public static bool ExRobotReady{ get; set; }

        [Display(Description = "DI_PreAlignerReady", GroupName = "System")]
        public static bool ExPreAlignerReady{ get; set; }

        [Display(Description = "DI_RobotError", GroupName = "System")]
        public static bool ExRobotError{ get; set; }

        [Display(Description = "DI_PreAlignerError", GroupName = "System")]
        public static bool ExPreAlignerError{ get; set; }

        [Display(Description = "DI_TeachingPendantInUse", GroupName = "System")]
        public static bool ExTeachingPendantInUse{ get; set; }

        [Display(Description = "DI_Loadport1OperationalStatus", GroupName = "System")]
        public static bool ExLoadport1OperationalStatus{ get; set; }


        [Display(Description = "DI_Loadport2OperationalStatus", GroupName = "System")]
        public static bool ExLoadport2OperationalStatus{ get; set; }

        [Display(Description = "DI_IonizorError", GroupName = "System")]
        public static bool ExIonizorError{ get; set; }

        [Display(Description = "DI_FFUError", GroupName = "System")]
        public static bool ExFFUError{ get; set; }

        public const string AirPressureErrorForIonizer = "AirPressureErrorForIonizer";
        public const string AirPressureErrorForLoadport = "AirPressureErrorForLoadport";
        public const string AirPressureErrorForRobot = "AirPressureErrorForRobot";
        public const string AirPressureErrorForPA = "AirPressureErrorForPA";
        public const string VaccumErrorForPreAligner = "VaccumErrorForPreAligner";
        public const string LP2MappingError = "LP2MappingError";
        public const string VaccumErrorForLoadport = "VaccumErrorForLoadport";

        [Display(Description = AirPressureErrorForRobot, GroupName = "System")]
        public static bool Ex2MainAirErrorForRobot { get; set; }

        [Display(Description = AirPressureErrorForLoadport, GroupName = "System")]
        public static bool Ex2MainAirErrorForLoadport { get; set; }

        [Display(Description = AirPressureErrorForIonizer, GroupName = "System")]
        public static bool ExMainAirPressureErrorForIonizer { get; set; }

        [Display(Description = AirPressureErrorForPA, GroupName = "System")]
        public static bool ExMainAirPressureErrorForPA { get; set; }

        [Display(Description = VaccumErrorForPreAligner, GroupName = "System")]
        public static bool ExMainVaccumErrorForPreAligner { get; set; }


        [Display(Description = "DI_MainAirErrorForRobot", GroupName = "System")]
        public static bool ExMainAirErrorForRobot { get; set; }

        [Display(Description = "DI_MainAirErrorForIonizor", GroupName = "System")]
        public static bool ExMainAirErrorForIonizor { get; set; }

        [Display(Description = "DI_MainAirErrorForLoadport", GroupName = "System")]
        public static bool ExMainAirErrorForLoadport { get; set; }


        [Display(Description = "DI_VaccumError", GroupName = "System")]
        public static bool ExVaccumError{ get; set; }

 



 
    }
}
