using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.Common.DeviceData;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITSensor.xaml 的交互逻辑
    /// </summary>
    public partial class AITSensor : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(AITSensorData), typeof(AITSensor), new FrameworkPropertyMetadata(new AITSensorData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public AITSensorData DeviceData
        {
            get { return (AITSensorData)this.GetValue(DeviceDataProperty); }
            set { this.SetValue(DeviceDataProperty, value); }
        }

        public bool GreenColor
        {
            get { return (bool)GetValue(GreenColorProperty); }
            set { SetValue(GreenColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GreenColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GreenColorProperty =
            DependencyProperty.Register("GreenColor", typeof(bool), typeof(AITSensor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool EnableToolTip
        {
            get { return (bool)GetValue(EnableToolTipProperty); }
            set { SetValue(EnableToolTipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableToolTip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableToolTipProperty =
            DependencyProperty.Register("EnableToolTip", typeof(bool), typeof(AITSensor), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool LightOnValue
        {
            get { return (bool)GetValue(LightOnValueProperty); }
            set { SetValue(LightOnValueProperty, value); }
        }

        public static readonly DependencyProperty LightOnValueProperty =
            DependencyProperty.Register("LightOnValue", typeof(bool), typeof(AITSensor), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsInterlockMode
        {
            get { return (bool)GetValue(IsInterlockModeProperty); }
            set { SetValue(IsInterlockModeProperty, value); }
        }

        public static readonly DependencyProperty IsInterlockModeProperty =
            DependencyProperty.Register("IsInterlockMode", typeof(bool), typeof(AITSensor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        
        public bool IsCustomRender
        {
            get { return (bool)GetValue(IsCustomRenderProperty); }
            set { SetValue(IsCustomRenderProperty, value); }
        }
        public static readonly DependencyProperty IsCustomRenderProperty =
            DependencyProperty.Register("IsCustomRender", typeof(bool), typeof(AITSensor), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush CustomColorOn
        {
            get { return (Brush)GetValue(CustomColorOnProperty); }
            set { SetValue(CustomColorOnProperty, value); }
        }
        public static readonly DependencyProperty CustomColorOnProperty =
            DependencyProperty.Register("CustomColorOn", typeof(Brush), typeof(AITSensor), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Lime), FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush CustomColorOff
        {
            get { return (Brush)GetValue(CustomColorOffProperty); }
            set { SetValue(CustomColorOffProperty, value); }
        }
        public static readonly DependencyProperty CustomColorOffProperty =
            DependencyProperty.Register("CustomColorOff", typeof(Brush), typeof(AITSensor), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Gray), FrameworkPropertyMetadataOptions.AffectsRender));


        public AITSensor()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (IsCustomRender)
            {
                if (DeviceData == null || string.IsNullOrEmpty(DeviceData.DeviceName))
                {
                    sensor.Fill = LightOnValue? CustomColorOn  : CustomColorOff;
                }
                else
                {
                    sensor.Fill = DeviceData.Value ? CustomColorOn : CustomColorOff;
                }

                return;
            }

            if (DeviceData == null || string.IsNullOrEmpty(DeviceData.DeviceName))
            {
                if (LightOnValue)
                    sensor.Fill = (GreenColor ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF07FF07")) : new SolidColorBrush(Colors.Red));
                else
                    sensor.Fill = new SolidColorBrush(Colors.Gray);
                return;
            }
 
            if (IsInterlockMode)
            {
                sensor.Fill = DeviceData.Value ? new SolidColorBrush(Colors.Lime) : new SolidColorBrush(Colors.Red);
            }
            else
            {
                if (DeviceData.IsError)
                {
                    sensor.Fill = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    sensor.Fill = DeviceData.Value ? new SolidColorBrush(Colors.Lime) : new SolidColorBrush(Colors.Gray);
                }
            }



        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null && EnableToolTip)
            {
                ToolTip = $"Sensor \r\n {DeviceData.DisplayName} \r\n {DeviceData.Value}";
            }
        }
    }
}
