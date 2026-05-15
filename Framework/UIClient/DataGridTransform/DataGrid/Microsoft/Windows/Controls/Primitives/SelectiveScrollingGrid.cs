//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// Subclass of Grid that knows how to freeze certain cells in place when scrolled.
    /// Used as the panel for the DataGridRow to hold the header, cells, and details.
    /// </summary>
    public class SelectiveScrollingGrid : Grid
    {
        /// <summary>
        /// Attached property to specify the selective scroll behaviour of cells
        /// </summary>
        public static readonly DependencyProperty SelectiveScrollingOrientationProperty =
            DependencyProperty.RegisterAttached(
                "SelectiveScrollingOrientation",
                typeof(EGC.SelectiveScrollingOrientation),
                typeof(EGC.SelectiveScrollingGrid),
                new FrameworkPropertyMetadata(EGC.SelectiveScrollingOrientation.Both, new PropertyChangedCallback(OnSelectiveScrollingOrientationChanged)));

        /// <summary>
        /// Getter for the SelectiveScrollingOrientation attached property
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static EGC.SelectiveScrollingOrientation GetSelectiveScrollingOrientation(DependencyObject obj)
        {
            return (EGC.SelectiveScrollingOrientation)obj.GetValue(SelectiveScrollingOrientationProperty);
        }

        /// <summary>
        /// Setter for the SelectiveScrollingOrientation attached property
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetSelectiveScrollingOrientation(DependencyObject obj, EGC.SelectiveScrollingOrientation value)
        {
            obj.SetValue(SelectiveScrollingOrientationProperty, value);
        }

        /// <summary>
        /// Property changed call back for SelectiveScrollingOrientation property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectiveScrollingOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;
            EGC.SelectiveScrollingOrientation orientation = (EGC.SelectiveScrollingOrientation)e.NewValue;
            ScrollViewer scrollViewer = EGC.DataGridHelper.FindVisualParent<ScrollViewer>(element);
            if (scrollViewer != null && element != null)
            {
                Transform transform = element.RenderTransform;

                if (transform != null)
                {
                    BindingOperations.ClearBinding(transform, TranslateTransform.XProperty);
                    BindingOperations.ClearBinding(transform, TranslateTransform.YProperty);
                }

                if (orientation == EGC.SelectiveScrollingOrientation.Both)
                {
                    element.RenderTransform = null;
                }
                else
                {
                    TranslateTransform translateTransform = new TranslateTransform();

                    // Add binding to XProperty of transform if orientation is not horizontal
                    if (orientation != EGC.SelectiveScrollingOrientation.Horizontal)
                    {
                        Binding horizontalBinding = new Binding("ContentHorizontalOffset");
                        horizontalBinding.Source = scrollViewer;
                        BindingOperations.SetBinding(translateTransform, TranslateTransform.XProperty, horizontalBinding);
                    }

                    // Add binding to YProperty of transfrom if orientation is not vertical
                    if (orientation != EGC.SelectiveScrollingOrientation.Vertical)
                    {
                        Binding verticalBinding = new Binding("ContentVerticalOffset");
                        verticalBinding.Source = scrollViewer;
                        BindingOperations.SetBinding(translateTransform, TranslateTransform.YProperty, verticalBinding);
                    }

                    element.RenderTransform = translateTransform;
                }
            }
        }
    }
}
