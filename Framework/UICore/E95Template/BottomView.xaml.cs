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

namespace Aitex.Core.UI.View.Frame
{
    /// <summary>
    /// Interaction logic for BottomView.xaml
    /// </summary>
    /// 

    partial class BottomView : UserControl
    {
        private int ButtonMarginLeft = 60;

        Size ButtonSizeSelected = new Size(160, 45);
        Size ButtonSizeUnselected = new Size(145, 38);

        public event Action<string> ButtonClicked;

        public BottomView()
        {
            InitializeComponent();

            //btnBackward.Tag = new ViewItem()
            //{
            //    Id = "backward",
            //};
            btnExit.Tag = new ViewItem()
            {
                Id = "exit",
            };
            //btnForward.Tag = new ViewItem()
            //{
            //    Id = "forward",
            //};
        }

        public void CreateMenu(List<ViewItem> items)
        {
            //double marginLeft = btnBackward.Margin.Left + btnBackward.Width + 50;
            double marginLeft = ButtonSizeUnselected.Width;
            foreach (ViewItem item in items)
            {
                Button btn = new Button();
                btn.Style = Resources["navButton"] as Style;
                btn.Margin = new Thickness(marginLeft, btnExit.Margin.Top, 0, 10);
                
                btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                btn.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                btn.Content = item.Name;
                btn.Tag = item;
                btn.Click += new RoutedEventHandler(btn_Click);

                marginLeft += (btn.Width + ButtonMarginLeft);

                gridContent.Children.Add(btn);
            }
        }

        void btn_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonClicked != null)
            {
                ButtonClicked(((sender as Button).Tag as ViewItem).Id);
            }
        }

        public void SetSelection(string id)
        {
            ImageBrush NormalBrush = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Main/主菜单按钮150.png", UriKind.Absolute)));
            ImageBrush SelectedBrush = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.Common;component/Resources/Main/主菜单按钮_选中150.png", UriKind.Absolute)));

            foreach (var item in gridContent.Children)
            {
                Button btn = item as Button;

                if ((btn==null) || ((btn.Tag as ViewItem).Name == null))
                    continue;

                btn.Background = ((btn.Tag as ViewItem).Id == id) ? SelectedBrush : NormalBrush;
                btn.Height = ((btn.Tag as ViewItem).Id == id) ? 50: 38;
                btn.Width = ((btn.Tag as ViewItem).Id == id) ? 170 : 145;
            }
        }

        public void Enable(string id, bool enable)
        {
            foreach (var item in gridContent.Children)
            {
                Button btn = item as Button;

                if (btn == null)
                    continue;

                if ((btn.Tag as ViewItem).Name == null)
                    continue;

                if ((btn.Tag as ViewItem).Id != id)
                    continue;

                btn.Visibility = !enable ? Visibility.Hidden : Visibility.Visible;
            }        
        }

        public void SetCulture(string culture)
        {
            foreach (var item in gridContent.Children)
            {
                Button btn = item as Button;

                if (btn == null)
                    continue;

                ViewItem info = btn.Tag as ViewItem;

                if (info==null || info.Name==null || info.GlobalName==null || !info.GlobalName.ContainsKey(culture))
                    continue;

                btn.Content = info.GlobalName[culture];
            }
        }
    }
}
