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
    /// Interaction logic for CheckValve_V2.xaml
    /// </summary>
    public partial class CheckValve : UserControl
    {
        public CheckValve()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DisplayAngleProperty = DependencyProperty.Register(
        "Angle", typeof(int), typeof(CheckValve),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// display angle
        /// </summary>
        public int Angle
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
        /// override rendering method
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.rotateTransform.Angle = Angle;
        }

    }
}
