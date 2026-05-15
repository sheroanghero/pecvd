using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
	public class BoolVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
		    if (value == null)
		        return Visibility.Visible;

		    if (parameter != null)
		    {
                return (bool)value == System.Convert.ToBoolean(parameter) ? Visibility.Visible : Visibility.Hidden;
            }

            var v = (bool)value;
			return v == true ? Visibility.Visible : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

    public class BoolCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;

            if (parameter != null)
            {
                return (bool)value == System.Convert.ToBoolean(parameter) ? Visibility.Visible : Visibility.Collapsed;
            }

            var v = (bool)value;
            return v == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
