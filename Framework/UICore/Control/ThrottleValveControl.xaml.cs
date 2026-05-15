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
    /// Interaction logic for ThrottleValveControl.xaml
    /// </summary>
    public partial class ThrottleValveControl : UserControl
    {
        public ThrottleValveControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// define dependency property
        /// </summary>
        public static readonly DependencyProperty ActualValueProperty = DependencyProperty.Register(
          "ActualValue", typeof(double), typeof(ThrottleValveControl),
          new FrameworkPropertyMetadata(50.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DisplayAngleProperty = DependencyProperty.Register(
          "DisplayAngle", typeof(int), typeof(ThrottleValveControl),
          new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));



        /// <summary>
        /// set, get current progress value
        /// </summary>
        public double ActualValue
        {
            get
            {
                return (double)this.GetValue(ActualValueProperty);
            }
            set
            {
                this.SetValue(ActualValueProperty, value);
            }
        }

        /// <summary>
        /// display angle
        /// </summary>
        public int DisplayAngle
        {
            get
            {
                return (int)GetValue(DisplayAngleProperty);
            }
            set
            {
                SetValue(DisplayAngleProperty, value);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.rotateTransform.Angle = DisplayAngle;
        }
    }
}
