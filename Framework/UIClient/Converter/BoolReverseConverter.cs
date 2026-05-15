using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
	public class BoolReverseConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
            if (value == null)
                return true;

            return !((bool) value);
        }

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
