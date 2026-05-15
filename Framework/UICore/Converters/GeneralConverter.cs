using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.Converters
{
    public class LineColorConverter : IValueConverter
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

    public class bool2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                    return Visibility.Visible;
                return Visibility.Hidden;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class bool2VisibilityConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class Visibility2boolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                Visibility vi = (Visibility)value;
                if (vi == Visibility.Visible) return true;
                return false;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                bool isChecked = (bool)value;
                if (isChecked) return Visibility.Visible;
                return Visibility.Hidden;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Visibility.Visible;
        }
    }

    public class RolloverDataTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                DateTime dt;
                var isSucc = DateTime.TryParse(value.ToString(), out dt);
                if (isSucc)
                    return string.Format("{0}{1}{2}", " 【", dt.ToString("yyyy/MM/dd HH:mm:ss"), "】 ");
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public class BoolToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            return ((bool)value) ? parameter : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return object.Equals(value, parameter);
        }
    }
    public class RadioBoolToIntConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            int integer = (int)value;

            if (integer == int.Parse(parameter.ToString()))

                return true;

            else

                return false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            return parameter;

        }

    }

    public class FlowConverterForWeiQiMultiBinding : IMultiValueConverter
    {
        #region IMultiValueConverter 成员

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str_Result = string.Empty;
            bool isOpen = false;
            for (int i = 0; i < values.Length; i++)
            {
                bool.TryParse(values.GetValue(i) + "", out isOpen);
                if (isOpen == false)
                    return isOpen;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

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

    /// <summary>
    /// 第一项false则关闭，后面的只要有true就打开
    /// </summary>
    public class FlowConverterFirstFalseMultiBinding : IMultiValueConverter
    {
        #region IMultiValueConverter 成员

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2) return false;

            bool isOpen = false;
            bool.TryParse(values.GetValue(0) + "", out isOpen);

            if (!isOpen) return false;
            
            isOpen = false;
            for (int i = 1; i < values.Length; i++)
            {
                bool.TryParse(values.GetValue(i) + "", out isOpen);
                if (isOpen == true)
                    return true;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    public class TrimFolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = value as string;
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            int indexOf = str.LastIndexOf('\\');
            if (indexOf <= 0) return string.Empty;
            return str.Substring(indexOf + 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
