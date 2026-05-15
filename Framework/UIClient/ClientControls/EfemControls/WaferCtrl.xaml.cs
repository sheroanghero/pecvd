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

namespace MECF.Framework.UI.Client.ClientControls.EfemControls
{
    /// <summary>
    /// WaferCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class WaferCtrl : UserControl
    {
        public WaferCtrl()
        {
            InitializeComponent();
        }
        public WaferInfo WaferData
        {
            get { return (WaferInfo)GetValue(WaferDataProperty); }
            set { SetValue(WaferDataProperty, value); }
        }

        public static readonly DependencyProperty WaferDataProperty =
            DependencyProperty.Register("WaferData", typeof(WaferInfo), typeof(WaferCtrl), new PropertyMetadata(null));
    }
}
