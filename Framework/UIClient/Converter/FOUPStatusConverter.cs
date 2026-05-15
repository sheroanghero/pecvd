using System;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
    public class FOUPStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string result = string.Empty;            
            int iValue;         
            if (int.TryParse(value.ToString(), out iValue))
            {
                if (iValue == 0)
                    result = "Loaded";
                else if (iValue == 1)
                    result = "Mapped";
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
