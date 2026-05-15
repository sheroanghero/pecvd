using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.DBCore
{
    public class RecipeDataRecorder
    {
        public static void RecipeStart(string guid, string waferDataGuid, string recipeName, string settingTime, string chamber)
        {
            string sql = string.Format(
                "INSERT INTO \"recipe_data\"(\"guid\", \"wafer_data_guid\", \"recipe_begin_time\", \"recipe_name\", \"recipe_setting_time\" , \"chamber\"  )VALUES ('{0}', '{1}', '{2}' , '{3}', '{4}', '{5}' );",
                guid,
                waferDataGuid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                recipeName,
                settingTime,
                chamber);

            DB.Insert(sql);
        }

        public static void RecipeEnd(string guid)
        {
            string sql = string.Format(
                "UPDATE \"recipe_data\" SET \"recipe_end_time\"='{0}' WHERE \"guid\"='{1}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid);

            DB.Insert(sql);
        }

        public static void RecipeStepStart(string guid, int recipeStepNo, string recipeStepName, string settingTime)
        {
            string sql = string.Format(
                "INSERT INTO \"recipe_step_data\"(\"guid\", \"recipe_step_no\", \"recipe_step_name\", \"recipe_step_setting_time\" , \"recipe_step_begin_time\")VALUES ('{0}', '{1}', '{2}' , '{3}', '{4}' );",
                guid,
                recipeStepNo,
                recipeStepName,
                settingTime,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));

            DB.Insert(sql);
        }

        public static void RecipeStepEnd(string guid, int recipeStepNo)
        {
            string sql = string.Format(
                "UPDATE \"recipe_step_data\" SET \"recipe_step_end_time\"='{0}' WHERE \"guid\"='{1}' AND \"recipe_step_no\"='{2}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid,
                recipeStepNo);

            DB.Insert(sql);
        }

        public static WaferHistoryRecipe GetWaferHistoryRecipe(string id)
        {
            WaferHistoryRecipe result = new WaferHistoryRecipe();
            try
            {
                string sql = string.Format("SELECT * FROM \"recipe_data\" where \"guid\" = '{0}'  limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    result.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
                    result.Type = WaferHistoryItemType.Recipe;
                    result.Chamber = dataSet.Tables[0].Rows[i]["chamber"].ToString();
                    result.SettingTime = dataSet.Tables[0].Rows[i]["recipe_setting_time"].ToString();

                    if (!dataSet.Tables[0].Rows[i]["recipe_begin_time"].Equals(DBNull.Value))
                        result.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_begin_time"].ToString());

                    if (!dataSet.Tables[0].Rows[i]["recipe_end_time"].Equals(DBNull.Value))
                        result.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["recipe_end_time"].ToString());

                    result.ActualTime = result.EndTime.CompareTo(result.StartTime) <= 0 ? "" : result.EndTime.Subtract(result.StartTime).ToString();
                    result.Recipe = dataSet.Tables[0].Rows[i]["recipe_name"].ToString();
                    result.Name = dataSet.Tables[0].Rows[i]["recipe_name"].ToString();

                    result.Steps = GetRecipeStepInfoList(id);
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return result;
        }
        public static List<WaferHistoryWafer> GetWaferHistoryWafers(string id)
        {
            List<WaferHistoryWafer> result = new List<WaferHistoryWafer>();

            try
            {
                string sql = string.Format("SELECT * FROM \"wafer_data\" where \"carrier_data_guid\" = '{0}' and \"lot_id\" <> '' order by  \"wafer_id\"  ASC  limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    WaferHistoryWafer item = new WaferHistoryWafer();

                    item.ID = dataSet.Tables[0].Rows[i]["guid"].ToString();
                    item.Type = WaferHistoryItemType.Wafer;
                    item.Name = dataSet.Tables[0].Rows[i]["wafer_id"].ToString();

                    if (!dataSet.Tables[0].Rows[i]["sequence_name"].Equals(DBNull.Value))
                    {
                        item.ProcessJob = dataSet.Tables[0].Rows[i]["sequence_name"].ToString();
                    }

                    if (!dataSet.Tables[0].Rows[i]["create_time"].Equals(DBNull.Value))
                        item.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["create_time"].ToString());

                    if (!dataSet.Tables[0].Rows[i]["delete_time"].Equals(DBNull.Value))
                        item.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["delete_time"].ToString());

                    result.Add(item);
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return result;
        }
        private static List<RecipeStep> GetRecipeStepInfoList(string guid)
        {
            List<RecipeStep> result = new List<RecipeStep>();
            DataSet recipeStepDataSet = DB.ExecuteDataset(string.Format("SELECT * FROM \"recipe_step_data\" where \"guid\" = '{0}'  limit 1000;", guid));
            if (recipeStepDataSet != null && recipeStepDataSet.Tables.Count != 0 && recipeStepDataSet.Tables[0].Rows.Count != 0)
            {
                for (int i = 0; i < recipeStepDataSet.Tables[0].Rows.Count; i++)
                {
                    RecipeStep rs = new RecipeStep();
                    rs.No = int.Parse(recipeStepDataSet.Tables[0].Rows[i]["recipe_step_no"].ToString());
                    rs.Name = recipeStepDataSet.Tables[0].Rows[i]["recipe_step_name"].ToString();
                    rs.SettingTime = recipeStepDataSet.Tables[0].Rows[i]["recipe_step_setting_time"].ToString();

                    if (!recipeStepDataSet.Tables[0].Rows[i]["recipe_step_begin_time"].Equals(DBNull.Value))
                        rs.StartTime = DateTime.Parse(recipeStepDataSet.Tables[0].Rows[i]["recipe_step_begin_time"].ToString());

                    if (!recipeStepDataSet.Tables[0].Rows[i]["recipe_step_end_time"].Equals(DBNull.Value))
                        rs.EndTime = DateTime.Parse(recipeStepDataSet.Tables[0].Rows[i]["recipe_step_end_time"].ToString());

                    rs.ActualTime = rs.EndTime.CompareTo(rs.StartTime) <= 0 ? "" : rs.EndTime.Subtract(rs.StartTime).ToString();
                    result.Add(rs);
                }
            }

            return result;
        }

        public static List<WaferHistoryRecipe> GetWaferHistoryRecipes(string id)
        {
            List<WaferHistoryRecipe> result = new List<WaferHistoryRecipe>();
            try
            {
                string sql = string.Format("SELECT * FROM \"process_data\" where \"wafer_data_guid\" = '{0}'  limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    result.Add(GetWaferHistoryRecipe(dataSet.Tables[0].Rows[i]["guid"].ToString()));
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return result;
        }

        public static List<WaferHistorySecquence> GetWaferHistorySecquences(string id)
        {
            List<WaferHistorySecquence> result = new List<WaferHistorySecquence>();
            try
            {
                string sql = string.Format("SELECT * FROM \"wafer_data\" where \"guid\" = '{0}'  and \"lot_id\" <> '' order by  \"wafer_id\"  ASC  limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;
                int index = 0;
                try
                {
                    sql = string.Format("SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{0}' order by \"arrive_time\" ASC limit 1000;", id);

                    DataSet dataSeta = DB.ExecuteDataset(sql);

                    if (dataSeta.Tables.Count == 0 || dataSeta.Tables[0].Rows.Count == 0)
                        return result;
                    
                    for (int i = 0; i < dataSeta.Tables[0].Rows.Count; i++)
                    {
                        if (dataSeta.Tables[0].Rows[i]["station"].ToString() == "Cassette")
                        {
                            index = i + 1;
                            break;
                        }
                        if (i == dataSeta.Tables[0].Rows.Count - 1)
                            index = i + 1;
                    }
                    if (dataSeta.Tables[0].Rows.Count % index > 0)
                        index = dataSeta.Tables[0].Rows.Count / index + 1;
                    else
                        index = dataSeta.Tables[0].Rows.Count / index;
                }
                catch (Exception e)
                {
                    LOG.Write(e);
                }
                for (int i = 0; i < index; i++)
                {
                    result.Add(GetWaferHistorySecquence(id, dataSet.Tables[0].Rows[0]["sequence_name"].ToString(),i));
                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return result;
        }
        public static WaferHistorySecquence GetWaferHistorySecquence(string id,string SecquenceName,int count)
        {
            WaferHistorySecquence result = new WaferHistorySecquence();
            try
            {
                string sql = string.Format("SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{0}' order by \"arrive_time\" ASC limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;
                int index = 0;
                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    if (dataSet.Tables[0].Rows[i]["station"].ToString() == "Cassette")
                    {
                        index = i;
                        break;
                    }
                    if (i == dataSet.Tables[0].Rows.Count - 1)
                        index = i;
                }
                for (int i = 0; i < dataSet.Tables[0].Rows.Count;)
                {
                    i = count > 0 ? index * count + count - 1: index * count + count;
                    result.SecquenceName = SecquenceName;
                    result.Recipe = SecquenceName;
                    result.SecQuenceStartTime = dataSet.Tables[0].Rows[i]["arrive_time"].ToString();
                    result.StartTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["arrive_time"].ToString());
                    i = index * count + count;
                    i += index;
                    if (i >= dataSet.Tables[0].Rows.Count)
                    {
                        result.SecQuenceEndTime = dataSet.Tables[0].Rows[dataSet.Tables[0].Rows.Count - 1]["arrive_time"].ToString();
                        result.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[dataSet.Tables[0].Rows.Count - 1]["arrive_time"].ToString());
                    }
                    else
                    {
                        result.SecQuenceEndTime = dataSet.Tables[0].Rows[i]["arrive_time"].ToString();
                        result.EndTime = DateTime.Parse(dataSet.Tables[0].Rows[i]["arrive_time"].ToString());
                    }
                    result.ActualTime = result.EndTime.CompareTo(result.StartTime) <= 0 ? "" : result.EndTime.Subtract(result.StartTime).ToString();
                    break;
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
