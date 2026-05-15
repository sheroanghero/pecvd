using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Converters
{
    public class ValveVisibleConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool vInOnOff = false;

            if (value != null)
            {
                bool.TryParse((value as GasValveDataItem).Feedback + "", out vInOnOff);
            }

            return vInOnOff ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
