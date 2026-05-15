using System;
using System.Windows.Data;
using OpenSEMI.Ctrlib.Controls;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
    public class UnitStatusBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string status = (string) value;
                if (!string.IsNullOrEmpty(status))
                    status = status.Trim().ToLower();

                switch (status)
                {
                    case "error":
                        return "OrangeRed";
                    case "idle":
                        return "LightBlue";
                    case "init":
                    case "notconnect":
                        return "Yellow";
                    default:
                        return "LawnGreen";
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
