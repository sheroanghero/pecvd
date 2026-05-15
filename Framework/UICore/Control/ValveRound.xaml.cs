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
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for ValveRound.xaml
    /// </summary>
    public partial class ValveRound : UserControl
    {
        public ValveRound()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                "Command", typeof(ICommand), typeof(ValveRound),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
                "CommandParameter", typeof(string), typeof(ValveRound),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveNameProperty = DependencyProperty.Register(
                "ValveName", typeof(string), typeof(ValveRound),
                new FrameworkPropertyMetadata("未知阀门", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
                "Orientation", typeof(ValveDirection), typeof(ValveRound),
                new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FastValveStateProperty = DependencyProperty.Register(
                "FastValveState", typeof(GasValveDataItem), typeof(ValveRound),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SlowValveStateProperty = DependencyProperty.Register(
                "SlowValveState", typeof(GasValveDataItem), typeof(ValveRound),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Command to execute when opening/closing valve.
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

        public string CommandParameter
        {
            get
            {
                return (string)this.GetValue(CommandParameterProperty);
            }
            set
            {
                this.SetValue(CommandParameterProperty, value);
            }
        }
        /// <summary>
        /// Fast valve state
        /// </summary>
        public GasValveDataItem FastValveState
        {
            get
            {
                return (GasValveDataItem)GetValue(FastValveStateProperty);
            }
            set
            {
                SetValue(FastValveStateProperty, value);
            }
        }

        /// <summary>
        /// Slow valve state
        /// </summary>
        public GasValveDataItem SlowValveState
        {
            get
            {
                return (GasValveDataItem)GetValue(SlowValveStateProperty);
            }
            set
            {
                SetValue(SlowValveStateProperty, value);
            }
        }

        /// <summary>
        /// 0关闭,1快充，2慢充
        /// </summary>
        private int ValveState;

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

        /// <summary>
        /// valve name
        /// </summary>
        public string ValveName
        {
            get
            {
                return (string)GetValue(ValveNameProperty);
            }
            set
            {
                SetValue(ValveNameProperty, value);
            }
        }

        private BitmapSource GetImage(string uri)
        {
            BitmapImage image = new BitmapImage(new Uri(string.Format("pack://application:,,,/MECF.Framework.Common;component/Resources/Valve/{0}", uri)));
            return image;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
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
            }

            if (FastValveState!=null && FastValveState.Feedback)
            {
                ValveState = 1;
            }
            else if (SlowValveState!=null && SlowValveState.Feedback)
            {
                ValveState = 2;
            }
            else
            {
                ValveState = 0;
            }

            switch (ValveState)
            {
                case 1:
                    imagePicture.Source = GetImage("FastPumpValve.png");
                    break;
                case 2:
                    imagePicture.Source = GetImage("SlowPumpValve.png");
                    break;
                default:
                    imagePicture.Source = GetImage("ValveClosed.png");
                    break;
            }

            base.OnRender(drawingContext);

            this.ToolTip = this.ValveName;
        }

        /// <summary>
        /// Turns on/off fast/slow valves. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetValve(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            if (FastValveState == null || SlowValveState == null)
                return;
            

            if (Command != null)
            {
                switch (int.Parse(mi.Tag.ToString()))
                {
                    case 0:
                        Command.Execute(new object[] { FastValveState.DeviceName, GasValveOperation.GVTurnValve, true });
                        break;
                    case 1:
                        Command.Execute(new object[] { SlowValveState.DeviceName, GasValveOperation.GVTurnValve, true });
                        break;
                    case 2:
                        Command.Execute(new object[] { FastValveState.DeviceName, GasValveOperation.GVTurnValve, false });
                        break;
                    case 3:
                        Command.Execute(new object[] { SlowValveState.DeviceName, GasValveOperation.GVTurnValve, false });
                        break;
                }
            }
        }
    }
}
