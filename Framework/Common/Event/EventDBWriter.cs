using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.DBCore;
using System.Data;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Event
{
    class EventDBWriter
    {
        EventDB _db = new EventDB();

        public void Initialize()
        {
            Dictionary<string, Type> columns = new Dictionary<string,Type>();
            columns["event_id"] = typeof(int);
            columns["role_id"] = typeof(int);
            columns["user_name"] = typeof(string);
            columns["event_enum"] = typeof(string);
            columns["type"] = typeof(string);
            columns["source"] = typeof(string);
            columns["description"] = typeof(string);
            columns["level"] = typeof(string);
            columns["occur_time"] = typeof(DateTime);

            DB.CreateTableIfNotExisted("event_data", columns, true, "gid");
        }
 

        public void WriteEvent(EventItem ev)
        {
            string executeInsert = string.Format(
                    @"Insert into ""event_data""(""event_id"",""role_id"",""user_name"",""event_enum"",""type"",""source"",""description"",""level"",""occur_time"") values({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')",
                    ev.Id, ev.RoleId, ev.UserName, ev.EventEnum, ev.Type.ToString(), ev.Source, ev.Description.Replace("'", "''"), ev.Level.ToString(), ev.OccuringTime.ToString("yyyy/MM/dd HH:mm:ss.fff"));
            DB.Insert(executeInsert);
        }

        public List<EventItem> QueryDBEvent(string sql)
        {
            List<EventItem> result = new List<EventItem>();

            DataSet ds = DB.ExecuteDataset(sql);
            if (ds == null)
                return result;
            try
            {
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    EventItem ev = new EventItem();

                    int id = 0;
                    if (int.TryParse(ds.Tables[0].Rows[i]["event_id"].ToString(), out id))
                        ev.Id = id;
                    int roleId = 0;
                    if(ds.Tables[0].Columns.Contains("role_id"))
                    {
                        if (int.TryParse(ds.Tables[0].Rows[i]["role_id"].ToString(), out roleId))
                            ev.RoleId = roleId;
                    }
                    if(ds.Tables[0].Columns.Contains("user_name"))
                        ev.UserName = ds.Tables[0].Rows[i]["user_name"].ToString();

                    EventType type = EventType.EventUI_Notify;
                    if (Enum.TryParse<EventType>(ds.Tables[0].Rows[i]["type"].ToString(), out type))
                        ev.Type = type;

                    ev.Source = ds.Tables[0].Rows[i]["source"].ToString();

                    ev.EventEnum = ds.Tables[0].Rows[i]["event_enum"].ToString();

                    ev.Description = ds.Tables[0].Rows[i]["description"].ToString();

                    EventLevel level = EventLevel.Information;
                    if (Enum.TryParse<EventLevel>(ds.Tables[0].Rows[i]["level"].ToString(), out level))
                        ev.Level = level;

                    if (!ds.Tables[0].Rows[i]["occur_time"].Equals(DBNull.Value))
                        ev.OccuringTime = ((DateTime)ds.Tables[0].Rows[i]["occur_time"]) ;

                    //DateTime dt = (DateTime)ds.Tables[0].Rows[i]["occur_time"];
                    //if (DateTime.TryParse(ds.Tables[0].Rows[i]["occur_time"].ToString(), out dt ))
                    //ev. = dt;

                    result.Add(ev);
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
