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

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// Door.xaml 的交互逻辑
    /// </summary>
    public partial class Door : UserControl
    {
        public bool IsDoorOpen
        {
            get { return (bool)GetValue(IsDoorOpenProperty); }
            set { SetValue(IsDoorOpenProperty, value); }
        }
 
        public static readonly DependencyProperty IsDoorOpenProperty =
            DependencyProperty.Register("IsDoorOpen", typeof(bool), typeof(Door), new FrameworkPropertyMetadata(false));

        public Door()
        {
            InitializeComponent();
        }
    }
}
