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

namespace MECF.Framework.RT.Core.Backend
{
    /// <summary>
    /// BackendMainView.xaml 的交互逻辑
    /// </summary>
    public partial class BackendMainView : Window
    {
        public BackendMainView()
        {
            InitializeComponent();

            this.Closing += BackendMainView_Closing;
        }

        private void BackendMainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        public void AddItem(string header, UserControl uc)
        {
            TabItem item = new TabItem {Header = header};

            item.Content = uc;

            tab.Items.Add(item);
        }
    }
}
