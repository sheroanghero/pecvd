using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class BathDataItem
    {
        [DataMember]
        public string BathName;

        [DataMember]
        public bool IsCommErr;

        [DataMember]
        public bool IsOutofTempRange;

        [DataMember]
        public bool IsCutoffAlarm;

        [DataMember]
        public bool IsLevelWarning;

        [DataMember]
        public double TemperatureReading;
    }
}
