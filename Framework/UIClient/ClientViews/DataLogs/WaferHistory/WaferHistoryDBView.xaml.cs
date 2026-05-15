using Aitex.Sorter.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{

    /// <summary>
    /// WaferHistoryDB.xaml 的交互逻辑
    /// </summary>
    public partial class WaferHistoryDBView : UserControl
    {
        public WaferHistoryDBView()
        {
            InitializeComponent();
        }
    }

    public class HistoryLayoutSelectorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var type = ((WaferHistoryItem)value).Type;
                switch (type)
                {
                    case WaferHistoryItemType.None:
                        return LotsLayout;
                    case WaferHistoryItemType.Lot:
                        return WaferLayout;
                    case WaferHistoryItemType.Wafer:
                        return MovementLayout;
                    case WaferHistoryItemType.Recipe:
                        return RecipeLayout;
                    default:
                        break;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public object WaferLayout { get; set; }
        public object MovementLayout { get; set; }
        public object RecipeLayout { get; set; }
        public object LotsLayout { get; set; }
    }

    public class HideMinTimeConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime)
                return (DateTime)value == DateTime.MinValue ? "" : ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");

            if (DateTime.TryParse((string)value, out DateTime dateTime))
                return dateTime == DateTime.MinValue ? "" : dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class MinTime2BoolConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime)
                return (DateTime)value != DateTime.MinValue;

            if (DateTime.TryParse((string)value, out DateTime dateTime))
                return dateTime != DateTime.MinValue;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class RecipeStepNull2Empty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && (value is List<RecipeStep>) && ((List<RecipeStep>)value).Count > 0)
            {
                return value;
            }

            return new List<RecipeStep>() { new RecipeStep() { Name = "", StartTime = DateTime.MinValue, EndTime = DateTime.MinValue, ActualTime = "", SettingTime = "" } };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
