//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;
using MS.Internal;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// Container for the Column header object.  This is instantiated by the DataGridColumnHeadersPresenter.
    /// </summary>
    [TemplatePart(Name = "PART_LeftHeaderGripper", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_RightHeaderGripper", Type = typeof(Thumb))]
    public class DataGridColumnHeader : ButtonBase, EGC.IProvideDataGridColumn
    {
        #region Constructors

        static DataGridColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(typeof(EGC.DataGridColumnHeader)));

            ContentProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceContent));
            ContentTemplateProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceContentTemplate));
            ContentTemplateSelectorProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceContentTemplateSelector));
            ContentStringFormatProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStringFormat));
            StyleProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnNotifyPropertyChanged, OnCoerceStyle));
            HeightProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceHeight));

            FocusableProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(false));
            ClipProperty.OverrideMetadata(typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null, OnCoerceClip));            
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridColumnHeader()
        {
            _tracker = new EGC.ContainerTracking<EGC.DataGridColumnHeader>(this);
        }

        #endregion 

        #region Public API

        /// <summary>
        ///     The Column associated with this DataGridColumnHeader.
        /// </summary>
        public EGC.DataGridColumn Column
        {
            get
            {
                return _column;
            }
        }

        /// <summary>
        ///     Property that indicates the brush to use when drawing seperators between headers.
        /// </summary>
        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeperatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register("SeparatorBrush", typeof(Brush), typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Property that indicates the Visibility for the header seperators.
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeperatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register("SeparatorVisibility", typeof(Visibility), typeof(EGC.DataGridColumnHeader), new FrameworkPropertyMetadata(Visibility.Visible));

        #endregion 

        #region Column Header Generation

        /// <summary>
        /// Prepares a column header to be used.  Sets up the association between the column header and its column.
        /// </summary>
        internal void PrepareColumnHeader(object item, EGC.DataGridColumn column)
        {
            Debug.Assert(column != null, "This header must have been generated with for a particular column");
            Debug.Assert(column.Header == item, "The data item for a ColumnHeader is the Header property of a column");
            _column = column;
            TabIndex = column.DisplayIndex;

            EGC.DataGridHelper.TransferProperty(this, ContentProperty);
            EGC.DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            EGC.DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            EGC.DataGridHelper.TransferProperty(this, ContentStringFormatProperty);
            EGC.DataGridHelper.TransferProperty(this, StyleProperty);
            EGC.DataGridHelper.TransferProperty(this, HeightProperty);

            CoerceValue(CanUserSortProperty);
            CoerceValue(SortDirectionProperty);
            CoerceValue(IsFrozenProperty);
            CoerceValue(ClipProperty);
            CoerceValue(DisplayIndexProperty);
        }

        internal void ClearHeader()
        {
            _column = null;
        }

        /// <summary>
        ///     Used by the DataGridRowGenerator owner to send notifications to the cell container.
        /// </summary>
        internal EGC.ContainerTracking<EGC.DataGridColumnHeader> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Resize Gripper

        /// <summary>
        /// DependencyPropertyKey for DisplayIndex property
        /// </summary>
        private static readonly DependencyPropertyKey DisplayIndexPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "DisplayIndex",
                        typeof(int),
                        typeof(EGC.DataGridColumnHeader),
                        new FrameworkPropertyMetadata(-1, new PropertyChangedCallback(OnDisplayIndexChanged), new CoerceValueCallback(OnCoerceDisplayIndex)));

        /// <summary>
        ///     The DependencyProperty for the DisplayIndex property.
        /// </summary>
        public static readonly DependencyProperty DisplayIndexProperty = DisplayIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// The property which represents the displayindex of the  column corresponding to this header
        /// </summary>
        public int DisplayIndex
        {
            get { return (int)GetValue(DisplayIndexProperty); }
        }

        /// <summary>
        /// Coercion callback for DisplayIndex property
        /// </summary>
        private static object OnCoerceDisplayIndex(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            EGC.DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.DisplayIndex;
            }

            return -1;
        }

        /// <summary>
        /// Property changed call back for DisplayIndex property.
        /// Sets the visibility of resize grippers accordingly
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnDisplayIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            EGC.DataGridColumn column = header.Column;
            if (column != null)
            {
                EGC.DataGrid dataGrid = column.DataGridOwner;
                if (dataGrid != null)
                {
                    header.SetLeftGripperVisibility();
                    EGC.DataGridColumnHeader nextColumnHeader = dataGrid.ColumnHeaderFromDisplayIndex(header.DisplayIndex + 1);
                    if (nextColumnHeader != null)
                    {
                        nextColumnHeader.SetLeftGripperVisibility(column.CanUserResize);
                    }
                }
            }
        }

        /// <summary>
        /// Override for <see cref="System.Windows.FrameworkElement.OnApplyTemplate">FrameworkElement.OnApplyTemplate</see>
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            HookupGripperEvents();
        }

        /// <summary>
        /// Find grippers and register drag events
        ///
        /// The default style for DataGridHeader is
        /// +-------------------------------+
        /// +---------+           +---------+
        /// + Gripper +  Header   + Gripper +
        /// +         +           +         +
        /// +---------+           +---------+
        /// +-------------------------------+
        /// 
        /// The reason we have two grippers is we can't extend the right gripper to straddle the line between two 
        /// headers; the header to the right would render on top of it.
        /// We resize a column by grabbing the gripper to the right; the leftmost gripper thus adjusts the width of
        /// the column to its left.
        /// </summary>
        private void HookupGripperEvents()
        {
            UnhookGripperEvents();

            _leftGripper = GetTemplateChild(LeftHeaderGripperTemplateName) as Thumb;
            _rightGripper = GetTemplateChild(RightHeaderGripperTemplateName) as Thumb;

            if (_leftGripper != null)
            {
                _leftGripper.DragStarted += new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _leftGripper.DragDelta += new DragDeltaEventHandler(OnColumnHeaderResize);
                _leftGripper.DragCompleted += new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _leftGripper.MouseDoubleClick += new MouseButtonEventHandler(OnGripperDoubleClicked);
                SetLeftGripperVisibility();
            }

            if (_rightGripper != null)
            {
                _rightGripper.DragStarted += new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _rightGripper.DragDelta += new DragDeltaEventHandler(OnColumnHeaderResize);
                _rightGripper.DragCompleted += new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _rightGripper.MouseDoubleClick += new MouseButtonEventHandler(OnGripperDoubleClicked);
                SetRightGripperVisibility();
            }
        }

        /// <summary>
        /// Clear gripper event
        /// </summary>
        private void UnhookGripperEvents()
        {
            if (_leftGripper != null)
            {
                _leftGripper.DragStarted -= new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _leftGripper.DragDelta -= new DragDeltaEventHandler(OnColumnHeaderResize);
                _leftGripper.DragCompleted -= new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _leftGripper.MouseDoubleClick -= new MouseButtonEventHandler(OnGripperDoubleClicked);
                _leftGripper = null;
            }

            if (_rightGripper != null)
            {
                _rightGripper.DragStarted -= new DragStartedEventHandler(OnColumnHeaderGripperDragStarted);
                _rightGripper.DragDelta -= new DragDeltaEventHandler(OnColumnHeaderResize);
                _rightGripper.DragCompleted -= new DragCompletedEventHandler(OnColumnHeaderGripperDragCompleted);
                _rightGripper.MouseDoubleClick -= new MouseButtonEventHandler(OnGripperDoubleClicked);
                _rightGripper = null;
            }
        }
        
        /// <summary>
        /// Returns either this header or the one before it depending on which Gripper fired the event.
        /// </summary>
        /// <param name="sender"></param>
        private EGC.DataGridColumnHeader HeaderToResize(object gripper)
        {
            return (gripper == _rightGripper) ? this : PreviousHeader;
        }

        // Save the original widths before header resize
        private void OnColumnHeaderGripperDragStarted(object sender, DragStartedEventArgs e)
        {
            EGC.DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null)
            {
                if (header.Column != null)
                {
                    EGC.DataGrid dataGrid = header.Column.DataGridOwner;
                    if (dataGrid != null)
                    {
                        dataGrid.InternalColumns.OnColumnResizeStarted();
                    }
                }

                e.Handled = true;
            }
        }

        private void OnColumnHeaderResize(object sender, DragDeltaEventArgs e)
        {
            EGC.DataGridColumnHeader header = HeaderToResize(sender);
            if (header != null)
            {
                RecomputeColumnWidthsOnColumnResize(header, e.HorizontalChange);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Method which recomputes the widths of columns on resize of a header
        /// </summary>
        private static void RecomputeColumnWidthsOnColumnResize(EGC.DataGridColumnHeader header, double horizontalChange)
        {
            Debug.Assert(header != null, "Header should not be null");

            EGC.DataGridColumn resizingColumn = header.Column;
            if (resizingColumn == null)
            {
                return;
            }

            EGC.DataGrid dataGrid = resizingColumn.DataGridOwner;
            if (dataGrid == null)
            {
                return;
            }
         
            dataGrid.InternalColumns.RecomputeColumnWidthsOnColumnResize(resizingColumn, horizontalChange, false);
        }

        private void OnColumnHeaderGripperDragCompleted(object sender, DragCompletedEventArgs e)
        {
            EGC.DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null)
            {
                if (header.Column != null)
                {
                    EGC.DataGrid dataGrid = header.Column.DataGridOwner;
                    if (dataGrid != null)
                    {
                        dataGrid.InternalColumns.OnColumnResizeCompleted(e.Canceled);
                    }
                }

                e.Handled = true;
            }
        }

        private void OnGripperDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            EGC.DataGridColumnHeader header = HeaderToResize(sender);

            if (header != null && header.Column != null)
            {
                header.Column.Width = EGC.DataGridLength.Auto;
                e.Handled = true;
            }
        }

        private EGC.DataGridLength ColumnWidth
        {
            get { return Column != null ? Column.Width : EGC.DataGridLength.Auto; }
        }

        private double ColumnActualWidth
        {
            get { return Column != null ? Column.ActualWidth : ActualWidth; }
        }

        #endregion

        #region Property Change Notification Propagation

        /// <summary>
        ///     Notifies the Header of a property change.
        /// </summary>
        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumnHeader)d).NotifyPropertyChanged(d, e);
        }

        /// <summary>
        ///     Notification for column header-related DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumn column = d as EGC.DataGridColumn;
            if ((column != null) && (column != Column))
            {
                // This notification does not apply to this column header
                return;
            }

            if (e.Property == EGC.DataGridColumn.WidthProperty)
            {
                EGC.DataGridHelper.OnColumnWidthChanged(this, e);
            }
            else if (e.Property == EGC.DataGridColumn.HeaderProperty || e.Property == ContentProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, ContentProperty);
            }
            else if (e.Property == EGC.DataGridColumn.HeaderTemplateProperty || e.Property == ContentTemplateProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            }
            else if (e.Property == EGC.DataGridColumn.HeaderTemplateSelectorProperty || e.Property == ContentTemplateSelectorProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            }
            else if (e.Property == EGC.DataGridColumn.HeaderStringFormatProperty || e.Property == ContentStringFormatProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, ContentStringFormatProperty);
            }
            else if (e.Property == EGC.DataGrid.ColumnHeaderStyleProperty || e.Property == EGC.DataGridColumn.HeaderStyleProperty || e.Property == StyleProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, StyleProperty);
            }
            else if (e.Property == EGC.DataGrid.ColumnHeaderHeightProperty || e.Property == HeightProperty)
            {
                EGC.DataGridHelper.TransferProperty(this, HeightProperty);
            }
            else if (e.Property == EGC.DataGridColumn.DisplayIndexProperty)
            {
                CoerceValue(DisplayIndexProperty);
                TabIndex = column.DisplayIndex;
            }
            else if (e.Property == EGC.DataGrid.CanUserResizeColumnsProperty)
            {
                OnCanUserResizeColumnsChanged();
            }
            else if (e.Property == EGC.DataGridColumn.CanUserSortProperty)
            {
                CoerceValue(CanUserSortProperty);
            }
            else if (e.Property == EGC.DataGridColumn.SortDirectionProperty)
            {
                CoerceValue(SortDirectionProperty);
            }
            else if (e.Property == EGC.DataGridColumn.IsFrozenProperty)
            {
                CoerceValue(IsFrozenProperty);
            }
            else if (e.Property == EGC.DataGridColumn.CanUserResizeProperty)
            {
                OnCanUserResizeChanged();
            }
        }

        private void OnCanUserResizeColumnsChanged()
        {
            Debug.Assert(Column != null, "column can't be null if we got a notification for this property change");
            if (Column.DataGridOwner != null)
            {
                SetLeftGripperVisibility();
                SetRightGripperVisibility();
            }
        }

        private void OnCanUserResizeChanged()
        {
            Debug.Assert(Column != null, "column can't be null if we got a notification for this property change");
            EGC.DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null)
            {
                EGC.DataGridColumnHeader nextColumnHeader = dataGrid.ColumnHeaderFromDisplayIndex(DisplayIndex + 1);
                if (nextColumnHeader != null)
                {
                    nextColumnHeader.SetLeftGripperVisibility(Column.CanUserResize);
                }

                SetRightGripperVisibility();
            }
        }

        private void SetLeftGripperVisibility()
        {
            if (_leftGripper == null || Column == null)
            {
                return;
            }

            int displayIndex = DisplayIndex;
            bool canPrevColumnResize = (displayIndex > 0 ? Column.DataGridOwner.ColumnFromDisplayIndex(displayIndex - 1).CanUserResize : false);
            SetLeftGripperVisibility(canPrevColumnResize);
        }

        private void SetLeftGripperVisibility(bool canPreviousColumnResize)
        {
            if (_leftGripper == null || Column == null)
            {
                return;
            }

            EGC.DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null && dataGrid.CanUserResizeColumns && canPreviousColumnResize)
            {
                _leftGripper.Visibility = Visibility.Visible;
            }
            else
            {
                _leftGripper.Visibility = Visibility.Collapsed;
            }
        }

        private void SetRightGripperVisibility()
        {
            if (_rightGripper == null || Column == null)
            {
                return;
            }

            EGC.DataGrid dataGrid = Column.DataGridOwner;
            if (dataGrid != null && dataGrid.CanUserResizeColumns && Column.CanUserResize)
            {
                _rightGripper.Visibility = Visibility.Visible;
            }
            else
            {
                _rightGripper.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Style and Template Coercion callbacks

        /// <summary>
        ///     Coerces the Content property.  We're choosing a value between Column.Header and the Content property on ColumnHeader.
        /// </summary>
        private static object OnCoerceContent(DependencyObject d, object baseValue)
        {
            var header = d as EGC.DataGridColumnHeader;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                header, 
                baseValue, 
                ContentProperty,
                header.Column,
                EGC.DataGridColumn.HeaderProperty);
        }

        /// <summary>
        ///     Coerces the ContentTemplate property based on the templates defined on the Column.
        /// </summary>
        private static object OnCoerceContentTemplate(DependencyObject d, object baseValue)
        {
            var columnHeader = d as EGC.DataGridColumnHeader;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader, 
                baseValue, 
                ContentTemplateProperty,
                columnHeader.Column,
                EGC.DataGridColumn.HeaderTemplateProperty);
        }

        /// <summary>
        ///     Coerces the ContentTemplateSelector property based on the selector defined on the Column.
        /// </summary>
        private static object OnCoerceContentTemplateSelector(DependencyObject d, object baseValue)
        {
            var columnHeader = d as EGC.DataGridColumnHeader;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader, 
                baseValue, 
                ContentTemplateSelectorProperty,
                columnHeader.Column,
                EGC.DataGridColumn.HeaderTemplateSelectorProperty);
        }

        /// <summary>
        ///     Coerces the ContentStringFormat property based on the templates defined on the Column.
        /// </summary>
        private static object OnCoerceStringFormat(DependencyObject d, object baseValue)
        {
            var columnHeader = d as EGC.DataGridColumnHeader;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader, 
                baseValue, 
                ContentStringFormatProperty,
                columnHeader.Column,
                EGC.DataGridColumn.HeaderStringFormatProperty);
        }

        /// <summary>
        ///     Coerces the Style property based on the templates defined on the Column or DataGrid.
        /// </summary>
        private static object OnCoerceStyle(DependencyObject d, object baseValue)
        {
            var columnHeader = d as EGC.DataGridColumnHeader;
            var column = columnHeader.Column;
            var dataGrid = column != null ? column.DataGridOwner : null;

            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                columnHeader, 
                baseValue, 
                StyleProperty,
                column,
                EGC.DataGridColumn.HeaderStyleProperty,
                dataGrid,
                EGC.DataGrid.ColumnHeaderStyleProperty);
        }
        
        #endregion 

        #region Auto Sort

        /// <summary>
        /// DependencyPropertyKey for CanUserSort property
        /// </summary>
        private static readonly DependencyPropertyKey CanUserSortPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanUserSort",
                        typeof(bool),
                        typeof(EGC.DataGridColumnHeader), 
                        new FrameworkPropertyMetadata(true, null, new CoerceValueCallback(OnCoerceCanUserSort)));

        /// <summary>
        ///     The DependencyProperty for the CanUserSort property.
        /// </summary>
        public static readonly DependencyProperty CanUserSortProperty = CanUserSortPropertyKey.DependencyProperty;

        /// <summary>
        ///     CanUserSort is the flag which determines if the datagrid can be sorted based on the column of this header
        /// </summary>
        public bool CanUserSort
        {
            get { return (bool)GetValue(CanUserSortProperty); }
        }

        /// <summary>
        /// DependencyPropertyKey for SortDirection property
        /// </summary>
        private static readonly DependencyPropertyKey SortDirectionPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "SortDirection",
                        typeof(Nullable<ListSortDirection>),
                        typeof(EGC.DataGridColumnHeader),
                        new FrameworkPropertyMetadata(null, null, new CoerceValueCallback(OnCoerceSortDirection)));

        /// <summary>
        ///     The DependencyProperty for the SortDirection property.
        /// </summary>
        public static readonly DependencyProperty SortDirectionProperty = SortDirectionPropertyKey.DependencyProperty;

        /// <summary>
        /// The property for current sort direction of the column of this header
        /// </summary>
        public Nullable<ListSortDirection> SortDirection
        {
            get { return (Nullable<ListSortDirection>)GetValue(SortDirectionProperty); }
        }

        /// <summary>
        /// The override of ButtonBase.OnClick.
        /// Informs the owning datagrid to sort itself after the execution of usual button stuff
        /// </summary>
        protected override void OnClick()
        {
            if (!SuppressClickEvent)
            {
                if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                    if (peer != null)
                    {
                        peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
                    }
                }

                base.OnClick();

                if (Column != null &&
                    Column.DataGridOwner != null)
                {
                    Column.DataGridOwner.PerformSort(Column);
                }
            }
        }

        /// <summary>
        /// Coercion callback for Height property.
        /// </summary>
        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            var header = d as EGC.DataGridColumnHeader;
            EGC.DataGrid dataGrid = header.Column != null ? header.Column.DataGridOwner : null;

            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                header, 
                baseValue, 
                HeightProperty,
                dataGrid,
                EGC.DataGrid.ColumnHeaderHeightProperty);
        }

        /// <summary>
        /// Coercion callback for CanUserSort property. Checks for the value of CanUserSort on owning column
        /// and returns accordingly
        /// </summary>
        private static object OnCoerceCanUserSort(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            EGC.DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.CanUserSort;
            }

            return baseValue;
        }

        /// <summary>
        /// Coercion callback for SortDirection property
        /// </summary>
        private static object OnCoerceSortDirection(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            EGC.DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.SortDirection;
            }

            return baseValue;
        }

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new EGC.DataGridColumnHeaderAutomationPeer(this);
        }

        // Called from DataGridColumnHeaderAutomationPeer
        internal void Invoke()
        {
            this.OnClick();
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// DependencyPropertyKey for IsFrozen property
        /// </summary>
        private static readonly DependencyPropertyKey IsFrozenPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "IsFrozen",
                        typeof(bool),
                        typeof(EGC.DataGridColumnHeader),
                        new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(OnCoerceIsFrozen)));

        /// <summary>
        ///     The DependencyProperty for the IsFrozen property.
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty = IsFrozenPropertyKey.DependencyProperty;

        /// <summary>
        /// The property to determine if the column corresponding to this header is frozen or not
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
        }

        /// <summary>
        /// Coercion callback for IsFrozen property
        /// </summary>
        private static object OnCoerceIsFrozen(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            EGC.DataGridColumn column = header.Column;

            if (column != null)
            {
                return column.IsFrozen;
            }

            return baseValue;
        }

        /// <summary>
        /// Coercion call back for clip property which ensures that the header overlapping with frozen 
        /// column gets clipped appropriately.
        /// </summary>
        private static object OnCoerceClip(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumnHeader header = (EGC.DataGridColumnHeader)d;
            Geometry geometry = baseValue as Geometry;
            Geometry frozenGeometry = EGC.DataGridHelper.GetFrozenClipForCell(header);
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

        #region Column Reordering

        internal EGC.DataGridColumnHeadersPresenter ParentPresenter
        {
            get
            {
                if (_parentPresenter == null)
                {
                    _parentPresenter = ItemsControl.ItemsControlFromItemContainer(this) as EGC.DataGridColumnHeadersPresenter;
                }

                return _parentPresenter;
            }
        }

        /// <summary>
        /// Property which determines if click event has to raised or not. Used during column drag drop which could
        /// be mis-interpreted as a click
        /// </summary>
        internal bool SuppressClickEvent
        {
            get
            {
                return _suppressClickEvent;
            }

            set
            {
                _suppressClickEvent = value;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // If clickmode is hover then during the mouse move the hover events will be sent
            // all the headers in the path. To avoid that we are using a capture
            if (ClickMode == ClickMode.Hover && e.ButtonState == MouseButtonState.Pressed)
            {
                CaptureMouse();
            }

            ParentPresenter.OnHeaderMouseLeftButtonDown(e);
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ParentPresenter.OnHeaderMouseMove(e);
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (ClickMode == ClickMode.Hover && IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            ParentPresenter.OnHeaderMouseLeftButtonUp(e);
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            ParentPresenter.OnHeaderLostMouseCapture(e);
            e.Handled = true;
        }

        #endregion

        #region Helpers

        EGC.DataGridColumn IProvideDataGridColumn.Column
        {
            get
            {
                return _column;
            }
        }

        private Panel ParentPanel
        {
            get
            {
                return VisualParent as Panel;
            }
        }

        /// <summary>
        ///     Used by the resize code -- this is the header that the left gripper should be resizing.
        /// </summary>
        private EGC.DataGridColumnHeader PreviousHeader
        {
            get
            {
                // TODO: we need to be able to find the corner header too. 
                EGC.DataGridColumn column = Column;
                if (column != null && column.DataGridOwner != null)
                {
                    return column.DataGridOwner.ColumnHeaderFromDisplayIndex(column.DisplayIndex - 1);
                }

                return null;
            }
        }

        #endregion

        #region Data

        private EGC.DataGridColumn _column;
        private EGC.ContainerTracking<EGC.DataGridColumnHeader> _tracker;
        private EGC.DataGridColumnHeadersPresenter _parentPresenter;

        private Thumb _leftGripper, _rightGripper;
        private bool _suppressClickEvent;
        private const string LeftHeaderGripperTemplateName = "PART_LeftHeaderGripper";
        private const string RightHeaderGripperTemplateName = "PART_RightHeaderGripper";

        #endregion
    }
}