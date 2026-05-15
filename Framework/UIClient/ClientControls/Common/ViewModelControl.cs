using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Core.Utilities;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
	public class ViewModelControl : UserControl, INotifyPropertyChanged, IViewModelControl
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void SubscribeKeys(SubscriptionViewModelBase baseModel)
		{
			baseModel.SubscribeKeys(this);
		}

		public void InvokePropertyChanged()
		{
			if (PropertyChanged != null)
			{
				var ps = this.GetType().GetProperties();
				foreach (var p in ps)
				{
					if (!p.GetCustomAttributes(false).Any(attribute => attribute is IgnorePropertyChangeAttribute))
						PropertyChanged(this, new PropertyChangedEventArgs(p.Name));

					if (p.PropertyType == typeof(ICommand))
					{
						if (p.GetValue(this, null) is IDelegateCommand cmd)
							cmd.RaiseCanExecuteChanged();
					}
				}
			}
		}

	}
}
