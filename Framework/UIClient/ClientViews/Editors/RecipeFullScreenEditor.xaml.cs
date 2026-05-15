using System.Windows;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{
    /// <summary>
    /// RecipeFullScreenEditor.xaml 的交互逻辑
    /// </summary>
    public partial class RecipeFullScreenEditor : Window
    {
        public RecipeFullScreenEditor()
        {
            InitializeComponent();
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
