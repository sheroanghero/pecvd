using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.ClientControls.VceControls
{
    /// <summary>
    /// VceT1Control.xaml 的交互逻辑
    /// </summary>
    public partial class VceT2Control : UserControl
    {
        public bool IsFoupOn
        {
            get { return (bool)GetValue(IsFoupOnProperty); }
            set { SetValue(IsFoupOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFoupOnProperty =
            DependencyProperty.Register("IsFoupOn", typeof(bool), typeof(VceT2Control), new PropertyMetadata(false, IsFoupOnChanged));

        public static void IsFoupOnChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is VceT2Control))
                return;

            VceT2Control ctrl = (VceT2Control)obj;

            ctrl.foup.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Hidden;
        }

        public VceT2Control()
        {
            InitializeComponent();
            foup.Visibility = Visibility.Hidden;
        }
    }
}
