using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Aitex.Core.RT.Log;

namespace MECF.Framework.UI.Core.Converters
{
    public class BoolSensorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
             
            return (bool)value ? "LightGreen" : "Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;

        }
    }

    public class BoolWaterConverter : IValueConverter
    {
	    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	    {

		    return (bool)value ? "#5CACEE" : "Transport";
	    }

	    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	    {
		    return null;

	    }
    }

    public class ReserveBoolSensorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
             
            return !(bool)value ? "LightGreen" : "Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;

        }
    }
}
