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

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.MFCVerification
{
    /// <summary>
    /// MFCVerificationView.xaml 的交互逻辑
    /// </summary>
    public partial class MFCVerificationView : UserControl
    {
        public MFCVerificationView()
        {
            InitializeComponent();
        }

        private void VerificationHistoryDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var viewModel = this.DataContext as MFCVerificationViewModel;
            if (viewModel != null)
                viewModel.SelectHistory();
        }

        private void VerificationHistoryDataGrid_LoseFocus(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MFCVerificationViewModel;
            if (viewModel != null)
                viewModel.SelectedVerificationData = null;
        }
    }
}
