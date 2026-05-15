using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.Util;

namespace Aitex.Core.RT.DataCenter
{
    public static class DATA
    {
        public static string ModuleKey(string module, string key)
        {
            return string.Format("{0}.{1}", module, key);
        }
        public static ICommonData InnerDataManager { set; private get; }

        public static void Subscribe<T>(T instance, string keyPrefix = null) where T : class
        {
            if (InnerDataManager != null)
                InnerDataManager.Subscribe<T>(instance, keyPrefix);
        }

        public static void Subscribe(string key, Func<object> getter, SubscriptionAttribute.FLAG flag = SubscriptionAttribute.FLAG.SaveDB)
        {
            if (InnerDataManager != null)
                InnerDataManager.Subscribe(key, getter, flag);
        }

        public static void Subscribe(object key, Func<object> getter, SubscriptionAttribute.FLAG flag = SubscriptionAttribute.FLAG.SaveDB)
        {
            if (InnerDataManager != null)
                InnerDataManager.Subscribe(key.ToString(), getter, flag);
        }

        public static void Subscribe(string module, string key, Func<object> getter, SubscriptionAttribute.FLAG flag = SubscriptionAttribute.FLAG.SaveDB)
        {
            if (InnerDataManager != null)
                InnerDataManager.Subscribe(string.Format("{0}.{1}", module, key), getter, flag);
        }

        public static void Subscribe(string moduleKey, DataItem<object> dataItem, SubscriptionAttribute.FLAG flag = SubscriptionAttribute.FLAG.SaveDB)
        {
            if (InnerDataManager != null)
                InnerDataManager.Subscribe(moduleKey, dataItem, flag);
        }
        public static object Poll(string paramName)
        {
            return (InnerDataManager == null) ? null : InnerDataManager.Poll(paramName);
        }

        public static object Poll(string module, string paramName)
        {
            return (InnerDataManager == null) ? null : InnerDataManager.Poll(string.Format("{0}.{1}", module, paramName));
        }

        public static T Poll<T>(string module, string paramName)
        {
            return  (T)InnerDataManager.Poll(string.Format("{0}.{1}", module, paramName));
        }

        public static Dictionary<string, object> PollData(IEnumerable<string> keys)
        {
            return (InnerDataManager == null) ? null : InnerDataManager.PollData(keys);
        }

        public static void Traverse(object instance, string keyPrefix)
        {
            if (InnerDataManager != null)
                InnerDataManager.Traverse(instance, keyPrefix);
        }

        public static SortedDictionary<string, Func<object>> GetDBRecorderList()
        {
            return (InnerDataManager == null) ? null : InnerDataManager.GetDBRecorderList();
        }
    }
}
