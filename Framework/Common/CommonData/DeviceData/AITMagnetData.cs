using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITMagnetData : NotifiableItem, IDeviceData
    {

        [DataMember]
        public string DeviceModule { get; set; }

        /// <summary>
        /// 唯一名称，UI与RT交互的ID
        /// </summary>
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

 
        [DataMember]
        public bool IsRotation { get; set; }

        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public float RotationVelocity { get; set; }

        [DataMember]
        public string RotationVelocityDisplay { get; set; }

        public AITMagnetData()
        {
            DisplayName = "Undefined";
        }

        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

   
}
