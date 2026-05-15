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
    /// AITWaterFlowSensor.xaml 的交互逻辑
    /// </summary>
    public partial class AITWaterFlowSensor : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
        "DeviceData", typeof(AITWaterFlowSensorData), typeof(AITWaterFlowSensor),
        new FrameworkPropertyMetadata(new AITWaterFlowSensorData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITWaterFlowSensorData DeviceData
        {
            get
            {
                return (AITWaterFlowSensorData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }


        public AITWaterFlowSensor()
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
            else if (DeviceData.IsWarning || DeviceData.IsOutOfTolerance)
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
                    string.Format(Application.Current.Resources["GlobalLableWaterFlowSensorToolTip"].ToString(),
                        "WaterFlow",
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.FeedBack.ToString("F1"),
                        DeviceData.Unit,
                        DeviceData.IsError ? "Alarm" : (DeviceData.IsWarning ? "Warning" : (DeviceData.IsOutOfTolerance ? "Abnormal" : "Normal"))
                        );

                ToolTip = tooltipValue;
            }
        }


        
    }
}

