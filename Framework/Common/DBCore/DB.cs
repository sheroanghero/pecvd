using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Aitex.Core.RT.DBCore
{
    public static class DB
    {
        public static ICommonDB Instance { set; private get; }

        public static void Insert(string sql)
        {
            if (Instance != null)
                Instance.Insert(sql);
        }

        public static void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey)
        {
            if (Instance != null)
                Instance.CreateTableIfNotExisted(table, columns, addPID, primaryKey);

        }

        public static void CreateTableIndexIfNotExisted(string table, string index, string sql)
        {
            if (Instance != null)
                Instance.CreateTableIndexIfNotExisted(table, index, sql);

        }

        public static void CreateTableColumn(string table, Dictionary<string, Type> columns )
        {
            if (Instance != null)
                Instance.CreateTableColumn(table, columns );

        }
 
        public static DataSet ExecuteDataset(string cmdText, params object[] p)
        {
            if (Instance != null)
                return Instance.ExecuteDataset(cmdText, p);

            return null;
        }
    }
}
