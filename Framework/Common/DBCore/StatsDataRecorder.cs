using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.DBCore
{
    public class StatsDataRecorder
    {
        public static List<StatsStatisticsData> QueryStatsStatistics(string sql)
        {
            List<StatsStatisticsData> result = new List<StatsStatisticsData>();
            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    string name = ds.Tables[0].Rows[i]["name"].ToString();
                    string description = ds.Tables[0].Rows[i]["description"].ToString();
                    string value =ds.Tables[0].Rows[i]["value"].ToString();
                    string[] nameSplit = name.Split('.');
                    if (nameSplit != null && nameSplit.Length == 3)
                    {
                        var tempStats = result.Where(x => x.Date == nameSplit[2]).FirstOrDefault();
                        if (tempStats != null)
                        {
                            switch (description)
                            {
                                case "Unknown":
                                    tempStats.Unknown = value;
                                    break;
                                case "Setup":
                                    tempStats.Setup = value;
                                    break;
                                case "Idle":
                                    tempStats.Idle = value;
                                    break;
                                case "Ready":
                                    tempStats.Ready = value;
                                    break;
                                case "Executing":
                                    tempStats.Executing = value;
                                    break;
                                case "Pause":
                                    tempStats.Pause = value;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            StatsStatisticsData ev = new StatsStatisticsData();
                            ev.Date = nameSplit[2];
                            switch (description)
                            {
                                case "Unknown":
                                    ev.Unknown = value;
                                    break;
                                case "Setup":
                                    ev.Setup = value;
                                    break;
                                case "Idle":
                                    ev.Idle = value;
                                    break;
                                case "Ready":
                                    ev.Ready = value;
                                    break;
                                case "Executing":
                                    ev.Executing = value;
                                    break;
                                case "Pause":
                                    ev.Pause = value;
                                    break;
                                default:
                                    break;
                            }

                            result.Add(ev);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return result;
        }

    }
}
