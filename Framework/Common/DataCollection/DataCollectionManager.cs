using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;
using Npgsql;
using System.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DBCore;
using System.IO;
using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using System.Collections.Concurrent;
using MECF.Framework.Common.Communications;
using Aitex.Core.RT.SCCore;

namespace Aitex.Core.RT.DataCollection
{
    public class DataCollectionManager : Singleton<DataCollectionManager>, IConnection
    {
        public string Address
        {
            get { return "DB:DataCollection"; }

        }
        public bool IsConnected { get; set; }

        NpgsqlConnection _conn;

         bool _bAlive = true;
         Dictionary<string, Func<object>> _preSubscribedRecordedData; //for other thread to subscribe data
         Dictionary<string, Func<object>> _subscribedRecordedData; //internal use 
         object _lock = new object();
        F_TRIG _connFailTrig = new F_TRIG();
        private int dataCollectionInterval;

         IDataCollectionCallback _callback;

        private string[] _modules = new string[] {"Data"};

        private bool _dbFailed;
        public DataCollectionManager()
        {
            _preSubscribedRecordedData = new Dictionary<string, Func<object>>();
            _subscribedRecordedData = new Dictionary<string, Func<object>>();
            dataCollectionInterval = SC.ContainsItem("System.DataCollectionInterval") ? SC.GetValue<int>("System.DataCollectionInterval") : 1000;
        }

        public  void Initialize(IDataCollectionCallback callback)
        {
            _callback = callback == null ? new DefaultDataCollectionCallback() : callback;

            Connect();

            System.Threading.Tasks.Task.Factory.StartNew(DataRecorderThread);
        }

        public void Initialize(string[] modules, string dbName)
        {
            _callback = new DefaultDataCollectionCallback(dbName);

            if (modules!=null && modules.Length>0 && !string.IsNullOrEmpty(modules[0]))
                _modules = modules;

            Connect();

            System.Threading.Tasks.Task.Factory.StartNew(DataRecorderThread);
        }

        public void Initialize(string dbName)
        {
            _callback =  new DefaultDataCollectionCallback(dbName)  ;

            Connect();

            System.Threading.Tasks.Task.Factory.StartNew(DataRecorderThread);
        }
 
        public void Terminate()
        {
            _bAlive = false;
        }

        /// <summary>
        /// initialization
        /// </summary>
        public  void SubscribeData(string dataName, string alias, Func<object> dataValue)
        {
            lock (_lock)
            {
                if (!_preSubscribedRecordedData.Keys.Contains(dataName))
                {
                    _preSubscribedRecordedData[dataName] = dataValue;
                }
            }
        }


        private void MonitorDataCenter()
        {
            lock (_lock)
            {
                SortedDictionary<string, Func<object>> data = DATA.GetDBRecorderList();
                foreach (var item in data)
                {
                    if (!_subscribedRecordedData.ContainsKey(item.Key))
                    {
                        object o = item.Value.Invoke();
                        if (o == null)
                            continue;
                        Type dataType = o.GetType();

                        if (dataType == typeof(Boolean) || dataType == typeof(double) || dataType == typeof(float) ||
                            dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))
                        {
                            _subscribedRecordedData[item.Key] = item.Value;

                            _preSubscribedRecordedData[item.Key] = item.Value;
                        }
                    }
                }
            }
        }


        public bool Connect()
        {
            bool result = true;
            try
            {
                if (_conn != null)
                    _conn.Close();
                _conn = new NpgsqlConnection(PostgresqlHelper.ConnectionString);
                _conn.Open();

                _conn.ChangeDatabase(_callback.GetDBName());

                EV.PostInfoLog("DataCollection", "Connected with database");
            }
            catch (Exception ex)
            {
                if (_conn != null)
                {
                    _conn.Close();
                    _conn = null;
                }
                result = false;
                EV.PostInfoLog("DataCollection", "Can not connect database");
                LOG.Write(ex);
            }

            IsConnected = result;

            return result;
        }

