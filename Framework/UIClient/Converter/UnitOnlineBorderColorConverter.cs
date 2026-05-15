using System;
using System.Windows.Data;
using OpenSEMI.Ctrlib.Controls;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
    public class UnitOnlineBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool) value)
                    return "LimeGreen";// "#AFC2D3";
                else
                {
                    return "Gray";
                }
            }
            return "Transparent";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
