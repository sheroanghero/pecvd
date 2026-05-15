using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Aitex.Core.UI.ControlDataContext
{
    [DataContract]
    [Serializable]
    public class PumpDataItem
    {
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
        public string DeviceId { get; set; }

        [DataMember]
        public bool MainPumpEnable { get; set; }


        [DataMember]
        public bool IsWarning { get; set; }

    }

    public class PumpOperation
    {
        public const string PumpON = "Open";

        /// <summary>
        /// 无参数
        /// </summary>
        public const string PumpOff = "Close";
    }
}
