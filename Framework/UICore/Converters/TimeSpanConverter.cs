using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MECF.Framework.UI.Core.Converters
{
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is TimeSpan))
            {
                return null;
            }

            TimeSpan ts = (TimeSpan)value;

            if (ts.Days > 0)
                return $"{ts.Days}d {ts.Hours}h";
            else return $"{ts.Hours}h {ts.Minutes}m";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
