using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
	public class VisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int target = 1;
			if (parameter != null)
			{
				target = int.Parse(parameter.ToString());
			}
			return (int)value == target ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
