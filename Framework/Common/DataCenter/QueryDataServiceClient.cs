using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using Aitex.Sorter.Common;
using MECF.Framework.Common.IOCore;

namespace MECF.Framework.Common.DataCenter
{
    public class QueryDataClient : Singleton<QueryDataClient>
    {
        public bool InProcess { get; set; }

        private IQueryDataService _service;
        public IQueryDataService Service
        {
            get
            {
                if (_service == null)
                {
                    if (InProcess)
                        _service = new QueryDataService();
                    else
                        _service = new QueryDataServiceClient();
                }

                return _service;
            }
        }
    }

    public class QueryDataServiceClient : ServiceClientWrapper<IQueryDataService>, IQueryDataService
    {
        public QueryDataServiceClient()
            : base("Client_IQueryDataService", "QueryDataService")
        {
        }

        public Dictionary<string, object> PollData(IEnumerable<string> keys)
        {
            Dictionary<string, object> result = null;
            Invoke(svc => { result = svc.PollData(keys); });
            return result;
        }

        public Dictionary<string, object> PollConfig(IEnumerable<string> keys)
        {
            Dictionary<string, object> result = null;
            Invoke(svc => { result = svc.PollConfig(keys); });
            return result;
        }

        public Dictionary<string, object> PollConfigByModule(string module, IEnumerable<string> keys)
        {
            Dictionary<string, object> result = null;
            Invoke(svc => { result = svc.PollConfigByModule(module, keys); });
            return result;
        }

        public object GetData(string key)
        {
            object result = null;
            Invoke(svc => { result = svc.GetData(key); });
            return result;
        }

        public object GetConfig(string key)
        {
            object result = null;
            Invoke(svc => { result = svc.GetConfig(key); });
            return result;
        }
        public void SetItemValue(string key, int value)
        {
            Invoke(svc => { svc.SetItemValue(key, value); });
        }
        public object GetConfigByModule(string module, string key)
        {
            object result = null;
            Invoke(svc => { result = svc.GetConfigByModule(module, key); });
            return result;
        }

        public string GetConfigFileContent()
        {
            string result = null;
            Invoke(svc => { result = svc.GetConfigFileContent(); });
            return result;
        }

        public string GetConfigFileContentByModule(string module)
        {
            string result = null;
            Invoke(svc => { result = svc.GetConfigFileContentByModule(module); });
            return result;
        }

        public List<SCConfigItem> GetConfigItemList()
        {
            List<SCConfigItem> result = null;
            Invoke(svc => { result = svc.GetConfigItemList(); });
            return result;
        }

        public List<EventItem> QueryDBEvent(string sql)
        {
            List<EventItem> result = null;
            Invoke(svc => { result = svc.QueryDBEvent(sql); });
            return result;
        }

        public List<HistoryCarrierData> QueryDBCarrier(string sql)
        {
            List<HistoryCarrierData> result = null;
            Invoke(svc => { result = svc.QueryDBCarrier(sql); });
            return result;
        }
        public List<HistoryProcessData> QueryDBProcessCarrier(string sql)
        {
            List<HistoryProcessData> result = null;
            Invoke(svc => { result = svc.QueryDBProcessCarrier(sql); });
            return result;
        }
        public List<HistoryProcessData> QueryDBProcessLot(string sql)
        {
            List<HistoryProcessData> result = null;
            Invoke(svc => { result = svc.QueryDBProcessLot(sql); });
            return result;
        }
        public List<HistoryStatisticsOCRData> QueryDBOCRStatistics(string sql)
        {
            List<HistoryStatisticsOCRData> result = null;
            Invoke(svc => { result = svc.QueryDBOCRStatistics(sql); });
            return result;
        }

        public List<HistoryOCRData> QueryDBOCRHistory(string sql)
        {
            List<HistoryOCRData> result = null;
            Invoke(svc => { result = svc.QueryDBOCRHistory(sql); });
            return result;
        }

        public List<HistoryFfuDiffPressureData> QueryDBFfuDiffPressureStatistics(string sql)
        {
            List<HistoryFfuDiffPressureData> result = null;
            Invoke(svc => { result = svc.QueryDBFfuDiffPressureStatistics(sql); });
            return result;
        }

        public List<StatsStatisticsData> QueryStatsDBStatistics(string sql)
        {
            List<StatsStatisticsData> result = null;
            Invoke(svc => { result = svc.QueryStatsDBStatistics(sql); });
            return result;
        }

        public List<HistoryProcessData> QueryDBProcess(string sql)
        {
            List<HistoryProcessData> result = null;
            Invoke(svc => { result = svc.QueryDBProcess(sql); });
            return result;
        }

        public List<HistoryWaferData> QueryDBWafer(string sql)
        {
            List<HistoryWaferData> result = null;
            Invoke(svc => { result = svc.QueryDBWafer(sql); });
            return result;
        }

