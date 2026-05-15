using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Aitex.Core.RT.DBCore
{
    public interface ICommonDB
    {
        void CreateTableIfNotExisted(string table, Dictionary<string, Type> columns, bool addPID, string primaryKey);

        void CreateTableIndexIfNotExisted(string table, string index, string sql);

        void CreateTableColumn(string table, Dictionary<string, Type> columns);

        void Insert(string sql);

        DataSet ExecuteDataset(string cmdText, params object[] p);
    }
}
