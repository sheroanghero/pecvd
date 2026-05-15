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
using System.Reflection;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Core.Applications;
using Autofac;

namespace Aitex.Core.UI.View.Frame
{
    /// <summary>
    /// Interaction logic for CenterView.xaml
    /// </summary>
    public partial class CenterView : UserControl
    {
        Dictionary<string, CenterTabView> _views = new Dictionary<string,CenterTabView>();

        public CenterView()
        {
            InitializeComponent();
        }

        public void CreateView(List<ViewItem> views )
        {
            foreach (ViewItem item in views)
            {
                if (item.SubView == null || item.SubView.Count == 0)
                    continue;

                CenterTabView tab = new CenterTabView();
                tab.Tag = item;
 
                foreach (ViewItem subItem in item.SubView)
                {
                    Type t = Assembly.Load(subItem.AssemblyName).GetType(subItem.ViewClass);

					if (t == null)
                        throw new ApplicationException(string.Format("The ui layout config file not valid, can not find {0} at assembly {1}", subItem.ViewClass, subItem.AssemblyName));

                    UserControl uc;
                    try
                    {
						using (var scope = UiApplication.Instance.Container.BeginLifetimeScope())
						{
 
							    if (scope.TryResolve(t, out var target))
							    {
							        uc = target as UserControl;
							    }
							    else
							    {
							        uc = (UserControl)Activator.CreateInstance(t);
                                }
 
						}
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        throw new ApplicationException(string.Format("Failed to initialize UI window {0}, {1}", subItem.ViewClass, ex.Message));
                    }

                    tab.Add(subItem, uc);
                }

                _views[item.Id] = tab;
            }
        }

        public void SetSelection(string id)
        {
            if (!_views.ContainsKey(id))
                return;
            _views[id].Height = this.Height ;
            gridContent.Children.Clear();
            gridContent.Children.Add(_views[id]);
        }

        public string GetCurrentViewName(string culture)
        {
            if (gridContent.Children.Count == 0)
                return string.Empty;

            ViewItem info = (gridContent.Children[0] as CenterTabView).Tag as ViewItem;

            if (!string.IsNullOrEmpty(culture) && info.GlobalName.ContainsKey(culture))
                return info.GlobalName[culture];

            return info.Name;

        }

        public UserControl GetView(string id)
        {
            foreach (var item in _views)
            {
                if (item.Value.FindView(id) != null)
                    return item.Value.FindView(id);
            }

            return null;
        }

        public TabItem GetTab(string id) 
        {
            foreach (var item in _views)
            {
                if (item.Value.FindTab(id) != null)
                    return item.Value.FindTab(id);
            }

            return null;
        }

        public void SetCulture(string culture)
        {
            foreach (var item in _views)
            {
                item.Value.SetCulture(culture);
            }        
        }
    }
}
