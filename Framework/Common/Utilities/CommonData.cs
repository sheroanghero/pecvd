using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Aitex.Core.Util
{
    /// <summary>
    /// Analog device data
    /// </summary>
    [DataContract]
    [Serializable]
    public class AnalogDeviceData
    {
        [DataMember]
        public string TechnicalName { get; set; }

        [DataMember]
        public string DeviceName { get; set; }

        [DataMember]
        public string DeviceId { get; set; }

        [DataMember]
        public string Unit { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double Scale { get; set; }

        /// <summary>
        /// 设定值
        /// </summary>
        [DataMember]
        public double SetPoint { get; set; }

        /// <summary>
        /// 实际反馈值
        /// </summary>
        [DataMember]
        public double FeedBack { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public double DefaultValue { get; set; }

        /// <summary>
        /// 是否有报警
        /// </summary>
        [DataMember]
        public bool IsWarning { get; set; }

        /// <summary>
        /// alarm或是erro时显示的信息
        /// </summary>
        [DataMember]
        public string ErroMessage { get; set; }

        private double _factor = 1.0;

        [DataMember]
        public double Factor { get { return _factor; } set { _factor = value; } }

        public override string ToString()
        {
            return FeedBack.ToString();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AnalogDeviceData()
        {
        }

        /// <summary>
        /// Copy constructor of this object.
        /// </summary>
        /// <param name="deviceData"></param>
        public AnalogDeviceData(AnalogDeviceData deviceData)
        {
            TechnicalName = deviceData.TechnicalName;

            DeviceName = deviceData.DeviceName;

            DeviceId = deviceData.DeviceId;

            Unit = deviceData.Unit;

            Description = deviceData.Description;

            Scale = deviceData.Scale;

            SetPoint = deviceData.SetPoint;

            FeedBack = deviceData.FeedBack;

            DefaultValue = deviceData.DefaultValue;

            IsWarning = deviceData.IsWarning;

            ErroMessage = deviceData.ErroMessage;
        }
    }

 

    [Serializable]
    public class DataItem
    {
        public string DataName { get; set; }
        //public bool IsPreLoaded { get; set; }
        public List<DateTime> TimeStamp { get; set; }
        public List<float> RawData { get; set; }
    }

 
    /// <summary>
    /// 标识不需要数据库记录的数据类型
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field)]
    public class DatabaseSaveIgnoreAttribute : Attribute { }


    public class ModuleStatusBackground
    {
        public const string Init = "Yellow";
        public const string Error = "OrangeRed";
        public const string Busy = "LawnGreen";
        public const string Idle = "White";
        public const string Offline = "Gray";
        public const string UnInstalled = "Gray";


        public static string GetStatusBackground(string status)
        {
            if (status != null)
                status = status.Trim().ToLower();

            switch (status)
            {
                case "error":
                    return Error;
                case "idle":
                case "manual_idle":
                case "manual":
                    return _dicCustom.ContainsKey(Idle) ? _dicCustom[Idle] : Idle;
                case "notconnect":
                case "init":
                    return Init;
                case "offline":
                    return Offline;
                case "notinstall":
                    return UnInstalled;
                case "uninstall":
                    return UnInstalled;
                default:
                    return Busy;
            }
        }

        private static Dictionary<string, string> _dicCustom = new Dictionary<string, string>();
        public static void CustomColor(string type, string color)
        {
            _dicCustom[type] = color;
        }
    }

}
