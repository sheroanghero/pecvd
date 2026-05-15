using OpenSEMI.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace MECF.Framework.UI.Client.CenterViews.Dialogs
{
    /// <summary>
    /// ContinueSelectDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class ContinueSelectDialogView : UserControl
    {
        public ContinueSelectDialogView()
        {
            InitializeComponent();
        }
    }

    public class ContinueSelectDialogViewModel : DialogViewModel<string>
    {
        public void TreeSelectChanged(string sl)
        {
            this.currentSelected = sl;
        }

        public void TreeMouseDoubleClick(string sl)
        {
            this.currentSelected = sl;
            OK();
        }

        public void OK()
        {
            if (!string.IsNullOrEmpty(currentSelected))
            {
                this.DialogResult = currentSelected;
                IsCancel = false;
                TryClose(true);
            }
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }

        public ObservableCollection<string> Selections { get; set; }
        private string currentSelected;
    }
}
