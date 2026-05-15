using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    /// <summary>
    /// Interaction logic for RoleView.xaml
    /// </summary>
    public partial class RoleView : UserControl
    {
        public RoleView()
        {
            InitializeComponent();
        }

        private void TextBox2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string newText = textBox2.Text + e.Text;
            if (int.TryParse(newText, out int number) && number < int.MaxValue)
            {
                e.Handled = false;
            }
            else
            {
                textBox2.Text = "0";
                e.Handled = true;
            }
        }

        //private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    ScrollViewer viewer = scrollviewer_LotList;
        //    if (viewer == null)
        //        return;
        //    double num = Math.Abs((int)(e.Delta / 2));
        //    double offset = 0.0;
        //    if (e.Delta > 0)
        //    {
        //        offset = Math.Max((double)0.0, (double)(viewer.VerticalOffset - num));
        //    }
        //    else
        //    {
        //        offset = Math.Min(viewer.ScrollableHeight, viewer.VerticalOffset + num);
        //    }
        //    if (offset != viewer.VerticalOffset)
        //    {
        //        viewer.ScrollToVerticalOffset(offset);
        //        e.Handled = true;
        //    }
        //}
    }
}
