using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Aitex.Core.Util
{
    /// <summary>
    /// 其中有一项为true就是true
    /// </summary>
    public class FlowConverterForAllTrueMultiBinding : IMultiValueConverter
    {
        #region IMultiValueConverter 成员

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str_Result = string.Empty;
            bool isOpen = false;
            for (int i = 0; i < values.Length; i++)
            {
                bool.TryParse(values.GetValue(i) + "", out isOpen);
                if (isOpen == true)
                    return isOpen;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
