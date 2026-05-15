using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Aitex.Core.Util;
using MECF.Framework.UI.Client.ClientControls.EfemControls;

namespace MECF.Framework.UI.Client.ClientControls.PMControls
{
    /// <summary>
    /// PMT1Control.xaml 的交互逻辑
    /// </summary>
    public partial class PMT1Control : UserControl
    {
        public WaferInfo WaferData
        {
            get { return (WaferInfo)GetValue(WaferDataProperty); }
            set { SetValue(WaferDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferDataProperty =
            DependencyProperty.Register("WaferData", typeof(WaferInfo), typeof(PMT1Control), new PropertyMetadata(WaferDataChanged));

        public static void WaferDataChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is PMT1Control))
                return;

            PMT1Control ctrl = (PMT1Control)obj;
            var waferCtrl = ctrl.waferCtrl;

            if (waferCtrl.WaferData != (WaferInfo)e.NewValue)
                waferCtrl.WaferData = (WaferInfo)e.NewValue;
        }

        public PMT1Control()
        {
            InitializeComponent();
        }

    }
}
