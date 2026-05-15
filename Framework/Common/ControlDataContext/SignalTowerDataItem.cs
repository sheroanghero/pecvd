using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class SignalTowerDataItem
    {
        /// <summary>
        /// 阀的唯一名称，UI与RT交互的ID
        /// </summary>
        [DataMember]
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号
        /// </summary>
        [DataMember]
        public string DeviceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool IsRedLightOn { get; set; }
        [DataMember]
        public bool IsYellowLightOn { get; set; }
        [DataMember]
        public bool IsBlueLightOn { get; set; }
        [DataMember]
        public bool IsGreenLightOn { get; set; }

        public SignalTowerDataItem()
        {
            
        }
    }


}
