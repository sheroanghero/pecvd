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
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace DataGridTransform
{
    public class DataGridExtern : EGC.DataGrid
    {
        #region Constructor and Property

        static DataGridExtern()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridExtern), new FrameworkPropertyMetadata(typeof(DataGridExtern)));
        }

        public void UpdateColumnWidth( DataGridDynamicColumn col,bool isExpanded )
        {
            if (this.Columns.Contains(col))
            {
                col.Width = 0;
                this.UpdateLayout();

                if (isExpanded)
                    col.Width = new EGC.DataGridLength(1, EGC.DataGridLengthUnitType.Auto);
                else
                    col.Width = 34;
            }
        }

        public System.Windows.Controls.ScrollViewer CurrentScrollViewer
        {
            get
            {
                return this.InternalScrollHost;
            }
        }

        #endregion

        #region Dynamic Columns Properies

        public static readonly DependencyProperty DynamicColumnsProperty = DependencyProperty.Register("DynamicColumns",
            typeof(DataGridDynamicColumns), typeof(DataGridExtern), new FrameworkPropertyMetadata(OnNotifyDynamicColumnsChanged));

        public DataGridDynamicColumns DynamicColumns
        {
            get { return (DataGridDynamicColumns)GetValue(DynamicColumnsProperty); }
            set { SetValue(DynamicColumnsProperty, value); }
        }

        #endregion

        #region Dynamic Columns Functions

        private static void OnNotifyDynamicColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridExtern)d).NotifyDynamicColumnsPropertyChanged(d, e);
        }

        internal void NotifyDynamicColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (null == DynamicColumns)
                return;

            DynamicColumns.DataGridParent = this;

            ResetColumns();

            AddDynamicColumns(DynamicColumns);
        }

        public void OnDynamicColumnItemChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (DataGridDynamicColumn item in e.NewItems)
                {
                    item.parent = this;
                    this.Columns.Add(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (DataGridDynamicColumn item in e.NewItems)
                {
                    this.Columns.Remove(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetColumns();
            }
        }

        protected void ResetColumns()
        {
            if (this.Columns.Count > 0)
                this.Columns.Clear();
        }

        protected void AddDynamicColumns( DataGridDynamicColumns newColumns )
        {
            foreach (DataGridDynamicColumn col in newColumns)
            {
                col.parent = this;
                this.Columns.Add(col);
            }
        }

        public void ScrollToEnd()
        {
            if (ref_scrollviewe != null)
            {
                ref_scrollviewe.ScrollToEnd();
            }
        }
        #endregion

        #region Horizontal Scroll Event Changed

        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached("UseHorizontalScrolling", typeof(bool), typeof(DataGridExtern),
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
            DataGridExtern datagrid = dependencyObject as DataGridExtern;

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

        #endregion

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                case Key.Home:
                case Key.End:
                case Key.PageUp:
                case Key.PageDown:
                    base.OnKeyDown(e);
                    break;
            }

        }
    }
}
