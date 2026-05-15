using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Event
{
    class EventLogWriter
    {
        public void WriteEvent(EventItem ev)
        {
            if (ev.Description == null)
                ev.Description = "";

            if (ev.Level == EventLevel.Alarm)
                LOG.Error(ev.Description.Replace("'", "''"));
            else if (ev.Level == EventLevel.Warning)
            {
                LOG.Warning(ev.Description.Replace("'", "''"));
            }
            else
            {
                LOG.Info(ev.Description.Replace("'", "''"));
            }
        }
    }
}
