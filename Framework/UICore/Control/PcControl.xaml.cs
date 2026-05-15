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
    /// PcControl.xaml 的交互逻辑
    /// </summary>
    public partial class PcControl : UserControl
    {
        public PcControl()
        {
            InitializeComponent();
        }
        /// <summary>
        /// define dependency property
        /// </summary>
        public static readonly DependencyProperty ActualValueProperty = DependencyProperty.Register(
          "DeviceData", typeof(object), typeof(PcControl),
          new FrameworkPropertyMetadata(50.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SetPointProperty = DependencyProperty.Register(
          "SetPoint", typeof(double), typeof(PcControl),
          new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DisplayAngleProperty = DependencyProperty.Register(
        "DisplayAngle", typeof(int), typeof(PcControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// set, get current progress value
        /// </summary>
        public double SetPoint
        {
            get
            {
                return (double)this.GetValue(SetPointProperty);
            }
            set
            {
                this.SetValue(SetPointProperty, value);
            }
        }


        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public object ActualValue
        {
            get
            {
                return this.GetValue(ActualValueProperty);
            }
            set
            {
                this
                    .SetValue(ActualValueProperty, value);
            }
        }    /// <summary>
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
        /// override rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.rotateTransform.Angle = DisplayAngle;
            try
            {
                labelValue.Content=ActualValue;
            }
            catch (Exception ex)
            {
                //LOG.Write(ex);
                throw ex;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
