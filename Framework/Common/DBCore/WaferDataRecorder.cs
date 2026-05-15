using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
    public class WaferDataRecorder
    {
        public static void CreateWafer(string guid, string carrierGuid, string station, int slot, string waferId, string status = "")
        {
            string sql = string.Format(
                "INSERT INTO \"wafer_data\"(\"guid\", \"create_time\", \"carrier_data_guid\", \"create_station\", \"wafer_id\",\"create_slot\",\"process_status\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' );",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                carrierGuid,
                station,
                waferId,
                slot + 1,
                status);

            DB.Insert(sql);
        }

        public static void SetProcessInfo(string guid, string processGuid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"process_data_guid\"='{0}' WHERE \"guid\"='{1}';",
                processGuid,
                guid);

            DB.Insert(sql);
        }


        public static void SetWaferMarker(string guid, string marker)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"lasermarker1\"='{0}'  WHERE \"guid\"='{1}';",
                marker,
                guid);

            DB.Insert(sql);
        }

        public static void SetPjInfo(string guid, string pjGuid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"pj_data_guid\"='{0}' WHERE \"guid\"='{1}';",
                pjGuid,
                guid);

            DB.Insert(sql);
        }

        public static void SetCjInfo(string guid, string cjGuid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"lot_data_guid\"='{0}' WHERE \"guid\"='{1}';",
                cjGuid,
                guid);

            DB.Insert(sql);
        }
        public static void SetWaferLotId(string guid, string lotId)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"lot_id\"='{0}'  WHERE \"guid\"='{1}';",
                lotId,
                guid);

            DB.Insert(sql);
        }
        public static void SetWaferStatus(string guid, string status)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"process_status\"='{0}'  WHERE \"guid\"='{1}';",
                status,
                guid);

            DB.Insert(sql);
        }
        public static void SetWaferNotchAngle(string guid, float angle)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"notch_angle\"='{0}'  WHERE \"guid\"='{1}';",
                angle,
                guid);

            DB.Insert(sql);
        }
        public static void SetCjobID(string guid, string cjGuid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"control_job_guid\"='{0}' WHERE \"guid\"='{1}';",
                cjGuid,
                guid);

            DB.Insert(sql);
        }

        public static void SetPjobID(string guid, string cjGuid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"process_job_guid\"='{0}' WHERE \"guid\"='{1}';",
                cjGuid,
                guid);

            DB.Insert(sql);
        }
        public static void SetWaferSequence(string guid, string sequence)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"sequence_name\"='{0}'  WHERE \"guid\"='{1}';",
                sequence,
                guid);

            DB.Insert(sql);
        }

        public static void SetWaferMarkerWithScoreAndFileName(string guid, string marker, string score, string fileName, string filePath)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"lasermarker1\"='{0}' , \"lasermarker1Score\"='{1}' , \"fileName\"='{2}' , \"filePath\"='{3}' WHERE \"guid\"='{4}';",
                marker,
                score,
                fileName,
                filePath,
                guid);

            DB.Insert(sql);
        }

        public static void SetWaferT7Code(string guid, string t7code)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET  \"t7code1\"='{0}' WHERE \"guid\"='{1}';",
                t7code,
                guid);

            DB.Insert(sql);
        }

        public static void SetWaferT7CodeWithScoreAndFileName(string guid, string t7code, string score, string fileName, string filePath)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"t7code1\"='{0}' , \"t7code1Score\"='{1}' , \"fileName\"='{2}' , \"filePath\"='{3}' WHERE \"guid\"='{4}';",
                t7code,
                score,
                fileName,
                filePath,
                guid);

            DB.Insert(sql);
        }

        public static void SetWaferId(string guid, string marker1, string marker2, string marker3, string t7code1, string t7code2, string t7code3)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"lasermarker1\"='{0}', \"t7code1\"='{1}' WHERE \"guid\"='{2}';",
                marker1,
                t7code1,
                guid);

            DB.Insert(sql);
        }
        public static void UpdateWaferId(string guid, string waferID)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"wafer_id\"='{0}' WHERE \"guid\"='{1}';",
                waferID,
                guid);

            DB.Insert(sql);
        }

        public static void DeleteWafer(string guid)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"delete_time\"='{0}' WHERE \"guid\"='{1}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid);

            DB.Insert(sql);
        }
        public static void SetStatistics(string guid, float useCount, float useTime, float useThick)
        {
            string sql = string.Format("UPDATE \"wafer_data\" SET \"use_count\"='{0}', \"use_time\"='{1}', \"use_thick\"='{2}' WHERE \"guid\"='{3}';",
                useCount,
                useTime,
                useThick,
                guid);

            DB.Insert(sql);
        }
        public static List<HistoryWaferData> QueryDBWafer(string sql)
        {
            List<HistoryWaferData> result = new List<HistoryWaferData>();

            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds == null)
                    return result;

                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    HistoryWaferData ev = new HistoryWaferData();

                    ev.Guid = ds.Tables[0].Rows[i]["guid"].ToString();

                    ev.LaserMarker = ds.Tables[0].Rows[i]["lasermarker1"].ToString();

                    ev.T7Code = ds.Tables[0].Rows[i]["t7code1"].ToString();

                    if (!ds.Tables[0].Rows[i]["create_time"].Equals(DBNull.Value))
                        ev.CreateTime = ((DateTime)ds.Tables[0].Rows[i]["create_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");



                    if (!ds.Tables[0].Rows[i]["delete_time"].Equals(DBNull.Value))
                        ev.DeleteTime = ((DateTime)ds.Tables[0].Rows[i]["delete_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                    ev.LotId = ds.Tables[0].Rows[i]["lot_id"].ToString();
                    ev.Station = ds.Tables[0].Rows[i]["create_station"].ToString();

                    ev.Slot = ds.Tables[0].Rows[i]["create_slot"].ToString();

                    ev.CarrierGuid = ds.Tables[0].Rows[i]["carrier_data_guid"].ToString();

                    ev.WaferId = ds.Tables[0].Rows[i]["wafer_id"].ToString();

                    result.Add(ev);
                }

                result.Sort((x, y) => int.Parse(x.Slot) - int.Parse(y.Slot));
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
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
    }

    public class OcrDataRecorder
    {
        public static void RecordOcrReadResult(string guid, string waferid, int souceplp, string sourcecarrier,
            int sourceslot, int ocrno, string ocrjob, bool readresult, string lasermark, string ocrscore, string readperiod)
        {
            string sql = string.Format(
                "INSERT INTO \"ocr_data\"(\"guid\", \"wafer_id\", \"read_time\", \"source_lp\", " +
                "\"source_carrier\",\"source_slot\",\"ocr_no\" ,\"ocr_job\",\"read_result\",\"lasermark\" ," +
                "\"ocr_score\" ,\"read_period\")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}','{6}', '{7}', '{8}', '{9}', '{10}', '{11}' );",
                guid,
                waferid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                souceplp.ToString(),
                sourcecarrier,
                sourceslot.ToString(),
                ocrno.ToString(),
                ocrjob,
                readresult,
                lasermark,
                ocrscore,
                readperiod);

            DB.Insert(sql);
        }
    }

    public class WaferProcessDataRecorder
    {
        public static void WaferProcessDataRecord(string guid, string station, string dataName, string dataValue)
        {
            string sql = string.Format("INSERT INTO \"wafer_process_data\"(\"wafer_guid\", \"process_time\", \"station\", \"data_name\", \"data_value\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}' );",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                station,
                dataName,
                dataValue);

            DB.Insert(sql);
        }


        public static List<WaferHistoryMetrology> GetWaferProcessDatas(string id)
        {
            //WaferHistoryMovement temp = new WaferHistoryMovement();
            //temp.Source = "LP1";
            //temp.Destination = "PM1";
            //temp.InTime = DateTime.Now.ToString();

            List<WaferHistoryMetrology> result = new List<WaferHistoryMetrology>();
            try
            {
                string sql = string.Format("SELECT * FROM \"wafer_process_data\" where \"wafer_guid\" = '{0}' order by \"process_time\" ASC limit 1000;", id);

                DataSet dataSet = DB.ExecuteDataset(sql);

                if (dataSet == null)
                    return result;

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
                    return result;

                for (int i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    WaferHistoryMetrology item = new WaferHistoryMetrology();

                    item.stationname = dataSet.Tables[0].Rows[i]["station"].ToString();
                    item.processtime = dataSet.Tables[0].Rows[i]["processtime"].ToString();
                    item.dataname = dataSet.Tables[0].Rows[i]["data_name"].ToString();
                    item.datavalue = dataSet.Tables[0].Rows[i]["data_value"].ToString();
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
