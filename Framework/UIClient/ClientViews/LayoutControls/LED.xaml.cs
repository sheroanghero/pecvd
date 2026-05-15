using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MECF.Framework.UI.Client.CenterViews.Controls
{
    public partial class LED : UserControl
    {

        // Using a DependencyProperty as the backing store for On.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OnProperty =
            DependencyProperty.Register("On", typeof(bool), typeof(LED), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for IsRed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRedProperty =
            DependencyProperty.Register("IsRed", typeof(bool), typeof(LED), new PropertyMetadata(false));

        public LED()
        {
            InitializeComponent();
            root.DataContext = this;
        }

        public bool On
        {
            get => (bool) GetValue(OnProperty);
            set => SetValue(OnProperty, value);
        }

        public bool IsRed
        {
            get => (bool) GetValue(IsRedProperty);
            set => SetValue(IsRedProperty, value);
        }
    }
    
    public class LedConverter : IMultiValueConverter
    {
        static Brush GreenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF07FF07"));
        static Brush GrayBrush = new SolidColorBrush(Colors.LightGray);
        static Brush RedBrush = new SolidColorBrush(Colors.Red);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var value = (bool)values[0];
            var red = (bool)values[1];
            return value ? red ? RedBrush : GreenBrush : GrayBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}