//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;
using System.Collections;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A control for displaying a cell of the DataGrid.
    /// </summary>
    public class DataGridCell : ContentControl, EGC.IProvideDataGridColumn
    {
        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGridCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(typeof(EGC.DataGridCell)));
            StyleProperty.OverrideMetadata(typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStyle));
            ClipProperty.OverrideMetadata(typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceClip)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

            // Set SnapsToDevicePixels to true so that this element can draw grid lines.  The metadata options are so that the property value doesn't inherit down the tree from here.
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));

            EventManager.RegisterClassHandler(typeof(EGC.DataGridCell), MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnAnyMouseLeftButtonDownThunk), true);
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridCell()
        {
            _tracker = new EGC.ContainerTracking<EGC.DataGridCell>(this);
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new EGC.DataGridCellAutomationPeer(this);
        }

        #endregion

        #region Cell Generation

        /// <summary>
        ///     Prepares a cell for use.
        /// </summary>
        /// <remarks>
        ///     Updates the column reference.
        ///     This overload computes the column index from the ItemContainerGenerator.
        /// </remarks>
        internal void PrepareCell(object item, ItemsControl cellsPresenter, EGC.DataGridRow ownerRow)
        {
            int ItemIndex = cellsPresenter.ItemContainerGenerator.IndexFromContainer(this);
            // The the data source was a collection then fetch item for used
            object newItem = null;
            IList list = item as IList;
            if (list != null)
            {
                this.DataContext = list[ItemIndex];
            }
            else
            {
                newItem = item;
            }

            PrepareCell(newItem, ownerRow, ItemIndex);
        }

        /// <summary>
        ///     Prepares a cell for use.
        /// </summary>
        /// <remarks>
        ///     Updates the column reference.
        /// </remarks>
        internal void PrepareCell(object item, EGC.DataGridRow ownerRow, int index)
        {
            Debug.Assert(_owner == null || _owner == ownerRow, "_owner should be null before PrepareCell is called or the same value as the ownerRow.");

            _owner = ownerRow;

            EGC.DataGrid dataGrid = _owner.DataGridOwner;
            if (dataGrid != null)
            {
                // The index of the container should correspond to the index of the column
                if ((index >= 0) && (index < dataGrid.Columns.Count))
                {
                    // Retrieve the column definition and pass it to the cell container
                    EGC.DataGridColumn column = dataGrid.Columns[index];
                    Column = column;
                    TabIndex = column.DisplayIndex;
                }

                if (IsEditing)
                {
                    // If IsEditing was left on and this container was recycled, reset it here.
                    // Setting this property will result in BuildVisualTree being called.
                    IsEditing = false;
                }
                else if ((Content as FrameworkElement) == null)
                {
                    // If there isn't already a visual tree, then create one.
                    BuildVisualTree();

                    if (!NeedsVisualTree)
                    {
                        Content = item;
                    }
                }

                // Update cell Selection
                bool isSelected = dataGrid.SelectedCellsInternal.Contains(this);
                SyncIsSelected(isSelected);
            }

            EGC.DataGridHelper.TransferProperty(this, StyleProperty);
            EGC.DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
            CoerceValue(ClipProperty);
        }

        /// <summary>
        ///     Clears the cell of references.
        /// </summary>
        internal void ClearCell(EGC.DataGridRow ownerRow)
        {
            Debug.Assert(_owner == ownerRow, "_owner should be the same as the DataGridRow that is clearing the cell.");
            _owner = null;
        }

        /// <summary>
        ///     Used by the DataGridRowGenerator owner to send notifications to the cell container.
        /// </summary>
        internal EGC.ContainerTracking<EGC.DataGridCell> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Column Information

        /// <summary>
        ///     The column that defines how this cell should appear.
        /// </summary>
        public EGC.DataGridColumn Column
        {
            get { return (EGC.DataGridColumn)GetValue(ColumnProperty); }
            internal set { SetValue(ColumnPropertyKey, value); }
        }

        /// <summary>
        ///     The DependencyPropertyKey that allows writing the Column property value.
        /// </summary>
        private static readonly DependencyPropertyKey ColumnPropertyKey =
            DependencyProperty.RegisterReadOnly("Column", typeof(EGC.DataGridColumn), typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnColumnChanged)));

        /// <summary>
        ///     The DependencyProperty for the Columns property.
        /// </summary>
        public static readonly DependencyProperty ColumnProperty = ColumnPropertyKey.DependencyProperty;

        /// <summary>
        ///     Called when the Column property changes.
        ///     Calls the protected virtual OnColumnChanged.
        /// </summary>
        private static void OnColumnChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridCell cell = sender as EGC.DataGridCell;
            if (cell != null)
            {
                cell.OnColumnChanged((EGC.DataGridColumn)e.OldValue, (EGC.DataGridColumn)e.NewValue);
            }
        }

        /// <summary>
        ///     Called due to the cell's column definition changing.
        ///     Not called due to changes within the current column definition.
        /// </summary>
        /// <remarks>
        ///     Coerces ContentTemplate and ContentTemplateSelector.
        /// </remarks>
        /// <param name="oldColumn">The old column definition.</param>
        /// <param name="newColumn">The new column definition.</param>
        protected virtual void OnColumnChanged(EGC.DataGridColumn oldColumn, EGC.DataGridColumn newColumn)
        {
            // We need to call BuildVisualTree after changing the column (PrepareCell does this).
            Content = null;
            EGC.DataGridHelper.TransferProperty(this, StyleProperty);
            EGC.DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        ///     Notifies the Cell of a property change.
        /// </summary>
        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridCell)d).NotifyPropertyChanged(d, string.Empty, e, NotificationTarget.Cells);
        }

        /// <summary>
        ///     Cancels editing the current cell & notifies the cell of a change to IsReadOnly.
        /// </summary>
        private static void OnNotifyIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cell = (EGC.DataGridCell)d;
            var dataGrid = cell.DataGridOwner;
            if ((bool)e.NewValue && dataGrid != null)
            {
                dataGrid.CancelEdit(cell);
            }

            // re-evalutate the BeginEdit command's CanExecute.
            CommandManager.InvalidateRequerySuggested();

            cell.NotifyPropertyChanged(d, string.Empty, e, NotificationTarget.Cells);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, NotificationTarget target)
        {
            EGC.DataGridColumn column = d as EGC.DataGridColumn;
            if ((column != null) && (column != Column))
            {
                // This notification does not apply to this cell
                return;
            }

            // All the notifications which are to be handled by the cell
            if (EGC.DataGridHelper.ShouldNotifyCells(target))
            {
                if (e.Property == EGC.DataGridColumn.WidthProperty)
                {
                    EGC.DataGridHelper.OnColumnWidthChanged(this, e);
                }
                else if (e.Property == EGC.DataGrid.CellStyleProperty || e.Property == EGC.DataGridColumn.CellStyleProperty || e.Property == StyleProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, StyleProperty);
                }
                else if (e.Property == EGC.DataGrid.IsReadOnlyProperty || e.Property == EGC.DataGridColumn.IsReadOnlyProperty || e.Property == IsReadOnlyProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
                }
                else if (e.Property == EGC.DataGridColumn.DisplayIndexProperty)
                {
                    TabIndex = column.DisplayIndex;
                }
            }

            // All the notifications which needs forward to columns
            if (EGC.DataGridHelper.ShouldRefreshCellContent(target))
            {
                if (column != null && NeedsVisualTree)
                {
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        column.RefreshCellContent(this, propertyName);
                    }
                    else if (e != null && e.Property != null)
                    {
                        column.RefreshCellContent(this, e.Property.Name);
                    }
                }
            }
        }

        #endregion

        #region Style

        private static object OnCoerceStyle(DependencyObject d, object baseValue)
        {
            var cell = d as EGC.DataGridCell;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                cell, 
                baseValue, 
                StyleProperty,
                cell.Column,
                EGC.DataGridColumn.CellStyleProperty,
                cell.DataGridOwner,
                EGC.DataGrid.CellStyleProperty);
        }

        #endregion

        #region Template

        /// <summary>
        ///     Builds a column's visual tree if not using templates.
        /// </summary>
        internal void BuildVisualTree()
        {
            if (NeedsVisualTree)
            {
                var column = Column;
                if (column != null)
                {
                    // Work around a problem with BindingGroup not removing BindingExpressions.
                    var row = RowOwner;
                    if (row != null)
                    {
                        var bindingGroup = row.BindingGroup;
                        if (bindingGroup != null)
                        {
                            RemoveBindingExpressions(bindingGroup, Content as DependencyObject);
                        }
                    }

                    // Ask the column to build a visual tree and
                    // hook the visual tree up through the Content property.
                    Content = column.BuildVisualTree(IsEditing, RowDataItem, this);
                }
            }
        }

        private void RemoveBindingExpressions(BindingGroup bindingGroup, DependencyObject element)
        {
            // Walk the logical tree  looking for BindingBindingExpressions.
            // If it is found in the BindingGroup's BindingExpression list we will remove it.
            // We only search the logical tree for perf reasons, so some visual children could be skipped.  This 
            // should be ok for all the stock columns, and will be something that a custom column would need to be
            // aware of.
            if (element != null)
            {
                var bindingExpressions = bindingGroup.BindingExpressions;
                var localValueEnumerator = element.GetLocalValueEnumerator();
                while (localValueEnumerator.MoveNext())
                {
                    var bindingExpression = localValueEnumerator.Current.Value as BindingExpression;
                    if (bindingExpression != null)
                    {
                        for (int i = 0; i < bindingExpressions.Count; i++)
                        {
                            if (object.ReferenceEquals(bindingExpression, bindingExpressions[i]))
                            {
                                bindingExpressions.RemoveAt(i--);
                            }
                        }
                    }
                }

                // Recursively remove BindingExpressions from child elements.
                foreach (object child in LogicalTreeHelper.GetChildren(element))
                {
                    RemoveBindingExpressions(bindingGroup, child as DependencyObject);
                }
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Whether the cell is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        /// <summary>
        ///     Represents the IsEditing property.
        /// </summary>
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsEditingChanged)));

        private static void OnIsEditingChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridCell)sender).OnIsEditingChanged((bool)e.NewValue);
        }

        /// <summary>
        ///     Called when the value of IsEditing changes.
        /// </summary>
        /// <remarks>
        ///     Coerces the value of ContentTemplate.
        /// </remarks>
        /// <param name="isEditing">The new value of IsEditing.</param>
        protected virtual void OnIsEditingChanged(bool isEditing)
        {
            if (IsKeyboardFocusWithin && !IsKeyboardFocused)
            {
                // Keep focus on the cell when flipping modes
                Focus();
            }

            // If templates aren't being used, then a new visual tree needs to be built.
            BuildVisualTree();
        }

        /// <summary>
        ///     Whether the cell can be placed in edit mode.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
        }

        private static readonly DependencyPropertyKey IsReadOnlyPropertyKey =
            DependencyProperty.RegisterReadOnly("IsReadOnly", typeof(bool), typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(false, OnNotifyIsReadOnlyChanged, OnCoerceIsReadOnly));

        /// <summary>
        ///     The DependencyProperty for IsReadOnly.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = IsReadOnlyPropertyKey.DependencyProperty;

        private static object OnCoerceIsReadOnly(DependencyObject d, object baseValue)
        {
            var cell = d as EGC.DataGridCell;
            var column = cell.Column;
            var dataGrid = cell.DataGridOwner;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                cell, 
                baseValue, 
                IsReadOnlyProperty,
                column,
                EGC.DataGridColumn.IsReadOnlyProperty,
                dataGrid,
                EGC.DataGrid.IsReadOnlyProperty);
        }

        /// <summary>
        ///     An event reporting that the IsKeyboardFocusWithin property changed.
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGrid owner = DataGridOwner;
            if (owner != null)
            {
                owner.CellIsKeyboardFocusWithinChanged(this, (bool)e.NewValue);
            }
        }

        internal void BeginEdit(RoutedEventArgs e)
        {
            Debug.Assert(!IsEditing, "Should not call BeginEdit when IsEditing is true.");

            IsEditing = true;

            EGC.DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to store the original value
                column.BeginEdit(Content as FrameworkElement, e);
            }

            RaisePreparingCellForEdit(e);
        }

        internal void CancelEdit()
        {
            Debug.Assert(IsEditing, "Should not call CancelEdit when IsEditing is false.");

            EGC.DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to restore the original value
                column.CancelEdit(Content as FrameworkElement);
            }

            IsEditing = false;
        }

        internal bool CommitEdit()
        {
            Debug.Assert(IsEditing, "Should not call CommitEdit when IsEditing is false.");

            bool validationPassed = true;
            EGC.DataGridColumn column = Column;
            if (column != null)
            {
                // Ask the column to access the binding and update the data source
                // If validation fails, then remain in editing mode
                validationPassed = column.CommitEdit(Content as FrameworkElement);
            }

            if (validationPassed)
            {
                IsEditing = false;
            }

            return validationPassed;
        }

        private void RaisePreparingCellForEdit(RoutedEventArgs editingEventArgs)
        {
            EGC.DataGrid dataGridOwner = DataGridOwner;
            if (dataGridOwner != null)
            {
                FrameworkElement currentEditingElement = EditingElement;
                EGC.DataGridPreparingCellForEditEventArgs preparingCellForEditEventArgs = new EGC.DataGridPreparingCellForEditEventArgs(Column, RowOwner, editingEventArgs, currentEditingElement);
                dataGridOwner.OnPreparingCellForEdit(preparingCellForEditEventArgs);
            }
        }

        internal FrameworkElement EditingElement
        {
            get
            {
                // The editing element was stored in the Content property.
                return Content as FrameworkElement;
            }
        }

        #endregion

        #region Selection

        /// <summary>
        ///     Whether the cell is selected or not.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     Represents the IsSelected property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(EGC.DataGridCell), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridCell cell = (EGC.DataGridCell)sender;
            bool isSelected = (bool)e.NewValue;

            // There is no reason to notify the DataGrid if IsSelected's value came
            // from the DataGrid.
            if (!cell._syncingIsSelected)
            {
                EGC.DataGrid dataGrid = cell.DataGridOwner;
                if (dataGrid != null)
                {
                    // Notify the DataGrid that a cell's IsSelected property changed
                    // in case it was done programmatically instead of by the 
                    // DataGrid itself.
                    dataGrid.CellIsSelectedChanged(cell, isSelected);
                }
            }

            cell.RaiseSelectionChangedEvent(isSelected);
        }

        /// <summary>
        ///     Used to synchronize IsSelected with the DataGrid.
        ///     Prevents unncessary notification back to the DataGrid.
        /// </summary>
        internal void SyncIsSelected(bool isSelected)
        {
            bool originalValue = _syncingIsSelected;
            _syncingIsSelected = true;
            try
            {
                IsSelected = isSelected;
            }
            finally
            {
                _syncingIsSelected = originalValue;
            }
        }

        private void RaiseSelectionChangedEvent(bool isSelected)
        {
            if (isSelected)
            {
                OnSelected(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
            }
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EGC.DataGridCell));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add
            {
                AddHandler(SelectedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes true. Raises the Selected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EGC.DataGridCell));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public event RoutedEventHandler Unselected
        {
            add
            {
                AddHandler(UnselectedEvent, value);
            }

            remove
            {
                RemoveHandler(UnselectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes false. Raises the Unselected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region GridLines

        // Different parts of the DataGrid draw different pieces of the GridLines.
        // Cells draw a single line on their right side. 

        /// <summary>
        ///     Measure.  This is overridden so that the cell can extend its size to account for a grid line on the right.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            // Make space for the GridLine on the right:
            // Remove space from the constraint (since it implicitly includes the GridLine's thickness), 
            // call the base implementation, and add the thickness back for the returned size.
            if (EGC.DataGridHelper.IsGridLineVisible(DataGridOwner, /*isHorizontal = */ false))
            {
                double thickness = DataGridOwner.VerticalGridLineThickness;
                Size desiredSize = base.MeasureOverride(EGC.DataGridHelper.SubtractFromSize(constraint, thickness, /*height = */ false));
                desiredSize.Width += thickness;
                return desiredSize;
            }
            else
            {
                return base.MeasureOverride(constraint);
            }
        }

        /// <summary>
        ///     Arrange.  This is overriden so that the cell can position its content to account for a grid line on the right.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // We don't need to adjust the Arrange position of the content.  By default it is arranged at 0,0 and we're
            // adding a line to the right.  All we have to do is compress and extend the size, just like Measure.
            if (EGC.DataGridHelper.IsGridLineVisible(DataGridOwner, /*isHorizontal = */ false))
            {
                double thickness = DataGridOwner.VerticalGridLineThickness;
                Size returnSize = base.ArrangeOverride(EGC.DataGridHelper.SubtractFromSize(arrangeSize, thickness, /*height = */ false));
                returnSize.Width += thickness;
                return returnSize;
            }
            else
            {
                return base.ArrangeOverride(arrangeSize);
            }
        }

        /// <summary>
        ///     OnRender.  Overriden to draw a vertical line on the right.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (EGC.DataGridHelper.IsGridLineVisible(DataGridOwner, /*isHorizontal = */ false))
            {
                double thickness = DataGridOwner.VerticalGridLineThickness;
                Rect rect = new Rect(new Size(thickness, RenderSize.Height));
                rect.X = RenderSize.Width - thickness;

                drawingContext.DrawRectangle(DataGridOwner.VerticalGridLinesBrush, null, rect);
            }
        }

        #endregion

        #region Input

        private static void OnAnyMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            ((EGC.DataGridCell)sender).OnAnyMouseLeftButtonDown(e);
        }

        /// <summary>
        ///     The left mouse button was pressed
        /// </summary>
        /// TODO: Consider making a protected virtual.
        private void OnAnyMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            bool focusWithin = IsKeyboardFocusWithin;
            if (focusWithin && !e.Handled && !IsEditing && !IsReadOnly && IsSelected)
            {
                // The cell is focused and there are no other special selection gestures,
                // enter edit mode.
                EGC.DataGrid dataGridOwner = DataGridOwner;
                if (dataGridOwner != null)
                {
                    // The cell was clicked, which means that other cells may
                    // need to be de-selected, let the DataGrid handle that.
                    dataGridOwner.HandleSelectionForCellInput(this, /* startDragging = */ false, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ false);

                    // Enter edit mode
                    dataGridOwner.BeginEdit(e);
                    e.Handled = true;
                }
            }
            else if (!focusWithin || !IsSelected)
            {
                if (!focusWithin)
                {
                    // The cell should receive focus on click
                    Focus();
                }

                EGC.DataGrid dataGridOwner = DataGridOwner;
                if (dataGridOwner != null)
                {
                    // Let the DataGrid process selection
                    dataGridOwner.HandleSelectionForCellInput(this, /* startDragging = */ Mouse.Captured == null, /* allowsExtendSelect = */ true, /* allowsMinimalSelect = */ true);
                }

                e.Handled = true;
            }
