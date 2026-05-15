using System.Windows;
using System.Windows.Controls;
using OpenSEMI.Ctrlib.Types;

namespace OpenSEMI.Ctrlib.Controls
{
    public class GaslineJoint : Control
    {
        static GaslineJoint()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GaslineJoint), new FrameworkPropertyMetadata(typeof(GaslineJoint)));
        }

        public JointType JointType
        {
            get { return (JointType)GetValue(JointTypeProperty); }
            set { SetValue(JointTypeProperty, value); }
        }
        public static readonly DependencyProperty JointTypeProperty = DependencyProperty.Register("JointType", typeof(JointType), typeof(GaslineJoint), new PropertyMetadata(JointType.CROSS));

    }
}
