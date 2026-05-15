using System.Windows;
using System.Windows.Controls;

namespace OpenSEMI.Ctrlib.Controls
{
    public class Gasline : Control
    {
        static Gasline()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Gasline), new FrameworkPropertyMetadata(typeof(Gasline)));
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Gasline), new PropertyMetadata(Orientation.Horizontal));

    }
}
