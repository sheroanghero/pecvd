using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using Aitex.Core.RT.Log;
using System.Data;
using Aitex.Core.Utilities;

namespace Aitex.Core.RT.DBCore
{
    public class PostgresqlDB
    {
        NpgsqlConnection _conn;
        string _connectionString;
        string _dbName;
        Retry _retryConnection = new Retry();

        private bool _dbFailed;

        public PostgresqlDB()
        {

        }

        public bool Open(string connectionString, string dbName)
        {
            _connectionString = connectionString;
            _dbName = dbName;

            bool result = true;
            try
            {
                if (_conn != null) 
                    _conn.Close();
                _conn = new NpgsqlConnection(connectionString);
                _conn.Open();

                if (!string.IsNullOrEmpty(dbName))
                    CreateDBIfNotExisted(dbName);

                _dbFailed = false;
            }
            catch (Exception ex)
            {
                if (_conn != null)
                {
                    _conn.Close();
                    _conn = null;
                }
                result = false;

                if (!_dbFailed)
                {
                    LOG.Write(ex);
                    _dbFailed = true;
                }
            }

            _retryConnection.Result = result;

            //if (_retryConnection.IsErrored)
                

            return result;
        }

        bool Open()
        {
            return Open(_connectionString, _dbName);
        }

        void PrepareCommand(NpgsqlCommand cmd, NpgsqlConnection conn, string cmdText, params object[] p)
        {
            cmd.Parameters.Clear();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            cmd.CommandType = CommandType.Text;

            if (p != null)
            {
                foreach (object parm in p)
                    cmd.Parameters.AddWithValue(string.Empty, parm);
            }
        }

        public int ExecuteNonQuery(string cmdText, params object[] p)
        {
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    PrepareCommand(command, _conn, cmdText, p);
                    
                   return command.ExecuteNonQuery();
                }
            }
            catch
            {
                Close();
                LOG.Write($"DB failed to execute:{cmdText}");
                throw;
            }
        }

        public DataSet ExecuteDataset(string cmdText, params object[] p)
        {
            try
            {
                DataSet ds = new DataSet();
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    connection.ChangeDatabase(_dbName);
                    using (NpgsqlCommand command = new NpgsqlCommand())
                    {
                        try
                        {
                            PrepareCommand(command, connection, cmdText, p);
                            NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
                            da.Fill(ds);
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("执行查询出错，" + cmdText, ex);
                        }

                        
                    }
                }
                return ds;
            }
            catch(Exception ex)
            {
                LOG.Error("执行查询出错，"+cmdText, ex);
            }
            return null;
        }

        public NpgsqlDataReader ExecuteReader(string cmdText, params object[] p)
        {            
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    PrepareCommand(command, _conn, cmdText, p);
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch
            {
                Close();
                LOG.Write($"DB failed to execute:{cmdText}");
                throw;
            }
        }


        public bool ActiveConnection()
        {
            if (_conn!=null && _conn.State == ConnectionState.Open)
                return true;

            return Open();
        }

        public void Close()
        {
            try
            {
                if (_conn != null)
                    _conn.Close();
                _conn = null;

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

        public void CreateDBIfNotExisted(string db)
        {
            NpgsqlDataReader reader = ExecuteReader(string.Format(@"select datname from pg_catalog.pg_database where datname='{0}'", db));
            if (!reader.HasRows)
            {
                string sql = string.Format(@"
                                    CREATE DATABASE {0}
                                    WITH OWNER = postgres
                                   ENCODING = 'UTF8'
                                   TABLESPACE = pg_default
                                   CONNECTION LIMIT = -1", db);
                ExecuteNonQuery(sql);
            }

            try
            {
                _conn.ChangeDatabase(db);
            }
            catch
            {
                _conn.Close();
                throw;
            }
        }

        public void CreateTableIndexIfNotExisted(string table, string index, string sql)
        {
            NpgsqlDataReader reader = ExecuteReader($"select* from pg_indexes where tablename='{table}' and indexname = '{index}'");
            if (!reader.HasRows)
            {
                ExecuteNonQuery(sql);
            }
        }

        public void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
        {
            NpgsqlDataReader reader = ExecuteReader(string.Format(@"select column_name from information_schema.columns where table_name = '{0}'", table));
            if (!reader.HasRows)
            {
                string cols = addPID ? " \"PID\" serial NOT NULL," : "";
                foreach (var item in columns)
                {
                    if (item.Value == typeof(int) || item.Value == typeof(ushort) || item.Value == typeof(short))
                        cols += string.Format("\"{0}\" integer,", item.Key);
                    else if (item.Value == typeof(double) || item.Value == typeof(float))
                        cols += string.Format("\"{0}\" real,", item.Key);
                    else if (item.Value == typeof(string))
                        cols += string.Format("\"{0}\" text,", item.Key);
                    else if (item.Value == typeof(DateTime))
                        cols += string.Format("\"{0}\" timestamp without time zone,", item.Key);
                    else if (item.Value == typeof(bool))
                        cols += string.Format("\"{0}\" boolean,", item.Key);                     
                }
                if (string.IsNullOrEmpty(primaryKey))
                {
                    if (cols.LastIndexOf(',') == cols.Length-1)
                        cols = cols.Remove(cols.Length - 1);
                }else
                {
                    cols += string.Format("CONSTRAINT \"{0}_pkey\" PRIMARY KEY (\"{1}\" )", table, primaryKey);
                }

                ExecuteNonQuery(string.Format("CREATE TABLE \"{0}\"({1})WITH ( OIDS=FALSE );", table, cols));
            }
            else
            {
                CreateTableColumn(table, columns);
            }
        }



        public void CreateTableColumn(string tableName, Dictionary<string, Type> columns )
        {
            try
            {
                //query if table already exist?
                string sqlTblDefine = string.Format("select column_name from information_schema.columns where table_name = '{0}';", tableName);
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
                    foreach (var column in columns)
                    {
                        if (!colNameList.Contains(column.Key))
                        {
                            if (column.Value == typeof(Boolean))
                            {
                                tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "Boolean");
                            }
                            else if (column.Value == typeof(double) || column.Value == typeof(float)  )
                            {
                                tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "real");
                            }
                            else if (column.Value == typeof(DateTime) )
                            {
                                tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "timestamp without time zone");
                            }
                            else if (column.Value == typeof(int) || column.Value == typeof(ushort) || column.Value == typeof(short))
                            {
                                tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "integer");
                            }
                            else 
                            {
                                tblAlertString += string.Format("ALTER TABLE \"{0}\" ADD COLUMN \"{1}\" {2};", tableName, column.Key, "text");
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

                _dbFailed = true;
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
}
