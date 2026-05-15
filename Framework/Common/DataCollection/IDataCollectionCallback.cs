using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.DataCollection
{
    public interface IDataCollectionCallback
    {
        void PostDBFailedEvent();

        string GetDBName();

        string GetSqlUpdateFile();

        string GetDataTablePrefix();

    }
}
