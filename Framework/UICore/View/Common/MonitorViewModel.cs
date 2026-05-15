using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Aitex.Core.RT.Event;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.UI.View.Common
{
    public class AlarmItem
    {
        public string OccuringTime { get; set; }
        public string Description { get; set; }
        public Brush TextColor { get; set; }
        public string Type { get; set; }
        public bool IsEqualTo(AlarmItem item)
        {
            return item.OccuringTime == OccuringTime &&
                item.Description == Description &&
                item.Type == Type;
        }
        public int EventId { get; set; }
        public string Ceid { get; set; }
        public string EventEnum { get; set; }
        public string Explaination { get; set; }
        public string Solution { get; set; }
        public string Source { get; set; }
        public int Count { get; set; }
    }

    public class MonitorViewModel : ViewModelBase
    {
        public List<AlarmItem> AlarmEvents { get; set; }

        public void UpdateAlarmEvent(List<EventItem> evItems)
        {
            var alarmEvents = new List<AlarmItem>();
            foreach (EventItem item in evItems)
            {
                    var it = new AlarmItem()
                    {
                        Type = item.Level == EventLevel.Alarm ? "Alarm" : (item.Level == EventLevel.Information ? "Info" : "Warning"),
                        OccuringTime = item.OccuringTime.ToString("HH:mm:ss"),
                        Description = item.Description,
                        EventEnum = item.EventEnum,
                        EventId = item.Id,
                        Explaination = item.Explaination,
                        Solution = item.Solution,
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

            InvokePropertyChanged("AlarmEvents");
        }
    }
}
