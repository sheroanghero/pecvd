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
    /// Interaction logic for Knot.xaml
    /// </summary>
    public partial class Knot : UserControl
    {
        public Knot()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty KnotNameProperty = DependencyProperty.Register(
         "KnotName", typeof(string), typeof(Knot),
          new FrameworkPropertyMetadata("未知阀门", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty KnotDirectionProperty = DependencyProperty.Register(
          "KnotDirection", typeof(ValveDirection), typeof(Knot),
           new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty KnotTypeProperty = DependencyProperty.Register(
         "KnotT", typeof(KnotType), typeof(Knot),
          new FrameworkPropertyMetadata(KnotType.Knot, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Description
        /// </summary>
        public static readonly DependencyProperty ThroughInnerColorProperty =
            DependencyProperty.Register("ThroughInnerColor",
                                        typeof(string),
                                        typeof(Knot),
                                        new FrameworkPropertyMetadata("White"));

        /// <summary>
        /// A property wrapper for the <see cref="BalloonTextProperty"/>
        /// dependency property:<br/>
        /// Description
        /// </summary>
        public string ThroughInnerColor
        {
            get { return (string)GetValue(ThroughInnerColorProperty); }
            set { SetValue(ThroughInnerColorProperty, value); }
        }

        /// <summary>
        /// Description
        /// </summary>
        public static readonly DependencyProperty KnotElliColorProperty =
            DependencyProperty.Register("KnotElliColor",
                                        typeof(string),
                                        typeof(Knot),
                                        new FrameworkPropertyMetadata("Black"));

        /// <summary>
        /// A property wrapper for the <see cref="BalloonTextProperty"/>
        /// dependency property:<br/>
        /// Description
        /// </summary>
        public string KnotElliColor
        {
            get { return (string)GetValue(KnotElliColorProperty); }
            set { SetValue(KnotElliColorProperty, value); }
        }

        /// <summary>
        /// node or through tag
        /// </summary>
        public KnotType KnotT
        {
            get
            {
                return (KnotType)GetValue(KnotTypeProperty);
            }
            set
            {
                SetValue(KnotTypeProperty, value);
            }
        }

        /// <summary>
        /// direction of the valve
        /// </summary>
        public ValveDirection KnotDirection
        {
            get
            {
                return (ValveDirection)GetValue(KnotDirectionProperty);
            }
            set
            {
                SetValue(KnotDirectionProperty, value);
            }
        }

        /// <summary>
        /// valve name
        /// </summary>
        public string KnotName
        {
            get
            {
                return (string)GetValue(KnotNameProperty);
            }
            set
            {
                SetValue(KnotNameProperty, value);
            }
        }

        public PipeType PipeStyleType
        { get; set; }

        /// <summary>
        /// over rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            switch (KnotDirection)
            {
                case ValveDirection.ToBottom:

                case ValveDirection.ToTop:
                    rotateTransform.Angle = 90;
                    break;
                case ValveDirection.ToLeft:

                    break;
                case ValveDirection.ToRight:
                    rotateTransform.Angle = 180;
                    break;
                default:
                    break;
            }
            if (KnotT == KnotType.Knot)
            {
                knotElli.Visibility = Visibility.Visible;
                throughPath.Visibility = Visibility.Hidden;
            }
            else
            {
                knotElli.Visibility = Visibility.Hidden;
                throughPath.Visibility = Visibility.Visible;
            }
            switch (PipeStyleType)
            {
                case Control.PipeType.DarkThick:
                    knotElli.Stroke = Brushes.Black;
                    pathThrough.Stroke = Brushes.Black;
                    break;
                case Control.PipeType.GrayThick:
                    knotElli.Stroke = Brushes.Gray;
                    pathThrough.Stroke = Brushes.Gray;
                    break;
                case Control.PipeType.Normal:
                default:
                    //knotElli.Stroke = Brushes.Black;
                    //pathThrough.Stroke = Brushes.Black;
                    break;
            }
        }
    }

    public enum KnotType
    {
        Knot = 1,
        Through = 2
    }
}
