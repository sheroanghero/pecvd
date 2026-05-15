using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Statistics
{
    public class StatsDataListItem : NotifiableItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public string ValueSetPoint { get; set; }
        public string Description { get; set; }
        public string Total { get; set; }
        public string LastUpdateTime { get; set; }
        public string LastResetTime { get; set; }
        public string LastResetTotalTime { get; set; }
        public string AlarmValue { get; set; }
        public string AlarmValueSetPoint { get; set; }
        public bool AlarmEnable { get; set; }

        public bool AlarmTextSaved { get; set; }

        public bool ValueTextSaved { get; set; }
        public bool IsVisible { get; set; }
    }

    public class StatisticsViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        public ObservableCollection<StatsDataListItem> StatData { get; set; }

        protected override void OnInitialize()
        {
            StatData = new ObservableCollection<StatsDataListItem>();

            base.OnInitialize();
        }
        protected override void OnActivate()
        {
            base.OnActivate();

        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }

        protected override void Poll()
        {
            try
            {
                string sql = $"SELECT * FROM \"stats_data\" order by \"name\" ASC;";

                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    string[] clearedNameList = Array.ConvertAll<StatsDataListItem, string>(StatData.ToArray(), x => x.Name);

                    List<string> removableList = new List<string>();
                    if (clearedNameList.Length > 0)
                        removableList = clearedNameList.ToList();
                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        if (!dbData.Rows[i]["is_visible"].Equals(DBNull.Value) && !Convert.ToBoolean(dbData.Rows[i]["is_visible"].ToString()))
                            continue;

                        string name = dbData.Rows[i]["name"].ToString();
                        removableList.RemoveIfContains(name);

                        StatsDataListItem item = StatData.FirstOrDefault(x => x.Name == name);
                        if (item == null)
                        {
                            item = new StatsDataListItem();
                            item.Name = dbData.Rows[i]["name"].ToString();
                            item.AlarmEnable = false;
                            item.IsVisible = true;
                            item.AlarmTextSaved = true;
                            item.ValueTextSaved = true;
                            item.AlarmValueSetPoint = dbData.Rows[i]["alarm_value"].ToString();

                            item.ValueSetPoint = "0";

                            StatData.Add(item);

                        }

                        item.Description = dbData.Rows[i]["description"].ToString();


                        item.Value = dbData.Rows[i]["value"].ToString().ToDouble().ToString("0.00");
                        item.Total = dbData.Rows[i]["total"].ToString();


                        item.AlarmValue = dbData.Rows[i]["alarm_value"].ToString();


                        if (!dbData.Rows[i]["enable_alarm"].Equals(DBNull.Value))
                            item.AlarmEnable = Convert.ToBoolean(dbData.Rows[i]["enable_alarm"].ToString());



                        if (!dbData.Rows[i]["last_update_time"].Equals(DBNull.Value))
                            item.LastUpdateTime = ((DateTime)dbData.Rows[i]["last_update_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                        if (!dbData.Rows[i]["last_reset_time"].Equals(DBNull.Value))
                            item.LastResetTime = ((DateTime)dbData.Rows[i]["last_reset_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                        if (!dbData.Rows[i]["last_total_reset_time"].Equals(DBNull.Value))
                            item.LastResetTotalTime = ((DateTime)dbData.Rows[i]["last_total_reset_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                        item.InvokePropertyChanged(nameof(item.AlarmValue));
                        item.InvokePropertyChanged(nameof(item.Value));
                        item.InvokePropertyChanged(nameof(item.Total));
                        item.InvokePropertyChanged(nameof(item.LastUpdateTime));
                        item.InvokePropertyChanged(nameof(item.LastResetTime));
                        item.InvokePropertyChanged(nameof(item.LastResetTotalTime));
                    }

                    foreach (var name in removableList)
                    {
                        StatsDataListItem item = StatData.FirstOrDefault(x => x.Name == name);
                        if (item != null)
                            StatData.Remove(item);
                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }

        public void ResetValue(StatsDataListItem item)
        {
            InvokeClient.Instance.Service.DoOperation("System.Stats.ResetValue", item.Name);
        }

        public void ResetEnableAlarm(StatsDataListItem item)
        {
            InvokeClient.Instance.Service.DoOperation("System.Stats.EnableAlarm", item.Name, item.AlarmEnable);
        }

        public void SetAlarmValue(StatsDataListItem item)
        {
            if (string.IsNullOrEmpty(item.AlarmValueSetPoint) ||
                !int.TryParse(item.AlarmValueSetPoint, out int setValue))
            {
                DialogBox.ShowWarning("Alarm value not valid");
                return;
            }

            InvokeClient.Instance.Service.DoOperation("System.Stats.SetAlarmValue", item.Name, setValue);

            item.AlarmTextSaved = true;
            item.InvokePropertyChanged(nameof(item.AlarmTextSaved));
        }

        public void SetValue(StatsDataListItem item)
        {
            if (string.IsNullOrEmpty(item.ValueSetPoint) ||
                !int.TryParse(item.ValueSetPoint, out int setValue))
            {
                DialogBox.ShowWarning("Value not valid");
                return;
            }


            InvokeClient.Instance.Service.DoOperation("System.Stats.SetValue", item.Name, (float)setValue);

            item.ValueTextSaved = true;
            item.InvokePropertyChanged(nameof(item.ValueTextSaved));
        }

        public void ResetTotalValue(StatsDataListItem item)
        {
            InvokeClient.Instance.Service.DoOperation("System.Stats.ResetTotalValue", item.Name);
        }
    }
}
