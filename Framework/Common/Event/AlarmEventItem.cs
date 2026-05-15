using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.Event
{
    public interface IAlarmHandler
    {
        void AlarmStateChanged(AlarmEventItem item);
    }

    [DataContract]
    [Serializable]
    public class AlarmEventItem : EventItem
    {
        public Func<bool> ResetChecker { get; set; }

        private IAlarmHandler _alarmHandler;

        private bool _ignoreAlarm;

        public AlarmEventItem(string source, string name, string description, Func<bool> resetChecker, IAlarmHandler handler) : base(source, name, description,
            EventLevel.Alarm, EventType.EventUI_Notify)
        {
            ResetChecker = resetChecker;
            IsAcknowledged = true;
            _alarmHandler = handler;
        }
        public AlarmEventItem()
        {

        }

        public void SetIgnoreError(bool ignore)
        {
            if (_ignoreAlarm == ignore)
                return;
            
            _ignoreAlarm = ignore;

            if (ignore)
            {
                EV.PostWarningLog(Source, $"{Source} {EventEnum} error will be ignored");

                if (IsTriggered)
                {
                    IsAcknowledged = true;

                    if (_alarmHandler != null)
                        _alarmHandler.AlarmStateChanged(this);
                }
            }
            else
            {
                Reset();
            }
        }

        public void Reset()
        {
            if (_ignoreAlarm)
                return;

            if (!IsTriggered)
                return;

            if (ResetChecker == null || ResetChecker())
            {
                EV.PostInfoLog(Source, $"{Source} {EventEnum} is cleared");

                IsAcknowledged = true;

                if (_alarmHandler != null)
                    _alarmHandler.AlarmStateChanged(this);
            }
        }

        public void Set()
        {
            Set(null);
        }

        public void Set(string error)
        {
            if (_ignoreAlarm)
                return;

            if (IsTriggered)
                return;

            if (!string.IsNullOrEmpty(error))
            {
                Description = error;
                AdditionalDescription = error;
            }

            IsAcknowledged = false;

            OccuringTime = DateTime.Now;

            if (_alarmHandler != null)
                _alarmHandler.AlarmStateChanged(this);
        }
    }
}
