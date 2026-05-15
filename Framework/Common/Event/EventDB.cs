using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Event
{
    public class EventDB
    {
        const string Delete_Event = @"Delete from ""EventManager"" where ""OccurTime"" < '{0}' ";





        //public int DeleteEventData(int beforeMonth)
        //{
        //    try
        //    {
        //        if (beforeMonth < 6) return -1;
        //        string executeDelete = string.Format(Delete_Event, DateTime.Now.Subtract(new TimeSpan(beforeMonth*30,0,0,0)).ToString("yyyy/MM/dd HH:mm:ss.fff"));
        //        int retCount = PostgresqlHelper.ExecuteNonQuery(executeDelete);
        //        return retCount;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Write(ex);
        //        return -1;
        //    }
        //}
    }
}
