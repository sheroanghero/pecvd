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
    public class AITConfigData : INotifyPropertyChanged, IDeviceData
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

 
        [DataMember]
        public string SystemType { get; set; }
        [DataMember]
        public string SectionName { get; set; }
        [DataMember]
        public string EntryName { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Description_zh { get; set; }
        [DataMember]
        public string Description_en { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Unit { get; set; }
        [DataMember]
        public string Value { get; set; }
        [DataMember]
        public string Default { get; set; }
        [DataMember]
        public string RangeLowLimit { get; set; }
        [DataMember]
        public string RangeUpLimit { get; set; }
        [DataMember]
        public string XPath { get; set; }
 

        public AITConfigData()
        {
             
        }

        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }
 
}