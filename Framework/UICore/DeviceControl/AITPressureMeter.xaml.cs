using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.Common.DeviceData;

namespace Aitex.Core.UI.DeviceControl
{
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

        public static readonly DependencyProperty FontSizeSettingProperty = DependencyProperty.Register(
            "FontSizeSetting", typeof(int), typeof(AITPressureMeter),
            new FrameworkPropertyMetadata(13, FrameworkPropertyMetadataOptions.AffectsRender));

        public int FontSizeSetting
        {
            get
            {
                return (int)this.GetValue(FontSizeSettingProperty);
            }
            set
            {
                this.SetValue(FontSizeSettingProperty, value);
            }
        }

        public static readonly DependencyProperty ForegroundSettingProperty = DependencyProperty.Register(
            "ForegroundSetting", typeof(string), typeof(AITPressureMeter),
            new FrameworkPropertyMetadata("Yellow", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ForegroundSetting
        {
            get
            {
                return (string)this.GetValue(ForegroundSettingProperty);
            }
            set
            {
                this.SetValue(ForegroundSettingProperty, value);
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
                string tooltipValue =
                    string.Format("{0}：{1}\r\nID：{2}\r\n{3} {4}",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        !string.IsNullOrEmpty(DeviceData.FormatString) ? DeviceData.FeedBack.ToString(DeviceData.FormatString) : DeviceData.FeedBack.ToString("F1"),
                        DeviceData.Unit);

                ToolTip = tooltipValue;
            }
        }
    }
}
