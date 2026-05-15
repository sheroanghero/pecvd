using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                Color color = (Color)value;
                return new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                Brush brush = (SolidColorBrush)value;
                Color color = (Color)ColorConverter.ConvertFromString(brush.ToString());
                return color;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Colors.Black;
        }
    }
}
