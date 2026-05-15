using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.ServiceModel;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.CommonData.DeviceData;
using MECF.Framework.Common.CommonData.SorterDefines;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.FAServices.E40s;
using MECF.Framework.Common.FAServices.E94s;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.Jobs;
using MECF.Framework.Common.SubstrateTrackings;


namespace MECF.Framework.Common.DataCenter
{

    [ServiceContract]
    [ServiceKnownType(typeof(SignalTowerDataItem))]
    [ServiceKnownType(typeof(GateValveDataItem))]
    [ServiceKnownType(typeof(EventItem))]
    [ServiceKnownType(typeof(List<EventItem>))]
    [ServiceKnownType(typeof(AlarmEventItem))]
    [ServiceKnownType(typeof(List<AlarmEventItem>))]
    [ServiceKnownType(typeof(List<HistoryCarrierData>))]
    [ServiceKnownType(typeof(List<HistoryProcessData>))]
    [ServiceKnownType(typeof(List<HistoryWaferData>))]
    [ServiceKnownType(typeof(List<HistoryMoveData>))]

    [ServiceKnownType(typeof(List<NotifiableIoItem>))]

    [ServiceKnownType(typeof(List<ProcessJobInfo>))]
    [ServiceKnownType(typeof(List<ControlJobInfo>))]
    [ServiceKnownType(typeof(NotifiableIoItem))]

    [ServiceKnownType(typeof(AITUPCData))]
    [ServiceKnownType(typeof(AITChillerData))]
    [ServiceKnownType(typeof(AITChillerData1))]
    [ServiceKnownType(typeof(AITValveData))]
    [ServiceKnownType(typeof(AITMfcData))]
    [ServiceKnownType(typeof(AITGasSplitterData))]
    [ServiceKnownType(typeof(AITHeaterData))]
    [ServiceKnownType(typeof(AITThermalCoupleData))]
    [ServiceKnownType(typeof(AITWaterFlowMeterData))]
    [ServiceKnownType(typeof(AITPressureMeterData))]
    [ServiceKnownType(typeof(AITRfData))]
    [ServiceKnownType(typeof(AITThrottleValveData))]
    [ServiceKnownType(typeof(AITSensorData))]
    [ServiceKnownType(typeof(AITMagnetData))]
    [ServiceKnownType(typeof(AITPumpData))]
    [ServiceKnownType(typeof(AITSignalTowerData))]
    [ServiceKnownType(typeof(AITEmoData))]
    [ServiceKnownType(typeof(AITStatisticsData))]
    [ServiceKnownType(typeof(AITBoostPumpData))]
    [ServiceKnownType(typeof(AITCylinderData))]
    [ServiceKnownType(typeof(AITWaterFlowSensorData))]
    [ServiceKnownType(typeof(AITServoMotorData))]
    [ServiceKnownType(typeof(AITLidData))]
    [ServiceKnownType(typeof(ServoState))]
    [ServiceKnownType(typeof(AITRfPowerData))]
    [ServiceKnownType(typeof(AITRfMatchData))]
    [ServiceKnownType(typeof(FlowMeterAlarmItem))]
    [ServiceKnownType(typeof(WaferInfo))]
    [ServiceKnownType(typeof(WaferInfo[]))]
    [ServiceKnownType(typeof(CarrierInfo))]
    [ServiceKnownType(typeof(CarrierInfo[]))]
    [ServiceKnownType(typeof(NotifiableIoItem))]
    [ServiceKnownType(typeof(RobotMoveInfo))]
    [ServiceKnownType(typeof(IndicatorState))]
    [ServiceKnownType(typeof(FoupClampState))]
    [ServiceKnownType(typeof(FoupDoorState))]
    [ServiceKnownType(typeof(LoadportCassetteState))]
    [ServiceKnownType(typeof(AITRfidReaderData))]
    [ServiceKnownType(typeof(AITAlignerData))]
    [ServiceKnownType(typeof(AITWaferIdReaderData))]
    [ServiceKnownType(typeof(ModuleName))]
    [ServiceKnownType(typeof(DeviceState))]
    [ServiceKnownType(typeof(SorterRecipeXml))]
    [ServiceKnownType(typeof(SorterRecipeType))]
    [ServiceKnownType(typeof(SorterRecipePlaceModeOrder))]
    [ServiceKnownType(typeof(SorterRecipePlaceModeTransfer1To1))]
    [ServiceKnownType(typeof(SorterRecipePlaceModePack))]
    [ServiceKnownType(typeof(ObservableCollection<SorterRecipeTransferTableItem>))]
    [ServiceKnownType(typeof(SorterRecipeTransferTableItem))]
    [ServiceKnownType(typeof(SlotTransferInfo))]
    [ServiceKnownType(typeof(SlotTransferInfo[]))]
    [ServiceKnownType(typeof(List<string>))]
    [ServiceKnownType(typeof(List<SCConfigItem>))]
    [ServiceKnownType(typeof(SerializableDictionary<string, bool>))]
    [ServiceKnownType(typeof(SerializableDictionary<string, string>))]
    [ServiceKnownType(typeof(List<FAProcessJob>))]
    [ServiceKnownType(typeof(FAProcessJob))]
    [ServiceKnownType(typeof(List<FAControlJob>))]
    [ServiceKnownType(typeof(FAControlJob))]
    [ServiceKnownType(typeof(List<WCFProcessJobInterface>))]
    [ServiceKnownType(typeof(WCFProcessJobInterface))]
    [ServiceKnownType(typeof(List<WCFControlJobInterface>))]
    [ServiceKnownType(typeof(WCFControlJobInterface))]
    [ServiceKnownType(typeof(NotifiableConnectionItem))]
    [ServiceKnownType(typeof(List<NotifiableConnectionItem>))]

