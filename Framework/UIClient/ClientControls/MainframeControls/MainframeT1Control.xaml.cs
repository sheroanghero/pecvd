using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Client.ClientControls.PMControls;

namespace MECF.Framework.UI.Client.ClientControls.MainframeControls
{
    /// <summary>
    /// MainframeT1Control.xaml 的交互逻辑
    /// </summary>
    public partial class MainframeT1Control : UserControl
    {
        public WaferInfo WaferData1
        {
            get { return (WaferInfo)GetValue(WaferData1Property); }
            set { SetValue(WaferData1Property, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferData1Property =
            DependencyProperty.Register("WaferData1", typeof(WaferInfo), typeof(MainframeT1Control), new PropertyMetadata(WaferData1Changed));

        public static void WaferData1Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is MainframeT1Control))
                return;

            MainframeT1Control ctrl = (MainframeT1Control)obj;
            var waferCtrl = ctrl.waferCtrl1;

            if (waferCtrl.WaferData != (WaferInfo)e.NewValue)
                waferCtrl.WaferData = (WaferInfo)e.NewValue;
        }

        public WaferInfo WaferData2
        {
            get { return (WaferInfo)GetValue(WaferData2Property); }
            set { SetValue(WaferData2Property, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferData2Property =
            DependencyProperty.Register("WaferData2", typeof(WaferInfo), typeof(MainframeT1Control), new PropertyMetadata(WaferData2Changed));

        public static void WaferData2Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is MainframeT1Control))
                return;

            MainframeT1Control ctrl = (MainframeT1Control)obj;
            var waferCtrl = ctrl.waferCtrl2;

            if (waferCtrl.WaferData != (WaferInfo)e.NewValue)
                waferCtrl.WaferData = (WaferInfo)e.NewValue;
        }

        public MainframeT1Control()
        {
            InitializeComponent();
        }
    }
}
