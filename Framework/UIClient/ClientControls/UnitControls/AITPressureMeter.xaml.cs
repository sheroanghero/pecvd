using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    public class AITPressureMeterFillColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AITPressureMeterData DeviceData = (AITPressureMeterData) value;
            if (DeviceData!=null)
            {
                if (DeviceData.IsError) return "OrangeRed";

                if (DeviceData.IsWarning) return "Yellow";
                
            }
            return "Gray";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// AITPressureMeter.xaml 的交互逻辑
    /// </summary>
    public partial class AITPressureMeter : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
        "DeviceData", typeof(AITPressureMeterData), typeof(AITPressureMeter),
        new FrameworkPropertyMetadata(new AITPressureMeterData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITPressureMeterData DeviceData
        {
            get
            {
                return (AITPressureMeterData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public AITPressureMeter()
        {
            InitializeComponent();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                ToolTip = $"{DeviceData.Type}：{DeviceData.DisplayName}\r\n{DeviceData.FeedBack.ToString(DeviceData.FormatString)} {DeviceData.Unit}";
            }
        }
    }
}