        public bool Disconnect()
        {
            try
            {
                if (_conn != null)
                {
                    _conn.Close();
                    _conn = null;

                    EV.PostInfoLog("DataCollection", "Disconnected with database");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            IsConnected = false;

            return true;
        }

 
        /// <summary>
        /// 重置数据库连接出错事件
        /// </summary>
        public  void Reset()
        {
            _connFailTrig.CLK = false;
        }

        /// <summary>
        /// data recorder thread
        /// </summary>
         void DataRecorderThread()
        {
            while (_bAlive)
            {
                try
                {
                    Thread.Sleep(dataCollectionInterval);

                    _connFailTrig.CLK = IsConnected;// Connect();
                    if (_connFailTrig.Q)
                    {
                        EV.PostWarningLog("DataCollection", "Can not connect with database");
                    }

                    if (!_connFailTrig.M)
                        continue;
 
                    MonitorDataCenter();

                    var moduleDataItem = new Dictionary<string, Dictionary<string, Func<object>>>();
                    var moduleInsertSql = new Dictionary<string, string>();

                    foreach (var module in _modules)
                    {
                        moduleDataItem[module] = new Dictionary<string, Func<object>>();
                    }

                    string defaultModule = _modules.Contains("System") ? "System" : (_modules.Contains("Data") ? "Data" : (_modules[0]));
                    if (_subscribedRecordedData.Count > 0)
                    {
                        lock (_lock)
                        {
                            bool foundModule;
                            foreach (var dataName in _subscribedRecordedData.Keys)
                            {
                                foundModule = false;
                                foreach (var module in _modules)
                                {
                                    if (dataName.StartsWith(module+"."))
                                    {
                                        moduleDataItem[module][dataName] = _subscribedRecordedData[dataName];
                                        foundModule = true;
                                        break;
                                    }
                                }

                                if (!foundModule)
                                {
                                    moduleDataItem[defaultModule][dataName] = _subscribedRecordedData[dataName];
                                }
                            }
                            _preSubscribedRecordedData.Clear();
                        }
                    }

                    DateTime dtToday = DateTime.Now.Date;

                    foreach (var module in _modules)
                    {
                        string tableName = $"{dtToday:yyyyMMdd}.{module}";
                        UpdateTableSchema(tableName, moduleDataItem[module]);

                        string preCreatedInsertSQL = string.Format("INSERT INTO \"{0}\"(\"time\" ", tableName);
                        foreach (var dataName in moduleDataItem[module].Keys)
                        {
                            preCreatedInsertSQL += string.Format(",\"{0}\"", dataName);
                        }
                        preCreatedInsertSQL += ")";
                        moduleInsertSql[module] = preCreatedInsertSQL;
                    }

                    //insert data into database
                    StringBuilder sb = new StringBuilder(10000);
                    while (_bAlive)
                    {
                        //Thread.Sleep(990);
                        Thread.Sleep((int)(dataCollectionInterval * 0.99)); //for time delay in sleep function
                        foreach (var module in _modules)
                        {
                            sb.Remove(0, sb.Length);
                            sb.Append("Values(");
                            sb.Append(DateTime.Now.Ticks.ToString());
                            foreach (var dataName in moduleDataItem[module].Keys)
                            {
                                sb.Append(",");
                                var v1 = moduleDataItem[module][dataName].Invoke();
                                if (v1 == null)
                                {
                                    sb.Append("0");
                                }
                                else
                                {
                                    if (v1 is double || v1 is float)
                                    {
                                        double v2 = Convert.ToDouble(v1);
                                        if (double.IsNaN(v2))
                                            v2 = 0;
                                        sb.Append(v2.ToString());
                                    }
                                    else
                                    {
                                        sb.Append(v1.ToString());
                                    }
                                }
                            }

                            sb.Append(");");
                            NpgsqlCommand cmd = new NpgsqlCommand(moduleInsertSql[module] + sb.ToString(), _conn);

                            try
                            {
                                cmd.ExecuteNonQuery();
                                _dbFailed = false;
                            }
                            catch (Exception ex)
                            {
                                if (!_dbFailed)
                                {
                                    LOG.Write(ex, "数据记录发生异常" + moduleInsertSql[module]);
                                    _dbFailed = true;
                                }
                            }
                        }

                        //if alert to another day, create a new table 
                        if (DateTime.Now.Date != dtToday)
                            break;

                        //if preSubscribed data not empty, alert current table's structure

                        MonitorDataCenter();

                        if (_preSubscribedRecordedData.Count > 0)
                            break;

                    }//end while

                    _dbFailed = false;
                }//end while
                catch (Exception ex)
                {
                    if (!_dbFailed)
                    {
                        LOG.Write(ex, "数据库操作记录发生异常");
                        _dbFailed = true;
                    }
                }
            }
        }

        private string UpdateTableSchema(string tblName, Dictionary<string, Func<object>> dataItem)
        {
            //query if table already exist?
            string sqlTblDefine = string.Format("select column_name from information_schema.columns where table_name = '{0}';", tblName);
            NpgsqlCommand cmdTblDefine = new NpgsqlCommand(sqlTblDefine, _conn);

            var tblDefineData = cmdTblDefine.ExecuteReader();
            string tblAlertString = string.Empty;
            List<string> colNameList = new List<string>();
            while (tblDefineData.Read())
            {
                for (int i = 0; i < tblDefineData.FieldCount; i++)
                    colNameList.Add(tblDefineData[i].ToString());
            }
            tblDefineData.Close();
            if (colNameList.Count > 0)
            {
                //table exist
                foreach (var dataName in dataItem.Keys)
                {
                    if (!colNameList.Contains(dataName) )
                    {
                        var dataType = dataItem[dataName].Invoke().GetType();
                        if (dataType == typeof(Boolean))
                        {
                            tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tblName, dataName, "Boolean");
                        }
                        else if (dataType == typeof(double) || dataType == typeof(float) || dataType == typeof(int) || dataType == typeof(ushort) || dataType==typeof(short))
                        {
                            tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tblName, dataName, "Real");
                        }
                        else
                        {

                        }
                    }
                }
                if (!string.IsNullOrEmpty(tblAlertString))
                {
                    try
                    {
                        NpgsqlCommand alertTblCmd = new NpgsqlCommand(tblAlertString, _conn);
                        alertTblCmd.ExecuteNonQuery();
                        _dbFailed = false;
                    }
                    catch (Exception ex)
                    {
                        if (!_dbFailed)
                        {
                            LOG.Write(ex);
                            _dbFailed = true;
                        }
                    }
                }
            }
            else
            {
                //table not exist, auto generate a table
                string createTble = string.Format("CREATE TABLE \"{0}\"(Time bigint NOT NULL,", tblName);
                string commentStr = "";
                foreach (var dataName in dataItem.Keys)
                {
                    Type dataType = dataItem[dataName].Invoke().GetType();
                    if (dataType == typeof(Boolean))
                    {
                        createTble += string.Format("\"{0}\" Boolean,\n", dataName);
                    }
                    else if (dataType == typeof(double) || dataType == typeof(float) ||
                        dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))
                    {
                        createTble += string.Format("\"{0}\" Real,\n", dataName);
                    }
                    //if (!string.IsNullOrEmpty(alias) && ((dataType == typeof(Boolean)) || (dataType == typeof(double) || dataType == typeof(float) ||
                    //    dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))))
                    //    commentStr += string.Format("COMMENT ON COLUMN \"{0}\".\"{1}\" IS '{2}';\n", tblName, dataName, alias);
                }
                createTble += string.Format("CONSTRAINT \"{0}_pkey\" PRIMARY KEY (Time));", tblName);
                createTble += string.Format("GRANT SELECT ON TABLE \"{0}\" TO postgres;", tblName);

                // createTble += string.Format("CREATE INDEX \"{0}_time_idx\"  ON \"{0}\" USING btree (\"time\" );", tblName);


                createTble += commentStr;
                try
                {
                    NpgsqlCommand cmd = new NpgsqlCommand(createTble.ToString(), _conn);
                    cmd.ExecuteNonQuery();
                    _dbFailed = false;
                }
                catch (Exception ex1)
                {
                    if (!_dbFailed)
                    {
                        LOG.Write(ex1, "创建数据库表格失败");
                        _dbFailed = true;
                        //throw;
                    }
                }
            }

            return tblName;
        }

    }
}
