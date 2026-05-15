using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.FAServices
{
    /// <summary>
    /// Gem300的事件
    /// </summary>
    public static class Gem300Events
    {
        public const string LPT_NtoO = "LPT_NtoO";
        public const string LPT_NtoI = "LPT_NtoI";
        public const string LPT_OtoI = "LPT_OtoI";
        public const string LPT_ItoO = "LPT_ItoO";
        public const string LPT_ItoTR = "LPT_ItoTR";
        public const string LPT_ItoTB = "LPT_ItoTB";
        public const string LPT_TRtoRTL = "LPT_TRtoRTL";
        public const string LPT_TRtoRTU = "LPT_TRtoRTU";
        public const string LPT_RTLtoTB = "LPT_RTLtoTB";
        public const string LPT_RTUtoTB = "LPT_RTUtoTB";
        public const string LPT_TBtoRTL = "LPT_TBtoRTL";
        public const string LPT_TBtoRTU = "LPT_TBtoRTU";
        public const string LPT_TBtoTR = "LPT_TBtoTR";

        public const string CID_NtoWFH = "CID_NtoWFH";
        public const string CID_NtoIVO = "CID_NtoIVO";
        public const string CID_NtoIVF = "CID_NtoIVF";
        public const string CID_WFHtoIVO = "CID_WFHtoIVO";
        public const string CID_WFHtoIVF = "CID_WFHtoIVF";
        public const string CID_DELETED = "CID_DELETED";
        public const string CID_NtoINR = "CID_NtoINR";
        public const string CID_INRtoIVO_6 = "CID_INRtoIVO_6";
        public const string CID_INRtoIVO_11 = "CID_INRtoIVO_11";
        public const string CID_INRtoWFH_7 = "CID_INRtoWFH_7";
        public const string CID_INRtoWFH_10 = "CID_INRtoWFH_10";

        public const string CSM_SMNRtoWFH = "CSM_SMNRtoWFH";
        public const string CSM_SMNRtoSMVO = "CSM_SMNRtoSMVO";
        public const string CSM_WFHtoSMVO = "CSM_WFHtoSMVO";
        public const string CSM_WFHtoSMVF = "CSM_WFHtoSMVF";
        public const string CSM_SMNRtoSMVF = "CSM_SMNRtoSMVF";

        public const string CA_NAtoIA = "CA_NAtoIA";
        public const string CA_IAtoCC = "CA_IAtoCC";
        public const string CA_IAtoCS = "CA_IAtoCS";
        public const string CA_NAtoCS = "CA_NAtoCS";
        public const string CA_NtoNA = "CA_NtoNA";

        public const string LPR_NRtoR = "LPR_NRtoR";
        public const string LPR_RtoNR = "LPR_RtoNR";

        public const string LPCA_NAtoA = "LPCA_NAtoA";
        public const string LPCA_AtoNA = "LPCA_AtoNA";
        public const string LPCA_AtoA = "LPCA_AtoA";

        public const string AM_NtoA = "AM_NtoA";
        public const string AM_NtoM = "AM_NtoM";
        public const string AM_MtoA = "AM_MtoA";
        public const string AM_AtoM = "AM_AtoM";

        public const string CR_OPN = "CR_OPN";
        public const string CR_CLS = "CR_CLS";
        public const string CLCHG = "CLCHG";
        public const string CCLMP = "CCLMP";
        public const string CUCLMP = "CUCLMP";
        public const string CDOCK = "CDOCK";
        public const string CUDOCK = "CUDOCK";
        public const string CID_WRITE = "CID_WRITE";
        public const string CID_WRITE_FAIL = "CID_WRITE_FAIL";
        public const string CR_LOADED = "CR_LOADED";
        public const string CR_WAIT_FOR_UNLOAD = "CR_WAIT_FOR_UNLOAD";

        public const string CJ_CREATED = "CJ_CREATED";
        public const string CJ_CANCELED = "CJ_CANCELED";
        public const string CJ_SELECTED = "CJ_SELECTED";
        public const string CJ_DESELECT = "CJ_DESELECT";
        public const string CJ_EXECUTING_5 = "CJ_EXECUTING_5";
        public const string CJ_EXECUTING_7 = "CJ_EXECUTING_7";
        public const string CJ_WAITINGFORSTART = "CJ_WAITINGFORSTART";
        public const string CJ_PAUSED = "CJ_PAUSED";
        public const string CJ_RESUMED = "CJ_RESUMED";
        public const string CJ_COMPLETED = "CJ_COMPLETED";
        public const string CJ_STOPPED = "CJ_STOPPED";
        public const string CJ_ABORTED = "CJ_ABORTED";
        public const string CJ_DELETED = "CJ_DELETED";
        public const string PJ_CREATED = "PJ_CREATED";
        public const string PJ_SETTINGUP = "PJ_SETTINGUP";
        public const string PJ_WAITINGFORSTART = "PJ_WAITINGFORSTART";

        public const string PJ_PROCESSING_4 = "PJ_PROCESSING_4";
        public const string PJ_PROCESSING_5 = "PJ_PROCESSING_5";
        public const string PJ_PRCOMPLETED = "PJ_PRCOMPLETED";
        public const string PJ_COMPLETED = "PJ_COMPLETED";
        public const string PJ_PAUSING = "PJ_PAUSING";
        public const string PJ_PAUSED = "PJ_PAUSED";
        public const string PJ_RESUMED = "PJ_RESUMED";
        public const string PJ_STOPPING_11 = "PJ_STOPPING_11";
        public const string PJ_STOPPING_12 = "PJ_STOPPING_12";
        public const string PJ_STOPPED = "PJ_STOPPED";
        public const string PJ_ABORTING_13 = "PJ_ABORTING_13";
        public const string PJ_ABORTING_14 = "PJ_ABORTING_14";
        public const string PJ_ABORTING_15 = "PJ_ABORTING_15";
        public const string PJ_ABORTED = "PJ_ABORTED";
        public const string PJ_CANCELD = "PJ_CANCELD";

        public const string STS_ATSOURCE_1 = "STS_ATSOURCE_1";
        public const string STS_ATSOURCE_3 = "STS_ATSOURCE_3";
        public const string STS_ATSOURCE_8 = "STS_ATSOURCE_8";
        public const string STS_ATWORK_2 = "STS_ATWORK_2";
        public const string STS_ATWORK_4 = "STS_ATWORK_4";
        public const string STS_ATWORK_6 = "STS_ATWORK_6";
        public const string STS_ATDESTINATION = "STS_ATDESTINATION";
        public const string STS_NEEDSPROCESSING_10 = "STS_NEEDSPROCESSING_10";
        public const string STS_NEEDSPROCESSING_13 = "STS_NEEDSPROCESSING_13";
        public const string STS_INPROCESS = "STS_INPROCESS";
        public const string STS_PROCESSED = "STS_PROCESSED";
        public const string STS_ABORTED = "STS_ABORTED";
        public const string STS_STOPPED = "STS_STOPPED";
        public const string STS_REJECTED = "STS_REJECTED";
        public const string STS_LOST_12 = "STS_LOST_12";
        public const string STS_LOST_14 = "STS_LOST_14";
        public const string STS_SKIPPED_12 = "STS_SKIPPED_12";
        public const string STS_SKIPPED_14 = "STS_SKIPPED_14";
        public const string STS_DELETED_7 = "STS_DELETED_7";
        public const string STS_DELETED_9 = "STS_DELETED_9";

        public const string STS_OCCUPIED_1 = "STS_OCCUPIED_1";
        public const string STS_UNOCCUPIED_2 = "STS_UNOCCUPIED_2";
        public const string STS_OCCUPIED_3 = "STS_OCCUPIED_3";
        public const string STS_UNOCCUPIED_4 = "STS_UNOCCUPIED_4";
        public const string STS_NOSTATE_5 = "STS_NOSTATE_5";
        public const string STS_NOSTATE_6 = "STS_NOSTATE_6";

        /// <summary>
        /// 定义Gem300事件的ID
        /// </summary>
        public enum EventName
        {
            LPT_NtoO = 30200,
            LPT_NtoI = 30201,
            LPT_OtoI = 30202,
            LPT_ItoO = 30203,
            LPT_ItoTR = 30204,
            LPT_ItoTB = 30205,
            LPT_TRtoRTL = 30206,
            LPT_TRtoRTU = 30207,
            LPT_RTLtoTB = 30208,
            LPT_RTUtoTB = 30209,
            LPT_TBtoRTL = 30210,
            LPT_TBtoRTU = 30211,
            LPT_TBtoTR = 30212,

            CID_NtoWFH = 30220,
            CID_NtoIVO = 30221,
            CID_NtoIVF = 30222,
            CID_WFHtoIVO = 30223,
            CID_WFHtoIVF = 30224,
            CID_DELETED = 30225,
            CID_NtoINR = 30226,
            CID_INRtoIVO_6 = 30227,
            CID_INRtoIVO_11 = 30228,
            CID_INRtoWFH_7 = 30229,
            CID_INRtoWFH_10 = 30230,

            CSM_SMNRtoWFH = 30240,
            CSM_SMNRtoSMVO = 30241,
            CSM_WFHtoSMVO = 30242,
            CSM_WFHtoSMVF = 30243,
            CSM_SMNRtoSMVF = 30244,

            CA_NAtoIA = 30250,
            CA_IAtoCC = 30251,
            CA_IAtoCS = 30252,
            CA_NAtoCS = 30253,
            CA_NtoNA = 30254,

            LPR_NRtoR = 30260,
            LPR_RtoNR = 30261,

            LPCA_NAtoA = 30270,
            LPCA_AtoNA = 30271,
            LPCA_AtoA = 30272,

            AM_NtoA = 30280,
            AM_NtoM = 30281,
            AM_MtoA = 30282,
            AM_AtoM = 30283,

            CR_OPN = 30290,
            CR_CLS = 30291,
            CLCHG = 30292,
            CCLMP = 30293,
            CUCLMP = 30294,
            CDOCK = 30295,
            CUDOCK = 30296,
            CID_WRITE = 30305,
            CID_WRITE_FAIL = 30306,
            CR_LOADED = 30309,
            CR_WAIT_FOR_UNLOAD = 30310,

            CJ_CREATED = 30500,
            CJ_CANCELED = 30501,
            CJ_SELECTED = 30502,
            CJ_DESELECT = 30503,
            CJ_EXECUTING_5 = 30504,
            CJ_EXECUTING_7 = 30505,
            CJ_WAITINGFORSTART = 30506,
            CJ_PAUSED = 30507,
            CJ_RESUMED = 30508,
            CJ_COMPLETED = 30509,
            CJ_STOPPED = 30510,
            CJ_ABORTED = 30511,
            CJ_DELETED = 30512,

            PJ_CREATED = 30600,
            PJ_SETTINGUP = 30601,
            PJ_WAITINGFORSTART = 30602,
            PJ_PROCESSING_4 = 30603,
            PJ_PROCESSING_5 = 30604,
            PJ_PRCOMPLETED = 30605,
            PJ_COMPLETED = 30606,
            PJ_PAUSING = 30607,
            PJ_PAUSED = 30608,
            PJ_RESUMED = 30609,
            PJ_STOPPING_11 = 30610,
            PJ_STOPPING_12 = 30611,
            PJ_STOPPED = 30612,
            PJ_ABORTING_13 = 30613,
            PJ_ABORTING_14 = 30614,
            PJ_ABORTING_15 = 30615,
            PJ_ABORTED = 30616,
            PJ_CANCELD = 30617,

            STS_ATSOURCE_1=30700,
            STS_ATSOURCE_3=30701,
            STS_ATSOURCE_8=30702,
            STS_ATWORK_2=30703,
            STS_ATWORK_4=30704,
            STS_ATWORK_6=30705,
            STS_ATDESTINATION = 30706,
            STS_NEEDSPROCESSING_10=30707,
            STS_NEEDSPROCESSING_13=30708,
            STS_INPROCESS = 30709,
            STS_PROCESSED = 30710,
            STS_ABORTED = 30711,
            STS_STOPPED = 30711,
            STS_REJECTED = 30712,
            STS_LOST_12=30713,
            STS_LOST_14=30714,
            STS_SKIPPED_12=30715,
            STS_SKIPPED_14=30716,
            STS_DELETED_7=30717,
            STS_DELETED_9=30718,

            STS_OCCUPIED_1=30730,
            STS_UNOCCUPIED_2=30731,
            STS_OCCUPIED_3=30732,
            STS_UNOCCUPIED_4=30733,
            STS_NOSTATE_5=30734,
            STS_NOSTATE_6=30735,
        }

        /// <summary>
        /// 定义Gem200的事件所Link的VIDs
        /// </summary>
        public static Dictionary<string, VIDItem> Gem300EventsDictionary = new Dictionary<string, VIDItem>()
        {
            {LPT_NtoO, new VIDItem() {Name = LPT_NtoO, Index = (int)EventName.LPT_NtoO, LinkableVid = new[] {(int)DataVariables.DataName.PortID, (int)DataVariables.DataName.PortTransferState, 3},Description = "Load Port Transfer State change form NA to Out of Service"}},
            {LPT_NtoI, new VIDItem() {Name = LPT_NtoI, Index = (int)EventName.LPT_NtoI, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "Load Port transfer state change from NA to in service"}},
            {LPT_OtoI, new VIDItem() {Name = LPT_OtoI, Index = (int)EventName.LPT_OtoI, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "Out of service to in service"}},
            {LPT_ItoO, new VIDItem() {Name = LPT_ItoO, Index = (int)EventName.LPT_ItoO, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "In service to out of service"}},
            {LPT_ItoTR, new VIDItem() {Name = LPT_ItoTR, Index = (int)EventName.LPT_ItoTR, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "In service to transfer ready"}},
            {LPT_ItoTB, new VIDItem() {Name = LPT_ItoTB, Index = (int)EventName.LPT_ItoTB, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "In service to Transfer block"}},
            {LPT_TRtoRTL, new VIDItem() {Name = LPT_TRtoRTL, Index = (int)EventName.LPT_TRtoRTL, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "Transfer ready to Ready to load"}},
            {LPT_TRtoRTU, new VIDItem() {Name = LPT_TRtoRTU, Index = (int)EventName.LPT_TRtoRTU, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "Transfer ready to read to unload"}},
            {LPT_RTLtoTB, new VIDItem() {Name = LPT_RTLtoTB, Index = (int)EventName.LPT_RTLtoTB, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "Ready to unload to transfer block"}},
            {LPT_RTUtoTB, new VIDItem() {Name = LPT_RTUtoTB, Index = (int)EventName.LPT_RTUtoTB, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "RTU to TB"}},
            {LPT_TBtoRTL, new VIDItem() {Name = LPT_TBtoRTL, Index = (int)EventName.LPT_TBtoRTL, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "TB to RTL"}},
            {LPT_TBtoRTU, new VIDItem() {Name = LPT_TBtoRTU, Index = (int)EventName.LPT_TBtoRTU, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "TB to RTU"}},
            {LPT_TBtoTR, new VIDItem() {Name = LPT_TBtoTR, Index = (int)EventName.LPT_TBtoTR, LinkableVid = new[] {(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.PortTransferState,3},Description = "TB to TR"}},


             {CID_NtoWFH, new VIDItem() {Name = CID_NtoWFH, Index = (int)EventName.CID_NtoWFH, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "CarrierID change form NA to Wait for host"}},
            {CID_NtoIVO, new VIDItem() {Name = CID_NtoIVO, Index = (int)EventName.CID_NtoIVO, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "NA to ID verification OK"}},
            {CID_NtoIVF, new VIDItem() {Name = CID_NtoIVF, Index = (int)EventName.CID_NtoIVF, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "NA to ID Verification Failed"}},
            {CID_WFHtoIVO, new VIDItem() {Name = CID_WFHtoIVO, Index = (int)EventName.CID_WFHtoIVO, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "Wait for host to ID Verification OK"}},
            {CID_WFHtoIVF, new VIDItem() {Name = CID_WFHtoIVF, Index = (int)EventName.CID_WFHtoIVF, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "WFH to Verification Fail"}},
            {CID_DELETED, new VIDItem() {Name = CID_DELETED, Index = (int)EventName.CID_DELETED, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "ID  deleted"}},
            {CID_NtoINR, new VIDItem() {Name = CID_NtoINR, Index = (int)EventName.CID_NtoINR, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "N to ID not read"}},
            {CID_INRtoIVO_6, new VIDItem() {Name = CID_INRtoIVO_6, Index = (int)EventName.CID_INRtoIVO_6, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "ID not read to Verification OK transsision 6"}},
            {CID_INRtoIVO_11, new VIDItem() {Name = CID_INRtoIVO_11, Index = (int)EventName.CID_INRtoIVO_11, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "ID not read to Verification OK transsision 11"}},
            {CID_INRtoWFH_7, new VIDItem() {Name = CID_INRtoWFH_7, Index = (int)EventName.CID_INRtoWFH_7, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "ID not read to Verification OK transsision 7"}},
            {CID_INRtoWFH_10, new VIDItem() {Name = CID_INRtoWFH_10, Index = (int)EventName.CID_INRtoWFH_10, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "ID not read to Verification OK transsision 10"}},

            {CSM_SMNRtoWFH, new VIDItem() {Name = CSM_SMNRtoWFH, Index = (int)EventName.CSM_SMNRtoWFH, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.SlotMapList,3117,(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.SlotMapStatus,3},Description = "Carrier Slot map change from not read to wait for host"}},
            {CSM_SMNRtoSMVO, new VIDItem() {Name = CSM_SMNRtoSMVO, Index = (int)EventName.CSM_SMNRtoSMVO, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3114,(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.SlotMapStatus,3},Description = "Carrier Slot map change from not read to verification OK"}},
            {CSM_WFHtoSMVO, new VIDItem() {Name = CSM_WFHtoSMVO, Index = (int)EventName.CSM_WFHtoSMVO, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.SlotMapStatus,3},Description = "Wait for host to verification OK"}},
            {CSM_WFHtoSMVF, new VIDItem() {Name = CSM_WFHtoSMVF, Index = (int)EventName.CSM_WFHtoSMVF, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3114,(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.SlotMapStatus,3},Description = "Wait for host to verification failed"}},
            {CSM_SMNRtoSMVF, new VIDItem() {Name = CSM_SMNRtoSMVF, Index = (int)EventName.CSM_SMNRtoSMVF, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3114,(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.SlotMapStatus,3},Description = "Not read to verification failed"}},

            { CA_NAtoIA, new VIDItem() {Name = CA_NAtoIA, Index = (int)EventName.CA_NAtoIA, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.CarrierAccessingStatus,3},Description = "Carrier access state change from Not access to in access"}},
            { CA_IAtoCC, new VIDItem() {Name = CA_IAtoCC, Index = (int)EventName.CA_IAtoCC, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.CarrierAccessingStatus,3},Description = "Carrier access state change from In access to carrier complete."}},
            { CA_IAtoCS, new VIDItem() {Name = CA_IAtoCS, Index = (int)EventName.CA_IAtoCS, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.CarrierAccessingStatus,3},Description = "In access to carrier stopped."}},
            { CA_NAtoCS, new VIDItem() {Name = CA_NAtoCS, Index = (int)EventName.CA_NAtoCS, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.CarrierAccessingStatus,3},Description = "NA to Carrier stopped"}},
            { CA_NtoNA, new VIDItem() {Name = CA_NtoNA, Index = (int)EventName.CA_NtoNA, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.CarrierAccessingStatus,3},Description = "Carrier access state change from No State to Not access"}},


            { LPR_NRtoR, new VIDItem() {Name = LPR_NRtoR, Index = (int)EventName.LPR_NRtoR, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "Load port reserve state from Not reserve to reserved"}},
            { LPR_RtoNR, new VIDItem() {Name = LPR_RtoNR, Index = (int)EventName.LPR_RtoNR, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = ""}},
            { LPCA_NAtoA, new VIDItem() {Name = LPCA_NAtoA, Index = (int)EventName.LPCA_NAtoA, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortAssociationState,(int)DataVariables.DataName.PortID,3},Description = "Load Port carrier assocation from Not associated to associated"}},
            { LPCA_AtoNA, new VIDItem() {Name = LPCA_AtoNA, Index = (int)EventName.LPCA_AtoNA, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortAssociationState,3},Description = ""}},
            { LPCA_AtoA, new VIDItem() {Name = LPCA_AtoA, Index = (int)EventName.LPCA_AtoA, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortAssociationState,(int)DataVariables.DataName.PortID,3},Description = ""}},
            { AM_NtoA, new VIDItem() {Name = AM_NtoA, Index = (int)EventName.AM_NtoA, LinkableVid = new[] {(int)DataVariables.DataName.AccessMode,3},Description = "Access mode from NA to Auto"}},
            { AM_NtoM, new VIDItem() {Name = AM_NtoM, Index = (int)EventName.AM_NtoM, LinkableVid = new[] {(int)DataVariables.DataName.AccessMode,3},Description = "Access mode from NA to Manual"}},
            { AM_MtoA, new VIDItem() {Name = AM_MtoA, Index = (int)EventName.AM_MtoA, LinkableVid = new[] {(int)DataVariables.DataName.AccessMode,3},Description = "Access mode from Manual to Auto"}},
            { AM_AtoM, new VIDItem() {Name = AM_AtoM, Index = (int)EventName.AM_AtoM, LinkableVid = new[] {(int)DataVariables.DataName.AccessMode,3},Description = "Access mot changed from auto to manual"}},
            { CR_OPN, new VIDItem() {Name = CR_OPN, Index = (int)EventName.CR_OPN, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Opened"}},
            { CR_CLS, new VIDItem() {Name = CR_CLS, Index = (int)EventName.CR_CLS, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Clear"}},
            { CLCHG, new VIDItem() {Name = CLCHG, Index = (int)EventName.CLCHG, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Closed"}},
            { CCLMP, new VIDItem() {Name = CCLMP, Index = (int)EventName.CCLMP, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Clmp"}},
            { CUCLMP, new VIDItem() {Name = CUCLMP, Index = (int)EventName.CUCLMP, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Unclmp"}},
            { CDOCK, new VIDItem() {Name = CDOCK, Index = (int)EventName.CDOCK, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Docked"}},
            { CUDOCK, new VIDItem() {Name = CUDOCK, Index = (int)EventName.CUDOCK, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Undocked"}},
            { CID_WRITE, new VIDItem() {Name = CID_WRITE, Index = (int)EventName.CID_WRITE, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.PortID,3},Description = "Carrier ID Writed"}},
            { CID_WRITE_FAIL, new VIDItem() {Name = CID_WRITE_FAIL, Index = (int)EventName.CID_WRITE_FAIL, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier ID Writed Fail"}},
            { CR_LOADED, new VIDItem() {Name = CR_LOADED, Index = (int)EventName.CR_LOADED, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3},Description = "Carrier Loaded"}},
            { CR_WAIT_FOR_UNLOAD, new VIDItem() {Name = CR_WAIT_FOR_UNLOAD, Index = (int)EventName.CR_WAIT_FOR_UNLOAD, LinkableVid = new[] {(int)DataVariables.DataName.LocationID,(int)DataVariables.DataName.CarrierID,(int)DataVariables.DataName.SlotMap,3},Description = "Carrier Wait For Unload"}},

            { CJ_CREATED, new VIDItem() {Name = CJ_CREATED, Index = (int)EventName.CJ_CREATED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : (no state)→QUEUED"}},
            { CJ_CANCELED, new VIDItem() {Name = CJ_CANCELED, Index = (int)EventName.CJ_CANCELED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : QUEUED→(no state)"}},
            { CJ_SELECTED, new VIDItem() {Name = CJ_SELECTED, Index = (int)EventName.CJ_SELECTED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : QUEUED→SELECTED"}},
            { CJ_DESELECT, new VIDItem() {Name = CJ_DESELECT, Index = (int)EventName.CJ_DESELECT, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : SELECTED→QUEUED"}},
            { CJ_EXECUTING_5, new VIDItem() {Name = CJ_EXECUTING_5, Index = (int)EventName.CJ_EXECUTING_5, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : SELECTED→EXECUTING(Control Job State Transition _5)"}},
            { CJ_EXECUTING_7, new VIDItem() {Name = CJ_EXECUTING_7, Index = (int)EventName.CJ_EXECUTING_7, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : WAITING FOR START→EXECUTING(Control Job State Transition _7"}},
            { CJ_WAITINGFORSTART, new VIDItem() {Name = CJ_WAITINGFORSTART, Index = (int)EventName.CJ_WAITINGFORSTART, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : SELECTED→WAITING FOR START"}},
            { CJ_PAUSED, new VIDItem() {Name = CJ_PAUSED, Index = (int)EventName.CJ_PAUSED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : EXECUTING→PAUSED"}},
            { CJ_RESUMED, new VIDItem() {Name = CJ_RESUMED, Index = (int)EventName.CJ_RESUMED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : PAUSED→EXECUTING"}},
            { CJ_COMPLETED, new VIDItem() {Name = CJ_COMPLETED, Index = (int)EventName.CJ_COMPLETED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : EXECUTING→COMPLETED"}},
            { CJ_STOPPED, new VIDItem() {Name = CJ_STOPPED, Index = (int)EventName.CJ_STOPPED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "Stop Process.State change report : ACTIVE→COMPLETED"}},
            { CJ_ABORTED, new VIDItem() {Name = CJ_ABORTED, Index = (int)EventName.CJ_ABORTED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "Abort Process.State change report : ACTIVE→COMPLETED"}},
            { CJ_DELETED, new VIDItem() {Name = CJ_DELETED, Index = (int)EventName.CJ_DELETED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : COMPLETED→(no state)"}},

            { PJ_CREATED, new VIDItem() {Name = PJ_CREATED, Index = (int)EventName.PJ_CREATED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : (no state)→POOLED"}},
            { PJ_SETTINGUP, new VIDItem() {Name = PJ_SETTINGUP, Index = (int)EventName.PJ_SETTINGUP, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : POOLED→SETTING UP"}},
            { PJ_WAITINGFORSTART, new VIDItem() {Name = PJ_WAITINGFORSTART, Index = (int)EventName.PJ_WAITINGFORSTART, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = "State change report : SETTING UP→WAITING FOR START"}},
            { PJ_PROCESSING_4, new VIDItem() {Name = PJ_PROCESSING_4, Index = (int)EventName.PJ_PROCESSING_4, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_PROCESSING_5, new VIDItem() {Name = PJ_PROCESSING_5, Index = (int)EventName.PJ_PROCESSING_5, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_PRCOMPLETED, new VIDItem() {Name = PJ_PRCOMPLETED, Index = (int)EventName.PJ_PRCOMPLETED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_COMPLETED, new VIDItem() {Name = PJ_COMPLETED, Index = (int)EventName.PJ_COMPLETED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_PAUSING, new VIDItem() {Name = PJ_PAUSING, Index = (int)EventName.PJ_PAUSING, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_PAUSED, new VIDItem() {Name = PJ_PAUSED, Index = (int)EventName.PJ_PAUSED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_RESUMED, new VIDItem() {Name = PJ_RESUMED, Index = (int)EventName.PJ_RESUMED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_STOPPING_11, new VIDItem() {Name = PJ_STOPPING_11, Index = (int)EventName.PJ_STOPPING_11, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_STOPPING_12, new VIDItem() {Name = PJ_STOPPING_12, Index = (int)EventName.PJ_STOPPING_12, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_STOPPED, new VIDItem() {Name = PJ_STOPPED, Index = (int)EventName.PJ_STOPPED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_ABORTING_13, new VIDItem() {Name = PJ_ABORTING_13, Index = (int)EventName.PJ_ABORTING_13, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_ABORTING_14, new VIDItem() {Name = PJ_ABORTING_14, Index = (int)EventName.PJ_ABORTING_14, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_ABORTING_15, new VIDItem() {Name = PJ_ABORTING_15, Index = (int)EventName.PJ_ABORTING_15, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_ABORTED, new VIDItem() {Name = PJ_ABORTED, Index = (int)EventName.PJ_ABORTED, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},
            { PJ_CANCELD, new VIDItem() {Name = PJ_CANCELD, Index = (int)EventName.PJ_CANCELD, LinkableVid = new[] {(int)DataVariables.DataName.PRJobID,(int)DataVariables.DataName.PRJobState,(int)DataVariables.DataName.PrvPRJobState,(int)DataVariables.DataName.PRMtlNameList,(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.RecVariableList,3},Description = ""}},


            { STS_ATSOURCE_1, new VIDItem() {Name = STS_ATSOURCE_1, Index = (int)EventName.STS_ATSOURCE_1, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At source_1"}},
            { STS_ATSOURCE_3, new VIDItem() {Name = STS_ATSOURCE_3, Index = (int)EventName.STS_ATSOURCE_3, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At source_3"}},
            { STS_ATSOURCE_8, new VIDItem() {Name = STS_ATSOURCE_8, Index = (int)EventName.STS_ATSOURCE_8, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At source_8"}},
            { STS_ATWORK_2, new VIDItem() {Name = STS_ATWORK_2, Index = (int)EventName.STS_ATWORK_2, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At Work_2"}},
            { STS_ATWORK_4, new VIDItem() {Name = STS_ATWORK_4, Index = (int)EventName.STS_ATWORK_4, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At Work_4"}},
            { STS_ATWORK_6, new VIDItem() {Name = STS_ATWORK_6, Index = (int)EventName.STS_ATWORK_6, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = "Substrate state change to At Work_6"}},
            { STS_ATDESTINATION, new VIDItem() {Name = STS_ATDESTINATION, Index = (int)EventName.STS_ATDESTINATION, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,(int)DataVariables.DataName.SubstHistory,(int)DataVariables.DataName.SubstDestination,3},Description = ""}},
            { STS_NEEDSPROCESSING_10, new VIDItem() {Name = STS_NEEDSPROCESSING_10, Index = (int)EventName.STS_NEEDSPROCESSING_10, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_NEEDSPROCESSING_13, new VIDItem() {Name = STS_NEEDSPROCESSING_13, Index = (int)EventName.STS_NEEDSPROCESSING_13, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_INPROCESS, new VIDItem() {Name = STS_INPROCESS, Index = (int)EventName.STS_INPROCESS, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_PROCESSED, new VIDItem() {Name = STS_PROCESSED, Index = (int)EventName.STS_PROCESSED, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState, (int)DataVariables.DataName.SubstHistory, 3},Description = ""}},
            { STS_ABORTED, new VIDItem() {Name = STS_ABORTED, Index = (int)EventName.STS_ABORTED, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState, (int)DataVariables.DataName.SubstHistory, 3},Description = ""}},
            { STS_STOPPED, new VIDItem() {Name = STS_STOPPED, Index = (int)EventName.STS_STOPPED, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState, (int)DataVariables.DataName.SubstHistory, 3},Description = ""}},
            { STS_REJECTED, new VIDItem() {Name = STS_REJECTED, Index = (int)EventName.STS_REJECTED, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_LOST_12, new VIDItem() {Name = STS_LOST_12, Index = (int)EventName.STS_LOST_12, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_LOST_14, new VIDItem() {Name = STS_LOST_14, Index = (int)EventName.STS_LOST_14, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_SKIPPED_12, new VIDItem() {Name = STS_SKIPPED_12, Index = (int)EventName.STS_SKIPPED_12, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_SKIPPED_14, new VIDItem() {Name = STS_SKIPPED_14, Index = (int)EventName.STS_SKIPPED_14, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_DELETED_7, new VIDItem() {Name = STS_DELETED_7, Index = (int)EventName.STS_DELETED_7, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},
            { STS_DELETED_9, new VIDItem() {Name = STS_DELETED_9, Index = (int)EventName.STS_DELETED_9, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstProcState,(int)DataVariables.DataName.PrvSubstProcState,3},Description = ""}},

            {STS_OCCUPIED_1, new VIDItem() {Name = STS_OCCUPIED_1, Index = (int)EventName.STS_OCCUPIED_1, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {STS_UNOCCUPIED_2, new VIDItem() {Name = STS_UNOCCUPIED_2, Index = (int)EventName.STS_UNOCCUPIED_2, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {STS_OCCUPIED_3, new VIDItem() {Name = STS_OCCUPIED_3, Index = (int)EventName.STS_OCCUPIED_3, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {STS_UNOCCUPIED_4, new VIDItem() {Name = STS_UNOCCUPIED_4, Index = (int)EventName.STS_UNOCCUPIED_4, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {STS_NOSTATE_5, new VIDItem() {Name = STS_NOSTATE_5, Index = (int)EventName.STS_NOSTATE_5, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {STS_NOSTATE_6, new VIDItem() {Name = STS_NOSTATE_6, Index = (int)EventName.STS_NOSTATE_6, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
        };

    }
}
