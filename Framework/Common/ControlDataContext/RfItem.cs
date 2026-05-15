using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class RfItem : ViewModelBase
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

        public double PowerDisplay
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

        [DataMember]
        public string UIDisplay
        {
            get
            {
                return string.Format("{0}:{1}", ForwardPower, ReflectPower);
            }
        }

        public string BackgroundColor
        {
            get
            {
                if (IsError) return "#FF0000";  
                if (!IsInterlockOn) return "#FFFF00";
                if (IsPowerOn) return "#E69826";

                return "#F1A2E4";
            }
        }

        public double ForwardPower { get; set; }
        public double ReflectPower { get; set; }

        public bool IsInterlockOn { get; set; }

        public bool IsPowerOn { get; set; }

        public bool IsError { get; set; }



        private double _factor = 1.0;

        [DataMember]
        public double Factor
        {
            get { return _factor; }
            set { _factor = value; }
        }

        public int State { get; set; }


        public RfItem()
        {
            DisplayName = "Undefined";
        }
        
        public void Update(RfItem item)
        {
            this.DefaultValue = item.DefaultValue;
            this.Description = item.Description;
            this.DeviceId = item.DeviceId;
            this.DeviceName = item.DeviceName;
            this.DisplayName = item.DisplayName;
            this.ErroMessage = item.ErroMessage;
            this.Factor = item.Factor;
            this.FeedBack = item.FeedBack;
            this.IsWarning = item.IsWarning;
            this.PowerDisplay = item.PowerDisplay;
            this.Scale = item.Scale;
            this.SetPoint = item.SetPoint;
            this.Unit = item.Unit;
            this.Type = item.Type;
            
            InvokePropertyChanged();
        }
    }

    public class RfOperation
    {
        public const string Ramp = "Ramp";
    }

}