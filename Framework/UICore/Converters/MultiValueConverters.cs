using Aitex.Sorter.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MECF.Framework.UI.Core.Converters
{
    public class OpenSlitEnableMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return null;
            if (!(values[0] is bool)) return null;
            if (!(values[1] is FoupDoorState)) return null;
            FoupDoorState state = (FoupDoorState)values[1];

            bool isMaunual = (bool)values[0];
            if (isMaunual && state == FoupDoorState.Unknown) return true;

            if (isMaunual && state == FoupDoorState.Close) return true;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CloseSlitEnableMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return null;
            if (!(values[0] is bool)) return null;
            if (!(values[1] is FoupDoorState)) return null;
            FoupDoorState state = (FoupDoorState)values[1];

            bool isMaunual = (bool)values[0];
            if (isMaunual && state == FoupDoorState.Unknown) return true;

            if (isMaunual && state == FoupDoorState.Open) return true;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
