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

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITMfcData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string Module { get; set; }

        [DataMember]
        public string UniqueName { get; set; }


        /// <summary>
        /// 设备的唯一名称，UI与RT交互的ID
        /// </summary>
        [DataMember]
        public string DeviceName
        {
            get;
            set;
        }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: M122
        /// </summary>
        [DataMember]
        public string DeviceSchematicId
        {
            get;
            set;
        }

        [DataMember]
        public string Unit
        {
            get;
            set;
        }

        [DataMember]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double Scale
        {
            get;
            set;
        }

        /// <summary>
        /// 量程范围
        /// </summary>
        [DataMember]
        public string Range
        {
            get;
            set;
        }

        /// <summary>
        /// 设定值
        /// </summary>
        [DataMember]
        public double SetPoint
        {
            get;
            set;
        }

        [DataMember]
        public double FeedBack
        {
            get;
            set;
        }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public double DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// 是否有报警
        /// </summary>
        [DataMember]
        public bool IsWarning
        {
            get;
            set;
        }

        [DataMember]
        public bool IsError
        {
            get;
            set;
        }

        [DataMember]
        public bool IsOffline
        {
            get;
            set;
        }

        [DataMember]
        public int Status
        {
            get;
            set;
        }

        /// <summary>
        /// alarm或是erro时显示的信息
        /// </summary>
        [DataMember]
        public string ErroMessage
        {
            get;
            set;
        }

        /// <summary>
        /// MFC,PC
        /// </summary>
        [DataMember]
        public string Type
        {
            get;
            set;
        }


        [DataMember]
        public double Factor
        {
            get;
            set;
        }

        private string _title;
        [DataMember]
        public string DisplayTitle
        {
            get
            {
                return string.Format("{0}({1})", DeviceSchematicId, DisplayName);
            }

            set
            {
                _title = value;
            }
        }

        public AITMfcData()
        {
            DisplayName = "Undefined";
            Factor = 1.0;
            Unit = "sccm";
            Type = "MFC";
            DeviceSchematicId = "Undefined";
            UniqueName = "";

        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public class AITMfcOperation
    {
        public const string Ramp = "Ramp";
    }

    public class AITMfcDataPropertyName
    {
        public const string IsOffline = "IsOffline";
        public const string FeedBack = "FeedBack";
        public const string SetPoint = "SetPoint";
        public const string DefaultSetPoint = "DefaultSetPoint";
        public const string Scale = "Scale";
        public const string Range = "Range";
        public const string IsEnabled = "IsEnabled";
        public const string IsOutOfTolerance = "IsOutOfTolerance";

        public const string IsEnableAlarm = "IsEnableAlarm";
        public const string AlarmRange = "AlarmRange";
        public const string AlarmTime = "AlarmTime";

        public const string Status = "Status";

    }

}
