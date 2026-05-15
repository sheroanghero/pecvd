using Aitex.Core.Common.DeviceData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.CommonData.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITChillerData1 : AITDeviceData
    {
        [DataMember]
        public string DeviceModule { get; set; }

        [DataMember]
        public float CH1Temperature { get; set; }
        [DataMember]
        public float CH2Temperature { get; set; }

        [DataMember]
        public float CH1TemperatureSetPoint { get; set; }
        [DataMember]
        public float CH2TemperatureSetPoint { get; set; }
        [DataMember]
        public bool IsCH1On { get; set; }
        [DataMember]
        public bool IsCH2On { get; set; }

        [DataMember]
        public bool IsCH1Warning { get; set; }
        [DataMember]
        public bool IsCH2Warning { get; set; }
        [DataMember]
        public bool IsCH1Alarm { get; set; }
        [DataMember]
        public bool IsCH2Alarm { get; set; }

        [DataMember]
        public double CH1WaterFlow { get; set; }
        [DataMember]
        public double CH2WaterFlow { get; set; }
        [DataMember]
        public float TemperatureHighLimit { get; set; }
        [DataMember]
        public float TemperatureLowLimit { get; set; }

        [DataMember]
        public string FormatString { get; set; }

        public AITChillerData1()
        {
            DisplayName = "未定义";
        }

    }
}
