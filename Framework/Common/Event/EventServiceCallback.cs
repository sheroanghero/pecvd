using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using System.ServiceModel;
using Aitex.Core.WCF.Interface;
using System.IO;

namespace Aitex.Core.WCF
{
    [CallbackBehavior(ConcurrencyMode=ConcurrencyMode.Multiple, UseSynchronizationContext=false)]
    public class EventServiceCallback : IEventServiceCallback
    {
        public event Action<EventItem> FireEvent;

        public void SendEvent(EventItem ev)
        {
            if (FireEvent != null)
            {
                FireEvent(ev);
            }
        }
    }
}
