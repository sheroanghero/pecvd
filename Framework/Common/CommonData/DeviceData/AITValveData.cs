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
    public class AITValveData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string UniqueName { get; set; }


        [DataMember]
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: V122
        /// </summary>
        [DataMember]
        public string DeviceSchematicId { get; set; }

        /// <summary>
        /// 当前设定值
        /// </summary>
        [DataMember]
        public bool SetPoint { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public bool DefaultValue { get; set; }

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public bool Feedback { get; set; }

        public string OpenStatus
        {
            get { return IsOpen? "Open":"Close"; }
        }

        public bool IsOpen {
            get { return Feedback;  }
        }

        public AITValveData()
        {
            DisplayName = "未定义阀门";
        }

        public void Update(IDeviceData data)
        {
            AITValveData item = data as AITValveData;

            if (item == null)
                return;

            this.DefaultValue = item.DefaultValue;
            this.DeviceSchematicId = item.DeviceSchematicId;
            this.DeviceName = item.DeviceName;
            this.DisplayName = item.DisplayName;

            this.Feedback = item.Feedback;
            this.SetPoint = item.SetPoint;

            InvokePropertyChanged();
        }
    }

    public class AITValveOperation
    {
        public const string GVTurnValve = "GVTurnValve";
    }

    public class AITValveDataPropertyName
    {
        public const string Status = "Status";
        public const string SetPoint = "SetPoint";
    }
}
