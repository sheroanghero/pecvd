using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITPump25.xaml 的交互逻辑
    /// </summary>
    public partial class AITPump25 : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
               "DeviceData25", typeof(AITPumpData), typeof(AITPump25),
               new FrameworkPropertyMetadata(new AITPumpData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITPumpData DeviceData25
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
            "EnableControl", typeof(bool), typeof(AITPump25),
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
            "IsShowSpeed", typeof(bool), typeof(AITPump25),
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
                "IsShowSensor", typeof(bool), typeof(AITPump25),
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
                "WaterFlowStatusColor", typeof(SolidColorBrush), typeof(AITPump25),
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
        "N2PressureStatusColor", typeof(SolidColorBrush), typeof(AITPump25),
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
        public AITPump25()
        {
            InitializeComponent();
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData25 == null)
                return;

            if (DeviceData25.IsError || DeviceData25.IsOverLoad)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/pump_error.png"));
            else if (DeviceData25.IsWarning)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/pump_warning.png"));
            else if (DeviceData25.IsOn)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/pump_on.png"));
            else
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Pump/pump_off.png"));

            StackPanelN2Pressure.Visibility = (DeviceData25.IsN2PressureEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;
            StackPanelWaterFlow.Visibility = (DeviceData25.IsWaterFlowEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;
            StackPanelDryPump.Visibility = (DeviceData25.IsDryPumpEnable && IsShowSensor) ? Visibility.Visible : Visibility.Hidden;

            WaterFlowStatusColor = DeviceData25.WaterFlowAlarm
                ? Brushes.Red
                : (DeviceData25.WaterFlowWarning ? Brushes.Yellow : Brushes.Lime);

            N2PressureStatusColor = DeviceData25.N2PressureAlarm
                ? Brushes.Red
                : (DeviceData25.N2PressureWarning ? Brushes.Yellow : Brushes.Lime);

            txtSpeed.Background = DeviceData25.AtSpeed ? Brushes.Transparent : Brushes.LimeGreen;
            txtTemp.Background = DeviceData25.OverTemp ? Brushes.OrangeRed : Brushes.Transparent;

            if (imagePicture.ContextMenu != null && imagePicture.ContextMenu.Items.Count == 3)
            {
                ((MenuItem)imagePicture.ContextMenu.Items[0]).IsEnabled = !DeviceData25.IsOn;
                ((MenuItem)imagePicture.ContextMenu.Items[1]).IsEnabled = DeviceData25.IsOn;
                ((MenuItem)imagePicture.ContextMenu.Items[2]).IsEnabled = DeviceData25.IsOn;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData25 != null)
            {
                string tooltipValue =
                    string.Format("{0}：{1}\r\n\r\nID：{2} ",
                        "",
                        DeviceData25.DisplayName,
                        DeviceData25.DeviceSchematicId);

                ToolTip = tooltipValue;
            }
        }

        private void PumpOn(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData25.DeviceModule}.{DeviceData25.DeviceName}.PumpStart");
            }
        }

        private void PumpOff(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData25.DeviceModule}.{DeviceData25.DeviceName}.PumpStop");
            }
        }

        private void PumpUtility(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData25.DeviceModule}.{DeviceData25.DeviceName}.PumpUtility");
            }
        }
    }
}
