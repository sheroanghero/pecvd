using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using Aitex.Core.RT.Device;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Device.Bases;

namespace Aitex.Core.Common.DeviceData
{
    public enum LidState
    {
        Close = 0,
        Open = 1,
        Unknown = 2,
        Error = 3,
    }

    [DataContract]
    [Serializable]
    public class AITLidData : NotifiableItem, IDeviceData
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
        /// IO 表中定义的物理编号，物理追溯使用 比如: V122
        /// </summary>
        [DataMember]
        public string DeviceSchematicId { get; set; }

        /// <summary>
        /// 当前设定值
        /// </summary>
        [DataMember]
        public int SetPoint { get; set; }

 

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public int Status { get; set; }

        public bool IsOpen 
        {
            get { return Status == (int)LidState.Open;  }
        }

        public bool IsClose
        {
            get
            {
                return Status == (int)LidState.Close;
            }
        }

        public AITLidData()
        {
            DisplayName = "Undefined Lid";
        }

        public void Update(IDeviceData data)
        {
 
        }
    }

    public class AITLidOperation
    {
        public const string OpenLid = "OpenLid";
        public const string CloseLid = "CloseLid";

    }
}
