using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.ClientControls.VceControls
{
    /// <summary>
    /// VceT1Control.xaml 的交互逻辑
    /// </summary>
    public partial class VceT1Control : UserControl
    {

        public bool IsFoupOn
        {
            get { return (bool)GetValue(IsFoupOnProperty); }
            set { SetValue(IsFoupOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFoupOnProperty =
            DependencyProperty.Register("IsFoupOn", typeof(bool), typeof(VceT1Control), new PropertyMetadata(false, IsFoupOnChanged));

        public static void IsFoupOnChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is VceT1Control))
                return;

            VceT1Control ctrl = (VceT1Control)obj;

            System.Windows.Visibility visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Hidden;
            if (ctrl.foup.Visibility != visibility)
                ctrl.foup.Visibility = visibility;
        }

        public VceT1Control()
        {
            InitializeComponent();
            foup.Visibility = Visibility.Hidden;
        }
    }
}
