using Aitex.Core.Common.DeviceData;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// ChillerControl.xaml 的交互逻辑
    /// </summary>
    public partial class ChillerControl : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(AITChillerData), typeof(ChillerControl),
            new FrameworkPropertyMetadata(new AITChillerData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITChillerData DeviceData
        {
            get
            {
                return (AITChillerData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }


        public static readonly DependencyProperty EnableControlProperty = DependencyProperty.Register(
            "EnableControl", typeof(bool), typeof(ChillerControl),
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

        public ChillerControl()
        {
            InitializeComponent();
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
                return;

            if (DeviceData.IsError)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.UI.Client;component/Resources/Images/units/chiller_right_error.png"));
            else if (DeviceData.IsRunning)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.UI.Client;component/Resources/Images/units/chiller_right_on.png"));
            else
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.UI.Client;component/Resources/Images/units/chiller_right_off.png"));


            if (imagePicture.ContextMenu != null && imagePicture.ContextMenu.Items.Count == 2)
            {
                ((MenuItem)imagePicture.ContextMenu.Items[0]).IsEnabled = !DeviceData.IsRunning;
                ((MenuItem)imagePicture.ContextMenu.Items[1]).IsEnabled = DeviceData.IsRunning;
            }
        }


        private void ChillerOn(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITChillerOperation.ChillerOn}");
            }
        }

        private void ChillerOff(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITChillerOperation.ChillerOff}");
            }
        }
    }
}
