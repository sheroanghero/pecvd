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
using System.ComponentModel;
using Aitex.Core.Util;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for Bath.xaml
    /// </summary> 
    public partial class Bath : UserControl
    {
        public Bath()
        {
            InitializeComponent();
        }

        //BathDataItem MoLineData { get { return (this.DataContext as MOSourceDataItem).BathData; } }

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                "DeviceData", typeof(BathDataItem), typeof(Bath),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public BathDataItem DeviceData
        {
            get
            {
                return (BathDataItem)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DeviceData == null) return;

            lblBathTemperature.Content = DeviceData.TemperatureReading + "℃";

            if (DeviceData.IsCommErr || DeviceData.IsLevelWarning || DeviceData.IsOutofTempRange || DeviceData.IsCutoffAlarm)
            {
                if (cavBath.Background != Brushes.Red)
                    cavBath.Background = Brushes.Red;
            }
            else
            {
                if (cavBath.Background != Brushes.SkyBlue)
                    cavBath.Background = Brushes.SkyBlue;
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData == null) return;

            string tooltipStr = string.Format("{0}\n\n通信状态：{1}\n水位状态：{2}\n温度监测：{3}\n水槽温度：{4} ℃\n水槽切断:{5}",
                     DeviceData.BathName,
                     DeviceData.IsCommErr ? "异常" : "正常",
                     DeviceData.IsLevelWarning ? "异常" : "正常",
                     DeviceData.IsOutofTempRange ? "异常" : "正常",
                     DeviceData.TemperatureReading.ToString("F1"),
                     DeviceData.IsCutoffAlarm ? "异常" : "正常");

            this.ToolTip = tooltipStr;
        }
    }
}
