using System;
using System.Collections.Generic;
using System.Data;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.DBCore;

namespace MECF.Framework.Common.DataCenter
{
    public class StatsData
    {
        private StatsDataItem _item;

        public StatsData(string name, string description, int initialValue, int alarmValue = 0,
            bool alarmEnable = false, bool isVisible = true)
        {
            StatsDataManager.Instance.Subscribe(name, description, initialValue, alarmValue, alarmEnable, isVisible);

            _item = StatsDataManager.Instance.GetItem(name);
        }

        public int SetAlarmValue(int value)
        {
            return StatsDataManager.Instance.SetAlarmValue(_item.Name, value);
        }

        public void EnableAlarm(bool enable)
        {
            StatsDataManager.Instance.EnableAlarm(_item.Name, enable);
        }

        public void EnableVisible(bool visible)
        {
            StatsDataManager.Instance.EnableVisible(_item.Name, visible);
        }

        public void SetValue(float value)
        {
            StatsDataManager.Instance.SetValue(_item.Name, value);
        }

        public float GetValue()
        {
            return StatsDataManager.Instance.GetValue(_item.Name);
        }

        public int Increase(int additionValue = 1)
        {
            return StatsDataManager.Instance.Increase(_item.Name, additionValue);
        }

        public int Reset()
        {
            return StatsDataManager.Instance.Reset(_item.Name);
        }

        public void ResetTotal()
        {
            StatsDataManager.Instance.ResetTotal(_item.Name);
        }
    }

    public class StatsDataItem
    {
        public string Module { get; set; }
        public string Unit { get; set; }
        public string Name { get; set; }
        public float Value { get; set; }
        public string Description { get; set; }
        public float Total { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime LastResetTime { get; set; }
        public DateTime LastResetTotalTime { get; set; }
        public int AlarmValue { get; set; }
        public bool AlarmEnable { get; set; }
        public bool IsVisible { get; set; }
    }

    public class StatsDataManager : Singleton<StatsDataManager>
    {
        private Dictionary<string, StatsDataItem> _items = new Dictionary<string, StatsDataItem>();

        public Dictionary<string, StatsDataItem> Item { get => _items; }

        private object _locker = new object();

        public StatsDataManager()
        {

        }

        public void Initialize()
        {
            try
            {

                OP.Subscribe("System.Stats.ResetValue", (method, args) =>
                {
                    Reset((string)args[0]);
                    return true;
                });

                OP.Subscribe("System.Stats.EnableAlarm", (method, args) =>
                {
                    EnableAlarm((string)args[0], (bool)args[1]);
                    return true;
                });

                OP.Subscribe("System.Stats.SetAlarmValue", (method, args) =>
                {
                    SetAlarmValue((string)args[0], (int)args[1]);
                    return true;
                });

                OP.Subscribe("System.Stats.SetValue", (method, args) =>
                {
                    SetValue((string)args[0], (float)args[1]);
                    return true;
                });

                OP.Subscribe("System.Stats.ResetTotalValue", (method, args) =>
                {
                    ResetTotal((string)args[0]);
                    return true;
                });

                DB.CreateTableIfNotExisted("stats_data", new Dictionary<string, Type>()
                {
                    {"name", typeof(string) },
                    {"value", typeof(string) },
                    {"total", typeof(string) },
                    {"description", typeof(string) },
                    {"last_update_time", typeof(DateTime) },
                    {"last_reset_time", typeof(DateTime) },
                    {"last_total_reset_time", typeof(DateTime) },
                    {"is_visible", typeof(string) },
                    {"enable_alarm", typeof(string) },
                    {"alarm_value", typeof(string) },
                }, false, "name");















                DataTable dt = DataQuery.Query("select * from \"stats_data\"");
                if (dt == null) return;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    StatsDataItem item = new StatsDataItem();
                    item.Name = dt.Rows[i]["name"].ToString();
                    item.Description = dt.Rows[i]["description"].ToString();
                    if (float.TryParse(dt.Rows[i]["value"].ToString(), out float value))
                        item.Value = value;
                    if (float.TryParse(dt.Rows[i]["total"].ToString(), out float total))
                        item.Total = total;
                    if (int.TryParse(dt.Rows[i]["alarm_value"].ToString(), out int alarmValue))
                        item.AlarmValue = alarmValue;
                    if (!dt.Rows[i]["enable_alarm"].Equals(DBNull.Value))
                        item.AlarmEnable = Convert.ToBoolean(dt.Rows[i]["enable_alarm"].ToString());
                    if (!dt.Rows[i]["is_visible"].Equals(DBNull.Value))
                        item.IsVisible = Convert.ToBoolean(dt.Rows[i]["is_visible"].ToString());

                    if (!dt.Rows[i]["last_update_time"].Equals(DBNull.Value))
                        item.LastUpdateTime = (DateTime)dt.Rows[i]["last_update_time"];
                    if (!dt.Rows[i]["last_reset_time"].Equals(DBNull.Value))
                        item.LastResetTime = (DateTime)dt.Rows[i]["last_reset_time"];
                    if (!dt.Rows[i]["last_total_reset_time"].Equals(DBNull.Value))
                        item.LastResetTotalTime = (DateTime)dt.Rows[i]["last_total_reset_time"];

                    _items[item.Name] = item;
                }

            }
            catch (Exception ex)
            {
                LOG.Error("init stats data manager failed", ex);
            }

        }

        public int SetAlarmValue(string name, int value)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not set {name} alarm value, not defined item");
                    return -1;
                }