        public List<HistoryMoveData> QueryDBMovement(string sql)
        {
            List<HistoryMoveData> result = null;
            Invoke(svc => { result = svc.QueryDBMovement(sql); });
            return result;
        }
        public List<HistoryJobMoveData> QueryDBJobMovementByJobGuid(string jobGuid)
        {
            List<HistoryJobMoveData> result = null;
            Invoke(svc => { result = svc.QueryDBJobMovementByJobGuid(jobGuid); });
            return result;
        }

        public List<HistoryJobMoveData> QueryDBJobMovementByJobGuidAndStationName(string jobGuid, string stationName)
        {
            List<HistoryJobMoveData> result = null;
            Invoke(svc => { result = svc.QueryDBJobMovementByJobGuidAndStationName(jobGuid, stationName); });
            return result;
        }

        public List<HistoryDataItem> GetOneDayHistoryData(IEnumerable<string> keys, DateTime begin, string module)
        {
            List<HistoryDataItem> result = null;
            Invoke(svc => { result = svc.GetOneDayHistoryData(keys, begin, module); });
            return result;
        }

        public List<HistoryDataItem> GetHistoryDataFromStartToEnd(IEnumerable<string> keys, DateTime begin, DateTime end, string module)
        {
            List<HistoryDataItem> result = null;
            Invoke(svc => { result = svc.GetHistoryDataFromStartToEnd(keys, begin, end, module); });
            return result;
        }

        public DataTable QueryData(string sql)
        {
            DataTable result = null;
            Invoke(svc => { result = svc.QueryData(sql); });
            return result;
        }

        public List<HistoryDataItem> GetHistoryData(IEnumerable<string> keys, string recipeRunGuid, string module)
        {
            List<HistoryDataItem> result = null;
            Invoke(svc => { result = svc.GetHistoryData(keys, recipeRunGuid, module); });
            return result;
        }

        public List<NotifiableIoItem> GetDiList(string key)
        {
            List<NotifiableIoItem> result = null;
            Invoke(svc => { result = svc.GetDiList(key); });
            return result;
        }

        public List<NotifiableIoItem> GetDoList(string key)
        {
            List<NotifiableIoItem> result = null;
            Invoke(svc => { result = svc.GetDoList(key); });
            return result;
        }

        public List<NotifiableIoItem> GetAiList(string key)
        {
            List<NotifiableIoItem> result = null;
            Invoke(svc => { result = svc.GetAiList(key); });
            return result;
        }

        public List<NotifiableIoItem> GetAoList(string key)
        {
            List<NotifiableIoItem> result = null;
            Invoke(svc => { result = svc.GetAoList(key); });
            return result;
        }

        public List<WaferHistoryWafer> GetWaferHistoryWafers(string id)
        {
            List<WaferHistoryWafer> result = null;
            Invoke(svc => { result = svc.GetWaferHistoryWafers(id); });
            return result;
        }

        public WaferHistoryRecipe GetWaferHistoryRecipe(string id)
        {
            WaferHistoryRecipe result = null;
            Invoke(svc => { result = svc.GetWaferHistoryRecipe(id); });
            return result;
        }

        public List<WaferHistoryRecipe> GetWaferHistoryRecipes(string id)
        {
            List<WaferHistoryRecipe> result = null;
            Invoke(svc => { result = svc.GetWaferHistoryRecipes(id); });
            return result;
        }

        public List<WaferHistoryMovement> GetWaferHistoryMovements(string id)
        {
            List<WaferHistoryMovement> result = null;
            Invoke(svc => { result = svc.GetWaferHistoryMovements(id); });
            return result;
        }

        public List<WaferHistoryLot> QueryWaferHistoryLotsBySql(string sql)
        {
            List<WaferHistoryLot> result = null;
            Invoke(svc => { result = svc.QueryWaferHistoryLotsBySql(sql); });
            return result;
        }

        public List<WaferHistoryLot> GetWaferHistoryLots(DateTime startTime, DateTime endTime, string keyWord)
        {
            List<WaferHistoryLot> result = null;
            Invoke(svc => { result = svc.GetWaferHistoryLots(startTime, endTime, keyWord); });
            return result;
        }

        public string GetTypedConfigContent(string type, string contentPath)
        {
            string result = null;
            Invoke(svc => { result = svc.GetTypedConfigContent(type, contentPath); });
            return result;
        }

        public void SetTypedConfigContent(string type, string contentPath, string content)
        {

            Invoke(svc => { svc.SetTypedConfigContent(type, contentPath, content); });

        }

        public List<WaferHistorySecquence> GetWaferHistorySecquences(string id)
        {
            List<WaferHistorySecquence> result = null;
            Invoke(svc => { result = svc.GetWaferHistorySecquences(id); });
            return result;
        }

        public List<string> GetLayoutRecipeContent(string recipeContent, string slotCount, string cassetteSlotCount)
        {
            List<string> result = null;
            Invoke(svc => { result = svc.GetLayoutRecipeContent(recipeContent, slotCount, cassetteSlotCount); });
            return result;
        }
    }
}
