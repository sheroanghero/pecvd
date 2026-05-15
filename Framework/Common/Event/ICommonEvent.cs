using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Event
{
    public interface ICommonEvent
    {
        void WriteEvent(string eventName);
        void WriteEvent(string eventName, SerializableDictionary<string, string> dvid);
        void WriteEvent(string eventName, SerializableDictionary<string, object> dvid);
        void WriteEvent(string module, string eventName, params object[] args);
        void WriteEvent(string module, string eventName, SerializableDictionary<string, string> dvid, params object[] args);
 
        void PostNotificationMessage(string message);

        void PostPopDialogMessage(EventLevel level, string title, string message);

        void PostKickoutMessage(string message);

        void PostSoundMessage(string message);

        List<EventItem> GetAlarmEvent();

        void ClearAlarmEvent();
        void ClearAlarmEvent(string name);

        List<EventItem> QueryDBEvent(string sql);

        void Subscribe(EventItem item);

        void PostInfoLog(string module, string message);
        void PostWarningLog(string module, string message);
        void PostAlarmLog(string module, string message);
        void PostAlarmDefineLog(string module, string name, string additionalDescription);

    }
}
