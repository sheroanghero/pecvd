using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;
using SciChart.Charting.Common.Databinding;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITPressureMeterData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string Module { get; set; }
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
        public string DeviceSchematicId { get; set; }

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

        [DataMember]
        public double FeedBack { get; set; }

        [DataMember]
        public double Precision
        {
            get; set;
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

                [DataMember]
        public bool IsError { get; set; }

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
        public double Factor { get; set; }

        public string Display
        {
            get
            {
                string value = (FeedBack > Precision && Precision > 1)
                    ? Precision.ToString(FormatString)
                    : FeedBack.ToString(FormatString);
                return DisplayWithUnit ? value + " " + Unit : value;
            }
        }
        [DataMember]
        public string FormatString
        {
            get ;set;
        }

        [DataMember]
        public bool DisplayWithUnit
        {
            get;
            set;
        }

        public AITPressureMeterData()
        {
            DisplayName = "Undefined";
            Factor = 1.0;
            Unit = "";
            Type = "";
            Precision = double.MaxValue;
            FormatString = "F3";
        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

        public class AITPressureMeterPropertyName
    {
        public const string Feedback = "Feedback";
    }
}