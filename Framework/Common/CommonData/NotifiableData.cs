using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace MECF.Framework.Common.CommonData
{
    [Serializable]
    [DataContract]
    public class NotifiableItem : INotifyPropertyChanged
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

    }


    [DataContract]
    [Serializable]
    public class WCFProcessJobInterface
    {
        [DataMember]
        public string ObjtID { get; set; }
        [DataMember]
        public string ObjType { get; set; }
        [DataMember]
        public string PRMtlType { get; set; }
        [DataMember]
        public bool PRProcessStart { get; set; }
        [DataMember]
        public string PRRecipeMethod { get; set; }
        [DataMember]
        public string PJstate { get; set; }
        [DataMember]
        public DateTime CreateTime { get; set; }
        [DataMember]
        public DateTime CompleteTime { get; set; }
        [DataMember]
        public string RecID { get; set; }

    }
    [DataContract]
    [Serializable]
    public class WCFControlJobInterface
    {
        [DataMember]
        public string ObjtID { get; set; }
        [DataMember]
        public string ObjType { get; set; }
        [DataMember]
        public string ProcessingOrderMgmt { get; set; }
        [DataMember]
        public bool StartMethod { get; set; }
        [DataMember]
        public string state { get; set; }
        [DataMember]
        public DateTime CreateTime { get; set; }
        [DataMember]
        public DateTime CompleteTime { get; set; }
    }

}
