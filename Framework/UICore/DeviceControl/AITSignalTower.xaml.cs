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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.DeviceControl
{
    public partial class AITSignalTower : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                                     "DeviceData", typeof(AITSignalTowerData), typeof(AITSignalTower),
                                     new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BuzzingWarningConfigCommandProperty = DependencyProperty.Register(
            "BuzzingWarningConfigCallback", typeof(Action), typeof(AITSignalTower),
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

        public AITSignalTowerData DeviceData
        {
            get
            {
                return (AITSignalTowerData)GetValue(DeviceDataProperty);
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
        DropShadowEffect buzzerLightEffect = new DropShadowEffect();
        public double OutLights = 0.2;

        public AITSignalTower()
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

            buzzerLightEffect.BlurRadius = 20;
            buzzerLightEffect.Opacity = 1;
            buzzerLightEffect.Color = Colors.White;
        }




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

            if (DeviceData.IsBlueLightOn)
            {
                if (rectangle4.Opacity != 1)
                {
                    rectangle4.Opacity = 1;
                    rectangle4.Effect = blueLightEffect;
                }
            }
            else
            {
                if (rectangle4.Opacity != OutLights)
                {
                    rectangle4.Opacity = OutLights;
                    rectangle4.Effect = null;
                }
            }

            if (DeviceData.IsBuzzerOn || DeviceData.IsBuzzer1On|| DeviceData.IsBuzzer2On|| DeviceData.IsBuzzer3On|| DeviceData.IsBuzzer4On)
            {
                if (rectangle5.Opacity != 1)
                {
                    rectangle5.Opacity = 1;
                    rectangle5.Effect = buzzerLightEffect;
                }
            }
            else
            {
                if (rectangle5.Opacity != OutLights)
                {
                    rectangle5.Opacity = OutLights;
                    rectangle5.Effect = null;
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
