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
    public class AITAlignerData : INotifyPropertyChanged, IDeviceData
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
        public bool IsInitalized { get; set; }

        [DataMember]
        public bool IsBusy { get; set; }
 
        [DataMember]
        public bool IsCommunicationError { get; set; }

        [DataMember]
        public int State { get; set; }

        [DataMember]
        public int ErrorCode { get; set; }

        [DataMember]
        public int ElapseTime { get; set; }

        [DataMember]
        public int Notch { get; set; }

 

        public bool IsError
        {
            get { return ErrorCode > 0 || IsCommunicationError; }
        }


        public AITAlignerData()
        {
            DisplayName = "Undefined";
        }

        public void Update(IDeviceData data)
        {
 
        }
    }

    public class AITAlignerOperation
    {
        public const string Home = "Home";
        public const string Align = "Align";
        public const string Reset = "Reset";
        public const string Stop = "Stop";
    }

    public class AITAlignerPropertyName
    {
         
    }
}
