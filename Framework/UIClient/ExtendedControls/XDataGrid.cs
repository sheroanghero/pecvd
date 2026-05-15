using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OpenSEMI.Controls.Controls
{
    public class XDataGrid : ExtendedGrid.Microsoft.Windows.Controls.DataGrid
    {
        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached("UseHorizontalScrolling", typeof(bool), typeof(XDataGrid),
              new PropertyMetadata(default(bool), UseHorizontalScrollingChangedCallback));

        public bool UseHorizontalScrolling
        {
            get { return (bool)GetValue(UseHorizontalScrollingProperty); }
            set { SetValue(UseHorizontalScrollingProperty, value); }
        }

        public System.Windows.Controls.ScrollViewer ref_scrollviewe = null;
        private void _UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            this.PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs args)
            {
                System.Windows.Controls.ScrollViewer scrollViewer = GetTemplateChild("DG_ScrollViewer") as System.Windows.Controls.ScrollViewer;
                if (scrollViewer != null)
                {
                    if (args.Delta > 0)
                        scrollViewer.LineLeft();
                    else
                        scrollViewer.LineRight();
                }
                ref_scrollviewe = scrollViewer;
                args.Handled = true;
            };
        }
        private static void UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            XDataGrid datagrid = dependencyObject as XDataGrid;

            if (datagrid == null)
                throw new ArgumentException("Element is not an ItemsControl");

            datagrid._UseHorizontalScrollingChangedCallback(dependencyObject, dependencyPropertyChangedEventArgs);
        }

        public static void SetUseHorizontalScrolling(System.Windows.Controls.ItemsControl element, bool value)
        {
            element.SetValue(UseHorizontalScrollingProperty, value);
        }

        public static bool GetUseHorizontalScrolling(System.Windows.Controls.ItemsControl element)
        {
            return (bool)element.GetValue(UseHorizontalScrollingProperty);
        }
    }
}
