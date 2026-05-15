using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
    [Serializable]
    public enum EnumRfPowerRegulationMode
    {
        [EnumMember]
        Undefined = 0,

        [EnumMember]
        Forward = 1,

        [EnumMember]
        Load = 2,

        [EnumMember]
        DcBias = 3,

        [EnumMember]
        VALimit = 4,

        [EnumMember]
        Power = 6,

        [EnumMember]
        Voltage = 7,

        [EnumMember]
        Current = 8,
    }

    [Serializable]
    public enum EnumRfPowerWorkMode
    {
        [EnumMember]
        Undefined = 0,

        [EnumMember]
        ContinuousWaveMode = 1,

        [EnumMember]
        PulsingMode = 2,
    }

    [Serializable]
    public enum EnumRfPowerClockMode
    {
        [EnumMember]
        Undefined = 0,

        [EnumMember]
        Phase = 1,

        [EnumMember]
        FreqManual = 2,

        [EnumMember]
        FreqAuto = 3,
    }

    [DataContract]
    [Serializable]
    public class AITRfPowerData : NotifiableItem, IDeviceData
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
        public string Description { get; set; }

        [DataMember]
        public string UnitPower { get; set; }
        [DataMember]
        public string UnitFrequency { get; set; }
        [DataMember]
        public string UnitDuty { get; set; }
        [DataMember]
        public string UnitVoltage { get; set; }
        [DataMember]
        public string UnitCurrent { get; set; }

        [DataMember]
        public double ScalePower { get; set; }
        [DataMember]
        public double ScaleFrequency { get; set; }
        [DataMember]
        public double ScaleDuty { get; set; }

        [DataMember]
        public float ScaleCurrent { get; set; }

        [DataMember]
        public float ScaleVoltage { get; set; }

        [DataMember]
        public float PowerSetPoint { get; set; }
        [DataMember]
        public float FrequencySetPoint { get; set; }
        [DataMember]
        public float DutySetPoint { get; set; }

        [DataMember]
        public float VoltageSetPoint { get; set; }

        [DataMember]
        public float CurrentSetPoint { get; set; }

        [DataMember]
        public bool IsInterlockOk { get; set; }


        [DataMember]
        public bool IsPulseType { get; set; }

        [DataMember]
        public bool IsRfOn { get; set; }

        [DataMember]
        public bool IsRfAlarm { get; set; }

        [DataMember]
        public float ForwardPower { get; set; }

        [DataMember]
        public float ReflectPower { get; set; }

        [DataMember]
        public float Voltage { get; set; }

        [DataMember]
        public float Current { get; set; }

        [DataMember]
        public bool IsToleranceError
        {
            get;
            set;
        }
        [DataMember]
        public bool IsToleranceWarning
        {
            get;
            set;
        }

        [DataMember]
        public float Frequency { get; set; }

        [DataMember]
        public float PulsingFrequency { get; set; }

        [DataMember]
        public float PulsingDutyCycle { get; set; }

        [DataMember]
        public float PulsingReverseTime { get; set; }

        public string TextOnOff
        {
            get
            {
                return IsRfOn ? "On" : "Off";
            }
        }

        [DataMember]
        public EnumRfPowerRegulationMode RegulationMode { get; set; }
        [DataMember]
        public EnumRfPowerClockMode ClockMode { get; set; }

        [DataMember]
        public EnumRfPowerClockMode ClockModeSetpoint { get; set; }

        public AITRfPowerData()
        {
            DisplayName = "Undefined";
        }

        public void Update(IDeviceData data)
        {
             
        }
    }

    public enum AITRfPowerOperation
    {
        SetPowerOnOff,
        SetMode,
        
        SetContinuousPower,
        SetPulsingPower,

        SetPower,
        SetPulsingFrequency,
        SetPulsingDuty,

        SetMatchMode,
        SetMatchProcessMode,
        SetMatchPosition, //同时设置2个
        SetMatchPositionC1,
        SetMatchPositionC2,
    }

    public class AITRfPowerProperty
    {
        public const string RFEnable = "RFEnable";
        public const string RFSetPoint = "RFSetPoint";
        public const string RFForwardPower = "RFForwardPower";
        public const string RFReflectPower = "RFReflectPower";
        public const string RFRatio = "RFRatio";
        public const string RFInterlock = "RFInterlock";
        public const string RFDuty = "RFDuty";
        public const string RFFrequency = "RFFrequency";
        public const string RFMode = "RFMode";
        public const string RFMatchPositionC1Feedback = "RFMatchPositionC1Feedback";
        public const string RFMatchPositionC2Feedback = "RFMatchPositionC2Feedback";
        public const string RFMatchPositionC1SetPoint = "RFMatchPositionC1SetPoint";
        public const string RFMatchPositionC2SetPoint = "RFMatchPositionC2SetPoint";
        public const string RFAlarm = "RFAlarm";

        public const string Voltage = "Voltage";
        public const string Current = "Current";

        public const string IsOverTemp = "IsOverTemp";

    }

}