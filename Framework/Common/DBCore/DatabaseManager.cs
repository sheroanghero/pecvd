using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using System.Data;
using System.IO;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.DBCore;

namespace Aitex.Core.RT.DBCore
{
    public class DatabaseManager : ICommonDB
    {
        PeriodicJob _thread;
        FixSizeQueue<string> _sqlQueue = new FixSizeQueue<string>(1500);
        PostgresqlDB _db = new PostgresqlDB();
        DatabaseCleaner _cleaner = new DatabaseCleaner();
        private string _dbName;

        public void Initialize(string connectionString, string dbName, string sqlFile)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ApplicationException("数据库连接字段未设置");
            }
            _dbName = dbName;

            PostgresqlHelper.ConnectionString = connectionString;

            if (!_db.Open(connectionString, dbName))
            {
                LOG.Error("数据库连接失败");
            }
            else
            {
                PrepareDatabaseTable(sqlFile);
            }

            _thread = new PeriodicJob(100, this.PeriodicRun, "DBJob", true);

            DB.Instance = this;


            DatabaseTable.UpgradeDataTable();
            StartDataCleaner();
        }

        public void StartDataCleaner()
        {
            _cleaner.Initialize(_dbName);
        }


        public void Terminate()
        {
            if (_thread != null)
            {
                _thread.Stop();
                _thread = null;
            }

            _cleaner.Terminate();

            _db.Close();
        }

        bool PeriodicRun()
        {
            if (!_db.ActiveConnection())
                return true;

            string sql;


            while (_sqlQueue.TryDequeue(out sql))
            {
                try
                {
                    _db.ExecuteNonQuery(sql);
                }
                catch (Exception ex)
                {
                    LOG.Error(string.Format("DB operation error, {0}, {1}", ex.Message, sql));
                }
            }



            return true;
        }

        public void Insert(string sql)
        {
            _sqlQueue.Enqueue(sql);
        }

        public void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
        {
            _db.CreateTableIfNotExisted(table, columns, addPID, primaryKey);
        }

        public void CreateTableIndexIfNotExisted(string table, string index, string sql)
        {
            _db.CreateTableIndexIfNotExisted(table, index, sql);
        }

        public void CreateTableColumn(string table, Dictionary<string, Type> columns)
        {
            _db.CreateTableColumn(table, columns);
        }

        public DataSet ExecuteDataset(string cmdText, params object[] p)
        {
            return _db.ExecuteDataset(cmdText, p);
        }

        void PrepareDatabaseTable(string sqlFile)
        {
            if (string.IsNullOrEmpty(sqlFile) || !File.Exists(sqlFile))
            {
                LOG.Info("没有更新Sql数据库，文件：" + sqlFile);
                return;
            }
            try
            {
                using (StreamReader fs = new System.IO.StreamReader(sqlFile))
                {
                    string sql = fs.ReadToEnd();
                    _db.ExecuteNonQuery(sql);
                    LOG.Write($"Database updated by sql file {sqlFile}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
    }
}
