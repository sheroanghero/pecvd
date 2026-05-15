using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class PressureTransducerDataItem
    {
        /// <summary>
        /// 设备的唯一名称，UI与RT交互的ID
        /// </summary>
        [DataMember]
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: M122
        /// </summary>
        [DataMember]
        public string DeviceId { get; set; }

        [DataMember]
        public double Value { get; set; }

        [DataMember]
        public bool IsEnable { get; set; }

        public PressureTransducerDataItem()
        {
            DisplayName = "未定义设备";
        }
    }

    public class PressureTransducerOperation
    {
        public const string DPEnable = "Enable";
    }
}
