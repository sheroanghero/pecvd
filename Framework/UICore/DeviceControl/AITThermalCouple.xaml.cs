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
using Aitex.Core.UI.Control;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITThermalCouple.xaml 的交互逻辑
    /// </summary>
    public partial class AITThermalCouple : UserControl
    {
        public AITThermalCouple()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITThermalCoupleData), typeof(AITThermalCouple),
                        new FrameworkPropertyMetadata(new AITThermalCoupleData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(AITThermalCouple),
                         new FrameworkPropertyMetadata(Brushes.DarkMagenta, FrameworkPropertyMetadataOptions.AffectsRender));

                public static readonly DependencyProperty IsControlTcProperty = DependencyProperty.Register(
                        "IsControlTc", typeof(bool), typeof(AITThermalCouple),
                         new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsControlTc
        {
            get
            {
                return (bool)this.GetValue(IsControlTcProperty);
            }
            set
            {
                this.SetValue(IsControlTcProperty, value);
            }
        }


        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITThermalCoupleData DeviceData
        {
            get
            {
                return (AITThermalCoupleData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public Brush BackColor
        {
            get
            {
                return (Brush)this.GetValue(BackColorProperty);
            }
            set
            {
                this.SetValue(BackColorProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Brush background = Brushes.DarkMagenta;
            Brush foreground = Brushes.LightYellow;

            if (!IsControlTc)
            {
                background = Brushes.DarkGray;
                foreground = Brushes.Black;
            }

            if (DeviceData != null)
            {
                //rectBkground.Stroke = DeviceData.IsOffline ? Brushes.Pink : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0X37, 0X37, 0X37));
                //rectBkground.StrokeThickness = DeviceData.IsOffline ? 2 : 1;

 
                //else
                //    labelValue.Foreground = DeviceData.IsOffline ? Brushes.Red : Brushes.LightYellow;

                if (DeviceData.IsBroken || DeviceData.IsAlarm)
                {
                    background = Brushes.Red;
                }else if (DeviceData.IsOutOfTolerance)
                {
                    background = Brushes.Gold;
                }

            }

            rectBkground.Fill = background;
            labelValue.Foreground = foreground;
        }

        public Window AnalogOwner { get; set; }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format(Application.Current.Resources["GlobalLableThermoMeterToolTip"].ToString(),
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.FeedBack.ToString("F1"),
                        DeviceData.Unit,
                        DeviceData.IsAlarm ? "Alarm" : (DeviceData.IsWarning ? "Warning" : (DeviceData.IsOutOfTolerance ? "Abnormal" : "Normal")),
                        DeviceData.IsBroken ? "Broken" : "Normal");

                ToolTip = tooltipValue;
            }






            






        }
    }
}

