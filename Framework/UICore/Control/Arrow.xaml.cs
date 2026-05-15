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
    /// Interaction logic for Arrow.xaml111
    /// </summary>
    public partial class Arrow : UserControl
    {
        public Arrow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty GasOnOffProperty = DependencyProperty.Register(
          "GasOnOff", typeof(bool), typeof(Arrow),
          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasColorProperty = DependencyProperty.Register(
          "GasColor", typeof(Brush), typeof(Arrow),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DisplayAngleProperty = DependencyProperty.Register(
          "DisplayAngle", typeof(int), typeof(Arrow),
          new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));


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
        /// pipe gas on/off state
        /// </summary>
        public bool GasOnOff
        {
            get
            {
                return (bool)this.GetValue(GasOnOffProperty);
            }
            set
            {
                this.SetValue(GasOnOffProperty, value);
            }
        }

        /// <summary>
        /// pipe gas on color
        /// </summary>
        public Brush GasColor
        {
            get
            {
                return (Brush)this.GetValue(GasColorProperty);
            }
            set
            {
                this.SetValue(GasColorProperty, value);
            }
        }

        /// <summary>
        /// override rendering method
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.Opacity = GasOnOff ? 1 : 0.5;
            this.arrow1.Fill = GasColor;
            this.rotateTransform.Angle = DisplayAngle;
        }
    }
}
