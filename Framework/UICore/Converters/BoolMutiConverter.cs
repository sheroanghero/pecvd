using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Aitex.Core.RT.Log;

namespace MECF.Framework.UI.Core.Converters
{
    public class BoolAndMutiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 0) return false;
            
            try
            {
                bool r = true;
                for (int ii = 0; ii < values.Length; ii++)
                {
                    bool b = (bool)values[ii];
                    if (!b) { r = false; break; }
                }
                return r;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolOrMutiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 0) return false;

            try
            {
                bool r = false;
                for (int ii = 0; ii < values.Length; ii++)
                {
                    bool b = (bool)values[ii];
                    if (b) { r = true; break; }
                }
                return r;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolAnd2VisibilityMutiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 0) return false;

            try
            {
                Visibility r = Visibility.Visible;
                for (int ii = 0; ii < values.Length; ii++)
                {
                    bool b = (bool)values[ii];
                    if (!b) { r = Visibility.Collapsed; break; }
                }
                return r;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolOr2VisibilityMutiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 0) return false;

            try
            {
                Visibility r = Visibility.Collapsed;
                for (int ii = 0; ii < values.Length; ii++)
                {
                    bool b = (bool)values[ii];
                    if (b) { r = Visibility.Visible; break; }
                }
                return r;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
