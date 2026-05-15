using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Sorter.Common;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.DBCore
{
    public class ProcessDataRecorder
    {
        public static void Start(string guid, string recipeName)
        {
            string sql = string.Format(
                "INSERT INTO \"process_data\"(\"guid\", \"process_begin_time\", \"recipe_name\"  )VALUES ('{0}', '{1}', '{2}' );",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                recipeName);

            DB.Insert(sql);
        }
        public static void Start(string guid, string recipeName, string waferDataGuid, string processIn, float recipeSettingTime = 0)
        {
            string sql = string.Format(
                "INSERT INTO \"process_data\"(\"guid\", \"process_begin_time\", \"recipe_name\" , \"wafer_data_guid\", \"process_in\", \"recipe_setting_time\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}' );",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                recipeName,
                waferDataGuid,
                processIn,
                recipeSettingTime);

            DB.Insert(sql);
        }

        public static void UpdateStatus(string guid, string status)
        {
            string sql = string.Format(
                "UPDATE \"process_data\" SET \"process_status\"='{0}' WHERE \"guid\"='{1}';",
                status,
                guid);

            DB.Insert(sql);
        }
        public static void End(string guid, string status)
        {
            string sql = string.Format(
                "UPDATE \"process_data\" SET \"process_end_time\"='{0}',\"process_status\"='{1}' WHERE \"guid\"='{2}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                status,
                guid);

            DB.Insert(sql);
        }
        public static void End(string guid)
        {
            string sql = string.Format(
                "UPDATE \"process_data\" SET \"process_end_time\"='{0}' WHERE \"guid\"='{1}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid);

            DB.Insert(sql);
        }

        public static void StepStart(string recipeGuid, int stepNumber, string stepName, float stepTime)
        {
            string guid = Guid.NewGuid().ToString();

            string sql = $"INSERT INTO \"recipe_step_data\"(\"guid\", \"step_begin_time\", \"step_name\" , \"step_time\", \"process_data_guid\", \"step_number\")VALUES ('{guid}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{stepName}', '{stepTime}', '{recipeGuid}', '{stepNumber}' );" ;

            //System.Diagnostics.Trace.WriteLine(sql);

            DB.Insert(sql);
        }


        public static void StepEnd(string recipeGuid, int stepNumber, List<FdcDataItem> stepData=null)
        {
            string sql = $"UPDATE \"recipe_step_data\" SET \"step_end_time\"='{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}' WHERE \"process_data_guid\"='{recipeGuid}' and \"step_number\"='{stepNumber}';" ;

            DB.Insert(sql);

            if (stepData != null && stepData.Count > 0)
            {
                foreach (var item in stepData)
                {
                    if (item.MinValue.ToString().Contains("+"))
                    {
                        item.MinValue = item.SetPoint;
                    }
                    if (item.MaxValue.ToString().Contains("+"))
                    {
                        item.MaxValue = item.SetPoint;
                    }
                    sql = $"INSERT INTO \"step_fdc_data\"(\"process_data_guid\", \"create_time\", \"step_number\" , \"parameter_name\", \"sample_count\", \"min_value\", \"max_value\", \"setpoint\", \"std_value\", \"mean_value\")VALUES ('{recipeGuid}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{stepNumber}', '{item.Name}', '{item.SampleCount}', '{item.MinValue}', '{item.MaxValue}', '{item.SetPoint}', '{item.StdValue}', '{item.MeanValue}' );";


                    DB.Insert(sql);
                }
            }
        }

        public List<string> GetHistoryRecipeList(DateTime begin, DateTime end)
        {
            List<string> result = new List<string>();

            string sql = string.Format("SELECT * FROM \"RecipeRunHistory\" where \"ProcessBeginTime\" >= '{0}' and \"ProcessBeginTime\" <= '{1}' order by \"ProcessBeginTime\" ASC;",
                begin.ToString("yyyy/MM/dd HH:mm:ss.fff"), end.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            DataSet ds = DB.ExecuteDataset(sql);
            if (ds == null)
                return result;

            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                string recipe = ds.Tables[0].Rows[i]["RecipeName"].ToString();
                if (!result.Contains(recipe))
                    result.Add(recipe);

            }

            ds.Clear();

            return result;
        }
        public static List<HistoryProcessData> QueryDBProcess(string sql)
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


                    ev.RecipeName = ds.Tables[0].Rows[i]["recipe_name"].ToString();

                    ev.Result = ds.Tables[0].Rows[i]["process_status"].ToString();

                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();

                    if (!ds.Tables[0].Rows[i]["process_begin_time"].Equals(DBNull.Value))
                        ev.StartTime = ((DateTime)ds.Tables[0].Rows[i]["process_begin_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    if (!ds.Tables[0].Rows[i]["process_end_time"].Equals(DBNull.Value))
                        ev.EndTime = ((DateTime)ds.Tables[0].Rows[i]["process_end_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    result.Add(ev);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return result;
        }

        public static List<HistoryDataItem> GetHistoryDataFromStartToEnd(IEnumerable<string> keys, DateTime begin, DateTime end, string module)
        {
            List<HistoryDataItem> result = new List<HistoryDataItem>();
            try
            {
                DateTime begintime = new DateTime(begin.Year, begin.Month, begin.Day, begin.Hour, begin.Minute, begin.Second, begin.Millisecond);

                DateTime endtime = new DateTime(begin.Year, begin.Month, end.Day, end.Hour, end.Minute, end.Second, end.Millisecond);

                string sql = "select time AS InternalTimeStamp";
                foreach (var dataId in keys)
                {
                    sql += "," + string.Format("\"{0}\"", dataId);
                }
                sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 86400;",
                    begin.ToString("yyyyMMdd") + "." + module, begintime.Ticks, endtime.Ticks);

                DataSet dataSet = DB.ExecuteDataset(sql);
                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;
                DateTime dt = new DateTime();
                Dictionary<int, string> colName = new Dictionary<int, string>();
                for (int colNo = 0; colNo < dataSet.Tables[0].Columns.Count; colNo++)
                    colName.Add(colNo, dataSet.Tables[0].Columns[colNo].ColumnName);
                for (int rowNo = 0; rowNo < dataSet.Tables[0].Rows.Count; rowNo++)
                {
                    var row = dataSet.Tables[0].Rows[rowNo];

                    for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
                    {
                        HistoryDataItem data = new HistoryDataItem();
                        if (i == 0)
                        {
                            long ticks = (long)row[i];
                            dt = new DateTime(ticks);
                            continue;
                        }
                        else
                        {
                            string dataId = colName[i];
                            if (row[i] is DBNull || row[i] == null)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = 0;
                            }
                            else if (row[i] is bool)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = (bool)row[i] ? 1 : 0;
                            }
                            else
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = float.Parse(row[i].ToString());
                            }
                        }
                        result.Add(data);
                    }
                }
                dataSet.Clear();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }

        public static List<HistoryDataItem> GetOneDayHistoryData(IEnumerable<string> keys, DateTime begin, string module)
        {
            List<HistoryDataItem> result = new List<HistoryDataItem>();
            try
            {
                DateTime begintime = new DateTime(begin.Year, begin.Month, begin.Day, 0, 0, 0, 0);

                DateTime endtime = new DateTime(begin.Year, begin.Month, begin.Day, 23, 59, 59, 999);

                string sql = "select time AS InternalTimeStamp";
                foreach (var dataId in keys)
                {
                    sql += "," + string.Format("\"{0}\"", dataId);
                }
                sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 86400;",
                    begin.ToString("yyyyMMdd") + "." + module, begintime.Ticks, endtime.Ticks);

                DataSet dataSet = DB.ExecuteDataset(sql);
                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;
                DateTime dt = new DateTime();
                Dictionary<int, string> colName = new Dictionary<int, string>();
                for (int colNo = 0; colNo < dataSet.Tables[0].Columns.Count; colNo++)
                    colName.Add(colNo, dataSet.Tables[0].Columns[colNo].ColumnName);
                for (int rowNo = 0; rowNo < dataSet.Tables[0].Rows.Count; rowNo++)
                {
                    var row = dataSet.Tables[0].Rows[rowNo];

                    for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
                    {
                        HistoryDataItem data = new HistoryDataItem();
                        if (i == 0)
                        {
                            long ticks = (long)row[i];
                            dt = new DateTime(ticks);
                            continue;
                        }
                        else
                        {
                            string dataId = colName[i];
                            if (row[i] is DBNull || row[i] == null)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = 0;
                            }
                            else if (row[i] is bool)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = (bool)row[i] ? 1 : 0;
                            }
                            else
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = float.Parse(row[i].ToString());
                            }
                        }
                        result.Add(data);
                    }
                }
                dataSet.Clear();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }

        public static List<HistoryDataItem> GetHistoryData(IEnumerable<string> keys, string recipeRunGuid, string module)
        {
            List<HistoryDataItem> result = new List<HistoryDataItem>();

            try
            {

                string sql = string.Format("SELECT * FROM \"process_data\" where \"guid\" = '{0}'",
                    recipeRunGuid);

                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                if (ds.Tables[0].Rows.Count == 0)
                    return result;

                object from = ds.Tables[0].Rows[0]["process_begin_time"];

                if (from is DBNull)
                {
                    LOG.Write(string.Format("{0} not set start time", recipeRunGuid));
                    return result;
                }

                DateTime begin = (DateTime)from;

                object to = ds.Tables[0].Rows[0]["process_end_time"];
                if (to is DBNull)
                {
                    to = new DateTime(begin.Year, begin.Month, begin.Day, 23, 59, 59, 999);
                }

                DateTime end = (DateTime)to;

                sql = "select time AS InternalTimeStamp";
                foreach (var dataId in keys)
                {
                    sql += "," + string.Format("\"{0}\"", dataId);
                }
                sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc LIMIT 2000;",
                    begin.ToString("yyyyMMdd") + "." + module, begin.Ticks, end.Ticks);

                DataSet dataSet = DB.ExecuteDataset(sql);
                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;
                DateTime dt = new DateTime();
                Dictionary<int, string> colName = new Dictionary<int, string>();
                for (int colNo = 0; colNo < dataSet.Tables[0].Columns.Count; colNo++)
                    colName.Add(colNo, dataSet.Tables[0].Columns[colNo].ColumnName);
                for (int rowNo = 0; rowNo < dataSet.Tables[0].Rows.Count; rowNo++)
                {
                    var row = dataSet.Tables[0].Rows[rowNo];

                    for (int i = 0; i < dataSet.Tables[0].Columns.Count; i++)
                    {
                        HistoryDataItem data = new HistoryDataItem();
                        if (i == 0)
                        {
                            long ticks = (long)row[i];
                            dt = new DateTime(ticks);
                            continue;
                        }
                        else
                        {
                            string dataId = colName[i];
                            if (row[i] is DBNull || row[i] == null)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = 0;
                            }
                            else if (row[i] is bool)
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = (bool)row[i] ? 1 : 0;
                            }
                            else
                            {
                                data.dateTime = dt;
                                data.dbName = colName[i];
                                data.value = float.Parse(row[i].ToString());
                            }
                        }
                        result.Add(data);
                    }
                }
                dataSet.Clear();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }



    }


}
