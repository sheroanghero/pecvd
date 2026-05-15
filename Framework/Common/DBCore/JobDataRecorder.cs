using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;

namespace MECF.Framework.Common.DBCore
{
    public class JobDataRecorder
    {
        public  static void StartCJ(string guid, string carrierGuid, string name, string portIn, string portOut, int totalWafer)
        {
            string sql = string.Format(
                "INSERT INTO \"cj_data\"(\"guid\", \"start_time\", \"carrier_data_guid\", \"name\",\"input_port\",\"output_port\",\"total_wafer_count\")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                carrierGuid,
                name,
                portIn,
                portOut,
                totalWafer);

            DB.Insert(sql);
        }
 

        public static void EndCJ(string guid, int abortWafer, int unprocessedWafer)
        {
            string sql = string.Format("UPDATE \"cj_data\" SET \"end_time\"='{0}', \"abort_wafer_count\"='{1}', \"unprocessed_wafer_count\"='{2}'  WHERE \"guid\"='{3}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                abortWafer, 
                unprocessedWafer,
                guid);

            DB.Insert(sql);
        }

        public static void StartPJ(string guid, string carrierGuid, string cjGuid, string name, string portIn, string portOut, int totalWafer)
        {
            string sql = string.Format(
                "INSERT INTO \"pj_data\"(\"guid\", \"start_time\", \"carrier_data_guid\", \"cj_data_guid\",\"name\",\"input_port\",\"output_port\",\"total_wafer_count\")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}');",
                guid,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                carrierGuid,
                cjGuid,
                name,
                portIn,
                portOut,
                totalWafer);

            DB.Insert(sql);
        }


        public static void EndPJ(string guid, int abortWafer, int unprocessedWafer)
        {
            string sql = string.Format("UPDATE \"pj_data\" SET \"end_time\"='{0}', \"abort_wafer_count\"='{2}', \"unprocessed_wafer_count\"='{3}' WHERE \"guid\"='{1}';",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                guid,abortWafer, unprocessedWafer);

            DB.Insert(sql);
        }

    }
}
