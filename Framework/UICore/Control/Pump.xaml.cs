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

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for Pump.xaml
    /// </summary>
    public partial class Pump : UserControl
    {
        public Pump()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(Pump),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(ValveDirection), typeof(Pump),
            new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty PumpAlarmProperty = DependencyProperty.Register(
            "PumpAlarm", typeof(object), typeof(Pump),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PumpRunningProperty = DependencyProperty.Register(
            "PumpRunning", typeof(object), typeof(Pump),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Command to execute when turning on/off pump.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }

        public object PumpAlarm
        {
            get
            {
                return GetValue(PumpAlarmProperty);
            }
            set
            {
                SetValue(PumpAlarmProperty, value);
            }
        }

        public object PumpRunning
        {
            get
            {
                return GetValue(PumpRunningProperty);
            }
            set
            {
                SetValue(PumpRunningProperty, value);
            }
        }

        /// <summary>
        /// direction of the valve
        /// </summary>
        public ValveDirection Orientation
        {
            get
            {
                return (ValveDirection)GetValue(OrientationProperty);
            }
            set
            {
                SetValue(OrientationProperty, value);
            }
        }

        private BitmapSource GetImage(string uri)
        {
            BitmapImage image = new BitmapImage(new Uri(string.Format("pack://application:,,,/MECF.Framework.Common;component/Resources/Valve/{0}", uri)));
            return image;
            //return BitmapFrame.Create(new Uri(string.Format(@"{0}", uri), UriKind.Relative));
        }

        int GetPumpState()
        {
            if ((PumpAlarm + "").ToLower() == "true")
            {
                //报警红
                return 3;
            }
            else
            {
                //非报警状态
                if ((PumpRunning + "").ToLower() == "true")
                {
                    //runing 绿
                    return 0;
                }
                else if ((PumpRunning + "").ToLower() == "false")
                {
                    //关闭黑
                    return 1;
                }
                else
                {
                    //返回null，灰色
                    return 2;

                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int pumpState = GetPumpState();
            switch (pumpState)
            {
                case 0:
                    imgPump.Source = GetImage("PumpGreen.png");
                    break;
                case 1: imgPump.Source = GetImage( "PumpBlack.png");
                    break;
                case 2: imgPump.Source = GetImage( "PumpGray.png");
                    break;
                case 3: imgPump.Source = GetImage("PumpRed.png");
                    break;
                default: imgPump.Source = GetImage( "PumpRed.png");
                    break;

            }

            switch (Orientation)
            {
                case ValveDirection.ToBottom:
                case ValveDirection.ToTop:
                    rotateTransform.Angle = 0;
                    break;

                case ValveDirection.ToLeft:
                case ValveDirection.ToRight:
                    rotateTransform.Angle = 90;
                    break;

                default:
                    break;
            }

            base.OnRender(drawingContext);
        }

        /// <summary>
        /// open 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenPump(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            Command.Execute(new object[] { this.Tag, "Open"});
        }

        private void ClosePump(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;


            Command.Execute(new object[] { this.Tag, "Close" });
        }
    }
}
