using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class PcItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
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

        public double _feedback;
        public double FeedBack
        {
            get
            {
                return _feedback;
            }
            set
            {
                _feedback = value;
                InvokePropertyChanged("FeedBack");
            }
        }

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
        public double Factor
        {
            get { return _factor; }
            set { _factor = value; }
        }

        public PcItem()
        {
            DisplayName = "未定义设备";
        }
    }

    public class PcOperation
    {
        public const string Ramp = "Ramp";
    }
}