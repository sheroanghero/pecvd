using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.DBCore
{
    public class LeakCheckDataRecorder
    {
        public static void Add(int leakCheckTime, int beginPressure, int endPressure, double leakRate, string status, string mode, string module_name = "System", string gaslineSelection = "")
        {
            string sql = string.Format(
                "INSERT INTO \"leak_check_data\"(\"guid\", \"operate_time\", \"status\" , \"leak_rate\", \"start_pressure\", \"stop_pressure\", \"mode\", \"leak_check_time\", \"module_name\", \"gasline_selection\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}' , '{8}', '{9}' );",
                Guid.NewGuid(),
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                status,
                leakRate,
                beginPressure,
                endPressure,
                mode,
                leakCheckTime,
                module_name,
                gaslineSelection);

            DB.Insert(sql);
        }

        public static void Add(int leakCheckTime, double beginPressure, double endPressure, double leakRate, string status, string mode, string module_name="System", string gaslineSelection="")
        {
            string sql = string.Format(
                "INSERT INTO \"leak_check_data\"(\"guid\", \"operate_time\", \"status\" , \"leak_rate\", \"start_pressure\", \"stop_pressure\", \"mode\", \"leak_check_time\", \"module_name\", \"gasline_selection\" )VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}' , '{8}', '{9}' );",
                Guid.NewGuid(),
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                status,
                leakRate,
                beginPressure,
                endPressure,
                mode,
                leakCheckTime,
                module_name,
                gaslineSelection);
 
            DB.Insert(sql);
        }
 
    }
}
