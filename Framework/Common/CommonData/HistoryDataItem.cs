using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using SciChart.Core.Extensions;

namespace Aitex.Sorter.Common
{
    [DataContract]
    [Serializable]
    public class HistoryCarrierData
    {
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string Rfid { get; set; }
        [DataMember]
        public string Station { get; set; }
        [DataMember]
        public string LoadTime { get; set; }
        [DataMember]
        public string UnloadTime { get; set; }
        [DataMember]
        public string LotId { get; set; }
        [DataMember]
        public string ProductCategory { get; set; }
        [DataMember]
        public string ProcessPriority { get; set; }
        [DataMember]
        public string ProcessState { get; set; }
    }
    [DataContract]
    [Serializable]
    public class HistoryProcessData
    {
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string StartTime { get; set; }
        [DataMember]
        public string EndTime { get; set; }
        [DataMember]
        public string RecipeName { get; set; }
        [DataMember]
        public string Result { get; set; }

        //[DataMember]
        //public string Guid { get; set; }
        [DataMember]
        public string Rfid { get; set; }
        [DataMember]
        public string Station { get; set; }
        [DataMember]
        public string LotId { get; set; }
        [DataMember]
        public string ProductCategory { get; set; }
    }

    [DataContract]
    [Serializable]
    public class HistoryStatisticsOCRData
    {
        [DataMember]
        public string Date { get; set; }
        [DataMember]
        public string Totaltimes { get; set; }
        [DataMember]
        public string Successfueltimes { get; set; }
        [DataMember]
        public string Failuretimes { get; set; }
        [DataMember]
        public string Result { get; set; }
    }

    [DataContract]
    [Serializable]
    public class StatsStatisticsData
    {
        [DataMember]
        public string Date { get; set; }
        [DataMember]
        public string Unknown { get; set; }
        [DataMember]
        public string Setup { get; set; }
        [DataMember]
        public string Idle { get; set; }
        [DataMember]
        public string Ready { get; set; }
        [DataMember]
        public string Executing { get; set; }
        [DataMember]
        public string Pause { get; set; }
    }

    [DataContract]
    [Serializable]
    public class HistoryOCRData
    {
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string wafer_id { get; set; }
        [DataMember]
        public string read_time { get; set; }
        [DataMember]
        public string source_lp { get; set; }
        [DataMember]
        public string source_carrier { get; set; }

        [DataMember]
        public string source_slot { get; set; }
        [DataMember]
        public string ocr_no { get; set; }
        [DataMember]
        public string ocr_job { get; set; }

        [DataMember]
        public string read_result { get; set; }
        [DataMember]
        public string lasermark { get; set; }
        [DataMember]
        public string ocr_score { get; set; }

        [DataMember]
        public string read_period { get; set; }

    }


    [DataContract]
    [Serializable]
    public class HistoryWaferData
    {
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string CreateTime { get; set; }
        [DataMember]
        public string DeleteTime { get; set; }
        [DataMember]
        public string Station { get; set; }
        [DataMember]
        public string Slot { get; set; }
        [DataMember]
        public string LaserMarker { get; set; }
        [DataMember]
        public string LaserMarkerScore { get; set; }
        [DataMember]
        public string T7Code { get; set; }
        [DataMember]
        public string T7CodeScore { get; set; }
        [DataMember]
        public string LotId { get; set; }
        [DataMember]
        public string CarrierGuid { get; set; }

        [DataMember]
        public string WaferId { get; set; }

        [DataMember]
        public string ImageFileName { get; set; }
        [DataMember]
        public string ImageFilePath { get; set; }
    }
    [DataContract]
    [Serializable]
    public class HistoryMoveData
    {
        [DataMember]
        public string WaferGuid { get; set; }
        [DataMember]
        public string ArriveTime { get; set; }
        [DataMember]
        public string Station { get; set; }
        [DataMember]
        public string Slot { get; set; }
        [DataMember]
        public string Result { get; set; }
    }

    [DataContract]
    [Serializable]
    public class HistoryJobMoveData
    {
        [DataMember]
        public string JobGuid { get; set; }
        [DataMember]
        public string Station { get; set; }
        [DataMember]
        public string ProcessTime { get; set; }
        [DataMember]
        public string ArriveTime { get; set; }
        [DataMember]
        public string LeaveTime { get; set; }
    }
    [DataContract]
    [Serializable]
    public class HistoryBatData
    {
        [DataMember]
        public string Guid { get; set; }
        [DataMember]
        public string StartTime { get; set; }
        [DataMember]
        public string EndTime { get; set; }
        [DataMember]
        public string RecipeName { get; set; }
        [DataMember]
        public string Result { get; set; }
    }

    [DataContract]
    [Serializable]
    public class HistoryFfuDiffPressureData
    {
        [DataMember]
        public string Time { get; set; }
        [DataMember]
        public string FFU1Speed { get; set; }
        [DataMember]
        public string FFU2Speed { get; set; }
        [DataMember]
        public string DiffPressure1 { get; set; }
        [DataMember]
        public string DiffPressure2 { get; set; }
    }

