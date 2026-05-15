using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.DBCore
{
    public static class DataQuery
    {
        public static DataTable Query(string sql)
        {
            DataTable result = new DataTable("result");
            try
            {
                DataSet ds = DB.ExecuteDataset(sql);
                if (ds != null)
                {
                    result = ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }
    }
}
