using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{
    /// <summary>
    /// WaferHistory2View.xaml 的交互逻辑
    /// </summary>
    public partial class WaferHistory2View : UserControl
    {
        public WaferHistory2View()
        {
            InitializeComponent();
        }

        private void dataGrid_LotList_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            dataGrid_LotList.CanUserAddRows = false;
            if (dataGrid_LotList.Items == null || dataGrid_LotList.Items.Count == 0)
                dataGrid_LotList.CanUserAddRows = true;
        }
    }
}
