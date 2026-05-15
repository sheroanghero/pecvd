using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.RT.Log;
using System.Xml;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.UI.View.Frame;

namespace Aitex.Core.UI.View.Common
{
    /// <summary>
    /// Interaction logic for ParameterView.xaml
    /// </summary>
    public partial class ParameterView : UserControl
    {
        bool _isLoaded;
        ParameterViewModel _vm;
        string _gridEditorOldValue;
        bool _isGridModified;

        public ParameterView()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(ParameterView_Loaded);
        }

        void ParameterView_Loaded(object sender, RoutedEventArgs e)
        {
            //set permission    
            this.IsEnabled = true;
            this.Name = "Parameter";

            Account.ViewPermission viewPermission;    // chaossong@add 20160414 for permission
            if (ViewManager.LoginAccount != null)
                viewPermission = ViewManager.LoginAccount.Permission[this.Name];
            else
                viewPermission = Account.ViewPermission.FullyControl;

            switch (viewPermission)
            { 
                case Account.ViewPermission.FullyControl:
                    btnResetParam.IsEnabled = true;
                    btnSaveParam.IsEnabled = true;
                    dataGrid1.IsEnabled = true;
                    break;
                default:
                    btnResetParam.IsEnabled = false;
                    btnSaveParam.IsEnabled = false;
                    dataGrid1.IsEnabled = false;
                    break;
            }

            if (_isLoaded)
                return;

            _vm = DataContext as ParameterViewModel;
            if (_vm == null)
                throw new ApplicationException("系统配置页面没有设置DataContext，需要在窗口创建之前，设置初始配置数据");

            InitTree();

            _isLoaded = true;
        }

        void InitTree()
        {
            treeView1.Items.Clear();

            List<KeyValuePair<string, string>> lstSections = _vm.GetSectionList();
            foreach (KeyValuePair<string, string> item in lstSections)
            {
                var fNode = new TreeViewFolderItem(item.Key);

                fNode.Tag = item;
                fNode.Selected += new RoutedEventHandler(sectionNode_Selected);

                treeView1.Items.Add(fNode);
            }

        }

        void sectionNode_Selected(object sender, RoutedEventArgs e)
        {
            var selectedNode = sender as TreeViewFolderItem;
            if (selectedNode == null)
                return;

            if (_isGridModified)
            {
                if (MessageBox.Show("是否需要保存未存盘的参数？", "未保存", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    btnSaveParam_Click(null, null);
                }
                else
                {
                    _isGridModified = false;
                }
            }

            KeyValuePair<string, string> keyPair = (KeyValuePair<string, string>)selectedNode.Tag;
            labelTile.Content = keyPair.Key;
            dataGrid1.Tag = keyPair.Value;

            dataGrid1.ItemsSource = _vm.GetConfigEntries(keyPair.Value);          
        }

        private void GridEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
                _isGridModified = tb.Text != _gridEditorOldValue;
        }

        private void GridEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
                _gridEditorOldValue = tb.Text;
        }

        private void btnSaveParam_Click(object sender, RoutedEventArgs e)
        {
            _isGridModified = false;

            string reason;
            if (!VerifyInputData(out reason))
            {
                MessageBox.Show(reason, "Aitex", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string xpath = dataGrid1.Tag as string;
            if (_vm.SaveConfigSection(xpath.Substring(xpath.IndexOf("'") + 1, xpath.LastIndexOf("'") - xpath.IndexOf("'")-1), dataGrid1.ItemsSource as List<ConfigEntry>))
                MessageBox.Show("设定值保存成功！", "Aitex", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("设定值保存失败！", "Aitex", MessageBoxButton.OK, MessageBoxImage.Error);

            btnResetParam_Click(null, null);
        }

        private void btnResetParam_Click(object sender, RoutedEventArgs e)
        {
            dataGrid1.ItemsSource = _vm.GetConfigEntries(dataGrid1.Tag as string);
        }

        private bool VerifyInputData(out string reason)
        {
            bool ret = true;
            dataGrid1.CancelEdit();
            var entryList = this.dataGrid1.ItemsSource.Cast<object>();
            reason = string.Empty;
            foreach (ConfigEntry entry in entryList)
            {
                try
                {
                    var inputText = entry.Value;
                    var typeText = entry.Type;
                    var minText = entry.RangeLowLimit;
                    var maxText = entry.RangeUpLimit;

                    if (string.IsNullOrEmpty(inputText))
                        continue;

                    if (String.Compare(typeText, "Int32", true) == 0)
                    {
                        int res = 0;
                        if (!int.TryParse(inputText, out res))
                            throw new Exception();
                        int min = Convert.ToInt32(minText);
                        int max = Convert.ToInt32(maxText);
                        if (res < min || res > max)
                        {
                            ret = false;
                            reason += string.Format("{0} 输入值错误！\n", entry.Description);
                            var textBlock = dataGrid1.Columns[7].GetCellContent(entry) as System.Windows.Controls.TextBlock;
                            if (textBlock != null)
                                textBlock.Background = Brushes.Red;
                        }
                    }
                    else if (String.Compare(typeText, "Double", true) == 0)
                    {
                        double res = 0;
                        if (!double.TryParse(inputText, out res))
                            throw new Exception();
                        double min = Convert.ToDouble(minText);
                        double max = Convert.ToDouble(maxText);
                        if (res < min || res > max)
                        {
                            ret = false;
                            reason += string.Format("{0} 输入值错误！\n", entry.Description);
                            var textBlock = dataGrid1.Columns[7].GetCellContent(entry) as System.Windows.Controls.TextBlock;
                            if (textBlock != null)
                                textBlock.Background = Brushes.Red;
                        }
                    }
                }
                catch (Exception)
                {
                    reason += string.Format("{0} 输入值错误！\n", entry.Description);
                    ret = false;
                }
            }
            return ret;
        }
    }
}
