using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Aitex.Core.UI.View.Common;

namespace Aitex.Core.UI.Dialog
{
    /// <summary>
    /// Interaction logic for RecipeSelectDialog.xaml
    /// </summary>
    public partial class RecipeSelectDialog : Window
    {
        private string _recipeList;

        public RecipeSelectDialog(string recipeList)
        {
            _recipeList = recipeList;

            InitializeComponent();

            Loaded += new RoutedEventHandler(RecipeSelectDialog_Loaded);
        }

        /// <summary>
        /// load recipe file list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RecipeSelectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(_recipeList);
            CreateTreeViewItems(doc.DocumentElement, this.treeView1);
        }

        /// <summary>
        /// Create TreeViewItems
        /// </summary>
        /// <param name="curElementNode"></param>
        void CreateTreeViewItems(XmlElement curElementNode, ItemsControl itemsControl)
        {
            foreach (XmlElement ele in curElementNode.ChildNodes)
            {
                if (ele.Name == "File")
                {
                    string fileName = ele.Attributes["Name"].Value;
                    fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    TreeViewFileItem item = new TreeViewFileItem(ele.Attributes["Name"].Value);
                    item.Tag = ele.Attributes["Name"].Value;
                    item.ToolTip = ele.Attributes["Name"].Value;
                    itemsControl.Items.Add(item);
                }
                else if (ele.Name == "Folder")
                {
                    string folderName = ele.Attributes["Name"].Value;
                    folderName = folderName.Substring(folderName.LastIndexOf('\\') + 1);
                    TreeViewFolderItem item = new TreeViewFolderItem(folderName);
                    item.Tag = ele.Attributes["Name"].Value;
                    CreateTreeViewItems(ele, item);
                    item.IsExpanded = false;
                    itemsControl.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Selected recipe file
        /// </summary>
        public string SelectedRecipe
        {
            get
            {
                var file = treeView1.SelectedItem as TreeViewFileItem;
                if (file != null)
                    return file.FileName;
                return string.Empty;
            }
        }

        /// <summary>
        /// On click cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Hide();
        }

        /// <summary>
        /// On click ok button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void TreeView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnOK_Click(sender, e);
        }
    }
}
