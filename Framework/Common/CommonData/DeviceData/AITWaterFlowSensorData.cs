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
    public class AITWaterFlowSensorData : NotifiableItem, IDeviceData
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
 

        [DataMember]
        public double FeedBack { get; set; }
 
        [DataMember]
        public bool IsWarning { get; set; }

        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public bool IsOutOfTolerance { get; set; }

        public AITWaterFlowSensorData()
        {
            DisplayName = "Undefined";
            Unit = "slm";
 
 
        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public class AITWaterFlowSensorPropertyName
    {
        public const string Feedback = "Feedback";
        public const string IsWarning = "IsWarning";
        public const string IsError = "IsError";
    }
}
