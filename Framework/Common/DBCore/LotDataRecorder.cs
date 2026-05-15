using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
    public class LotDataRecorder
    {
        public  static void StartLot(string guid, string carrierGuid, string cjGuid, string name, string portIn, string portOut, int totalWafer)
        {
            string sql = 
                $"INSERT INTO \"lot_data\"(\"guid\", \"start_time\", \"carrier_data_guid\", \"cj_data_guid\",\"name\",\"input_port\",\"output_port\",\"total_wafer_count\")VALUES ('{guid}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{carrierGuid}', '{cjGuid}', '{name}', '{portIn}', '{portOut}', '{totalWafer}');";

            DB.Insert(sql);
        }
 

        public static void EndLot(string guid, int abortWafer, int unprocessedWafer)
        {
            string sql = $"UPDATE \"lot_data\" SET \"end_time\"='{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', \"abort_wafer_count\"='{abortWafer}', \"unprocessed_wafer_count\"='{unprocessedWafer}'  WHERE \"guid\"='{guid}';" ;

            DB.Insert(sql);
        }

        public static void InsertLotWafer(string lotGuid, string waferGuid)
        {
            string guid = Guid.NewGuid().ToString();
            string sql =
                $"INSERT INTO \"lot_wafer_data\"(\"guid\", \"create_time\", \"lot_data_guid\", \"wafer_data_guid\")VALUES ('{guid}', '{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}', '{lotGuid}', '{waferGuid}');";

            DB.Insert(sql);
        }
    }
}
