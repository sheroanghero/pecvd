using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class ReadonlyGaugeDataItem
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
        /// 显示在界面上的单位
        /// </summary>
        [DataMember]
        public string Unit { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: V122
        /// </summary>
        [DataMember]
        public string DeviceId { get; set; }

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public double Value { get; set; }

        public ReadonlyGaugeDataItem()
        {
            DisplayName = "未定义";
        }
    }
}
