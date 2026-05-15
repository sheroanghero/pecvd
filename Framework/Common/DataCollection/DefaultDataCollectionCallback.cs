using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Common.Util;

namespace Aitex.Core.RT.DataCollection
{
    public class DefaultDataCollectionCallback : IDataCollectionCallback
    {
        private string _db = "postgres";

        public DefaultDataCollectionCallback( )
        {
        }

        public DefaultDataCollectionCallback(string db)
        {
            _db = db;
        }
        public void PostDBFailedEvent()
        {
           
        }

        public string GetSqlUpdateFile()
        {
            return PathManager.GetCfgDir() + "SqlUpdate.sql";
        }


        public string GetDataTablePrefix()
        {
            return "Data";
        }


        public string GetDBName()
        {
            return _db;
        }
    }
}
