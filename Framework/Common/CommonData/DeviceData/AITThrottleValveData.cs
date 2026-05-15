using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
    [Serializable]
    [DataContract]
    public enum PressureCtrlMode
    {

        [EnumMember]
        TVPositionCtrl = 0,

        [EnumMember]
        TVPressureCtrl = 1,

        [EnumMember]
        TVOpen = 3,

        [EnumMember]
        TVClose = 4,

        [EnumMember]
        TVCalib = 5,

        [EnumMember]
        Undefined = 6,
        
    }

    [DataContract]
    [Serializable]
    public class AITThrottleValveData : NotifiableItem, IDeviceData
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
        public string UnitPosition { get; set; }

        [DataMember]
        public string UnitPressure { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double MaxValuePosition { get; set; }

        [DataMember]
        public double MaxValuePressure { get; set; }

        /// <summary>
        /// MFC,PC
        /// </summary>
        [DataMember]
        public string Type { get; set; }


        [DataMember]
        public double Factor { get; set; }

        [DataMember]
        public int Mode { get; set; } 

        [DataMember]
        public float PositionFeedback { get; set; }

        [DataMember]
        public float PressureFeedback { get; set; }

        [DataMember]
        public float PressureSetPoint { get; set; }

        [DataMember]
        public float PositionSetPoint { get; set; }

        [DataMember]
        public int State { get; set; }

        public string TextMode
        {
            get
            {
                switch (Mode)
                {
                    case (int)(PressureCtrlMode.TVPositionCtrl):
                        return "Position";
                    case (int)(PressureCtrlMode.TVPressureCtrl):
                        return "Pressure";
                    case (int)(PressureCtrlMode.TVOpen):
                        return "Open";
                    case (int)(PressureCtrlMode.TVClose):
                        return "Close";
                    default:
                        return "Undefined";
                }
            }
        }

        public AITThrottleValveData()
        {
            DisplayName = "Undefined";
            Factor = 1.0;
            UnitPosition = "%";
            UnitPressure = "mTorr";
            Type = "TV";
            MaxValuePosition = 100;
            MaxValuePressure = 1000;
        }

        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public enum AITThrottleValveOperation
    {
        SetMode,
        SetPosition,
        SetPressure,
    }


    public class AITThrottleValvePropertyName
    {
        public const string TVPositionSetPoint = "TVPositionSetPoint";
        public const string TVPosition = "TVPosition";
        public const string TVPressureSetPoint = "TVPressureSetPoint";
        public const string TVPressure = "TVPressure";
 

    }


    public class PressureCtrlModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            var status = (PressureCtrlMode)value;
            switch (status)
            {
                case PressureCtrlMode.TVOpen:
                    return "Open";
                case PressureCtrlMode.TVClose:
                    return "Close";
                case PressureCtrlMode.TVPositionCtrl:
                    return "Position";
                case PressureCtrlMode.TVPressureCtrl:
                    return "Pressure";
                case PressureCtrlMode.TVCalib:
                    return "Calibration";
                case PressureCtrlMode.Undefined:
                    return "Unknown";
                default:
                    break;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}