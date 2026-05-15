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
    public class AITPumpData : AITDeviceData
    {
        [DataMember]
        public string DeviceModule { get; set; }

        /// <summary>
        /// 当前设定值
        /// </summary>
        [DataMember]
        public bool IsOn { get; set; }

        [DataMember]
        public bool IsOk { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public bool IsWarning { get; set; }

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public int Speed { get; set; }


        [DataMember]
        public bool OverTemp { get; set; }

        [DataMember]
        public bool AtSpeed { get; set; }

        [DataMember]
        public int Temperature { get; set; }

        [DataMember]
        public int LocalRemoteMode { get; set; }

        [DataMember]
        public double WaterFlow { get; set; }

        [DataMember]
        public bool IsDryPumpEnable { get; set; }

        [DataMember]
        public bool IsN2PressureEnable { get; set; }

        [DataMember]
        public bool N2PressureWarning { get; set; }

        [DataMember]
        public bool N2PressureAlarm { get; set; }

        [DataMember]
        public bool IsWaterFlowEnable { get; set; }

        [DataMember]
        public bool WaterFlowWarning { get; set; }

        [DataMember]
        public bool WaterFlowAlarm { get; set; }


        [DataMember]
        public bool IsOverLoad { get; set; }

        [DataMember]
        public float FstStageTemp { get; set; }
        [DataMember]
        public float SecStageTemp { get; set; }
        [DataMember]
        public float TCPress { get; set; }

        [DataMember]
        public string TCPressUnit { get; set; }

        [DataMember]
        public float Velocity { get; set; }

        [DataMember]
        public float AlarmCode { get; set; }

        public AITPumpData()
        {
            DisplayName = "未定义";
        }

    }

    public enum AITPumpOperation
    {
        SetOnOff,
        SetPowerOnOff,

        PumpOn,
        PumpOff,
        PumpReset,
    }

    public class AITPumpProperty
    {
        public const string EnableWaterFlow = "EnableWaterFlow";
        public const string WaterFlowValue = "WaterFlowValue";
        public const string WaterFlowMinValue = "WaterFlowMinValue";
        public const string WaterFlowMaxValue = "WaterFlowMaxValue";
        public const string WaterFlowAlarm = "WaterFlowAlarm";
        public const string WaterFlowAlarmSetPoint = "WaterFlowAlarmSetPoint";
        public const string WaterFlowWarning = "WaterFlowWarning";
        public const string WaterFlowAlarmTime = "WaterFlowAlarmTime";
        public const string WaterFlowWarningTime = "WaterFlowWarningTime";

        public const string EnableN2Pressure = "EnableN2Pressure";
        public const string N2PressureValue = "N2PressureValue";
        public const string N2PressureMinValue = "N2PressureMinValue";
        public const string N2PressureMaxValue = "N2PressureMaxValue";
        public const string N2PressureAlarm = "N2PressureAlarm";
        public const string N2PressureAlarmSetPoint = "N2PressureAlarmSetPoint";
        public const string N2PressureWarning = "N2PressureWarning";
        public const string N2PressureAlarmTime = "N2PressureAlarmTime";
        public const string N2PressureWarningTime = "N2PressureWarningTime";

        public const string EnableDryPump = "EnableDryPump";

        public const string PumpBreakerStatus = "PumpBreakerStatus";

        public const string IsOverTemp = "IsOverTemp";
        public const string IsRunning = "IsRunning";
        public const string IsStart = "IsStart";
        public const string IsStop = "IsStop";
    }
}
