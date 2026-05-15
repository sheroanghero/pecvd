using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.FAServices
{
    /// <summary>
    /// Gem200的事件
    /// </summary>
    public static class UniversalEvents
    {
        public const string EquipmentOFFLINE = "EquipmentOFFLINE";
        public const string ControlStateLOCAL = "ControlStateLOCAL";
        public const string ControlStateREMOTE = "ControlStateREMOTE";
        public const string ProcessingStarted = "ProcessingStarted";
        public const string ProcessingCompleted = "ProcessingCompleted";
        public const string ProcessingStopped = "ProcessingStopped";
        public const string ProcessingStateChanged = "ProcessingStateChanged";
        public const string EquipmentConstantChanged = "EquipmentConstantChanged";
        public const string ProcessProgramChanged = "ProcessProgramChanged";
        public const string ProcessProgramSelected = "ProcessProgramSelected";
        public const string ProcessProgramCreated = "ProcessProgramCreated";
        public const string SpoolingActivated = "SpoolingActivated";
        public const string SpoolingDeactivated = "SpoolingDeactivated";
        public const string SpoolingFailed = "SpoolingFailed";
        public const string EquipmentChangeToAuto = "EquipmentChangeToAuto";
        public const string EquipmentChangeToManual = "EquipmentChangeToManual";
        public const string WAFER_LEFT_POSITION = "WAFER_LEFT_POSITION";
        public const string WAFER_ARRIVE_POSITION = "WAFER_ARRIVE_POSITION";
        public const string CARRIER_ARRIVED = "CARRIER_ARRIVED";
        public const string CARRIER_REMOVED = "CARRIER_REMOVED";
        public const string CARRIER_ID_READ = "CARRIER_ID_READ";
        public const string CARRIER_ID_READ_FAILED = "CARRIER_ID_READ_FAILED";
        public const string CARRIER_ID_WRITE = "CARRIER_ID_WRITE";
        public const string CARRIER_ID_WRITE_FAILED = "CARRIER_ID_WRITE_FAILED";
        public const string CARRIER_LOADED = "CARRIER_LOADED";
        public const string CARRIER_UNLOADED = "CARRIER_UNLOADED";
        public const string SLOT_MAP_AVAILABLE = "SLOT_MAP_AVAILABLE";
        public const string PortReadyToLoad = "PortReadyToLoad";
        public const string PortReadyToUnload = "PortReadyToUnload";
        public const string CarrierProcessStart = "CarrierProcessStart";
        public const string CarrierProcessComplete = "CarrierProcessComplete";
        public const string PortJobStarted = "PortJobStarted";
        public const string PortJobStopped = "PortJobStopped";
        public const string PortJobPaused = "PortJobPaused";
        public const string PortJobResumed = "PortJobResumed";
        public const string PortJobAborted = "PortJobAborted";
        public const string PortJobFinished = "PortJobFinished";
        public const string PortJobFailed = "PortJobFailed";
        public const string PortMapFailed = "PortMapFailed";
        public const string PortPPSelected = "PortPPSelected";
        public const string PortPPSelectFailed = "PortPPSelectFailed";
        public const string RecipeStart = "RecipeStart";
        public const string RecipeComplete = "RecipeComplete";
        public const string RecipeStepStart = "RecipeStepStart";
        public const string RecipeStepEnd = "RecipeStepEnd";
        public const string RecipeFailed = "RecipeFailed";

        public const string CH1RecipeStart = "CH1RecipeStart";
        public const string CH1RecipeComplete = "CH1RecipeComplete";
        public const string CH1RecipeStepStart = "CH1RecipeStepStart";
        public const string CH1RecipeStepEnd = "CH1RecipeStepEnd";
        public const string CH1RecipeFailed = "CH1RecipeFailed";

        public const string CH2RecipeStart = "CH2RecipeStart";
        public const string CH2RecipeComplete = "CH2RecipeComplete";
        public const string CH2RecipeStepStart = "CH2RecipeStepStart";
        public const string CH2RecipeStepEnd = "CH2RecipeStepEnd";
        public const string CH2RecipeFailed = "CH2RecipeFailed";

        public const string CH3RecipeStart = "CH3RecipeStart";
        public const string CH3RecipeComplete = "CH3RecipeComplete";
        public const string CH3RecipeStepStart = "CH3RecipeStepStart";
        public const string CH3RecipeStepEnd = "CH3RecipeStepEnd";
        public const string CH3RecipeFailed = "CH3RecipeFailed";

        public const string CH4RecipeStart = "CH4RecipeStart";
        public const string CH4RecipeComplete = "CH4RecipeComplete";
        public const string CH4RecipeStepStart = "CH4RecipeStepStart";
        public const string CH4RecipeStepEnd = "CH4RecipeStepEnd";
        public const string CH4RecipeFailed = "CH4RecipeFailed";

        public const string CH5RecipeStart = "CH5RecipeStart";
        public const string CH5RecipeComplete = "CH5RecipeComplete";
        public const string CH5RecipeStepStart = "CH5RecipeStepStart";
        public const string CH5RecipeStepEnd = "CH5RecipeStepEnd";
        public const string CH5RecipeFailed = "CH5RecipeFailed";

        public const string CH6RecipeStart = "CH6RecipeStart";
        public const string CH6RecipeComplete = "CH6RecipeComplete";
        public const string CH6RecipeStepStart = "CH6RecipeStepStart";
        public const string CH6RecipeStepEnd = "CH6RecipeStepEnd";
        public const string CH6RecipeFailed = "CH6RecipeFailed";

        public const string CH7RecipeStart = "CH7RecipeStart";
        public const string CH7RecipeComplete = "CH7RecipeComplete";
        public const string CH7RecipeStepStart = "CH7RecipeStepStart";
        public const string CH7RecipeStepEnd = "CH7RecipeStepEnd";
        public const string CH7RecipeFailed = "CH7RecipeFailed";

        public const string VCEA_PlatformInFinished = "VCEA_PlatformInFinished";
        public const string VCEB_PlatformInFinished = "VCEB_PlatformInFinished";
        public const string VCEA_PlatformOutFinished = "VCEA_PlatformOutFinished";
        public const string VCEB_PlatformOutFinished = "VCEB_PlatformOutFinished";

        public const string STS_INPROCESSING = "STS_INPROCESSING";
        public const string STS_PROCESSED = "STS_PROCESSED";

        /// <summary>
        /// 定义Gem200事件的ID
        /// </summary>
        public enum EventName
        {
            EquipmentOFFLINE = 1,
            ControlStateLOCAL = 2,
            ControlStateREMOTE = 3,
            ProcessingStarted = 4,
            ProcessingCompleted = 5,
            ProcessingStopped = 6,
            ProcessingStateChanged = 7,
            EquipmentConstantChanged = 8,
            ProcessProgramChanged = 9,
            ProcessProgramSelected = 10,
            ProcessProgramCreated = 11,
            SpoolingActivated = 160,
            SpoolingDeactivated = 161,
            SpoolingFailed = 162,
            EquipmentChangeToAuto = 501,
            EquipmentChangeToManual = 502,
            WAFER_LEFT_POSITION = 503,
            WAFER_ARRIVE_POSITION = 504,
            CARRIER_ARRIVED = 1000,
            CARRIER_REMOVED = 1001,
            CARRIER_ID_READ = 1002,
            CARRIER_ID_READ_FAILED = 1003,
            CARRIER_ID_WRITE = 1004,
            CARRIER_ID_WRITE_FAILED = 1005,
            CARRIER_LOADED = 1006,
            CARRIER_UNLOADED = 1007,
            SLOT_MAP_AVAILABLE = 1008,
            PortReadyToLoad = 1010,
            PortReadyToUnload = 1011,

            CarrierProcessStart = 1020,
            CarrierProcessComplete = 1021,
            PortJobStarted = 1022,
            PortJobStopped = 1023,
            PortJobPaused = 1024,
            PortJobResumed = 1025,
            PortJobAborted = 1026,
            PortJobFinished = 1027,
            PortJobFailed = 1028,
            PortMapFailed = 1029,
            PortPPSelected = 1030,
            PortPPSelectFailed = 1031,

            RecipeStart = 2000,
            RecipeComplete = 2001,
            RecipeStepStart = 2002,
            RecipeStepEnd = 2003,
            RecipeFailed = 2004,

            CH1RecipeStart = 20001,
            CH1RecipeComplete = 20011,
            CH1RecipeStepStart = 20021,
            CH1RecipeStepEnd = 20031,
            CH1RecipeFailed = 20041,

            CH2RecipeStart = 20002,
            CH2RecipeComplete = 20012,
            CH2RecipeStepStart = 20022,
            CH2RecipeStepEnd = 20032,
            CH2RecipeFailed = 20042,

            CH3RecipeStart = 20003,
            CH3RecipeComplete = 20013,
            CH3RecipeStepStart = 20023,
            CH3RecipeStepEnd = 20033,
            CH3RecipeFailed = 20043,

            CH4RecipeStart = 20004,
            CH4RecipeComplete = 20014,
            CH4RecipeStepStart = 20024,
            CH4RecipeStepEnd = 20034,
            CH4RecipeFailed = 20044,

            CH5RecipeStart = 20005,
            CH5RecipeComplete = 20015,
            CH5RecipeStepStart = 20025,
            CH5RecipeStepEnd = 20035,
            CH5RecipeFailed = 20045,

            CH6RecipeStart = 20006,
            CH6RecipeComplete = 20016,
            CH6RecipeStepStart = 20026,
            CH6RecipeStepEnd = 20036,
            CH6RecipeFailed = 20046,

            CH7RecipeStart = 20007,
            CH7RecipeComplete = 20017,
            CH7RecipeStepStart = 20027,
            CH7RecipeStepEnd = 20037,
            CH7RecipeFailed = 20047,

            VCEA_PlatformInFinished = 3000,
            VCEB_PlatformInFinished = 3001,
            VCEA_PlatformOutFinished = 3002,
            VCEB_PlatformOutFinished = 3003,

            STS_INPROCESSING = 30709,
            STS_PROCESSED = 30710,
        }

        /// <summary>
        /// 定义Gem200的事件所Link的VIDs
        /// </summary>
        public static Dictionary<string, VIDItem> UniversalEventsDictionary = new Dictionary<string, VIDItem>()
        {
            {EquipmentOFFLINE, new VIDItem() {Name = EquipmentOFFLINE, Index = (int)EventName.EquipmentOFFLINE, LinkableVid = new[] {3,4}}},
            {ControlStateLOCAL, new VIDItem() {Name = ControlStateLOCAL, Index = (int)EventName.ControlStateLOCAL, LinkableVid = new[] {3,4}}},
            {ControlStateREMOTE, new VIDItem() {Name = ControlStateREMOTE, Index = (int)EventName.ControlStateREMOTE, LinkableVid = new[] {3,4}}},
            {ProcessingStarted, new VIDItem() {Name = ProcessingStarted, Index = (int)EventName.ProcessingStarted, LinkableVid = new[] {3,7}}},
            {ProcessingCompleted, new VIDItem() {Name = ProcessingCompleted, Index = (int)EventName.ProcessingCompleted, LinkableVid = new[] {3,7}}},
            {ProcessingStopped, new VIDItem() {Name = ProcessingStopped, Index = (int)EventName.ProcessingStopped, LinkableVid = new[] {3,7}}},
            {ProcessingStateChanged, new VIDItem() {Name = ProcessingStateChanged, Index = (int)EventName.ProcessingStateChanged, LinkableVid = new[] {3,8,7}}},
            {EquipmentConstantChanged, new VIDItem() {Name = EquipmentConstantChanged, Index = (int)EventName.EquipmentConstantChanged, LinkableVid = new[] {(int)DataVariables.DataName.ECChangedID}}},
            {ProcessProgramChanged, new VIDItem() {Name = ProcessProgramChanged, Index = (int)EventName.ProcessProgramChanged, LinkableVid = new[] {(int)DataVariables.DataName.SequenceID,(int)DataVariables.DataName.PPChangeName,(int)DataVariables.DataName.PPChangeStatus}}},
            {ProcessProgramSelected, new VIDItem() {Name = ProcessProgramSelected, Index = (int)EventName.ProcessProgramSelected, LinkableVid = new[] {(int)DataVariables.DataName.SequenceID}}},
            {ProcessProgramCreated, new VIDItem() {Name = ProcessProgramCreated, Index = (int)EventName.ProcessProgramCreated, LinkableVid = new[] {(int)DataVariables.DataName.SequenceID}}},
            {SpoolingActivated, new VIDItem() {Name = SpoolingActivated, Index = (int)EventName.SpoolingActivated}},
            {SpoolingDeactivated, new VIDItem() {Name = SpoolingDeactivated, Index = (int)EventName.SpoolingDeactivated}},
            {SpoolingFailed, new VIDItem() {Name = SpoolingFailed, Index = (int)EventName.SpoolingFailed}},
            {EquipmentChangeToAuto, new VIDItem() {Name = EquipmentChangeToAuto, Index = (int)EventName.EquipmentChangeToAuto}},
            {EquipmentChangeToManual, new VIDItem() {Name = EquipmentChangeToManual, Index = (int)EventName.EquipmentChangeToManual}},
            {WAFER_LEFT_POSITION, new VIDItem() {Name = WAFER_LEFT_POSITION, Index = (int)EventName.WAFER_LEFT_POSITION, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {WAFER_ARRIVE_POSITION, new VIDItem() {Name = WAFER_ARRIVE_POSITION, Index = (int)EventName.WAFER_ARRIVE_POSITION, LinkableVid = new[] {(int)DataVariables.DataName.SubstID,(int)DataVariables.DataName.SubstLocID,(int)DataVariables.DataName.SubstLocState,3}}},
            {CARRIER_ARRIVED, new VIDItem() {Name = CARRIER_ARRIVED, Index = (int)EventName.CARRIER_ARRIVED, LinkableVid = new[] {(int)DataVariables.DataName.PortID,3}}},
            {CARRIER_REMOVED, new VIDItem() {Name = CARRIER_REMOVED, Index = (int)EventName.CARRIER_REMOVED, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},

            { CARRIER_ID_READ, new VIDItem() {Name = CARRIER_ID_READ, Index = (int)EventName.CARRIER_ID_READ, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID, 3 }}},
            { CARRIER_ID_READ_FAILED, new VIDItem() {Name = CARRIER_ID_READ_FAILED, Index = (int)EventName.CARRIER_ID_READ_FAILED, LinkableVid = new[] { (int)DataVariables.DataName.PortID, 3 }}},
            { CARRIER_ID_WRITE, new VIDItem() {Name = CARRIER_ID_WRITE, Index = (int)EventName.CARRIER_ID_WRITE, LinkableVid = new[] {(int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID, 3 }}},
            { CARRIER_ID_WRITE_FAILED, new VIDItem() {Name = CARRIER_ID_WRITE_FAILED, Index = (int)EventName.CARRIER_ID_WRITE_FAILED, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            { CARRIER_LOADED, new VIDItem() {Name = CARRIER_LOADED, Index = (int)EventName.CARRIER_LOADED, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            { CARRIER_UNLOADED, new VIDItem() {Name = CARRIER_UNLOADED, Index = (int)EventName.CARRIER_UNLOADED, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},

            { SLOT_MAP_AVAILABLE, new VIDItem() {Name = SLOT_MAP_AVAILABLE, Index = (int)EventName.SLOT_MAP_AVAILABLE, LinkableVid = new[] {(int)DataVariables.DataName.SlotMapList, (int)DataVariables.DataName.PortID, 3 }}},
            { PortReadyToLoad, new VIDItem() {Name = PortReadyToLoad, Index = (int)EventName.PortReadyToLoad, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            { PortReadyToUnload, new VIDItem() {Name = PortReadyToUnload, Index = (int)EventName.PortReadyToUnload, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},

            { CarrierProcessStart, new VIDItem() {Name = CarrierProcessStart, Index = (int)EventName.CarrierProcessStart, LinkableVid = new[] {(int)DataVariables.DataName.LotID, (int)DataVariables.DataName.PortID, (int)DataVariables.DataName.SequenceID, 3}}},
            {CarrierProcessComplete, new VIDItem() {Name = CarrierProcessComplete, Index = (int)EventName.CarrierProcessComplete, LinkableVid = new[] { (int)DataVariables.DataName.LotID, (int)DataVariables.DataName.PortID, (int)DataVariables.DataName.SequenceID, 3 }}},

            {PortJobStarted, new VIDItem() {Name = PortJobStarted, Index = (int)EventName.PortJobStarted, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID,(int)DataVariables.DataName.CJID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobStopped, new VIDItem() {Name = PortJobStopped, Index = (int)EventName.PortJobStopped, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobPaused, new VIDItem() {Name = PortJobPaused, Index = (int)EventName.PortJobPaused, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobResumed, new VIDItem() {Name = PortJobResumed, Index = (int)EventName.PortJobResumed, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobAborted, new VIDItem() {Name = PortJobAborted, Index = (int)EventName.PortJobAborted, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobFinished, new VIDItem() {Name = PortJobFinished, Index = (int)EventName.PortJobFinished, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortJobFailed, new VIDItem() {Name = PortJobFailed, Index = (int)EventName.PortJobFailed, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortMapFailed, new VIDItem() {Name = PortMapFailed, Index = (int)EventName.PortMapFailed, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID, (int)DataVariables.DataName.CJID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortPPSelected, new VIDItem() {Name = PortPPSelected, Index = (int)EventName.PortPPSelected, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},
            {PortPPSelectFailed, new VIDItem() {Name = PortPPSelectFailed, Index = (int)EventName.PortPPSelectFailed, LinkableVid = new[] {(int)DataVariables.DataName.LotID,(int)DataVariables.DataName.JobID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.RecipeID}}},

            {RecipeStart, new VIDItem() {Name = RecipeStart, Index = (int)EventName.RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {RecipeComplete, new VIDItem() {Name = RecipeComplete, Index = (int)EventName.RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {RecipeStepStart, new VIDItem() {Name = RecipeStepStart, Index = (int)EventName.RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {RecipeStepEnd, new VIDItem() {Name = RecipeStepEnd, Index = (int)EventName.RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {RecipeFailed, new VIDItem() {Name = RecipeFailed, Index = (int)EventName.RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH1RecipeStart, new VIDItem() {Name = CH1RecipeStart, Index = (int)EventName.CH1RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH1RecipeComplete, new VIDItem() {Name = CH1RecipeComplete, Index = (int)EventName.CH1RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH1RecipeStepStart, new VIDItem() {Name = CH1RecipeStepStart, Index = (int)EventName.CH1RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH1RecipeStepEnd, new VIDItem() {Name = CH1RecipeStepEnd, Index = (int)EventName.CH1RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH1RecipeFailed, new VIDItem() {Name = CH1RecipeFailed, Index = (int)EventName.CH1RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH2RecipeStart, new VIDItem() {Name = CH2RecipeStart, Index = (int)EventName.CH2RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH2RecipeComplete, new VIDItem() {Name = CH2RecipeComplete, Index = (int)EventName.CH2RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH2RecipeStepStart, new VIDItem() {Name = CH2RecipeStepStart, Index = (int)EventName.CH2RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH2RecipeStepEnd, new VIDItem() {Name = CH2RecipeStepEnd, Index = (int)EventName.CH2RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH2RecipeFailed, new VIDItem() {Name = CH2RecipeFailed, Index = (int)EventName.CH2RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH3RecipeStart, new VIDItem() {Name = CH3RecipeStart, Index = (int)EventName.CH3RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH3RecipeComplete, new VIDItem() {Name = CH3RecipeComplete, Index = (int)EventName.CH3RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH3RecipeStepStart, new VIDItem() {Name = CH3RecipeStepStart, Index = (int)EventName.CH3RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH3RecipeStepEnd, new VIDItem() {Name = CH3RecipeStepEnd, Index = (int)EventName.CH3RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH3RecipeFailed, new VIDItem() {Name = CH3RecipeFailed, Index = (int)EventName.CH3RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH4RecipeStart, new VIDItem() {Name = CH4RecipeStart, Index = (int)EventName.CH4RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH4RecipeComplete, new VIDItem() {Name = CH4RecipeComplete, Index = (int)EventName.CH4RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH4RecipeStepStart, new VIDItem() {Name = CH4RecipeStepStart, Index = (int)EventName.CH4RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH4RecipeStepEnd, new VIDItem() {Name = CH4RecipeStepEnd, Index = (int)EventName.CH4RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH4RecipeFailed, new VIDItem() {Name = CH4RecipeFailed, Index = (int)EventName.CH4RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH5RecipeStart, new VIDItem() {Name = CH5RecipeStart, Index = (int)EventName.CH5RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH5RecipeComplete, new VIDItem() {Name = CH5RecipeComplete, Index = (int)EventName.CH5RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH5RecipeStepStart, new VIDItem() {Name = CH5RecipeStepStart, Index = (int)EventName.CH5RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH5RecipeStepEnd, new VIDItem() {Name = CH5RecipeStepEnd, Index = (int)EventName.CH5RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH5RecipeFailed, new VIDItem() {Name = CH5RecipeFailed, Index = (int)EventName.CH5RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH6RecipeStart, new VIDItem() {Name = CH6RecipeStart, Index = (int)EventName.CH6RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH6RecipeComplete, new VIDItem() {Name = CH6RecipeComplete, Index = (int)EventName.CH6RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH6RecipeStepStart, new VIDItem() {Name = CH6RecipeStepStart, Index = (int)EventName.CH6RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH6RecipeStepEnd, new VIDItem() {Name = CH6RecipeStepEnd, Index = (int)EventName.CH6RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH6RecipeFailed, new VIDItem() {Name = CH6RecipeFailed, Index = (int)EventName.CH6RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {CH7RecipeStart, new VIDItem() {Name = CH7RecipeStart, Index = (int)EventName.CH7RecipeStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH7RecipeComplete, new VIDItem() {Name = CH7RecipeComplete, Index = (int)EventName.CH7RecipeComplete, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},
            {CH7RecipeStepStart, new VIDItem() {Name = CH7RecipeStepStart, Index = (int)EventName.CH7RecipeStepStart, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.RecipeStepNumber,(int)DataVariables.DataName.SlotID}}},
            {CH7RecipeStepEnd, new VIDItem() {Name = CH7RecipeStepEnd, Index = (int)EventName.CH7RecipeStepEnd, LinkableVid = new[] {(int)DataVariables.DataName.RecipeID,(int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName, (int)DataVariables.DataName.RecipeStepNumber, (int)DataVariables.DataName.SlotID}}},
            {CH7RecipeFailed, new VIDItem() {Name = CH7RecipeFailed, Index = (int)EventName.CH7RecipeFailed,  LinkableVid = new[] {(int)DataVariables.DataName.RecipeID, (int)DataVariables.DataName.CarrierID, (int)DataVariables.DataName.PortID,(int)DataVariables.DataName.StationName,(int)DataVariables.DataName.SlotID}}},

            {VCEA_PlatformInFinished, new VIDItem() {Name = VCEA_PlatformInFinished, Index = (int)EventName.VCEA_PlatformInFinished, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            {VCEB_PlatformInFinished, new VIDItem() {Name = VCEB_PlatformInFinished, Index = (int)EventName.VCEB_PlatformInFinished, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            {VCEA_PlatformOutFinished, new VIDItem() {Name = VCEA_PlatformOutFinished, Index = (int)EventName.VCEA_PlatformOutFinished, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},
            {VCEB_PlatformOutFinished, new VIDItem() {Name = VCEB_PlatformOutFinished, Index = (int)EventName.VCEB_PlatformOutFinished, LinkableVid = new[] {(int)DataVariables.DataName.PortID, 3 }}},

             {STS_INPROCESSING, new VIDItem() {Name = STS_INPROCESSING, Index = (int)EventName.STS_INPROCESSING, LinkableVid = new[] {(int)DataVariables.DataName.SubstID, (int)DataVariables.DataName.LotID, (int)DataVariables.DataName.PortID, (int)DataVariables.DataName.Clock}}},
            {STS_PROCESSED, new VIDItem() {Name = STS_PROCESSED, Index = (int)EventName.STS_PROCESSED,  LinkableVid = new[] {(int)DataVariables.DataName.SubstID, (int)DataVariables.DataName.LotID, (int)DataVariables.DataName.PortID, (int)DataVariables.DataName.Clock}}},

        };

    }
}
