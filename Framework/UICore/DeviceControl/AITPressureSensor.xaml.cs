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
    /// AITPressureSensor.xaml 的交互逻辑
    /// </summary>
    public partial class AITPressureSensor : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
        "DeviceData", typeof(AITPressureSensorData), typeof(AITPressureSensor),
        new FrameworkPropertyMetadata(new AITPressureSensorData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITPressureSensorData DeviceData
        {
            get
            {
                return (AITPressureSensorData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }


        public AITPressureSensor()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
            {
                sensor.Fill = new SolidColorBrush(Colors.DarkGray);
                return;
            }

            if (DeviceData.IsError)
                sensor.Fill = new SolidColorBrush(Colors.OrangeRed);
            else if (DeviceData.IsWarning || DeviceData.IsOutOfRange)
                sensor.Fill = new SolidColorBrush(Colors.Yellow);
            else
            {
                sensor.Fill = new SolidColorBrush(Colors.Lime);
            }
        }


        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format(Application.Current.Resources["GlobalLablePressureSensorToolTip"].ToString(),
                        "PS",
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.FeedBack.ToString("F1"),
                        DeviceData.Unit,
                        DeviceData.IsError ? "Alarm" : (DeviceData.IsWarning ? "Warning" : (DeviceData.IsOutOfRange ? "Abnormal" : "Normal"))
                        );

                ToolTip = tooltipValue;
            }
        }
    }
}


