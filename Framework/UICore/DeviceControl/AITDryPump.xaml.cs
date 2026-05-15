using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITDryPump.xaml 的交互逻辑
    /// </summary>
    public partial class AITDryPump : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                "DeviceData", typeof(AITPumpData), typeof(AITDryPump),
                new FrameworkPropertyMetadata(new AITPumpData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITPumpData DeviceData
        {
            get
            {
                return (AITPumpData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }
        

        public static readonly DependencyProperty EnableControlProperty = DependencyProperty.Register(
            "EnableControl", typeof(bool), typeof(AITDryPump),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool EnableControl
        {
            get
            {
                return (bool)this.GetValue(EnableControlProperty);
            }
            set
            {
                this.SetValue(EnableControlProperty, value);
            }
        }

        public static readonly DependencyProperty IsShowSpeedProperty = DependencyProperty.Register(
            "IsShowSpeed", typeof(bool), typeof(AITDryPump),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsShowSpeed
        {
            get
            {
                return (bool)this.GetValue(IsShowSpeedProperty);
            }
            set
            {
                this.SetValue(IsShowSpeedProperty, value);
            }
        }

        public static readonly DependencyProperty IsShowSensorProperty = DependencyProperty.Register(
                "IsShowSensor", typeof(bool), typeof(AITDryPump),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsShowSensor
        {
            get
            {
                return (bool)this.GetValue(IsShowSensorProperty);
            }
            set
            {
                this.SetValue(IsShowSensorProperty, value);
            }
        }
 


        public static readonly DependencyProperty WaterFlowStatusColorProperty = DependencyProperty.Register(
                "WaterFlowStatusColor", typeof(SolidColorBrush), typeof(AITDryPump),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public SolidColorBrush WaterFlowStatusColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(WaterFlowStatusColorProperty);
            }
            set
            {
                this.SetValue(WaterFlowStatusColorProperty, value);
            }
        }

                public static readonly DependencyProperty N2PressureStatusColorProperty = DependencyProperty.Register(
                "N2PressureStatusColor", typeof(SolidColorBrush), typeof(AITDryPump),
                new FrameworkPropertyMetadata(Brushes.DarkGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public SolidColorBrush N2PressureStatusColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(N2PressureStatusColorProperty);
            }
            set
            {
                this.SetValue(N2PressureStatusColorProperty, value);
            }
        }


        public AITDryPump()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
                return;

            if (DeviceData.IsError || DeviceData.IsOverLoad)
            {
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/DryPump/pump_error.png"));
                txtState.Text = "Error";
            }
            else if (DeviceData.IsWarning)
            { 
                 imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/DryPump/pump_warning.png"));
                txtState.Text = "Warning";
            }
            else if (DeviceData.IsOn)
            {
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/DryPump/pump_on.png"));
                txtState.Text = "ON";
            }
            else
            {
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/DryPump/pump_off.png"));
                txtState.Text = "OFF";
            }

            StackPanelN2Pressure.Visibility = (DeviceData.IsN2PressureEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;
            StackPanelWaterFlow.Visibility = (DeviceData.IsWaterFlowEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;
            StackPanelDryPump.Visibility = (DeviceData.IsDryPumpEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;

            WaterFlowStatusColor = DeviceData.WaterFlowAlarm
                ? Brushes.Red
                : (DeviceData.WaterFlowWarning ? Brushes.Yellow : Brushes.Lime);

            N2PressureStatusColor = DeviceData.N2PressureAlarm
                ? Brushes.Red
                : (DeviceData.N2PressureWarning ? Brushes.Yellow : Brushes.Lime);

            txtSpeed.Background = DeviceData.AtSpeed ? Brushes.Transparent : Brushes.LimeGreen;
            txtTemp.Background = DeviceData.OverTemp ? Brushes.OrangeRed : Brushes.Transparent;

            if (imagePicture.ContextMenu!=null && imagePicture.ContextMenu.Items.Count == 2)
            {
                ((MenuItem)imagePicture.ContextMenu.Items[0]).IsEnabled = !DeviceData.IsOn;
                ((MenuItem)imagePicture.ContextMenu.Items[1]).IsEnabled = DeviceData.IsOn;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format("{0}：{1}\r\n\r\nID：{2} ",
                        "",
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId );

                ToolTip = tooltipValue;
            }
        }

        private void PumpOn(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.DeviceModule}.{DeviceData.DeviceName}.{AITPumpOperation.PumpOn}");
            }
        }

        private void PumpOff(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.DeviceModule}.{DeviceData.DeviceName}.{AITPumpOperation.PumpOff}");
            }
        }
    }
}
