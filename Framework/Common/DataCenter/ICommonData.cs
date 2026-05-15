using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Aitex.Core.Util;

namespace Aitex.Core.RT.DataCenter
{
    public interface ICommonData
    {
        void Subscribe<T>(T instance, string keyPrefix = null) where T : class;
        void Subscribe(string key, Func<object> getter, SubscriptionAttribute.FLAG flag);
        void Subscribe(string moduleKey, DataItem<object> dataItem, SubscriptionAttribute.FLAG flag);
        object Poll(string key);
        Dictionary<string, object> PollData(IEnumerable<string> keys);
        void Traverse(object instance, string keyPrefix);
        SortedDictionary<string, Func<object>> GetDBRecorderList();
    }
}
