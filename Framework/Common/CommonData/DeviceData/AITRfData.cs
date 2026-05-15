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
    [DataContract]
    public enum RfMode
    {
        [EnumMember]
        ContinuousWaveMode = 1,

        [EnumMember]
        PulsingMode = 2,
    }

    public enum PatronRfMatchMode
    {
        Manual = 1,
        Auto = 2,
    }

    public enum TritonRfMatchMode
    {
        Manual = 0,
        Auto = 1,
    }

 

    [DataContract]
    [Serializable]
    public class AITRfData : AITDeviceData
    {
 

        [DataMember]
        public string UnitPower { get; set; }
        [DataMember]
        public string UnitFrequency { get; set; }
        [DataMember]
        public string UnitDuty { get; set; }

        [DataMember]
        public double ScalePower { get; set; }
        [DataMember]
        public double ScaleFrequency { get; set; }
        [DataMember]
        public double ScaleDuty { get; set; }

        [DataMember]
        public float PowerSetPoint { get; set; }
        [DataMember]
        public float FrequencySetPoint { get; set; }
        [DataMember]
        public float DutySetPoint { get; set; }

        [DataMember]
        public bool IsInterlockOk { get; set; }

        [DataMember]
        public bool IsRfOn { get; set; }

        [DataMember]
        public bool IsRfAlarm { get; set; }

        [DataMember]
        public float ForwardPower { get; set; }

        [DataMember]
        public float ReflectPower { get; set; }

        [DataMember]
        public int WorkMode { get; set; }

        [DataMember]
        public string PowerOnElapsedTime
        {
            get;
            set;
        }
        [DataMember]
        public bool EnablePulsing
        {
            get;
            set;
        }
        [DataMember]
        public bool EnableC1C2Position
        {
            get;
            set;
        }
        [DataMember]
        public bool EnableReflectPower
        {
            get;
            set;
        }
        [DataMember]
        public bool EnableVoltageCurrent
        {
            get;
            set;
        }

        [DataMember]
        public int MatchMode
        {
            get;
            set;
        }

        [DataMember]
        public int MatchPresetMode
        {
            get;
            set;
        }

        [DataMember]
        public float MatchPositionC1
        {
            get;
            set;
        }

        [DataMember]
        public float MatchPositionC2
        {
            get;
            set;
        }

        [DataMember]
        public float MatchPositionC1SetPoint
        {
            get;
            set;
        }

        [DataMember]
        public float MatchPositionC2SetPoint
        {
            get;
            set;
        }

        [DataMember]
        public float Voltage
        {
            get;
            set;
        }

        [DataMember]
        public float Current
        {
            get;
            set;
        }

        public string TextWorkMode
        {
            get
            {
                return WorkMode == (int) RfMode.ContinuousWaveMode ? "Continuous" : "Pulsing";
            }
        }

        public string TextOnOff
        {
            get
            {
                return IsRfOn ? "On" : "Off";
            }
        }
 
        public bool HasError
        {
            get
            {
                return IsRfAlarm || !IsInterlockOk;
            }
        }
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

        public AITRfData()
        {
            DisplayName = "Undefined";
        }

 
    }

    public enum AITRfOperation
    {
        SetPowerOnOff,
        SetMode,
        
        SetContinuousPower,
        SetPulsingPower,

        SetPower,
        SetPulseFrequency,
        SetPulsingFrequency,
        SetPulsingDuty,

        SetVoltage,
        SetCurrent,

        SetMatchMode,
        SetMatchProcessMode,
        SetMatchPosition, //同时设置2个
        SetMatchPositionC1,
        SetMatchPositionC2,

        SetHeatOnOff,
        SetPowerOn,
        SetPowerOff,
    }

    public class AITRfProperty
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