using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using SciChart.Core.Extensions;

namespace MECF.Framework.Common.DBCore
{
	/*
     *
  guid text NOT NULL,
  wafer_data_guid text,
  arrive_time timestamp without time zone,
  station timestamp without time zone,
  slot text,
  status text,
  CONSTRAINT wafer_move_history_pkey PRIMARY KEY (guid)
     *
     *
     *
     *
     */
	public class JobMoveHistoryRecorder
	{
 
		public static void JobArrived(string jobGuid, string stationName)
		{
 
				string sql = string.Format("INSERT INTO \"job_move_history\"(\"job_guid\", \"arrive_time\", \"station\")VALUES ('{0}', '{1}', '{2}');",
				jobGuid,
				DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
				stationName);

				DB.Insert(sql);
 
		}

		public static void JobLeft(string jobGuid, string stationName,string processTime)
		{
 
				string sql = string.Format(
							"UPDATE \"job_move_history\" SET \"leave_time\"='{1}' , \"process_time\"='{2}' WHERE \"job_guid\"='{0}' AND \"station\"='{3}' AND \"leave_time\" is null ;",
							jobGuid,
							DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
							processTime,
							stationName);

				DB.Insert(sql);
 
		}

		public static List<HistoryJobMoveData> QueryJobMovement(string jobGuid)
		{
			List<HistoryJobMoveData> result = new List<HistoryJobMoveData>();
 
				try
				{
					if (jobGuid.IsNullOrEmpty())
						return result;
					string sql = string.Format(
						"SELECT * FROM \"job_move_history\" WHERE \"job_guid\"='{0}';",
						jobGuid);

					DataSet ds = DB.ExecuteDataset(sql);
					if (ds == null)
						return result;

					for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
					{
						HistoryJobMoveData ev = new HistoryJobMoveData();

						ev.JobGuid = ds.Tables[0].Rows[i]["job_guid"].ToString();
						ev.Station = ds.Tables[0].Rows[i]["station"].ToString();
						ev.ProcessTime = ds.Tables[0].Rows[i]["process_time"].ToString();
					if (!ds.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
							ev.ArriveTime = ((DateTime)ds.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
						if (!ds.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
							ev.LeaveTime = ((DateTime)ds.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

						result.Add(ev);
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
 

			return result;
		}

		public static List<HistoryJobMoveData> QueryJobMovement(string jobGuid, string stationName)
		{
			List<HistoryJobMoveData> result = new List<HistoryJobMoveData>();
 
				try
				{
					string sql = string.Format(
						"SELECT * FROM \"job_move_history\" WHERE \"job_guid\"='{0}' AND \"station\"='{1}' ;",
						jobGuid,
						stationName);

					DataSet ds = DB.ExecuteDataset(sql);
					if (ds == null)
						return result;

					for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
					{
						HistoryJobMoveData ev = new HistoryJobMoveData();

						ev.JobGuid = ds.Tables[0].Rows[i]["job_guid"].ToString();
						ev.Station = ds.Tables[0].Rows[i]["station"].ToString();
						ev.ProcessTime = ds.Tables[0].Rows[i]["process_time"].ToString();
					if (!ds.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
							ev.ArriveTime =
								((DateTime)ds.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
						if (!ds.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
							ev.LeaveTime =
								((DateTime)ds.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

						result.Add(ev);
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
 

			return result;
		}


		public static List<HistoryJobMoveData> QueryJobMovementBysql(string sql)
		{
			List<HistoryJobMoveData> result = new List<HistoryJobMoveData>();
 
				try
				{
					DataSet ds = DB.ExecuteDataset(sql);
					if (ds == null)
						return result;

					for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
					{
						HistoryJobMoveData ev = new HistoryJobMoveData();

						ev.JobGuid = ds.Tables[0].Rows[i]["job_guid"].ToString();
						ev.Station = ds.Tables[0].Rows[i]["station"].ToString();
						ev.ProcessTime = ds.Tables[0].Rows[i]["process_time"].ToString();
						if (!ds.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
							ev.ArriveTime =
								((DateTime)ds.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
						if (!ds.Tables[0].Rows[i]["leave_time"].Equals(DBNull.Value))
							ev.LeaveTime =
								((DateTime)ds.Tables[0].Rows[i]["leave_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

						result.Add(ev);
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
 

			return result;
		}
	}

}
