using Aitex.Core.RT.DBCore;
using Aitex.Sorter.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.DBCore
{
    public class FfuDiffPressureDataRecorder
    {
        public static List<HistoryFfuDiffPressureData> FfuDiffPressureHistory(string sql)
        {
            var strs = sql.Split(';');
            var beginTime = strs[0];
            var endTime = strs[1];
            DateTime beginDateTime = new DateTime(long.Parse(beginTime));
            DateTime endDateTime = new DateTime(long.Parse(endTime));
            if (endDateTime <= beginDateTime) return null;
            if (beginDateTime.Day != endDateTime.Day)
            {
                string sql1 = string.Format(
                   "SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;",
                   long.Parse(beginTime), DateTime.Parse(beginDateTime.ToString("yyyy-MM-dd 23:59:59")).Ticks, beginDateTime.ToString("yyyyMMdd"));
                DataSet ds = DB.ExecuteDataset(sql1);
                List<HistoryFfuDiffPressureData> result = new List<HistoryFfuDiffPressureData>();
                DSToList(ds, result);
                int daytime = 1;
                while (beginDateTime.AddDays(daytime).Day != endDateTime.Day)
                {
                    DateTime newDate = beginDateTime.AddDays(daytime);
                    string sql2 = string.Format(
                  "SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{0}.Data\"  order by \"time\" ASC;",
                   newDate.ToString("yyyyMMdd"));
                    DataSet ds1 = DB.ExecuteDataset(sql2);
                    DSToList(ds1, result);
                    daytime++;
                }
                string sql3 = string.Format(
                  "SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;",
                  DateTime.Parse(endDateTime.ToString("yyyy-MM-dd 0:0:0")).Ticks, long.Parse(endTime), endDateTime.ToString("yyyyMMdd"));
                DataSet ds3 = DB.ExecuteDataset(sql3);
                DSToList(ds3, result);
                return result;
            }
            else
            {
                string sql1 = string.Format(
                   "SELECT \"time\",\"DiffPressure.DiffPressure1\",\"DiffPressure.DiffPressure2\",\"FFU.FFU1Speed\",\"FFU.FFU2Speed\" FROM \"{2}.Data\" where \"time\" >= '{0}' and \"time\" <= '{1}'  order by \"time\" ASC;",
                   long.Parse(beginTime), long.Parse(endTime), beginDateTime.ToString("yyyyMMdd"));
                DataSet ds = DB.ExecuteDataset(sql1);
                List<HistoryFfuDiffPressureData> result = new List<HistoryFfuDiffPressureData>();
                DSToList(ds, result);
                return result;
            }
           
        }

        private static void DSToList(DataSet ds, List<HistoryFfuDiffPressureData> result)
        {
            if (ds != null && ds.Tables.Count != 0)
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    result.Add(new HistoryFfuDiffPressureData()
                    {
                        Time = ds.Tables[0].Rows[i]["time"].ToString(),
                        DiffPressure1 = ds.Tables[0].Rows[i]["DiffPressure.DiffPressure1"].ToString(),
                        DiffPressure2 = ds.Tables[0].Rows[i]["DiffPressure.DiffPressure2"].ToString(),
                        FFU1Speed = ds.Tables[0].Rows[i]["FFU.FFU1Speed"].ToString(),
                        FFU2Speed = ds.Tables[0].Rows[i]["FFU.FFU2Speed"].ToString(),
                    });
                }
            }
        }
    }
}
