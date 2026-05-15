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
using System.Windows.Media.Effects;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for SignalTower.xaml
    /// </summary>
    public partial class SignalTower : UserControl
    {
        public SignalTower()
        {
            InitializeComponent();
            redLightEffect.BlurRadius = 20;
            redLightEffect.Opacity = 1;
            redLightEffect.Color = Colors.Red;

            yellowLightEffect.BlurRadius = 20;
            yellowLightEffect.Opacity = 1;
            yellowLightEffect.Color = Colors.Yellow;

            greenLightEffect.BlurRadius = 20;
            greenLightEffect.Opacity = 1;
            greenLightEffect.Color = Colors.Green;

            blueLightEffect.BlurRadius = 20;
            blueLightEffect.Opacity = 1;
            blueLightEffect.Color = Colors.Blue;
        }

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                                     "DeviceData", typeof(SignalTowerDataItem), typeof(SignalTower),
                                     new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BuzzingWarningConfigCommandProperty = DependencyProperty.Register(
            "BuzzingWarningConfigCallback", typeof(Action), typeof(SignalTower),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));

        public Action BuzzingWarningConfigCallback
        {
            get
            {
                return (Action)GetValue(BuzzingWarningConfigCommandProperty);
            }
            set
            {
                SetValue(BuzzingWarningConfigCommandProperty, value);
            }
        }

        public SignalTowerDataItem DeviceData
        {
            get
            {
                return (SignalTowerDataItem)GetValue(DeviceDataProperty);
            }
            set
            {
                SetValue(DeviceDataProperty, value);
            }
        }

        DropShadowEffect redLightEffect = new DropShadowEffect();
        DropShadowEffect yellowLightEffect = new DropShadowEffect();
        DropShadowEffect greenLightEffect = new DropShadowEffect();
        DropShadowEffect blueLightEffect = new DropShadowEffect();
        public double OutLights = 0.2;
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
                return;

            if (DeviceData.IsRedLightOn)
            {
                if (rectangle1.Opacity != 1)
                {
                    rectangle1.Opacity = 1;
                    rectangle1.Effect = redLightEffect;
                }
            }
            else
            {
                if (rectangle1.Opacity != OutLights)
                {
                    rectangle1.Opacity = OutLights;
                    rectangle1.Effect = null;
                }
            }

            if (DeviceData.IsYellowLightOn)
            {
                if (rectangle2.Opacity != 1)
                {
                    rectangle2.Opacity = 1;
                    rectangle2.Effect = yellowLightEffect;
                }
            }
            else
            {
                if (rectangle2.Opacity != OutLights)
                {
                    rectangle2.Opacity = OutLights;
                    rectangle2.Effect = null;
                }
            }

            if (DeviceData.IsGreenLightOn)
            {
                if (rectangle3.Opacity != 1)
                {
                    rectangle3.Opacity = 1;
                    rectangle3.Effect = greenLightEffect;
                }
            }
            else
            {
                if (rectangle3.Opacity != OutLights)
                {
                    rectangle3.Opacity = OutLights;
                    rectangle3.Effect = null;
                }
            }


        }

        private void rectangle2_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (BuzzingWarningConfigCallback != null)
            {
                BuzzingWarningConfigCallback.Invoke();
            }
        }
    }
}
