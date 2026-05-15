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
    public class OCRDataRecorder
    {
        public static void OcrReadComplete(string guid, string waferid, string sourcelp, string sourcecarrier, string sourceslot,
           string ocrno, string ocrjob, bool readresult, string lasermark, string ocrscore, string readperiod)
        {
            string sql = string.Format(
                "INSERT INTO \"ocr_data\"(\"guid\", \"wafer_id\", \"read_time\" , \"source_lp\", \"source_carrier\", \"source_slot\", " +
                "\"ocr_no\", \"ocr_job\", \"read_result\" , \"lasermark\", \"ocr_score\", \"read_period\")VALUES ('{0}', '{1}', '{2}','{3}','" +
                "{4}', '{5}', '{6}','{7}', '{8}', '{9}','{10}','{11}');",
                guid, waferid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                sourcelp, sourcecarrier, sourceslot, ocrno, ocrjob, readresult ? "1" : "0", lasermark, ocrscore, readperiod);


            DB.Insert(sql);
        }

        public static List<HistoryStatisticsOCRData> QueryDBOCRStatistics(string sql)
        {
            List<HistoryStatisticsOCRData> result = new List<HistoryStatisticsOCRData>();
            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;
                var TempValue = new List<Tuple<DateTime,bool>>();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    bool bresult = true;
                    switch (ds.Tables[0].Rows[i]["read_result"].ToString())
                    {
                        case "0":
                        case "false":
                        case "False":
                        case "FALSE":
                            bresult = false;
                            break;
                    }


                    TempValue.Add(new Tuple<DateTime, bool>(DateTime.Parse(ds.Tables[0].Rows[i]["read_time"].ToString()),
                        bresult)); 
                }
                 result = TempValue.GroupBy(time => time.Item1.Date)
                    .Select(x => new HistoryStatisticsOCRData
                    {
                        Date = x.Key.Date.ToString("yyyy-MM-dd"),
                        Totaltimes=x.Count().ToString(),
                        Successfueltimes=x.Where(data=> data.Item2==true).Count().ToString(),
                        Failuretimes = x.Where(data => data.Item2 == false).Count().ToString(),
                    }).ToList();
                return result;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return result;
        }


        public static List<HistoryOCRData> QueryDBOCRHistory(string sql)
        {
            List<HistoryOCRData> result = new List<HistoryOCRData>();
            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;
                var TempValue = new List<Tuple<DateTime, bool>>();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryOCRData ev = new HistoryOCRData();
                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();
                    ev.wafer_id = ds.Tables[0].Rows[i]["wafer_id"].ToString();
                    if (!ds.Tables[0].Rows[i]["read_time"].Equals(DBNull.Value))
                        ev.read_time = ((DateTime)ds.Tables[0].Rows[i]["read_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
                    ev.source_lp = ds.Tables[0].Rows[i]["source_lp"].ToString();
                    ev.source_carrier = ds.Tables[0].Rows[i]["source_carrier"].ToString();
                    ev.source_slot = ds.Tables[0].Rows[i]["source_slot"].ToString();
                    ev.ocr_no = ds.Tables[0].Rows[i]["ocr_no"].ToString();
                    ev.ocr_job = ds.Tables[0].Rows[i]["ocr_job"].ToString();
                    ev.read_result = ds.Tables[0].Rows[i]["read_result"].ToString();
                    ev.lasermark = ds.Tables[0].Rows[i]["lasermark"].ToString();
                    ev.ocr_score = ds.Tables[0].Rows[i]["ocr_score"].ToString();
                    ev.read_period = ds.Tables[0].Rows[i]["read_period"].ToString();
                    result.Add(ev);
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
