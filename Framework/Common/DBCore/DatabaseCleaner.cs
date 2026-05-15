using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using log4net.Repository.Hierarchy;
using Npgsql;

namespace MECF.Framework.Common.DBCore
{
    /// <summary>
    /// 单例模式，周期性地执行删除数据表的操作
    /// </summary>
    public sealed class DatabaseCleaner
    {
        NpgsqlConnection conn;
        List<string> tableNames = new List<string>();
        bool _isDataCleanEnabled = false;
        int _daysOfRetainData = 90;
        DateTime _dateDataKeepTo;

        private PeriodicJob _cleanThread;

        private string _dbName;

        public DatabaseCleaner()
        {

        }

        public void Initialize(string dbName)
        {
            _dbName = dbName;

            _isDataCleanEnabled = !SC.ContainsItem("System.EnableDataClean") || SC.GetValue<bool>("System.EnableDataClean");
            if (_isDataCleanEnabled)
            {
                GetDaysOfRetainData();
            }
            _cleanThread = new PeriodicJob(24 * 60 * 60 *1000, MonitorCleanData, "Database cleaner", true);
        }

        public void Terminate()
        {
            _cleanThread.Stop();
        }
 
        public bool MonitorCleanData( )
        {
            try
            {
                string sql = null;
                string log = null;
                int count = 0;
                NpgsqlCommand command = null;
                string[,] timeStamp = { { "carrier_data", "load_time" }, 
                    { "event_data", "occur_time" }, 
                    { "process_data", "process_begin_time" },
                    { "wafer_data", "create_time" }, 
                    { "wafer_move_history", "arrive_time" } };

                tableNames.Clear();
                GetTableNames();
                if (tableNames.Count == 0) return true;    // 数据库中没有需要删除的数据表

                // 实例化一个NpsqlConnection的对象
                conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
                conn.Open();
                conn.ChangeDatabase(_dbName);
                GetDaysOfRetainData();

                DeviceTimer timer = new DeviceTimer();

                for (int i = 0; i < timeStamp.GetLength(0); i++)
                {
                    // 判断数据库中是否存在指定的表和字段
                    sql = string.Format("select count(*) from information_schema.columns where table_schema='public' and table_name ='{0}' and  column_name='{1}'", timeStamp[i, 0], timeStamp[i, 1]);
                    command = new NpgsqlCommand(sql, conn);
                    count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 1) // 存在则返回1，不存在则返回0
                    {
                        // 删除指定日期前的所有记录
                        timer.Start(300 * 1000);
                        sql = string.Format("delete from \"{0}\" where \"{1}\" <= '{2}'", timeStamp[i, 0], timeStamp[i, 1], _dateDataKeepTo.ToString("yyyy/MM/dd HH:mm:ss.fff"));
                        command = new NpgsqlCommand(sql, conn);
                        command.ExecuteNonQuery();
                        double elapsedTime = timer.GetElapseTime() / 1000;
                        log = string.Format("当前日期为{0},删除目录表{1}里{2}天前的记录,用时{3}秒", System.DateTime.Now.ToString("D"), timeStamp[i, 0], _daysOfRetainData, elapsedTime);
                        LOG.Info(log);
                        System.Threading.Thread.Sleep(50);
                    }
                }

                foreach (string tableName in tableNames)
                {
                    timer.Start(300 * 1000);
                    sql = string.Format("drop table \"{0}\"", tableName);
                    command = new NpgsqlCommand(sql, conn);
                    command.ExecuteNonQuery();
                    double elapsedTime = timer.GetElapseTime() / 1000;
                    log = string.Format("当前日期为{0},删除{1}天前的数据表{2},用时{3}秒", System.DateTime.Now.ToString("D"), _daysOfRetainData, tableName, elapsedTime);
                    LOG.Info(log);
                    System.Threading.Thread.Sleep(50);
                }

                conn.Close();
                conn.ClearPool();
                conn = null;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                if (conn != null)
                {
                    conn.Close();
                    conn.ClearPool();
                }
                conn = null;

            }

            return true;
        }

        /// <summary>
        /// 获取数据表的总数
        /// </summary>
        /// <returns></returns>
        public void GetTableNames()
        {
            try
            {
                // 实例化一个NpsqlConnection的对象
                conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
                _dateDataKeepTo = System.DateTime.Now.AddDays(-_daysOfRetainData); // 获取90天前的年月日

                string sql = "select tablename from pg_tables where schemaname='public' and tablename like '20%' order by tablename asc";
                conn.Open();
                conn.ChangeDatabase(_dbName);

                NpgsqlCommand command = new NpgsqlCommand(sql, conn);
                NpgsqlDataReader dataReader = command.ExecuteReader();  // 获得一个结果集的检索结果    
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        string tableName = dataReader[i].ToString();
                        tableName = tableName.Substring(0, 8);

                        if (DateTime.ParseExact(tableName, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) <= _dateDataKeepTo) // 判断是否早于指定的日期
                        {
                            tableNames.Add(dataReader[i].ToString());
                        }
                    }
                }
                dataReader.Close();
                dataReader.Dispose();
                conn.Close();
                conn.ClearPool();
                conn = null;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                if (conn != null)
                {
                    conn.Close();
                    conn.ClearPool();
                }
                conn = null;
            }
        }

        /// <summary>
        /// 获取需要保留数据的天数
        /// </summary>
        public void GetDaysOfRetainData()
        {
            int days = 90;

            if (SC.ContainsItem("System.DataKeepDays"))
                days = SC.GetValue<int>("System.DataKeepDays");

            if (days < 10)
            {
                LOG.Warning($"database keep days should be at least 10 days.current setting {days}");
                days = 10;
            }

            if (days > 365)
            {
                LOG.Warning($"database keep days should be less than 365 days.current setting {days}");
                days = 365;
            }

            _daysOfRetainData = days;

        }
    }
}

