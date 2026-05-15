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
    /// AITFlowMeter.xaml 的交互逻辑
    /// </summary>
    public partial class AITFlowMeter : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
        "DeviceData", typeof(AITWaterFlowMeterData), typeof(AITFlowMeter),
        new FrameworkPropertyMetadata(new AITWaterFlowMeterData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITWaterFlowMeterData DeviceData
        {
            get
            {
                return (AITWaterFlowMeterData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
        "StringFormat", typeof(string), typeof(AITFlowMeter),
        new FrameworkPropertyMetadata("F1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string StringFormat
        {
            get
            {
                return (string)this.GetValue(StringFormatProperty);
            }
            set
            {
                this.SetValue(StringFormatProperty, value);
            }
        }


        public AITFlowMeter()
        {
            InitializeComponent();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format(Application.Current.Resources["GlobalLableFlowMeterToolTip"].ToString(),
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.FeedBack.ToString(StringFormat),
                        DeviceData.Unit);

                ToolTip = tooltipValue;
            }
        }
    }
}
