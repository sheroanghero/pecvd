using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
    internal class DatabaseTable
    {
        public static void UpgradeDataTable()
        {
            DB.CreateTableIfNotExisted("lot_data", new Dictionary<string, Type>()
            {
                {"guid", typeof(string) },
                {"start_time", typeof(DateTime) },
                {"end_time", typeof(DateTime) },
                {"carrier_data_guid", typeof(string) },
                {"cj_data_guid", typeof(string) },
                {"name", typeof(string) },
                {"input_port", typeof(string) },
                {"output_port", typeof(string) },
                {"total_wafer_count", typeof(int) },
                {"abort_wafer_count", typeof(int) },
                {"unprocessed_wafer_count", typeof(int) },
            }, false, "guid");

            DB.CreateTableIfNotExisted("recipe_step_data", new Dictionary<string, Type>()
            {
                {"guid", typeof(string) },
                {"step_begin_time", typeof(DateTime) },
                {"step_end_time", typeof(DateTime) },
                {"step_name", typeof(string) },
                {"step_time", typeof(float) },
                {"process_data_guid", typeof(string) },
                {"step_number", typeof(int) },
            }, false, "guid");
            DB.CreateTableIndexIfNotExisted("recipe_step_data", "recipe_step_data_idx1", "CREATE INDEX recipe_step_data_idx1 ON public.recipe_step_data USING btree" +
                "(\"process_data_guid\", \"step_number\");");




            DB.CreateTableIfNotExisted("step_fdc_data", new Dictionary<string, Type>()
            {
                {"process_data_guid", typeof(string) },
                {"step_number", typeof(int) },
                {"create_time", typeof(DateTime) },
                {"parameter_name", typeof(string) },
                {"sample_count", typeof(int) },
                {"min_value", typeof(float) },
                {"max_value", typeof(float) },
                {"setpoint", typeof(float) },
                {"std_value", typeof(float) },
                {"mean_value", typeof(float) },
            }, false, "");
            DB.CreateTableIndexIfNotExisted("step_fdc_data", "step_fdc_data_idx1", "CREATE INDEX step_fdc_data_idx1 ON public.step_fdc_data USING btree" +
                "(\"process_data_guid\", \"step_number\");");







            DB.CreateTableIfNotExisted("lot_wafer_data", new Dictionary<string, Type>()
            {
                {"guid", typeof(string) },
                {"create_time", typeof(DateTime) },
                {"lot_data_guid", typeof(string) },
                {"wafer_data_guid", typeof(string) },
            }, false, "guid");

            DB.CreateTableIndexIfNotExisted("lot_wafer_data", "lot_wafer_data_idx1", "CREATE INDEX lot_wafer_data_idx1 ON public.lot_wafer_data USING btree"+
                                            "(lot_data_guid COLLATE pg_catalog.\"default\", wafer_data_guid COLLATE pg_catalog.\"default\");");

            DB.CreateTableColumn("wafer_data", new Dictionary<string, Type>()
            {
                {"lot_id", typeof(string) },
                {"notch_angle", typeof(float) },
                {"sequence_name", typeof(string) },
                {"process_status", typeof(string) },
            });

            DB.CreateTableColumn("cj_data", new Dictionary<string, Type>()
            {
                {"total_wafer_count", typeof(int) },
                {"abort_wafer_count", typeof(int) },
                {"unprocessed_wafer_count", typeof(int) },
            });

            DB.CreateTableColumn("leak_check_data", new Dictionary<string, Type>()
            {
                {"module_name", typeof(string) },
                {"gasline_selection", typeof(string) },
            });

            DB.CreateTableColumn("process_data", new Dictionary<string, Type>()
            {
                {"recipe_setting_time", typeof(float) },
            });

            DB.CreateTableColumn("event_data", new Dictionary<string, Type>()
            {
                {"role_id", typeof(string) },
                {"user_name", typeof(string) },
            });

            DB.CreateTableColumn("wafer_data", new Dictionary<string, Type>()
            {
                {"use_count", typeof(float) },
                {"use_time", typeof(float) },
                {"use_thick", typeof(float) },
            });
        }
    }
}
