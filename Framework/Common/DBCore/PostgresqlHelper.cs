using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Data;

namespace Aitex.Core.RT.DBCore
{
    public class PostgresqlHelper
    {
        /// <summary>
        /// 连接字符串需要初始化
        /// </summary>
        public static string ConnectionString { get; set; }

        /// <summary>
        /// 获得连接对象
        /// </summary>
        /// <returns></returns>
        public static NpgsqlConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new ArgumentNullException("PostgresqlConn");

            return new NpgsqlConnection(ConnectionString);
        }

        static void PrepareCommand(NpgsqlCommand cmd, NpgsqlConnection conn, string cmdText, params object[] p)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
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

        public static DataSet ExecuteDataset(string cmdText, params object[] p)
        {
            DataSet ds = new DataSet();

            using (NpgsqlConnection connection = GetConnection())
            using (NpgsqlCommand command = new NpgsqlCommand())
            {
                PrepareCommand(command, connection, cmdText, p);
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
                da.Fill(ds);
            }
            return ds;
        }

        public static DataRow ExecuteDataRow(string cmdText, params object[] p)
        {
            DataSet ds = ExecuteDataset(cmdText, p);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                return ds.Tables[0].Rows[0];
            return null;
        }

        /// <summary>
        /// 返回受影响的行数
        /// </summary>
        public static int ExecuteNonQuery(string cmdText, params object[] p)
        {            
            using (NpgsqlConnection connection = GetConnection())
            using (NpgsqlCommand command = new NpgsqlCommand())
            {
                PrepareCommand(command, connection, cmdText, p);
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 返回NpgsqlDataReader对象
        /// </summary>
        public static NpgsqlDataReader ExecuteReader(string cmdText, params object[] p)
        {            
            NpgsqlConnection connection = GetConnection();
            NpgsqlCommand command = new NpgsqlCommand();

            try
            {
                PrepareCommand(command, connection, cmdText, p);
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// 返回结果集中的第一行第一列，忽略其他行或列
        /// </summary>
        public static object ExecuteScalar(string cmdText, params object[] p)
        {
            using (NpgsqlConnection connection = GetConnection())
            using (NpgsqlCommand command = new NpgsqlCommand())
            {
                PrepareCommand(command, connection, cmdText, p);
                return command.ExecuteScalar();
            }
        }

        /// <summary>
        /// 分页
        /// </summary>
        public static DataSet ExecutePager(ref int recordCount, int pageIndex, int pageSize, string cmdText, string countText, params object[] p)
        {
            if (recordCount < 0)
                recordCount = int.Parse(ExecuteScalar(countText, p).ToString());

            DataSet ds = new DataSet();
            
            using(NpgsqlConnection connection = GetConnection())
            using(NpgsqlCommand command = new NpgsqlCommand())
            {
                PrepareCommand(command, connection, cmdText, p);
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
                da.Fill(ds, (pageIndex - 1) * pageSize, pageSize, "result");
            }
            return ds;
        }
    }
}
