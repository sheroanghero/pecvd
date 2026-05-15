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
    public class AITHeaterData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string UniqueName { get; set; }

        [DataMember]
        public string Module { get; set; }
 
        [DataMember]
        public string DeviceName { get; set; }
 
        [DataMember]
        public string DisplayName { get; set; }
 
        [DataMember]
        public string DeviceSchematicId { get; set; }

        [DataMember]
        public string Unit { get; set; }

        [DataMember]
        public string Description { get; set; }
 
        [DataMember]
        public double Scale { get; set; }
 
        [DataMember]
        public double SetPoint { get; set; }

        [DataMember]
        public double FeedBack { get; set; }
 
        [DataMember]
        public double DefaultValue { get; set; }
 
        [DataMember]
        public double DefaultSetPoint { get; set; }
 
        [DataMember]
        public bool IsWarning { get; set; }
 
        [DataMember]
        public string ErroMessage { get; set; }
 
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public double Factor { get; set; }

        [DataMember]
        public bool IsPowerOn
        {
            get;
            set;
        }

        [DataMember]
        public bool IsPowerOnSetPoint
        {
            get;
            set;
        }

        [DataMember]
        public bool IsControlTcBroken
        {
            get;
            set;
        }

        [DataMember]
        public bool IsMonitorTcBroken
        {
            get;
            set;
        }

        [DataMember]
        public double MonitorTcFeedBack { get; set; }

        //for the PID control type
        [DataMember]
        public Dictionary<string, object> AttrValue { get; set; }

        public string Display
        {
            get
            {
                string value =  FeedBack.ToString(FormatString);
                return DisplayWithUnit ? value + " " + Unit : value;
            }
        }
        [DataMember]
        public string FormatString
        {
            get; set;
        }

        [DataMember]
        public bool DisplayWithUnit
        {
            get;
            set;
        }

        public AITHeaterData()
        {
            DisplayName = "Undefined";
            Factor = 1.0;
            Unit = "℃";
            Type = "Heater";
            FormatString = "F2";
            AttrValue = new Dictionary<string, object>();
        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public class AITHeaterOperation
    {
        public const string Ramp = "Ramp";
        public const string SetPowerOnOff = "SetPowerOnOff";
    }

    public class AITHeaterPropertyName
    {
        public const string ControlTcFeedback = "ControlTcFeedback";
        public const string MonitorTcFeedback = "MonitorTcFeedback";

        public const string ControlTcSetPoint = "ControlTcSetPoint";

        public const string IsControlTcBroken = "IsControlTcBroken";
        public const string IsMonitorTcBroken = "IsMonitorTcBroken";

        public const string IsPowerOnFeedback = "IsPowerOnFeedback";
        public const string IsPowerOnSetPoint = "IsPowerOnSetPoint";

        public const string IsTemperatureAlarm = "IsTemperatureAlarm";

    }
}
