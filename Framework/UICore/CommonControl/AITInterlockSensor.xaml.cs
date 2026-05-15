using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.Common.DeviceData;

namespace MECF.Framework.UI.Core.CommonControl
{
    /// <summary>
    /// AITInterlockSensor.xaml 的交互逻辑
    /// </summary>
    public partial class AITInterlockSensor : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(AITSensorData), typeof(AITInterlockSensor),
            new FrameworkPropertyMetadata(new AITSensorData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITSensorData DeviceData
        {
            get
            {
                return (AITSensorData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public bool LightOnValue
        {
            get { return (bool)GetValue(LightOnValueProperty); }
            set { SetValue(LightOnValueProperty, value); }
        }

        public static readonly DependencyProperty LightOnValueProperty =
            DependencyProperty.Register("LightOnValue", typeof(bool), typeof(AITInterlockSensor), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));


        public AITInterlockSensor()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
            {
                sensor.Fill = new SolidColorBrush(Colors.Gray);
                return;
            }

            if (DeviceData.Value == LightOnValue)
                sensor.Fill = new SolidColorBrush(Colors.Red);
            else
            {
                sensor.Fill = new SolidColorBrush(Colors.Gray);
            }

            //if (DeviceData.IsError)
            //    sensor.Fill = new SolidColorBrush(Colors.Red);
            //else
            //{
            //    sensor.Fill = new SolidColorBrush(Colors.Lime);
            //}
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format(Application.Current.Resources["GlobalLableSensorToolTip"].ToString(),
                        "Sensor",
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.Value);

                ToolTip = tooltipValue;
            }
        }

    }
}


