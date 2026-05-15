using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Aitex.Core.RT.Event
{
    [Serializable]
    public class EventDefine
    {
        [XmlElement("EventDefinition")]
        public List<EventItem> Items { get; set; }
    }
}
