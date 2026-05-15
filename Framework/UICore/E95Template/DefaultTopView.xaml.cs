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
using Aitex.Core.UI.View.Frame;

namespace MECF.Framework.UI.Core.E95Template
{
    /// <summary>
    /// DefaultTopView.xaml 的交互逻辑
    /// </summary>
    public partial class DefaultTopView : UserControl, ITopView
    {
        public DefaultTopView()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            
        }
    }
}
