using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using Aitex.Core.Util;
using System.Threading.Tasks;
using Aitex.Core.Account;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.WCF;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.FAServices;
using MECF.Framework.Common.Log;

namespace Aitex.Core.RT.DataCenter
{
    /// <summary>
    /// 
    /// 数据项命名规则：
    /// 
    /// 一般数据项： 模块名.数据项名
    /// 
    /// 设备类型数据项：模块名.设备类型.数据项名 
    /// 
    /// 界面显示设备数据项：模块名.数据项名
    /// 
    /// </summary>
    public class DataManager : ICommonData
    {
        ConcurrentDictionary<string, DataItem<object>> _keyValueMap;
        SortedDictionary<string, Func<object>> _dbRecorderList;
        Func<object, bool> _isSubscriptionAttribute;
        Func<MemberInfo, bool> _hasSubscriptionAttribute;
        object _locker = new object();
        private LogCleaner logCleaner=new LogCleaner();
        private DiskManager diskManager = new DiskManager();
        public DataManager()
        {

        }

        public void Initialize()
        {
            Initialize(true, true);
        }

        public void Initialize(bool enableService, bool enableStats=true)
        {
            _dbRecorderList = new SortedDictionary<string, Func<object>>();
            _keyValueMap = new ConcurrentDictionary<string, DataItem<object>>();
            _isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
            _hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);

            DATA.InnerDataManager = this;

            if (enableService)
            {
                Singleton<WcfServiceManager>.Instance.Initialize(new Type[]
                {
                    typeof(QueryDataService)
                });
            }

            if (enableStats)
            {
                StatsDataManager.Instance.Initialize();
            }

            CONFIG.Subscribe("System", "NumericDataList", ()=>NumericDataList);

            logCleaner.Run();

            diskManager.Run();

            OP.Subscribe("System.DBExecute", DatabaseExecute);
        }


        private bool DatabaseExecute(string arg1, object[] arg2)
        {
            try
            {
                string sql = (string)arg2[0];

                DB.Insert(sql);

                LOG.Write($"execute sql: {sql}");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public void Terminate()
        {
            logCleaner.Stop();
            diskManager.Stop();
        }

        public List<string> NumericDataList
        {
            get
            {
                List<string> result  = new List<string>();
                foreach (var dataItem in _keyValueMap)
                {
                    object o = dataItem.Value.Value;
                    if (o == null)
                        continue;
                    Type dataType = o.GetType();

                    if (dataType == typeof(Boolean) || dataType == typeof(double) || dataType == typeof(float) || dataType == typeof(bool) ||
                        dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))
                        result.Add(dataItem.Key);
                }

                return result;
            }
        }

        public List<VIDItem> VidDataList
        {
            get
            {
                List<VIDItem> result = new List<VIDItem>();
                foreach (var dataItem in _keyValueMap)
                {
                    object o = dataItem.Value.Value;
                    if (o == null)
                        continue;
                    Type dataType = o.GetType();

                    if (dataType == typeof(Boolean) || dataType == typeof(double) || dataType == typeof(float) ||
                        dataType == typeof(bool) ||
                        dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short))
                    {
                        result.Add(new VIDItem()
                        {
                            DataType = dataType.ToString(),
                            Description = "",
                            Index = 0,
                            Name = dataItem.Key,
                            Unit = "",
                        });
                    }
                }

                return result;
            }
        }


        public List<string> BuiltInDataList
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var dataItem in _keyValueMap)
                {
                    object o = dataItem.Value.Value;
                    if (o == null)
                        continue;
                    Type dataType = o.GetType();

                    if (dataType == typeof(Boolean) || dataType == typeof(double) || dataType == typeof(float) || dataType == typeof(bool) || 
                        dataType == typeof(int) || dataType == typeof(ushort) || dataType == typeof(short) || dataType == typeof(string))
                        result.Add(dataItem.Key);
                }

                return result;
            }
        }

        public List<string> FullDataList
        {
            get
            {
                return _keyValueMap.Keys.ToList();
            }
        }

        public Type  GetDataType(string name)
        {
            if (_keyValueMap.ContainsKey(name))
            {
                object o = _keyValueMap[name].Value;
                if (o != null)
                 return o.GetType();
            }
            return null;
        }

        public SortedDictionary<string, Func<object>> GetDBRecorderList()
        {
            SortedDictionary<string, Func<object>> result;
            lock (_locker)
            {
                result = new SortedDictionary<string, Func<object>>(_dbRecorderList);
            }
            return result;
        }

        public void Subscribe<T>(T instance, string keyPrefix = null) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            Traverse(instance, keyPrefix);
        }

        public void Subscribe(string key, Func<object> getter, SubscriptionAttribute.FLAG flag)
        {
            Subscribe(key, new DataItem<object>(getter), flag);

            if (flag != SubscriptionAttribute.FLAG.IgnoreSaveDB)
            {
                lock (_locker)
                {
                    _dbRecorderList[key] = getter;
                }              
            }
        }

        public void Subscribe(string key, DataItem<object> dataItem, SubscriptionAttribute.FLAG flag)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (_keyValueMap.ContainsKey(key))
                throw new Exception(string.Format("Duplicated Key:{0}", key));
            if (dataItem == null)
                throw new ArgumentNullException("dataItem");

            _keyValueMap.TryAdd(key, dataItem);
        }

        public Dictionary<string, object> Poll(IEnumerable<string> keys)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            foreach (string name in keys)
            {
                if (_keyValueMap.ContainsKey(name))
                    data[name] =_keyValueMap[name].Value;
            }

            return data;
        }

        public object Poll(string key)
        {
            return _keyValueMap.ContainsKey(key) ? _keyValueMap[key].Value : null;
        }

        public void Traverse(object instance, string keyPrefix)
        {
            Parallel.ForEach(instance.GetType().GetFields().Where<FieldInfo>(_hasSubscriptionAttribute), fi =>
            {
                string key = Parse(fi);
                key = string.IsNullOrWhiteSpace(keyPrefix) ? key : string.Format("{0}.{1}", keyPrefix, key);
                Subscribe(key, () => fi.GetValue(instance), 0);
            });

            Parallel.ForEach(instance.GetType().GetProperties().Where<PropertyInfo>(_hasSubscriptionAttribute), property =>
            {
                string key = Parse(property);
                key = string.IsNullOrWhiteSpace(keyPrefix) ? key : string.Format("{0}.{1}", keyPrefix, key);
                Subscribe(key, () => property.GetValue(instance, null), 0);
            });
        }

        string Parse(MemberInfo member)
        {
            return _hasSubscriptionAttribute(member) ? (member.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute).Key : null;
        }


        public Dictionary<string, object> PollData(IEnumerable<string> keys)
        {
            Dictionary<string, object> result = new Dictionary<string,object>();
            foreach (string key in keys)
            {
                if (_keyValueMap.ContainsKey(key))
                    result[key] = _keyValueMap[key].Value;
                else
                {
                    //LOG.Error("未定义的DataItem：" + key);
                }
            }

            return result;
        }
    }
}