using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Aitex.Core.RT.Event;
using Aitex.Core.UI.View.Common;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Alarms;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.Alarm
{
 
    public class AlarmViewModel : UiViewModelBase 
    {
        [IgnorePropertyChange]
        public List<AlarmItem> AlarmEvents { get; set; }
        [Subscription("System.AlarmTableID")]
        public string CurrentAlarmTable { get; set; }

        private Dictionary<string, Dictionary<string, EventItem>> _allGroupAlarmDic;
        private string _txtAnalysisText;
        private int _currentEventID;
        private string _currentEventName;

        public string TxtAnalysisText
        {
            get
            {
                return _txtAnalysisText;
            }
            set
            {
                _txtAnalysisText = value;
                NotifyOfPropertyChange("TxtAnalysisText");
            }
        }

        private bool _clearIsEnabled;

        public bool ClearIsEnabled
        {
            get => _clearIsEnabled;
            set
            {
                _clearIsEnabled = value;
                NotifyOfPropertyChange("ClearIsEnabled");
            }
        }

        private bool _abortIsEnabled;

        public bool AbortIsEnabled
        {
            get => _abortIsEnabled;
            set
            {
                _abortIsEnabled = value;
                NotifyOfPropertyChange("AbortIsEnabled");
            }
        }

        private bool _retryIsEnabled;

        public bool RetryIsEnabled
        {
            get => _retryIsEnabled;
            set
            {
                _retryIsEnabled = value;
                NotifyOfPropertyChange("RetryIsEnabled");
            }
        }

        private bool _continueIsEnabled;

        public bool ContinueIsEnabled
        {
            get => _continueIsEnabled;
            set
            {
                _continueIsEnabled = value;
                NotifyOfPropertyChange("ContinueIsEnabled");
            }
        }

        private string _targetModule;
        public string TargetModule
        {
            get
            {
                return _targetModule;
            }
            set
            {
                _targetModule = value;
                NotifyOfPropertyChange("TargetModule");
            }
        }
        public AlarmViewModel() 
        {
            Subscribe("System.LiveAlarmEvent");
        }

        protected override void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        {
            if (data.ContainsKey("System.LiveAlarmEvent"))
                UpdateAlarmEvent((List<EventItem>)data["System.LiveAlarmEvent"]);
        }
        protected override void OnViewLoaded(object view)
        {
            _allGroupAlarmDic = AlarmClient.Instance.Service.GetAlarmDefineTemplate();

            base.OnViewLoaded(view);
        }

        public void UpdateAlarmEvent(List<EventItem> evItems)
        {
            var alarmEvents = new List<AlarmItem>();
            foreach (EventItem item in evItems)
            {
                var it = new AlarmItem()
                {
                    Type = item.Level == EventLevel.Alarm ? "Alarm" : (item.Level == EventLevel.Information ? "Info" : "Warning"),
                    OccuringTime = item.OccuringTime.ToString("yyyy/MM/dd HH:mm:ss"),
                    Description = item.Description,
                    EventEnum = item.EventEnum,
                    EventId = item.Id,
                    Explaination = item.Explaination,
                    Solution = item.Solution,
                    Source = item.Source,
                    Count = item.Count,
                };
                switch (item.Level)
                {
                    case EventLevel.Alarm: it.TextColor = Brushes.Red; break;
                    case EventLevel.Warning: it.TextColor = Brushes.Yellow; break;
                    default: it.TextColor = Brushes.White; break;
                }
                alarmEvents.Add(it);
            }
            if (AlarmEvents == null || (alarmEvents.Count != AlarmEvents.Count))
            {
                AlarmEvents = alarmEvents;
            }
            else
            {
                bool isEqual = true;
                if (alarmEvents.Count == AlarmEvents.Count)
                {
                    for (int i = 0; i < alarmEvents.Count; i++)
                    {
                        if (!alarmEvents[i].IsEqualTo(AlarmEvents[i]))
                        {
                            isEqual = false;
                            break;
                        }
                    }
                }
                if (!isEqual)
                    AlarmEvents = alarmEvents;
            }

            NotifyOfPropertyChange("AlarmEvents");
        }
        public void SetActionIsEnabled(string action)
        {
            if (action == null || action == "")
            {
                ClearIsEnabled = false;
                AbortIsEnabled = false;
                RetryIsEnabled = false;
                ContinueIsEnabled = false;
            }
            else
            {
                string[] actions = action.Split('|');
                ClearIsEnabled = Array.IndexOf(actions, "Clear") != -1 ? true : false;
                AbortIsEnabled = Array.IndexOf(actions, "Abort") != -1 ? true : false;
                RetryIsEnabled = Array.IndexOf(actions, "Retry") != -1 ? true : false;
                ContinueIsEnabled = Array.IndexOf(actions, "Continue") != -1 ? true : false;
            }
        }
        public void DGOnSelectionChanged(object obj)
        {
            var e = (SelectionChangedEventArgs)obj;
            if (e.AddedItems.Count == 1)
            {
                var item = e.AddedItems[0] as AlarmItem;
                var findAlarm = _allGroupAlarmDic[CurrentAlarmTable].Values.Where(x => x.EventEnum == item.EventEnum).FirstOrDefault();
                if (findAlarm != null)
                {
                    SetActionIsEnabled(findAlarm.Action.ToString());
                    TxtAnalysisText = string.Format("Event Type：{0}\r\n\r\nEvent ID：{1}\r\n\r\nTime：{2}\r\n\r\nDescription：{3}\r\n\r\nCause：{4}\r\n\r\nRecovery：{5}",
                        item.Type,
                        item.EventId,
                        item.OccuringTime,
                        item.Description,
                        findAlarm.Explaination,
                        findAlarm.Solution
                    );
                }
                else
                {
                    SetActionIsEnabled("");
                    TxtAnalysisText = string.Format("Event Type：{0}\r\n\r\nEvent ID：{1}\r\n\r\nTime：{2}\r\n\r\nDescription：{3}",
                        item.Type,
                        item.EventId,
                        item.OccuringTime,
                        item.Description
                    );
                }

                TargetModule = item.Source;
                _currentEventID = item.EventId;
                _currentEventName = item.EventEnum;
            }
        }
    }
}