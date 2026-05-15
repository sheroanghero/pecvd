using OpenSEMI.Controls.Controls;
using RecipeEditorLib.DGExtension.CustomColumn;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    /// <summary>
    /// Interaction logic for ConfigCompare.xaml
    /// </summary>
    public partial class ConfigCompareView : UserControl
    {
        private ScrollViewer _stepScrollA;
        private ScrollViewer _stepScrollB;

        private ScrollViewer _paramScrollA;
        private ScrollViewer _paramScrollB;

        private ScrollViewer _lineScrollA;
        private ScrollViewer _lineScrollB;

        public ConfigCompareView()
        {
            InitializeComponent();

            this.Loaded += RecipesCompareView_Loaded;
        }

        private void RecipesCompareView_Loaded(object sender, RoutedEventArgs e)
        {
            _lineScrollA = ScrollViewerFromFrameworkElement(WholeGridA);
            _lineScrollB = ScrollViewerFromFrameworkElement(WholeGridB);

            _lineScrollA.ScrollChanged += new ScrollChangedEventHandler(scrollViewer_LeftScrollChanged3);
            _lineScrollB.ScrollChanged += new ScrollChangedEventHandler(scrollViewer_RightScrollChanged3);
        }

        private void scrollViewer_RightScrollChanged3(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_lineScrollA.VerticalOffset - _lineScrollB.VerticalOffset) > 0.001)
            {
                _lineScrollA.ScrollToVerticalOffset(_lineScrollB.VerticalOffset);
            }

            if (Math.Abs(_lineScrollA.HorizontalOffset - _lineScrollB.HorizontalOffset) > 0.001)
            {
                _lineScrollA.ScrollToHorizontalOffset(_lineScrollB.HorizontalOffset);
            }
        }

        private void scrollViewer_LeftScrollChanged3(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_lineScrollB.VerticalOffset - _lineScrollA.VerticalOffset) > 0.001)
            {
                _lineScrollB.ScrollToVerticalOffset(_lineScrollA.VerticalOffset);
            }

            if (Math.Abs(_lineScrollB.HorizontalOffset - _lineScrollA.HorizontalOffset) > 0.001)
            {
                _lineScrollB.ScrollToHorizontalOffset(_lineScrollA.HorizontalOffset);
            }
        }

        void scrollViewer_RightScrollChanged1(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_stepScrollA.VerticalOffset - _stepScrollB.VerticalOffset) > 0.001)
            {
                _stepScrollA.ScrollToVerticalOffset(_stepScrollB.VerticalOffset);
            }
        }

        void scrollViewer_LeftScrollChanged1(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_stepScrollB.VerticalOffset - _stepScrollA.VerticalOffset) > 0.001)
            {
                _stepScrollB.ScrollToVerticalOffset(_stepScrollA.VerticalOffset);
            }
        }


        void scrollViewer_RightScrollChanged2(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_paramScrollA.VerticalOffset - _paramScrollB.VerticalOffset) > 0.001)
            {
                _paramScrollA.ScrollToVerticalOffset(_paramScrollB.VerticalOffset);
            }
        }

        void scrollViewer_LeftScrollChanged2(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(_paramScrollA.VerticalOffset - _paramScrollB.VerticalOffset) > 0.001)
            {
                _paramScrollB.ScrollToVerticalOffset(_paramScrollA.VerticalOffset);
            }
        }

        ScrollViewer ScrollViewerFromFrameworkElement(FrameworkElement frameworkElement)
        {
            if (VisualTreeHelper.GetChildrenCount(frameworkElement) == 0) return null;

            FrameworkElement child = VisualTreeHelper.GetChild(frameworkElement, 0) as FrameworkElement;

            if (child == null) return null;

            if (child is ScrollViewer)
            {
                return (ScrollViewer)child;
            }

            return ScrollViewerFromFrameworkElement(child);
        }
    }

}
