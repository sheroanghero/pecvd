using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Threading;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Utilities;

namespace Aitex.Core.UI.MVVM
{
    public class ViewModelBase : INotifyPropertyChanged, IViewModelControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void InvokeAllPropertyChanged()
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

            //Parallel.ForEach(this.GetType().GetProperties(), property => InvokePropertyChanged(property.Name));
        }   

        public void InvokePropertyChanged()
        {
            PropertyInfo[] ps = this.GetType().GetProperties();
            foreach (PropertyInfo p in ps)
            {
                if (!p.GetCustomAttributes(false).Any(attribute=>attribute is IgnorePropertyChangeAttribute))
                    InvokePropertyChanged(p.Name);

				if (p.PropertyType == typeof(ICommand))
				{
					if (p.GetValue(this, null) is IDelegateCommand cmd)
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

            //Parallel.ForEach(this.GetType().GetProperties(), property => InvokePropertyChanged(property.Name));
        }   
    }
}
