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
    public enum PipeType
    { 
        Normal=1,
        DarkThick=2,
        GrayThick=3,
    }

    /// <summary>
    /// Interaction logic for PipeControl.xaml
    /// </summary>
    public partial class PipeControl : UserControl
    {
        public PipeControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// define dependency property
        /// </summary>
        public static readonly DependencyProperty PipeTypeProperty = DependencyProperty.Register(
          "PipeType", typeof(Orientation), typeof(PipeControl),
          new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasOnOffProperty = DependencyProperty.Register(
          "GasOnOff", typeof(bool), typeof(PipeControl),
          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasColorProperty = DependencyProperty.Register(
          "GasColor", typeof(Brush), typeof(PipeControl),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        

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
        /// set, get pipe control type
        /// </summary>
        public Orientation PipeType
        {
            get
            {
                return (Orientation)this.GetValue(PipeTypeProperty);
            }
            set
            {
                this.SetValue(PipeTypeProperty, value);
            }
        }

        public PipeType PipeStyleType
        { get; set; }

        private void LineSet(Line line1, double x1, double y1, double x2, double y2, PenLineCap startCap, PenLineCap endCap)
        {
            line1.X1  = x1;
            line1.X2  = x2;
            line1.Y1 = y1;
            line1.Y2  = y2;
            line1.StrokeStartLineCap  = startCap;
            line1.StrokeEndLineCap = endCap;
        }
        
        /// <summary>
        /// override this function, to make this control property change visualable during design mode
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //render line color
            if (GasOnOff)
            {
                Line1Center.Stroke = GasColor;
               // Line1Border.Stroke = GasColor;
            }
            else
            {
                Line1Center.Stroke = Brushes.White;
                //Line1Border.Stroke = GasColor;
            }
            switch (PipeStyleType)
            {
                case Control.PipeType.DarkThick:
                    Line1Center.StrokeThickness = 3;
                    Line1Center.Stroke = Brushes.Black;
                    break;
                case Control.PipeType.GrayThick:
                    Line1Center.StrokeThickness = 3;
                    Line1Center.Stroke = Brushes.Gray;
                    break;
                case Control.PipeType.Normal:
                default:
                    Line1Center.StrokeThickness = 1;
                  //  Line1Center.Stroke = Brushes.Black;
                    break;
            }
            switch (PipeType)
            {
                case Orientation.Vertical:
                    LineSet( Line1Center, Width / 2, 0, Width / 2, Height, PenLineCap.Round, PenLineCap.Round);
                    this.Width = 20;
                    break;
                case Orientation.Horizontal:
                    LineSet(  Line1Center, 0, Height / 2, Width, Height / 2, PenLineCap.Round, PenLineCap.Round);
                    this.Height = 20;
                    break;
            }
        }
    }
}
