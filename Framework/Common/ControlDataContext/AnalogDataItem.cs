using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class AnalogDeviceDataItem
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
        public string Unit { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double Scale { get; set; }

        /// <summary>
        /// 设定值
        /// </summary>
        [DataMember]
        public double SetPoint { get; set; }

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public double FeedBack { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public double DefaultValue { get; set; }

        /// <summary>
        /// 是否有报警
        /// </summary>
        [DataMember]
        public bool IsWarning { get; set; }

        /// <summary>
        /// alarm或是erro时显示的信息
        /// </summary>
        [DataMember]
        public string ErroMessage { get; set; }

        /// <summary>
        /// MFC,PC
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        private double _factor = 1.0;

        [DataMember]
        public double Factor { get { return _factor; } set { _factor = value; } }

        /// <summary>
        /// 格式化显示
        /// </summary>
        [DataMember]
        public string FormatString { get; set; }

        public AnalogDeviceDataItem()
        {
            DisplayName = "未定义设备";
            FormatString = "F1";
        }
    }

    public class AnalogDeviceOperation
    {
        public const string Ramp = "Ramp";
    }
}
