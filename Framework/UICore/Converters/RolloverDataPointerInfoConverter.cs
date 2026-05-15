using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.Converters
{
    public class RolloverDataPointerInfoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                DateTime time = (DateTime)values[0];
                double y = (double)values[1];

                return string.Format("【{0}】   {1}", time.ToString("yyyy/MM/dd HH:mm:ss"), y);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
