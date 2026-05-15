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
using Aitex.Core.Util;

namespace Aitex.Core.UI.View.Frame
{
    /// <summary>
    /// Interaction logic for CenterTabView.xaml
    /// </summary>
    public partial class CenterTabView : UserControl
    {
        UserControl _uc;
        private Dictionary<string, ViewItem> _lstTabs = new Dictionary<string, ViewItem>(); 
        public CenterTabView()
        {
            InitializeComponent();
        }

        public void Add(ViewItem item, UserControl view)
        {
            _uc = view;

            TabContainer.Items.Add(new TabItem()
            {
                Content = view,
                Header = item.Name,
                IsSelected = false,
                Tag = item.Id,
                Width=120,
            });

            _lstTabs[item.Id] = item;
        }

        public UserControl GetContentView()
        {
            return _uc;
        }

        public UserControl FindView(string id)
        {
            foreach (TabItem item in TabContainer.Items)
                if (item.Tag.ToString() == id)
                    return (UserControl)item.Content;

            return null;
        }

        public TabItem FindTab(string id)  
        {
            foreach (TabItem item in TabContainer.Items)
                if (item.Tag.ToString() == id)
                    return item;

            return null;
        }

        public void SetCulture(string culture)
        {
            foreach (TabItem item in TabContainer.Items)
            {
                ViewItem info = _lstTabs[item.Tag.ToString()];

                if (info.GlobalName.ContainsKey(culture))
                    item.Header = info.GlobalName[culture];
            }
        }
    }
}
