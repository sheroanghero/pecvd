using OpenSEMI.Ctrlib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenSEMI.Ctrlib.Controls
{
 
    public delegate void DragDropHandler(object sender, DragDropEventArgs e);

    public class DragDropEventArgs : EventArgs
    {
        public Slot TranferFrom { get; set; }
        public Slot TranferTo { get; set; }

        public DragDropEventArgs(Slot p_TranferFrom, Slot p_TranferTo)
        {
            this.TranferFrom = p_TranferFrom;
            this.TranferTo = p_TranferTo;
        }
    }

    [Flags]
    public enum SlotBorderStatus
    {
        MouseOver = 1,
        TransferSource = 2,
        TransferTarget = 4,
        Selected = 8,
        None = 16
    }

    public class Slot : ContentControl
    {
        public event MouseButtonEventHandler SlotMouseButtonDown;
        public event DragDropHandler WaferTransferStarted;

        static Slot()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Slot), new FrameworkPropertyMetadata(typeof(Slot)));
        }

 
        public bool IsDraggable { get; set; }
        public bool IsLeftMouseDown { get; set; }     //confirm drag source is current slot
 
        public string ViewType
        {
            get { return (string)GetValue(ViewTypeProperty); }
            set { SetValue(ViewTypeProperty, value); }
        }
        public static readonly DependencyProperty ViewTypeProperty =
            DependencyProperty.Register("ViewType", typeof(string), typeof(Slot),
            new UIPropertyMetadata("Front"));
 
 
        public int WaferStatus
        {
            get { return (int)GetValue(WaferStatusProperty); }
            set { SetValue(WaferStatusProperty, value); }
        }
        public static readonly DependencyProperty WaferStatusProperty =
            DependencyProperty.Register("WaferStatus", typeof(int), typeof(Slot),
           new UIPropertyMetadata(0, new PropertyChangedCallback(WaferStatusChangedCallBack)));
 
        public int SlotID
        {
            get { return (int)GetValue(SlotIDProperty); }
            set { SetValue(SlotIDProperty, value); }
        }
        public static readonly DependencyProperty SlotIDProperty =
            DependencyProperty.Register("SlotID", typeof(int), typeof(Slot), new UIPropertyMetadata(-1));
 
        public string ModuleID
        {
            get { return (string)GetValue(ModuleIDProperty); }
            set { SetValue(ModuleIDProperty, value); }
        }
        public static readonly DependencyProperty ModuleIDProperty =
            DependencyProperty.Register("ModuleID", typeof(string), typeof(Slot), new UIPropertyMetadata(string.Empty));
 
        public bool IsDragSource
        {
            get { return (bool)GetValue(IsDragSourceProperty); }
            set { SetValue(IsDragSourceProperty, value); }
        }
        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.Register("IsDragSource", typeof(bool), typeof(Slot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragSourcePropertyChangedCallBack)));
 
        public bool IsDropTarget
        {
            get { return (bool)GetValue(IsDropTargetProperty); }
            set { SetValue(IsDropTargetProperty, value); }
        }
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.Register("IsDropTarget", typeof(bool), typeof(Slot),
              new UIPropertyMetadata(false, new PropertyChangedCallback(IsDropTargetPropertyChangedCallBack)));
 
        public bool IsDragEnter
        {
            get { return (bool)GetValue(IsDragEnterProperty); }
            set { SetValue(IsDragEnterProperty, value); }
        }
        public static readonly DependencyProperty IsDragEnterProperty =
            DependencyProperty.Register("IsDragEnter", typeof(bool), typeof(Slot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragEnterPropertyChangedCallBack)));
 
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(Slot),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsSelectedPropertyChangedCallBack)));
 
        public bool HasWafer
        {
            get { return (bool)GetValue(HasWaferProperty); }
            set { SetValue(HasWaferProperty, value); }
        }
        public static readonly DependencyProperty HasWaferProperty =
         DependencyProperty.Register("HasWafer", typeof(bool), typeof(Slot),
        new UIPropertyMetadata(false, HasWaferChanged));
 
        public SlotBorderStatus BorderStatus
        {
            get { return (SlotBorderStatus)GetValue(BorderStatusProperty); }
            set { SetValue(BorderStatusProperty, value); }
        }
        public static readonly DependencyProperty BorderStatusProperty =
            DependencyProperty.Register("BorderStatus", typeof(SlotBorderStatus), typeof(Slot),
            new UIPropertyMetadata(SlotBorderStatus.None));
 
        public string SourceName
        {
            get { return (string)GetValue(SourceNameProperty); }
            set { SetValue(SourceNameProperty, value); }
        }
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.Register("SourceName", typeof(string), typeof(Slot), new UIPropertyMetadata(string.Empty));
 
 
        public bool CanDragDrop
        {
            get { return (bool)GetValue(CanDragDropProperty); }
            set { SetValue(CanDragDropProperty, value); }
        }
        public static readonly DependencyProperty CanDragDropProperty =
            DependencyProperty.Register("CanDragDrop", typeof(bool), typeof(Slot), new UIPropertyMetadata(true, new PropertyChangedCallback(CanDragDropPropertyChangedCallBack)));
 
        public string WaferTooltip
        {
            get { return (string)GetValue(WaferTooltipProperty); }
            set { SetValue(WaferTooltipProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipProperty =
            DependencyProperty.Register("WaferTooltip", typeof(string), typeof(Slot), new PropertyMetadata(string.Empty));
 
        public string WaferTooltipExt
        {
            get { return (string)GetValue(WaferTooltipExtProperty); }
            set { SetValue(WaferTooltipExtProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipExtProperty =
            DependencyProperty.Register("WaferTooltipExt", typeof(string), typeof(Slot), new PropertyMetadata(string.Empty));
 
        public Visibility DuplicatedVisibility
        {
            get { return (Visibility)GetValue(DuplicatedVisibilityProperty); }
            set { SetValue(DuplicatedVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DuplicatedVisibilityProperty =
            DependencyProperty.Register("DuplicatedVisibility", typeof(Visibility), typeof(Slot), new UIPropertyMetadata(Visibility.Collapsed));
 
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            DragDropStatusControl(this);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            this.IsDragEnter = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            this.IsDragEnter = false;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (MouseButtonState.Pressed == e.LeftButton)
                IsLeftMouseDown = true;
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            IsLeftMouseDown = false;

            MouseButtonEventHandler handler = SlotMouseButtonDown;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            this.BorderStatus = this.BorderStatus | SlotBorderStatus.MouseOver;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsLeftMouseDown = false;
            this.BorderStatus = this.BorderStatus & (~SlotBorderStatus.MouseOver);
        }

        private static void DragDropStatusControl(Slot p_slot)
        {
            p_slot.AllowDrop = false;
            p_slot.IsDraggable = false;

            if (p_slot.CanDragDrop)
            {
                if (!p_slot.IsDropTarget && p_slot.WaferStatus == 0)
                    p_slot.AllowDrop = true;

                if (!p_slot.IsDragSource && p_slot.WaferStatus != 0)
                    p_slot.IsDraggable = true;
            }
        }

        private static void IsDropTargetPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;

            if (m_slot.IsDropTarget)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferTarget;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferTarget); //remove

            DragDropStatusControl(m_slot);
        }

        private static void IsDragSourcePropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;

            if (m_slot.IsDragSource)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferSource;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferSource); //remove

            DragDropStatusControl(m_slot);
        }

        private static void IsSelectedPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;

            if (m_slot.IsSelected)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.Selected;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.Selected); //remove
        }

        private static void CanDragDropPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;
            DragDropStatusControl(m_slot);
        }

        private static void IsDragEnterPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;

            if (m_slot.IsDragEnter)
                m_slot.BorderStatus = m_slot.BorderStatus | SlotBorderStatus.TransferTarget;  //add
            else
                m_slot.BorderStatus = m_slot.BorderStatus & (~SlotBorderStatus.TransferTarget); //remove
        }

        private static void WaferStatusChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            Slot m_slot = d as Slot;
            m_slot.IsDragSource = false;
            m_slot.IsDropTarget = false;
            m_slot.IsDragEnter = false;
            DragDropStatusControl(m_slot);
        }

        public static void HasWaferChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Slot m_slot = d as Slot;
            if (m_slot != null)
            {
                if (m_slot.HasWafer)
                    m_slot.WaferStatus = 7;
                else
                    m_slot.WaferStatus = 0;
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && IsDraggable && IsLeftMouseDown)
            {
                DataObject data = new DataObject(typeof(Slot), this);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            if (this.AllowDrop)
            {
                try
                {
                    IDataObject data = e.Data;
                    if (data.GetDataPresent(typeof(Slot)))
                    {
                        Slot m_dragSource = (Slot)data.GetData(typeof(Slot));
                        m_dragSource.IsDragSource = true;   //source
                        this.IsDropTarget = true;           //target
                        if (WaferTransferStarted != null)
                        {
                            DragDropEventArgs m_arg = new DragDropEventArgs(m_dragSource, this);
                            WaferTransferStarted(this, m_arg);
                        }
                    }
                    else //to support another type of wafer
                    {
                        //var sourceWafer = (WaferInfo)e.Data.GetData("Object");
                        var sourceStation = e.Data.GetData("Station").ToString();
                        var sourceSlot = (int)e.Data.GetData("Slot");
                        Slot m_dragSource = new Slot() { ModuleID = sourceStation, SlotID = sourceSlot };
                        if (WaferTransferStarted != null)
                        {
                            DragDropEventArgs m_arg = new DragDropEventArgs(m_dragSource, this);
                            WaferTransferStarted(this, m_arg);
                        }
                    }
                }
                catch
                {
                }
            }
        }
 
        public bool IsValidSlot()
        {
            if (this.ModuleID.Length >0 && this.SlotID >= 0)
                return true;
            else
                return false;
        }

        public bool IsSameSlot(Slot slot)
        {
            if (slot.IsValidSlot() && this.IsValidSlot())
            {
                if (this.ModuleID == slot.ModuleID && this.SlotID == slot.SlotID)
                    return true;
            }
            return false;
        }
        public void ClearDragDropStatus()
        {
            this.IsDragEnter = false;
            this.IsDragSource = false;
            this.IsDropTarget = false;
        }
 

    }
}
