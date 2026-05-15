using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.IndustrialControl.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                if (System.Convert.ToBoolean(value)) return Visibility.Visible;
                else return Visibility.Hidden;
            }
            else
            {
                if (System.Convert.ToBoolean(value)) return Visibility.Hidden;
                else return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
