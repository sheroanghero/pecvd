using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Event
{
 
    public static class EV
    {
        public static ICommonEvent InnerEventManager { set; private get; }

        public static void PostMessage<T>(string module, T eventID, SerializableDictionary<string, string> dvid, params object[] args) where T : struct
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(module, eventID.ToString(), dvid, args);
        }

        public static void PostMessage<T>(string module, T eventID, params object[] args) where T : struct
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(module, eventID.ToString(), args);
        }

        public static void Notify(string eventName) 
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(eventName);
        }

        public static void Notify(string source, string eventName, string description)
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(source, eventName, description);
        }

        public static void Notify(string eventName, SerializableDictionary<string, string> dvid)
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(eventName,dvid);
        }

        public static void Notify(string eventName, SerializableDictionary<string, object> dvid)
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(eventName, dvid);
        }
        //public static void PostMessage(string module, T eventID, params object[] args) where T : struct
        //{
        //    if (InnerEventManager != null)
        //        InnerEventManager.WriteEvent(module, eventID.ToString(), args);
        //}

        public static void PostNotificationMessage(string message)
        {
            if (InnerEventManager != null)
                InnerEventManager.PostNotificationMessage(message);
        }

        public static void PostPopDialogMessage(EventLevel level, string title, string message)
        {
            if (InnerEventManager != null)
                InnerEventManager.PostPopDialogMessage(level, title, message);
        }

        public static void PostKickoutMessage(string message)
        {
            if (InnerEventManager != null)
                InnerEventManager.PostKickoutMessage(message);
        }

        public static void PostSoundMessage(string message)
        {
            if (InnerEventManager != null)
                InnerEventManager.PostSoundMessage(message);
        }

        public static List<EventItem> GetAlarmEvent()
        {
            if (InnerEventManager != null)
                return InnerEventManager.GetAlarmEvent();
            return null;
        }

        public static void ClearAlarmEvent()
        {
            if (InnerEventManager != null)
                InnerEventManager.ClearAlarmEvent();
        }
        public static void ClearAlarmEvent(string name)
        {
            if (InnerEventManager != null)
                InnerEventManager.ClearAlarmEvent(name);
        }
        public static List<EventItem> QueryDBEvent(string sql)
        {
            if (InnerEventManager != null)
                return InnerEventManager.QueryDBEvent(sql);
            return null;
        }

        public static void Subscribe(string evName)
        {
            if (InnerEventManager != null)
                InnerEventManager.Subscribe(new EventItem(evName));
        }

        public static void Subscribe(string evName, string description)
        {
            if (InnerEventManager != null)
                InnerEventManager.Subscribe(new EventItem(evName, description));
        }

        public static void Subscribe(EventItem item)
        {
            if (InnerEventManager != null)
                InnerEventManager.Subscribe(item);
        }

        public static void WriteEvent(string eventName)
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(eventName);
        }

        public static void WriteEvent(string eventName, SerializableDictionary<string, string> dvid)
        {
            if (InnerEventManager != null)
                InnerEventManager.WriteEvent(eventName, dvid);
        }
 
        public static void PostInfoLog(string module, string description, int traceLevel=2)
        {
            //LOG.Info(description, false, traceLevel+1);

            if (InnerEventManager != null)
            {
                InnerEventManager.PostInfoLog(module, description);
            }
        }


        public static void PostWarningLog(string module, string description, int traceLevel = 2)
        {
            //LOG.Warning(description,  traceLevel + 1);

            if (InnerEventManager != null)
            {
                InnerEventManager.PostWarningLog(module, description);
            }
        }


        public static void PostAlarmLog(string module, string description, int traceLevel = 2)
        {
            //LOG.Error(description, traceLevel + 1);

            if (InnerEventManager != null)
            {
                InnerEventManager.PostAlarmLog(module, description);
            }
        }
        public static void PostAlarmLog(string module, EventItem item)
        {
            if (InnerEventManager != null)
            {
                InnerEventManager.PostAlarmDefineLog(module, item.EventEnum, item.AdditionalDescription);
            }
        }

        public static void PostWarningLog(string module, EventItem item)
        {
            if (InnerEventManager != null)
            {
                InnerEventManager.PostAlarmDefineLog(module, item.EventEnum, item.AdditionalDescription);
            }
        }
    }
}