using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace Aitex.Sorter.RT.Module.DBRecorder
{
    /*

     */
    public class CarrierDataRecorder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid">唯一</param>
        /// <param name="station">位置</param>
        public static void Loaded(string guid, string station)
        {
            string sql = string.Format("INSERT INTO \"carrier_data\"(\"guid\", \"load_time\", \"station\" )VALUES ('{0}', '{1}', '{2}');",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                station);

            DB.Insert(sql);
        }

        public static void UpdateCarrierId(string guid, string rfid)
        {
            string sql = string.Format(
                "UPDATE \"carrier_data\" SET \"rfid\"='{0}' WHERE \"guid\"='{1}';",
                rfid, guid);

            DB.Insert(sql);
        }

        public static void UpdateLotId(string guid, string lotId)
        {
            string sql = string.Format(
                "UPDATE \"carrier_data\" SET \"lot_id\"='{0}' WHERE \"guid\"='{1}';",
                lotId, guid);

            DB.Insert(sql);
        }

        public static void UpdateProductCategory(string guid, string productCategory)
        {
            string sql = string.Format(
                "UPDATE \"carrier_data\" SET \"product_category\"='{0}' WHERE \"guid\"='{1}';",
                productCategory, guid);

            DB.Insert(sql);
        }

        public static void Unloaded(string guid)
        {
            string sql = string.Format(
                "UPDATE \"carrier_data\" SET \"unload_time\"='{0}' WHERE \"guid\"='{1}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid);

            DB.Insert(sql);
        }
        public static List<HistoryProcessData> QueryDBProcessCarrier(string sql)
        {
            List<HistoryProcessData> result = new List<HistoryProcessData>();

            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryProcessData ev = new HistoryProcessData();

                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();

                    ev.Rfid = ds.Tables[0].Rows[i]["rfid"].ToString();
                    ev.RecipeName = ds.Tables[0].Rows[i]["rfid"].ToString();
                    ev.LotId = ds.Tables[0].Rows[i]["lot_id"].ToString();
                    ev.ProductCategory = ds.Tables[0].Rows[i]["product_category"].ToString();

                    ev.Station = ds.Tables[0].Rows[i]["station"].ToString();

                    if (!ds.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
                        ev.StartTime = ((DateTime)ds.Tables[0].Rows[i]["load_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    if (!ds.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
                        ev.EndTime = ((DateTime)ds.Tables[0].Rows[i]["unload_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    result.Add(ev);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return result;
        }
        public static List<HistoryProcessData> QueryDBProcessLot(string sql)
        {
            List<HistoryProcessData> result = new List<HistoryProcessData>();

            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryProcessData ev = new HistoryProcessData();

                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();

                    ev.LotId = ds.Tables[0].Rows[i]["name"].ToString();

                    if (!ds.Tables[0].Rows[i]["start_time"].Equals(DBNull.Value))
                        ev.StartTime = ((DateTime)ds.Tables[0].Rows[i]["start_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    if (!ds.Tables[0].Rows[i]["end_time"].Equals(DBNull.Value))
                        ev.EndTime = ((DateTime)ds.Tables[0].Rows[i]["end_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    result.Add(ev);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return result;
        }

        public static List<HistoryCarrierData> QueryDBCarrier(string sql)
        {
            List<HistoryCarrierData> result = new List<HistoryCarrierData>();

            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryCarrierData ev = new HistoryCarrierData();

                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();

                    ev.Rfid = ds.Tables[0].Rows[i]["rfid"].ToString();
                    ev.LotId = ds.Tables[0].Rows[i]["lot_id"].ToString();
                    ev.ProductCategory = ds.Tables[0].Rows[i]["product_category"].ToString();

                    ev.Station = ds.Tables[0].Rows[i]["station"].ToString();

                    if (!ds.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
                        ev.LoadTime = ((DateTime)ds.Tables[0].Rows[i]["load_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    if (!ds.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
                        ev.UnloadTime = ((DateTime)ds.Tables[0].Rows[i]["unload_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    result.Add(ev);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return result;
        }


        public static List<WaferHistoryLot> QueryWaferHistoryLotsBySql(string sql)
        {
            List<WaferHistoryLot> result = new List<WaferHistoryLot>();
            try
            {
                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;
                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    WaferHistoryLot item = new WaferHistoryLot();

                    if (!dataSet.Tables[0].Rows[i]["lot_id"].Equals(DBNull.Value))
                    {
                        item.CarrierID = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
                        item.Name = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
                    }
                    else
                    {
                        continue;
                    }

                    item.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
                    item.Type = WaferHistoryItemType.Lot;

                    if (!dataSet.Tables[0].Rows[i]["rfid"].Equals(DBNull.Value))
                        item.Rfid = dataSet.Tables[0].Rows[i]["rfid"].ToString();

                    if (!dataSet.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
                        item.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["load_time"].ToString());

                    if (!dataSet.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
                        item.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["unload_time"].ToString());

                    DataSet subDataSet = DB.ExecuteDataset(string.Format("SELECT * FROM \"wafer_data\" Where \"carrier_data_guid\"='{0}' ;", item.ID));

                    if (subDataSet != null && subDataSet.Tables.Count != 0 && subDataSet.Tables[0].Rows.Count != 0)
                    {
                        item.WaferCount = subDataSet.Tables[0].Rows.Count;
                        item.FaultWaferCount = subDataSet.Tables[0].Select("process_status='Failed'").Length;
                    }

                    result.Add(item);
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }


            return result;
        }

        public static List<WaferHistoryLot> GetWaferHistoryLots(DateTime startTime, DateTime endTime, string keyWord)
        {
            List<WaferHistoryLot> result = new List<WaferHistoryLot>();

            try
            {
                string sqlFilter = "";
                if (keyWord != null && !string.IsNullOrEmpty(keyWord.Trim()))
                {
                    sqlFilter = "and (";
                    var keyStrings = keyWord.Split(',');

                    for (int i = 0; i < keyStrings.Length; i++)
                    {
                        sqlFilter += $"\"lot_id\" like '%{keyStrings[i].Trim()}%'";
                        if (i < keyStrings.Length - 1)
                        {
                            sqlFilter += " or ";
                        }
                    }
                    sqlFilter += ")";
                }

                string sql = string.Format("SELECT * FROM \"carrier_data\" where (\"unload_time\" is null and \"load_time\" >= '{2}') or (\"load_time\" >= '{0}' and \"load_time\" <= '{1}') or (\"unload_time\" >= '{0}' and \"unload_time\" <= '{1}') or (\"load_time\" <= '{0}' and \"unload_time\" >= '{1}')  {3} order by \"station\" ASC, \"load_time\" ASC limit 1000;", startTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), startTime.AddDays(-1).ToString("yyyy/MM/dd HH:mm:ss.fff"), endTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), sqlFilter);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;
                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    WaferHistoryLot item = new WaferHistoryLot();

                    if (!dataSet.Tables[0].Rows[i]["lot_id"].Equals(DBNull.Value))
                    {
                        item.CarrierID = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
                        item.Name = dataSet.Tables[0].Rows[i]["lot_id"].ToString();
                    }
                    else
                    {
                        continue;
                    }

                    item.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
                    item.Type = WaferHistoryItemType.Lot;

                    if (!dataSet.Tables[0].Rows[i]["rfid"].Equals(DBNull.Value))
                        item.Rfid = dataSet.Tables[0].Rows[i]["rfid"].ToString();

                    if (!dataSet.Tables[0].Rows[i]["load_time"].Equals(DBNull.Value))
                        item.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["load_time"].ToString());

                    if (!dataSet.Tables[0].Rows[i]["unload_time"].Equals(DBNull.Value))
                        item.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["unload_time"].ToString());

                    DataSet subDataSet = DB.ExecuteDataset(string.Format("SELECT * FROM \"wafer_data\" Where \"carrier_data_guid\"='{0}' ;", item.ID));

                    if (subDataSet != null && subDataSet.Tables.Count != 0 && subDataSet.Tables[0].Rows.Count != 0)
                    {
                        item.WaferCount = subDataSet.Tables[0].Rows.Count;
                        item.FaultWaferCount = subDataSet.Tables[0].Select("process_status='Failed'").Length;
                    }

                    result.Add(item);
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }


            return result;
        }
    }
}