                int preValue = _items[name].AlarmValue;
                _items[name].AlarmValue = value;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"alarm_value\"='{0}'WHERE \"name\"='{1}';", _items[name].AlarmValue,
                    name);
                DB.Insert(sql);

                EV.PostInfoLog("System", $"{name} stats alarm value changed from {preValue} to {value}");

                return preValue;
            }
        }

        public int GetAlarmValue(string name)
        {
            lock (_locker)
            {
                if (_items.ContainsKey(name))
                {
                    return _items[name].AlarmValue;
                }
            }

            LOG.Error($"Can not get {name} value, not defined item");

            return 0;
        }

        public bool GetAlarmEnable(string name)
        {
            lock (_locker)
            {
                if (_items.ContainsKey(name))
                {
                    return _items[name].AlarmEnable;
                }
            }

            LOG.Error($"Can not get {name} value, not defined item");

            return false;
        }

        public void EnableAlarm(string name, bool enable)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not set {name} alarm enable, not defined item");
                    return;
                }

                bool preValue = _items[name].AlarmEnable;
                _items[name].AlarmEnable = enable;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"enable_alarm\"='{0}'WHERE \"name\"='{1}';", _items[name].AlarmEnable,
                    name);
                DB.Insert(sql);

                EV.PostInfoLog("System", $"{name} stats alarm enable changed from {preValue} to {enable}");

                return;
            }
        }

        public void EnableVisible(string name, bool visible)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not set {name} alarm enable, not defined item");
                    return;
                }

                bool preValue = _items[name].IsVisible;
                _items[name].IsVisible = visible;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"is_visible\"='{0}'WHERE \"name\"='{1}';", _items[name].IsVisible,
                    name);
                DB.Insert(sql);

                EV.PostInfoLog("System", $"{name} stats visible changed from {preValue} to {visible}");

                return;
            }
        }

        public void Terminate()
        {

        }
        public void Subscribe(string name, string description, int initialValue, int alarmValue = 0, bool alarmEnable = false, bool isVisible = true)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    _items[name] = new StatsDataItem()
                    { Description = description, Value = initialValue, Name = name, Total = 0 };
                    string executeInsert = string.Format(
                        @"Insert into ""stats_data""(""name"",
                                                    ""value"",
                                                    ""total"",
                                                    ""description"",
                                                    ""last_update_time"",
                                                    ""last_reset_time"",
                                                    ""last_total_reset_time"",
                                                    ""is_visible"",
                                                    ""enable_alarm"",
                                                    ""alarm_value""
                                                    ) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')",
                        name, initialValue, initialValue, description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")
                        , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"), isVisible, alarmEnable, alarmValue);
                    DB.Insert(executeInsert);
                }
            }
        }

        public void SetValue(string name, float value)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not set {name} value, not defined item");
                    return;
                }

                _items[name].Value = value;
                _items[name].LastUpdateTime = DateTime.Now;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"value\"='{1}' WHERE \"name\"='{2}';",
                    _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    value,
                    name);
                DB.Insert(sql);
            }
        }

        public float GetValue(string name)
        {
            lock (_locker)
            {
                if (_items.ContainsKey(name))
                {
                    return _items[name].Value;
                }
            }

            LOG.Error($"Can not get {name} value, not defined item");

            return 0;
        }

        public StatsDataItem GetItem(string name)
        {
            lock (_locker)
            {
                if (_items.ContainsKey(name))
                {
                    return _items[name];
                }
            }

            LOG.Error($"Can not get {name} value, not defined item");

            return null;
        }

        public int Increase(string name, int additionValue = 1)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not increase {name} value, not defined item");
                    return -1;
                }

                _items[name].Value += additionValue;
                _items[name].Total += additionValue;
                _items[name].LastUpdateTime = DateTime.Now;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"value\"='{1}',\"total\"='{2}' WHERE \"name\"='{3}';",
                    _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].Value,
                    _items[name].Total,
                    name);
                DB.Insert(sql);

                return (int)_items[name].Value;
            }
        }

        public int Reset(string name)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not reset {name} value, not defined item");
                    return -1;
                }

                int preValue = (int)_items[name].Value;
                _items[name].Value = 0;
                _items[name].LastUpdateTime = DateTime.Now;
                _items[name].LastResetTime = DateTime.Now;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"last_reset_time\"='{1}',\"value\"='{2}' WHERE \"name\"='{3}';",
                    _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].LastResetTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].Value,
                    name);
                DB.Insert(sql);

                EV.PostInfoLog("System", $"{name} stats value reset to 0");

                return preValue;
            }


        }


        public void ResetTotal(string name)
        {
            lock (_locker)
            {
                if (!_items.ContainsKey(name))
                {
                    LOG.Error($"Can not reset {name} value, not defined item");
                    return;
                }

                _items[name].Value = 0;
                _items[name].Total = 0;

                _items[name].LastResetTime = DateTime.Now;
                _items[name].LastUpdateTime = DateTime.Now;
                _items[name].LastResetTotalTime = DateTime.Now;

                string sql = string.Format(
                    "UPDATE \"stats_data\" SET \"last_update_time\"='{0}',\"last_reset_time\"='{1}',\"last_total_reset_time\"='{2}',\"value\"='{3}',\"total\"='{4}' WHERE \"name\"='{5}';",
                    _items[name].LastUpdateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].LastResetTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].LastResetTotalTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    _items[name].Value,
                    _items[name].Total,
                    name);

                EV.PostInfoLog("System", $"{name} stats total value reset to 0");

                DB.Insert(sql);
            }
        }
    }
}