    public interface ITreeItem<T> where T : ITreeItem<T>, new()
    {
        string ID { set; get; }
    }

    [DataContract]
    [Serializable]
    public enum WaferHistoryItemType
    {
        [EnumMember]
        None,
        [EnumMember]
        Lot,
        [EnumMember]
        Wafer,
        [EnumMember]
        Recipe
    }

    [DataContract]
    [Serializable]
    public class WaferHistoryMovement
    {
        [DataMember]
        public string Source { get; set; }
        [DataMember]
        public string Destination { get; set; }
        [DataMember]
        public string InTime { get; set; }
    }

    [DataContract]
    [Serializable]
    public class WaferHistoryWafer : WaferHistoryItem
    {
        [DataMember]
        public string ProcessJob { get; set; }

        [DataMember]
        public string Sequence { get; set; }

        [DataMember]
        public string Status { get; set; }
        public DateTime? ProcessStartTime { get; set; }
        [DataMember]
        public DateTime? ProcessEndTime { get; set; }

        public string ProcessDuration
        {
            get
            {

                if (!ProcessStartTime.HasValue || !ProcessEndTime.HasValue) return string.Empty;
                return ProcessEndTime.Value.Subtract(ProcessStartTime.Value).ToString(@"hh\:mm\:ss");
            }
        }
    }

    [DataContract]
    [Serializable]
    public class RecipeStep
    {
        [DataMember]
        public int No { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public DateTime EndTime { get; set; }
        [DataMember]
        public string ActualTime { get; set; }
        [DataMember]
        public string SettingTime { get; set; }
    }

    [DataContract]
    [Serializable]
    public class RecipeStepFdcData
    {
        [DataMember]
        public int StepNumber { get; set; }
 
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public float SetPoint { get; set; }

        [DataMember]
        public int SampleCount { get; set; }

        [DataMember]
        public float MinValue { get; set; }

        [DataMember]
        public float MaxValue { get; set; }

        [DataMember]
        public float StdValue { get; set; }

        [DataMember]
        public float MeanValue { get; set; }
        [DataMember]
        public string ActualTime { get; set; }
        [DataMember]
        public string SettingTime { get; set; }
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public DateTime EndTime { get; set; }
        [DataMember]
        public string recipe_name { get; set; }
        
    }

    [DataContract]
    [Serializable]
    public class WaferHistoryRecipe : WaferHistoryItem
    {
        [DataMember]
        public string Chamber { get; set; }
        [DataMember]
        public string Recipe { get; set; }
        [DataMember]
        public string SettingTime { get; set; }
        [DataMember]
        public string ActualTime { get; set; }
        [DataMember]
        public List<RecipeStep> Steps { get; set; }

        [DataMember]
        public List<RecipeStepFdcData> Fdcs { get; set; }
    }

    public class WaferHistoryRecipe2 : WaferHistoryItem
    {
        [DataMember]
        public string Chamber { get; set; }
        [DataMember]
        public string Recipe { get; set; }
        [DataMember]
        public string SettingTime { get; set; }
        [DataMember]
        public string ActualTime { get; set; }
        [DataMember]
        public List<RecipeStep> Steps { get; set; }

        public object SelectedLot { get; set; }
        public object SelectedWafer { get; set; }
        public object SelectedProcess { get; set; }
        public bool IsToCompare { get; set; } = false;
        public object Cache { get; set; }// 临时的
    }
    [DataContract]
    [Serializable]
    public class WaferHistoryLot : WaferHistoryItem
    {
        [DataMember]
        public string CarrierID { get; set; }
        [DataMember]
        public string Rfid { get; set; }
        [DataMember]
        public int WaferCount { get; set; }
        [DataMember]
        public int FaultWaferCount { get; set; }
    }

    [DataContract]
    [Serializable]
    public class WaferHistorySecquence : WaferHistoryItem
    {
        [DataMember]
        public string SecquenceName { get; set; }
        [DataMember]
        public string Recipe { get; set; }
        [DataMember]
        public string SecQuenceStartTime { get; set; }
        [DataMember]
        public string SecQuenceEndTime { get; set; }
        [DataMember]
        public string ActualTime { get; set; }
    }
    [DataContract]
    [Serializable]
    public class WaferHistoryItem : ITreeItem<WaferHistoryItem>
    {
        [DataMember]
        public WaferHistoryItemType Type { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public DateTime EndTime { get; set; }
        public string Duration => EndTime.CompareTo(StartTime) < 0 ? "" : EndTime.Subtract(StartTime).ToString(@"hh\:mm\:ss");
        public string ItemInfo => Name.IsNullOrEmpty() ? "" : Name;
        [DataMember]
        public ITreeItem<WaferHistoryItem> SubItems { get; set; }

        [DataMember]
        public string RfID { get; set; }
    }

    [DataContract]
    [Serializable]
    public class WaferHistoryMetrology
    {
        [DataMember]
        public string dataname { get; set; }
        [DataMember]
        public string datavalue { get; set; }
        [DataMember]
        public string processtime { get; set; }
        [DataMember]
        public string stationname { get; set; }
    }
}