    [ServiceKnownType(typeof(WaferHistoryItemType))]
    [ServiceKnownType(typeof(RecipeStep))]
    [ServiceKnownType(typeof(List<RecipeStep>))]
    [ServiceKnownType(typeof(WaferHistoryItem))]
    [ServiceKnownType(typeof(WaferHistoryRecipe))]
    [ServiceKnownType(typeof(List<WaferHistoryWafer>))]
    [ServiceKnownType(typeof(List<WaferHistoryRecipe>))]
    [ServiceKnownType(typeof(List<WaferHistoryMovement>))]
    [ServiceKnownType(typeof(List<WaferHistoryLot>))]


    [ServiceKnownType(typeof(WaferSize))]
    [ServiceKnownType(typeof(AITDeviceData))]
    [ServiceKnownType(typeof(float[]))]
    [ServiceKnownType(typeof(bool[]))]
    [ServiceKnownType(typeof(int[]))]
    [ServiceKnownType(typeof(byte[]))]
    [ServiceKnownType(typeof(double[]))]
    [ServiceKnownType(typeof(Tuple<int, string>))]
    [ServiceKnownType(typeof(Tuple<int, string>[]))]
    [ServiceKnownType(typeof(ManualTransferTask))]
    [ServiceKnownType(typeof(ManualTransferTask[]))]
    [ServiceKnownType(typeof(CjTransferTask))]
    [ServiceKnownType(typeof(CjTransferTask[]))]
    [ServiceKnownType(typeof(CarrierConfigurationItem))]
    [ServiceKnownType(typeof(CarrierConfigurationItem[]))]
    public interface IQueryDataService
    {
        [OperationContract]
        object GetData(string key);

        [OperationContract]
        Dictionary<string, object> PollData(IEnumerable<string> keys);

        [OperationContract]
        Dictionary<string, object> PollConfig(IEnumerable<string> keys);
        [OperationContract]
        void SetItemValue(string key, int value);
        [OperationContract]
        object GetConfig(string key);
        [OperationContract]
        List<NotifiableIoItem> GetDiList(string key);
        [OperationContract]
        List<NotifiableIoItem> GetDoList(string key);
        [OperationContract]
        List<NotifiableIoItem> GetAiList(string key);
        [OperationContract]
        List<NotifiableIoItem> GetAoList(string key);

        [OperationContract]
        string GetConfigFileContent();

        [OperationContract]
        string GetConfigFileContentByModule(string module);
        [OperationContract]
        object GetConfigByModule(string module, string key);
        [OperationContract]
        List<SCConfigItem> GetConfigItemList();
        [OperationContract]
        Dictionary<string, object> PollConfigByModule(string module, IEnumerable<string> keys);

        [OperationContract]
        List<EventItem> QueryDBEvent(string sql);

        [OperationContract]
        List<HistoryCarrierData> QueryDBCarrier(string sql);
        [OperationContract]
        List<HistoryProcessData> QueryDBProcessCarrier(string sql);
        [OperationContract]
        List<HistoryProcessData> QueryDBProcessLot(string sql);

        [OperationContract]
        List<HistoryStatisticsOCRData> QueryDBOCRStatistics(string sql);

        [OperationContract]
        List<HistoryFfuDiffPressureData> QueryDBFfuDiffPressureStatistics(string sql);

        [OperationContract]
        List<StatsStatisticsData> QueryStatsDBStatistics(string sql);

        [OperationContract]
        List<HistoryOCRData> QueryDBOCRHistory(string sql);


        [OperationContract]
        List<HistoryProcessData> QueryDBProcess(string sql);

        [OperationContract]
        List<HistoryWaferData> QueryDBWafer(string sql);

        [OperationContract]
        List<HistoryMoveData> QueryDBMovement(string sql);

        [OperationContract]
        List<HistoryJobMoveData> QueryDBJobMovementByJobGuid(string jobGuid);

        [OperationContract]
        List<HistoryJobMoveData> QueryDBJobMovementByJobGuidAndStationName(string jobGuid, string stationName);


        [OperationContract]
        List<HistoryDataItem> GetHistoryData(IEnumerable<string> keys, string recipeRunGuid, string module);

        [OperationContract]
        List<HistoryDataItem> GetOneDayHistoryData(IEnumerable<string> keys, DateTime begin, string module);

        [OperationContract]
        List<HistoryDataItem> GetHistoryDataFromStartToEnd(IEnumerable<string> keys, DateTime begin, DateTime end, string module);

        [OperationContract]
        DataTable QueryData(string sql);

        [OperationContract]
        WaferHistoryRecipe GetWaferHistoryRecipe(string id);

        [OperationContract]
        List<WaferHistoryWafer> GetWaferHistoryWafers(string id);

        [OperationContract]
        List<WaferHistoryRecipe> GetWaferHistoryRecipes(string id);

        [OperationContract]
        List<WaferHistoryMovement> GetWaferHistoryMovements(string id);

        [OperationContract]
        List<WaferHistorySecquence> GetWaferHistorySecquences(string id);

        [OperationContract]
        List<WaferHistoryLot> QueryWaferHistoryLotsBySql(string sql);

        [OperationContract]
        List<WaferHistoryLot> GetWaferHistoryLots(DateTime startTime, DateTime endTime, string keyWord);

        [OperationContract]
        string GetTypedConfigContent(string type, string contentPath);

        [OperationContract]
        void SetTypedConfigContent(string type, string contentPath, string content);

        [OperationContract]
        List<string> GetLayoutRecipeContent(/*string chamberId, */string recipeFile, string slotCount, string cassetteSlotCount);
    }
}
