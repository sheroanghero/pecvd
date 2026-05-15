using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.FAServices
{
    public static class DataVariables
    {
        public const string AlarmsEnabled = "AlarmsEnabled";
        public const string AlarmsSet = "AlarmsSet";
        public const string Clock = "Clock";
        public const string ControlState = "ControlState";
        public const string EventsEnabled = "EventsEnabled";
        public const string PPExecName = "PPExecName";
        public const string PreviousProcessState = "PreviousProcessState";
        public const string ProcessState = "ProcessState";

        public const string RecipeEndDataSummary = "RecipeEndDataSummary";
        public const string RecipeStepEndDataSummary = "RecipeStepEndDataSummary";

        public const string EventLimit = "EventLimit";
        public const string LimitVariable = "LimitVariable";
        public const string PPChangeName = "PPChangeName";
        public const string PPChangeStatus = "PPChangeStatus";
        public const string TransitionType = "TransitionType";
        public const string AlarmID = "AlarmID";
        public const string ECChangedID = "ECChangedID";
        public const string DVID_NEW = "DVID_NEW";

        public const string EventName = "EventName";
        public const string LotID = "LotID";
        public const string RecipeID = "RecipeID";
        public const string JobID = "JobID";
        public const string CarrierID = "CarrierID";
        public const string SlotMap = "SlotMap";
        public const string SlotMapList = "SlotMapList";
        public const string SequenceID = "SequenceID";
        public const string StationName = "StationName";
        public const string RecipeStepNumber = "RecipeStepNumber";
        public const string SlotID = "SlotID";
        public const string AccessMode = "AccessMode";
        public const string PortAssociationState = "PortAssociationState";
        public const string PortID = "PortID";
        public const string CarrierAccessingStatus = "CarrierAccessingStatus";
        public const string PortTransferState = "PortTransferState";
        public const string Reason = "Reason";
        public const string LocationID = "LocationID";
        public const string SlotMapStatus = "SlotMapStatus";
        public const string WaferID = "WaferID";
        public const string PRJobID = "PRJobID";
        public const string PRJobState = "PRJobState";
        public const string PrvPRJobState = "PrvPRJobState";
        public const string PRMtlNameList = "PRMtlNameList";
        public const string RecVariableList = "RecVariableList";
        public const string CJID = "CJID";
        public const string CJState = "CJState";
        public const string PrvCJState = "PrvCJState";
        public const string SubstDestination = "SubstDestination";
        public const string SubstHistory = "SubstHistory";
        public const string SubstID = "SubstID";
        public const string SubstLocID = "SubstLocID";
        public const string SubstLocState = "SubstLocState";
        public const string SubstProcState = "SubstProcState";
        public const string PrvSubstProcState = "PrvSubstProcState";
        

        #region 自定义DVID，在此增加完后需更新DataNameEnum和DataVariablesDictionary

        public const string TotalStepTimePV = "TotalStepTimePV";
        public const string SingleStepTimeList = "SingleStepTimeList";
        public const string ChemicalUnitDataSummary = "ChemicalUnitDataSummary";

        #endregion

        public enum DataName
        {
            AlarmsEnabled=1,
            AlarmsSet=2,
            Clock =3,
            ControlState=4,
            EventsEnabled=5,
            PPExecName=6,
            PreviousProcessState=7,
            ProcessState=8,

            RecipeEndDataSummary = 501,
            RecipeStepEndDataSummary = 502,

            EventLimit = 3001,
            LimitVariable = 3002,
            PPChangeName = 3003,
            PPChangeStatus = 3004,
            TransitionType = 3005,
            AlarmID = 3006,
            ECChangedID = 3007,
            DVID_NEW = 3008,

            EventName = 3101,
            LotID = 3102,
            RecipeID = 3103,
            JobID = 3104,
            CarrierID = 3105,
            SlotMap = 3106,
            SequenceID = 3107,
            StationName = 3108,
            RecipeStepNumber = 3109,
            SlotID = 3110,

            AccessMode = 3111,
            PortAssociationState = 3112,
            PortID = 3113,
            CarrierAccessingStatus = 3114,
            PortTransferState=3116,
            Reason = 3117,
            LocationID =3118,
            SlotMapStatus =3119,
            WaferID =3120,
            WaferStatus = 3121,
            ContentMap = 3122,
            ChamberID = 3123,

            PRJobID =3201,
            PRJobState = 3202,
            PrvPRJobState = 3203,
            PRMtlNameList = 3205,
            RecVariableList = 3210,

            CJID =3301,
            CJState=3302,
            PrvCJState=3303,
            CarrierInputSpec = 3304,

            SubstDestination=3402,
            SubstHistory = 3403,
            SubstID=3404,
            SubstLocID = 3405,
            SubstLocState = 3406,
            SubstProcState = 3407,
            PrvSubstProcState = 3408,



            #region 自定义DVID，添加到 3500-4000 范围内
            
            ChemicalUnitDataSummary = 3500,
            TotalStepTimePV =3501,
            SingleStepTimeList = 3502,
            SlotMapList = 3503,
            #endregion

        }

        public static Dictionary<string, VIDItem> DataVariablesDictionary = new Dictionary<string, VIDItem>()
        {
            {AlarmsEnabled, new VIDItem() {Name = AlarmsEnabled, Index = (int) DataName.AlarmsEnabled,DataType = "List",Description = "Effective Alarm ID list"}},
            {AlarmsSet, new VIDItem() {Name = AlarmsSet, Index = (int) DataName.AlarmsSet,DataType = "List",Description = "Raised Alarm ID List"}},
            {Clock, new VIDItem() {Name = Clock, Index = (int) DataName.Clock,DataType = "String",Description = "Present date time\r\nYYYYMMDDhhmmsscc"}},
            {ControlState, new VIDItem() {Name = ControlState, Index = (int) DataName.ControlState,DataType = "String",Description = "Current control state\r\n\"1\"= Not defined\r\n\"2\"= Offline-Equipment OffLine\r\n\"3\"= Offline-Attempt OnLine\r\n\"4\"= On-Line Local\r\n \"5\"= On-Line Remote"}},
            {EventsEnabled, new VIDItem() {Name = EventsEnabled, Index = (int) DataName.EventsEnabled,DataType = "List",Description = "Raised EventID list"}},
            {PPExecName, new VIDItem() {Name = PPExecName, Index = (int) DataName.PPExecName,DataType = "String",Description = "Process program ID for the current or recently started process=Recipe name"}},
            {PreviousProcessState, new VIDItem() {Name = PreviousProcessState, Index = (int) DataName.PreviousProcessState,DataType = "Int",Description = "Immediately before process status\r\nRef.: VID8 ProcessState"}},
            {ProcessState, new VIDItem() {Name = ProcessState, Index = (int) DataName.ProcessState,DataType = "Int",Description = "Current process state\r\n0= init\r\n1= idle\r\n2= ready\r\n3= run\r\n4= pause\r\n5 = abort\r\n6= down"}},

            {RecipeEndDataSummary, new VIDItem() {Name = RecipeEndDataSummary, Index = (int) DataName.RecipeEndDataSummary,DataType = "List",Description = "Process data summary thrown when recipe end"}},
            {RecipeStepEndDataSummary, new VIDItem() {Name = RecipeStepEndDataSummary, Index = (int) DataName.RecipeStepEndDataSummary,DataType = "List",Description = "Process data summary thrown when recipe step end"}},

            {EventLimit, new VIDItem() {Name = EventLimit, Index = (int) DataName.EventLimit,DataType = "Int"}},
            {LimitVariable, new VIDItem() {Name = LimitVariable, Index = (int) DataName.LimitVariable,DataType = "Int"}},
            {PPChangeName, new VIDItem() {Name = PPChangeName, Index = (int) DataName.PPChangeName,DataType = "String",Description = "Recipe change name(RCPID)"}},
            {PPChangeStatus, new VIDItem() {Name = PPChangeStatus, Index = (int) DataName.PPChangeStatus,DataType = "Int",Description = "The type of change that occurred for the recipe indicated in RcpChangeName \r\n 0=No change\r\n 1=Created\r\n 2=Updated (modified)\r\n 3=Stored (new)\r\n 4=Replaced\r\n  5=Deleted\r\n 6=Copied\r\n 7=Renamed"}},
            {TransitionType, new VIDItem() {Name = TransitionType, Index = (int) DataName.TransitionType,DataType = "Binary"}},
            {AlarmID, new VIDItem() {Name = AlarmID, Index = (int) DataName.AlarmID,DataType = "Int",Description = "This variable is valid only upon the setting or clearing of an alarm condition and contains the current alarm identification (ALID), regardless of whether that alarm is enabled for reporting."}},
            {ECChangedID, new VIDItem() {Name = ECChangedID, Index = (int) DataName.ECChangedID,DataType = "Int",Description = "Changed ECID list L,n<ECID>"}},
            {DVID_NEW, new VIDItem() {Name = DVID_NEW, Index = (int) DataName.DVID_NEW,DataType = "String"}},

             {EventName, new VIDItem() {Name = EventName, Index = (int) DataName.EventName,DataType = "String"}},
            {LotID, new VIDItem() {Name = LotID, Index = (int) DataName.LotID,DataType = "String"}},
            {RecipeID, new VIDItem() {Name = RecipeID, Index = (int) DataName.RecipeID,DataType = "String"}},
            {JobID, new VIDItem() {Name = JobID, Index = (int) DataName.JobID,DataType = "String"}},
            {CarrierID, new VIDItem() {Name = CarrierID, Index = (int) DataName.CarrierID,DataType = "String",Description = "The ID of the carrier."}},
            {SlotMap, new VIDItem() {Name = SlotMap, Index = (int) DataName.SlotMap,DataType = "String",Description = "Presence status of wafers in the carrier.\r\nL, n (n = Number of SLOTSTATEs)\r\n<SLOTSTATE>\r\n0=UNDEFINED\r\n1=EMPTY\r\n2=NOT EMPTY\r\n3=CORRECTLY OCCUPIED\r\n4=DOUBLE SLOTTED\r\n5=CROSS SLOTTED"}},
            {SequenceID, new VIDItem() {Name = SequenceID, Index = (int) DataName.SequenceID,DataType = "String"}},
            {StationName, new VIDItem() {Name = StationName, Index = (int) DataName.StationName,DataType = "String"}},
            {RecipeStepNumber, new VIDItem() {Name = RecipeStepNumber, Index = (int) DataName.RecipeStepNumber,DataType = "String"}},
            {SlotID, new VIDItem() {Name = SlotID, Index = (int) DataName.SlotID,DataType = "String"}},

            {AccessMode, new VIDItem() {Name = AccessMode, Index = (int) DataName.AccessMode,DataType = "Int",Description = "Load Port Access Mode.\r\n0=MANUAL\r\n1=AUTO"}},
            {PortAssociationState, new VIDItem() {Name = PortAssociationState, Index = (int) DataName.PortAssociationState,DataType = "Int",Description = "The association state of a load port.\r\n0=NOT ASSOCIATED\r\n1= ASSOCIATED"}},
            {PortID, new VIDItem() {Name = PortID, Index = (int) DataName.PortID,DataType = "Int",Description = "Load port ID"}},
            {CarrierAccessingStatus, new VIDItem() {Name = CarrierAccessingStatus, Index = (int) DataName.CarrierAccessingStatus,DataType = "Int",Description = "The current accessing state of the carrier by the equipment.\r\n0=NOT ACCESSED\r\n1=IN ACCESS\r\n2=CARRIER COMPLETE\r\n3=CARRIER STOPPED"}},
            {PortTransferState, new VIDItem() {Name = PortTransferState, Index = (int) DataName.PortTransferState,DataType = "Int",Description = "The current transfer state of a load port. \r\n0=OUT OF SERVICE\r\n1=TRANSFER BLOCKED\r\n2=READY TO LOAD\r\n3=READY TO UNLOAD"}},
            {Reason, new VIDItem() {Name = Reason, Index = (int) DataName.Reason,DataType = "Int",Description = "The reason for slot map status, SLOT MAP NOT READ to WAITING FOR HOST\r\n0=Verification Needed\r\n1=Verification By Equipment Unsuccessful\r\n2=Read Fail\r\n3=Improper Substrate Position"}},
            {LocationID, new VIDItem() {Name = LocationID, Index = (int) DataName.LocationID,DataType = "String",Description = "The ID of a carrier location."}},
            {SlotMapStatus, new VIDItem() {Name = SlotMapStatus, Index = (int) DataName.SlotMapStatus,DataType = "Int",Description = "State of the carrier slot map status. \r\n0=SLOT MAP NOT READ\r\n1=WAITING FOR HOST\r\n2=SLOT MAP VERIFICATION OK\r\n3=SLOT MAP VERIFICATION FAIL"}},
            {WaferID, new VIDItem() {Name = WaferID, Index = (int) DataName.WaferID,DataType = "String"}},
            {PRJobID, new VIDItem() {Name = PRJobID, Index = (int) DataName.PRJobID,DataType = "String",Description = "Process job ID"}},
            {PRJobState, new VIDItem() {Name = PRJobState, Index = (int) DataName.PRJobState,DataType = "Int",Description = "Indicates the current state of a Process Job.\r\n0=POOLED\r\n1=SETTING UP\r\n2=WAITINGFORHOST\r\n3=PROCESSING\r\n4=PROCESSING COMPLETE\r\n5=EXECUTING\r\n6=PAUSING\r\n7=PAUSED\r\n8=STOPPING\r\n9=ABORTING\r\n255=(no state)"}},
            {PrvPRJobState, new VIDItem() {Name = PrvPRJobState, Index = (int) DataName.PrvPRJobState,DataType = "Int",Description = "The last process job state\r\n (Values same as DVVAL: 3202)"}},
            {PRMtlNameList, new VIDItem() {Name = PRMtlNameList, Index = (int) DataName.PRMtlNameList,DataType = "List",Description = ""}},
            {RecVariableList, new VIDItem() {Name = RecVariableList, Index = (int) DataName.RecVariableList,DataType = "List",Description = ""}},
            {CJID, new VIDItem() {Name = CJID, Index = (int) DataName.CJID,DataType = "String",Description = "Control Job ID"}},
            {CJState, new VIDItem() {Name = CJState, Index = (int) DataName.CJState,DataType = "Int",Description = "Indicates the current state of a Control Job.\r\n0=QUEUED\r\n1=SELECTED\r\n2=WAITINGFORSTART\r\n3=EXECUTING\r\n4=PAUSED (*1)\r\n5=COMPLETED\r\n255=(no state)"}},
            {PrvCJState, new VIDItem() {Name = PrvCJState, Index = (int) DataName.PrvCJState,DataType = "Int",Description = "The last control job state\r\n(Values same as DVVAL: 3302)"}},
            {SubstDestination, new VIDItem() {Name = SubstDestination, Index = (int) DataName.SubstDestination,DataType = "String",Description = "Destination Carrier Substrate Location for the substrate associated with the last event."}},
            {SubstHistory, new VIDItem() {Name = SubstHistory, Index = (int) DataName.SubstHistory,DataType = "List",Description = "List of history of locations visited for the substrate associated with the last event.\r\nL, n\r\nL, 3\r\n<SubstLocID> \r\n<TimeIn>\r\n<TimeOut>"}},
            {SubstID, new VIDItem() {Name = SubstID, Index = (int) DataName.SubstID,DataType = "String",Description = "Identifier of substrate associated with last event."}},
            {SubstLocID, new VIDItem() {Name = SubstLocID, Index = (int) DataName.SubstLocID,DataType = "String",Description = "Identifier for equipment substrate location associated with the last event."}},
            {SubstLocState, new VIDItem() {Name = SubstLocState, Index = (int) DataName.SubstLocState,DataType = "Int",Description = "State of substrate location associated with the last event.\r\n0=UNOCCUPIED\r\n1=OCCUPIED"}},
            { SubstProcState, new VIDItem() {Name = SubstProcState, Index = (int) DataName.SubstProcState,DataType = "Int",Description = "Processing state of the substrate associated with the last event.\r\n0=NEEDS PROCESSING\r\n1=IN PROCESS\r\n2=PROCESSED\r\n3=ABORTED\r\n4=STOPPED\r\n5=REJECT\r\n6=LOST\r\n7=SKIPPED\r\n255=(no state)"}},
            {PrvSubstProcState, new VIDItem() {Name = PrvSubstProcState, Index = (int) DataName.PrvSubstProcState,DataType = "Int",Description = "The last board process state\r\n(Values same as DVVAL: 3407)"}},


            #region 自定义DVID

            {ChemicalUnitDataSummary, new VIDItem() {Name = ChemicalUnitDataSummary, Index = (int) DataName.ChemicalUnitDataSummary,DataType = "List",Description = "Chemical Unit Data Summary"}},
            {TotalStepTimePV, new VIDItem() {Name = TotalStepTimePV, Index = (int) DataName.TotalStepTimePV,DataType = "Int",Description = "Total Step Time PV"}},
            {SingleStepTimeList, new VIDItem() {Name = SingleStepTimeList, Index = (int) DataName.SingleStepTimeList,DataType = "List",Description = "Single Step Time List"}},
             {SlotMapList, new VIDItem() {Name = SlotMapList, Index = (int) DataName.SlotMapList,DataType = "List",Description = "Slot map list"}},
            
            #endregion

        };



    }
}
