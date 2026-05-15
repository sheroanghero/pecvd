using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    /// <summary>
    /// SystemConfigView.xaml 的交互逻辑
    /// </summary>
    public partial class SystemConfigView : UserControl
    {
        public SystemConfigView()
        {
            InitializeComponent();
        }

        private void BtnCollapseAll(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var item in PART_TREE.Items)
            {
                DependencyObject dObject = PART_TREE.ItemContainerGenerator.ContainerFromItem(item);
                CollapseTreeviewItems(((TreeViewItem)dObject),false);
            }
        }

        private void BtnExpandAll(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var item in PART_TREE.Items)
            {
                DependencyObject dObject = PART_TREE.ItemContainerGenerator.ContainerFromItem(item);
               //((TreeViewItem)dObject).ExpandSubtree();
                CollapseTreeviewItems(((TreeViewItem)dObject), true);
            }   
        }
        private void CollapseTreeviewItems(TreeViewItem Item,bool status)
        {
            Item.IsExpanded = status;

            foreach (var item in Item.Items)
            {
                DependencyObject dObject = Item.ItemContainerGenerator.ContainerFromItem(item);

                if (dObject != null)
                {
                    ((TreeViewItem)dObject).IsExpanded = status;

                    if (((TreeViewItem)dObject).HasItems)
                    {
                        CollapseTreeviewItems(((TreeViewItem)dObject), status);
                    }
                }
            }
        }
    }
 }
