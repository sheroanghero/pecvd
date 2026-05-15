using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

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
    public class WaferMoveHistoryRecorder
    {

        public static void WaferMoved(string guid, string station, int slot, string status )
        {
            string sql = string.Format("INSERT INTO \"wafer_move_history\"(\"wafer_data_guid\", \"arrive_time\", \"station\", \"slot\", \"status\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}' );",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                station,
                slot+1,
                status);

            DB.Insert(sql);
        }

        public static List<HistoryMoveData> QueryDBMovement(string sql)
        {
            List<HistoryMoveData> result = new List<HistoryMoveData>();

            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryMoveData ev = new HistoryMoveData();

                    ev.WaferGuid = ds.Tables[0].Rows[i]["wafer_data_guid"].ToString();

                    ev.Station = ds.Tables[0].Rows[i]["station"].ToString();

                    ev.Slot = ds.Tables[0].Rows[i]["slot"].ToString();

                    ev.Result = ds.Tables[0].Rows[i]["status"].ToString();

                    if (!ds.Tables[0].Rows[i]["arrive_time"].Equals(DBNull.Value))
                        ev.ArriveTime = ((DateTime)ds.Tables[0].Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
 
                    result.Add(ev);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


            return result;
        }

        public static List<WaferHistoryMovement> GetWaferHistoryMovements(string id)
        {
            //WaferHistoryMovement temp = new WaferHistoryMovement();
            //temp.Source = "LP1";
            //temp.Destination = "PM1";
            //temp.InTime = DateTime.Now.ToString();

            List<WaferHistoryMovement> result = new List<WaferHistoryMovement>();
            try
            {
                string sql = string.Format("SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{0}' order by \"arrive_time\" ASC limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    WaferHistoryMovement item = new WaferHistoryMovement();

                    item.Source = $"station : {dataSet.Tables[0].Rows[i]["station"]} slot : {dataSet.Tables[0].Rows[i]["slot"]}";
                    if (i != dataSet.Tables[0].Rows.Count - 1)
                        item.Destination = $"station : {dataSet.Tables[0].Rows[i + 1]["station"]} slot : {dataSet.Tables[0].Rows[i + 1]["slot"]}";
                    else
                        item.Destination = "";
                    item.InTime = dataSet.Tables[0].Rows[i]["arrive_time"].ToString();

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
