using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Aitex.Core.UI.ControlDataContext;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class ConnectionStatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isConnected = value != null && (bool) value;

            return isConnected ? "Green" : "Yellow";
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