#if PUBLIC_ONINPUT
            else
            {
                SendInputToColumn(e);
            }
#endif
        }

        /// <summary>
        ///     Reporting text composition.
        /// </summary>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            SendInputToColumn(e);
        }

        /// <summary>
        ///     Reporting a key was pressed.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            SendInputToColumn(e);
        }

#if PUBLIC_ONINPUT
        // TODO: While DataGridColumn.OnInput is internal, these methods are not needed.
        // If that method becomes exposed, then it would make sense to include a better
        // range of input events.

        /// <summary>
        ///     Reporting a key was released
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            SendInputToColumn(e);
        }

        /// <summary>
        ///     Reporting the mouse button was released
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            SendInputToColumn(e);
        }
#endif

        private void SendInputToColumn(InputEventArgs e)
        {
            var column = Column;
            if (column != null)
            {
                column.OnInput(e);
            }
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Coercion call back for clip property which ensures that the cell overlapping with frozen 
        /// column gets clipped appropriately.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceClip(DependencyObject d, object baseValue)
        {
            EGC.DataGridCell cell = (EGC.DataGridCell)d;
            Geometry geometry = baseValue as Geometry;
            Geometry frozenGeometry = EGC.DataGridHelper.GetFrozenClipForCell(cell);
            if (frozenGeometry != null)
            {
                if (geometry == null)
                {
                    return frozenGeometry;
                }

                geometry = new CombinedGeometry(GeometryCombineMode.Intersect, geometry, frozenGeometry);
            }

            return geometry;
        }

        #endregion

        #region Helpers

        internal EGC.DataGrid DataGridOwner
        {
            get
            {
                if (_owner != null)
                {
                    EGC.DataGrid dataGridOwner = _owner.DataGridOwner;
                    if (dataGridOwner == null)
                    {
                        dataGridOwner = ItemsControl.ItemsControlFromItemContainer(_owner) as EGC.DataGrid;
                    }

                    return dataGridOwner;
                }

                return null;
            }
        }

        private Panel ParentPanel
        {
            get
            {
                return VisualParent as Panel;
            }
        }

        internal EGC.DataGridRow RowOwner
        {
            get { return _owner; }
        }

        internal object RowDataItem
        {
            get
            {
                EGC.DataGridRow row = RowOwner;
                if (row != null)
                {
                    return row.Item;
                }

                return DataContext;
            }
        }

        private EGC.DataGridCellsPresenter CellsPresenter
        {
            get
            {
                return ItemsControl.ItemsControlFromItemContainer(this) as EGC.DataGridCellsPresenter;
            }
        }

        private bool NeedsVisualTree
        {
            get
            {
                return (ContentTemplate == null) && (ContentTemplateSelector == null);
            }
        }

        #endregion

        #region Data

        private EGC.DataGridRow _owner;
        private EGC.ContainerTracking<EGC.DataGridCell> _tracker;
        private bool _syncingIsSelected;                    // Used to prevent unnecessary notifications

        #endregion
    }
}