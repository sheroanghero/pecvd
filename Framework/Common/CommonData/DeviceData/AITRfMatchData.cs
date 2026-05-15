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
    public enum EnumRfMatchTuneMode
    {
        [EnumMember]
        Undefined,

        [EnumMember]
        Auto = 1,    // auto tune

        [EnumMember]
        Manual = 2,  //Host control
 
    }


    /// <summary>
    /// 预置电容位置结构体
    /// </summary>
    [Serializable]
    public struct Presets
    {
        public byte PreNo;        //预置电容编号 1-4
        public byte TraSum;       //电容移动轨迹 0-3 0-没有
        public float LoadData;    //电容load预置值
        public float TuneData;    //电容Tune预置值
        public float[,] TraData;  //轨迹值 最多3个
        public void Init()
        {
            TraData = new float[3, 2];
            PreNo = 0;
            TraSum = 0;
            LoadData = 0;
            TuneData = 0;
        }
    }

    [DataContract]
    [Serializable]
    public class AITRfMatchData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string Module { get; set; }

        [DataMember]
        public string DeviceName { get; set; }
 
        [DataMember]
        public string DisplayName { get; set; }
 
        [DataMember]
        public string DeviceSchematicId { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string UnitPower { get; set; }

        [DataMember]
        public float DCBias { get; set; }
        [DataMember]
        public float BiasPeak { get; set; }
        [DataMember]
        public float LoadPosition1 { get; set; }
        [DataMember]
        public float LoadPosition2 { get; set; }
        [DataMember]
        public float TunePosition1 { get; set; }
        [DataMember]
        public float TunePosition2 { get; set; }

        [DataMember]
        public float LoadPosition1SetPoint { get; set; }
        [DataMember]
        public float TunePosition1SetPoint { get; set; }

        [DataMember]
        public float TuneRange { get; set; }

        [DataMember]
        public float LoadRange { get; set; }

        [DataMember]
        public EnumRfMatchTuneMode TuneMode1 { get; set; }

        [DataMember]
        public EnumRfMatchTuneMode TuneMode2 { get; set; }

        [DataMember]
        public EnumRfMatchTuneMode LoadMode1 { get; set; }

        [DataMember]
        public EnumRfMatchTuneMode LoadMode2 { get; set; }

        [DataMember]
        public bool IsInterlockOk { get; set; }

        public AITRfMatchData()
        {
            DisplayName = "Undefined";
        }

        public void Update(IDeviceData data)
        {
             
        }
    }

    public enum AITRfMatchOperation
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

    public class AITRfMatchProperty
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