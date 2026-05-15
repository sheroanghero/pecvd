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
    /// Interaction logic for Filter.xaml
    /// </summary>
    public partial class Filter : UserControl
    {
        public Filter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// define dependency property
        /// </summary>

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
          "Orientation", typeof(Orientation), typeof(Filter),
           new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// filter oritation
        /// </summary>
        public Orientation Orientation
        {
            get 
            { 
                return (Orientation)this.GetValue(OrientationProperty); 
            }
            set
            {
                this.SetValue(OrientationProperty, value);
            }
        }

        /// <summary>
        /// control rendering
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (Orientation == Orientation.Horizontal)
            {
                this.rotateTransform.Angle = 0;
            }
            else
            {
                this.rotateTransform.Angle = 90;
            }
        }
    }
}
