using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITBoostPumpData : INotifyPropertyChanged, IDeviceData
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void InvokePropertyChanged()
        {
            PropertyInfo[] ps = this.GetType().GetProperties();
            foreach (PropertyInfo p in ps)
            {
                InvokePropertyChanged(p.Name);

                if (p.PropertyType == typeof(ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this, null) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }

            FieldInfo[] fi = this.GetType().GetFields();
            foreach (FieldInfo p in fi)
            {
                InvokePropertyChanged(p.Name);

                if (p.FieldType == typeof(ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }
        }

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
 
        [DataMember]
        public bool IsRunning { get; set; }
 
        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public bool EnableFrequency { get; set; }

        [DataMember]
        public double EnableSetPoint { get; set; }

        [DataMember]
        public double PressureSetPoint { get; set; }

        [DataMember]
        public double PressureSetPointMax { get; set; }

        [DataMember]
        public double InverterFrequency { get; set; }

        public string PressureSetPointDisplay
        {
            get
            {
                return ((int)PressureSetPoint).ToString();
            }
        }

        public string FrequencyDisplay
        {
            get
            {
                return EnableFrequency ? string.Format("{0}%", (int)InverterFrequency) : "";
            }
        }

        public string InverterFrequencyDisplay
        {
            get
            {
                return InverterFrequency.ToString("F1");
            }
        }


        public AITBoostPumpData()
        {
            DisplayName = "未定义";
        }

        public void Update(IDeviceData data)
        {
            AITBoostPumpData item = data as AITBoostPumpData;

            if (item == null)
                return;

            InvokePropertyChanged();
        }
    }

    public enum AITBoostPumpOperation
    {
        SetEnable,
        SetPressure,
    }

    public class AITBoostPumpDataPropertyName
    {
        public const string EnableSetPoint = "EnableSetPoint";
        public const string PressureSetPoint = "PressureSetPoint";
        public const string IsError = "IsError";
        public const string IsWarning = "IsWarning";
        public const string InverterFrequency = "InverterFrequency";
    }
}