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
    public class AITPressureSensorData : INotifyPropertyChanged, IDeviceData
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

                if (p.PropertyType == typeof (ICommand))
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

                if (p.FieldType == typeof (ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }
        }
        /// <summary>
        /// 设备的唯一名称，UI与RT交互的ID
        /// </summary>
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
        public string Unit { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double Scale { get; set; }
 

        [DataMember]
        public double FeedBack { get; set; }
 
        [DataMember]
        public bool IsWarning { get; set; }

        [DataMember]
        public bool IsError { get; set; }

        [DataMember]
        public bool IsOutOfRange { get; set; } 
 

        public AITPressureSensorData()
        {
            DisplayName = "Undefined";
            Unit = "mTorr";
 
 
        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public class AITPressureSensorPropertyName
    {
        public const string Feedback = "Feedback";
        public const string IsWarning = "IsWarning";
        public const string IsError = "IsError";
    }
}
