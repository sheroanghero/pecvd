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
    public delegate void CarrierDragDropHandler(object sender, CarrierDragDropEventArgs e);

    public class CarrierDragDropEventArgs : EventArgs
    {
        public CarrierContentControl TranferFrom { get; set; }
        public CarrierContentControl TranferTo { get; set; }

        public CarrierDragDropEventArgs(CarrierContentControl p_TranferFrom, CarrierContentControl p_TranferTo)
        {
            this.TranferFrom = p_TranferFrom;
            this.TranferTo = p_TranferTo;
        }
    }

    [Flags]
    public enum CarrierBorderStatus
    {
        MouseOver = 1,
        TransferSource = 2,
        TransferTarget = 4,
        Selected = 8,
        None = 16
    }

    public class CarrierContentControl : ContentControl
    {
        public event MouseButtonEventHandler StageMouseButtonDown;
        public event CarrierDragDropHandler WaferTransferStarted;

        static CarrierContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CarrierContentControl), new FrameworkPropertyMetadata(typeof(CarrierContentControl)));
        }

 
        public bool IsDraggable { get; set; }
        public bool IsLeftMouseDown { get; set; }     //confirm drag source is current Stage
 
        public string ViewType
        {
            get { return (string)GetValue(ViewTypeProperty); }
            set { SetValue(ViewTypeProperty, value); }
        }
        public static readonly DependencyProperty ViewTypeProperty =
            DependencyProperty.Register("ViewType", typeof(string), typeof(CarrierContentControl),
            new UIPropertyMetadata("Front"));
 
 
        public int WaferStatus
        {
            get { return (int)GetValue(WaferStatusProperty); }
            set { SetValue(WaferStatusProperty, value); }
        }
        public static readonly DependencyProperty WaferStatusProperty =
            DependencyProperty.Register("WaferStatus", typeof(int), typeof(CarrierContentControl),
           new UIPropertyMetadata(0, new PropertyChangedCallback(WaferStatusChangedCallBack)));
 
        public int CarrierID
        {
            get { return (int)GetValue(CarrierIDProperty); }
            set { SetValue(CarrierIDProperty, value); }
        }
        public static readonly DependencyProperty CarrierIDProperty =
            DependencyProperty.Register("CarrierID", typeof(int), typeof(CarrierContentControl), new UIPropertyMetadata(-1));
 
        public string ModuleID
        {
            get { return (string)GetValue(ModuleIDProperty); }
            set { SetValue(ModuleIDProperty, value); }
        }
        public static readonly DependencyProperty ModuleIDProperty =
            DependencyProperty.Register("ModuleID", typeof(string), typeof(CarrierContentControl), new UIPropertyMetadata(string.Empty));
 
        public bool IsDragSource
        {
            get { return (bool)GetValue(IsDragSourceProperty); }
            set { SetValue(IsDragSourceProperty, value); }
        }
        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.Register("IsDragSource", typeof(bool), typeof(CarrierContentControl),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragSourcePropertyChangedCallBack)));
 
        public bool IsDropTarget
        {
            get { return (bool)GetValue(IsDropTargetProperty); }
            set { SetValue(IsDropTargetProperty, value); }
        }
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.Register("IsDropTarget", typeof(bool), typeof(CarrierContentControl),
              new UIPropertyMetadata(false, new PropertyChangedCallback(IsDropTargetPropertyChangedCallBack)));
 
        public bool IsDragEnter
        {
            get { return (bool)GetValue(IsDragEnterProperty); }
            set { SetValue(IsDragEnterProperty, value); }
        }
        public static readonly DependencyProperty IsDragEnterProperty =
            DependencyProperty.Register("IsDragEnter", typeof(bool), typeof(CarrierContentControl),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsDragEnterPropertyChangedCallBack)));
 
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(CarrierContentControl),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsSelectedPropertyChangedCallBack)));
 
        public bool HasWafer
        {
            get { return (bool)GetValue(HasWaferProperty); }
            set { SetValue(HasWaferProperty, value); }
        }
        public static readonly DependencyProperty HasWaferProperty =
         DependencyProperty.Register("HasWafer", typeof(bool), typeof(CarrierContentControl),
        new UIPropertyMetadata(false, HasWaferChanged));
 
        public CarrierBorderStatus BorderStatus
        {
            get { return (CarrierBorderStatus)GetValue(BorderStatusProperty); }
            set { SetValue(BorderStatusProperty, value); }
        }
        public static readonly DependencyProperty BorderStatusProperty =
            DependencyProperty.Register("BorderStatus", typeof(CarrierBorderStatus), typeof(CarrierContentControl),
            new UIPropertyMetadata(CarrierBorderStatus.None));
 
        public string SourceName
        {
            get { return (string)GetValue(SourceNameProperty); }
            set { SetValue(SourceNameProperty, value); }
        }
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.Register("SourceName", typeof(string), typeof(CarrierContentControl), new UIPropertyMetadata(string.Empty));
 
 
        public bool CanDragDrop
        {
            get { return (bool)GetValue(CanDragDropProperty); }
            set { SetValue(CanDragDropProperty, value); }
        }
        public static readonly DependencyProperty CanDragDropProperty =
            DependencyProperty.Register("CanDragDrop", typeof(bool), typeof(CarrierContentControl), new UIPropertyMetadata(true, new PropertyChangedCallback(CanDragDropPropertyChangedCallBack)));
 
        public string WaferTooltip
        {
            get { return (string)GetValue(WaferTooltipProperty); }
            set { SetValue(WaferTooltipProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipProperty =
            DependencyProperty.Register("WaferTooltip", typeof(string), typeof(CarrierContentControl), new PropertyMetadata(string.Empty));
 
        public string WaferTooltipExt
        {
            get { return (string)GetValue(WaferTooltipExtProperty); }
            set { SetValue(WaferTooltipExtProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipExtProperty =
            DependencyProperty.Register("WaferTooltipExt", typeof(string), typeof(CarrierContentControl), new PropertyMetadata(string.Empty));
  
        public string SeasoningWaferType
        {
            get { return (string)GetValue(SeasoningWaferTypeProperty); }
            set { SetValue(SeasoningWaferTypeProperty, value); }
        }
        public static readonly DependencyProperty SeasoningWaferTypeProperty =
            DependencyProperty.Register("SeasoningWaferType", typeof(string), typeof(CarrierContentControl), new PropertyMetadata(string.Empty));
 
        public Visibility DuplicatedVisibility
        {
            get { return (Visibility)GetValue(DuplicatedVisibilityProperty); }
            set { SetValue(DuplicatedVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DuplicatedVisibilityProperty =
            DependencyProperty.Register("DuplicatedVisibility", typeof(Visibility), typeof(CarrierContentControl), new UIPropertyMetadata(Visibility.Collapsed));
 
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

            MouseButtonEventHandler handler = StageMouseButtonDown;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            this.BorderStatus = this.BorderStatus | CarrierBorderStatus.MouseOver;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            IsLeftMouseDown = false;
            this.BorderStatus = this.BorderStatus & (~CarrierBorderStatus.MouseOver);
        }

        private static void DragDropStatusControl(CarrierContentControl p_Stage)
        {
            p_Stage.AllowDrop = false;
            p_Stage.IsDraggable = false;

            if (p_Stage.CanDragDrop)
            {
                if (!p_Stage.IsDropTarget && p_Stage.WaferStatus == 0)
                    p_Stage.AllowDrop = true;

                if (!p_Stage.IsDragSource && p_Stage.WaferStatus != 0)
                    p_Stage.IsDraggable = true;
            }
        }

        private static void IsDropTargetPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;

            if (m_Stage.IsDropTarget)
                m_Stage.BorderStatus = m_Stage.BorderStatus | CarrierBorderStatus.TransferTarget;  //add
            else
                m_Stage.BorderStatus = m_Stage.BorderStatus & (~CarrierBorderStatus.TransferTarget); //remove

            DragDropStatusControl(m_Stage);
        }

        private static void IsDragSourcePropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;

            if (m_Stage.IsDragSource)
                m_Stage.BorderStatus = m_Stage.BorderStatus | CarrierBorderStatus.TransferSource;  //add
            else
                m_Stage.BorderStatus = m_Stage.BorderStatus & (~CarrierBorderStatus.TransferSource); //remove

            DragDropStatusControl(m_Stage);
        }

        private static void IsSelectedPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;

            if (m_Stage.IsSelected)
                m_Stage.BorderStatus = m_Stage.BorderStatus | CarrierBorderStatus.Selected;  //add
            else
                m_Stage.BorderStatus = m_Stage.BorderStatus & (~CarrierBorderStatus.Selected); //remove
        }

        private static void CanDragDropPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;
            DragDropStatusControl(m_Stage);
        }

        private static void IsDragEnterPropertyChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;

            if (m_Stage.IsDragEnter)
                m_Stage.BorderStatus = m_Stage.BorderStatus | CarrierBorderStatus.TransferTarget;  //add
            else
                m_Stage.BorderStatus = m_Stage.BorderStatus & (~CarrierBorderStatus.TransferTarget); //remove
        }

        private static void WaferStatusChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;
            m_Stage.IsDragSource = false;
            m_Stage.IsDropTarget = false;
            m_Stage.IsDragEnter = false;
            DragDropStatusControl(m_Stage);
        }

        public static void HasWaferChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CarrierContentControl m_Stage = d as CarrierContentControl;
            if (m_Stage != null)
            {
                if (m_Stage.HasWafer)
                    m_Stage.WaferStatus = 7;
                else
                    m_Stage.WaferStatus = 0;
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && IsDraggable && IsLeftMouseDown)
            {
                DataObject data = new DataObject(typeof(CarrierContentControl), this);
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
                    if (data.GetDataPresent(typeof(CarrierContentControl)))
                    {
                        CarrierContentControl m_dragSource = (CarrierContentControl)data.GetData(typeof(CarrierContentControl));
                        m_dragSource.IsDragSource = true;   //source
                        this.IsDropTarget = true;           //target
                        if (WaferTransferStarted != null)
                        {
                            CarrierDragDropEventArgs m_arg = new CarrierDragDropEventArgs(m_dragSource, this);
                            WaferTransferStarted(this, m_arg);
                        }
                    }
                    else //to support another type of wafer
                    {
                        //var sourceWafer = (WaferInfo)e.Data.GetData("Object");
                        var sourceStation = e.Data.GetData("Station").ToString();
                        var sourceStage = (int)e.Data.GetData("Stage");
                        CarrierContentControl m_dragSource = new CarrierContentControl() { ModuleID = sourceStation, CarrierID = sourceStage };
                        if (WaferTransferStarted != null)
                        {
                            CarrierDragDropEventArgs m_arg = new CarrierDragDropEventArgs(m_dragSource, this);
                            WaferTransferStarted(this, m_arg);
                        }
                    }
                }
                catch
                {
                }
            }
        }
 
        public bool IsValidStage()
        {
            if (this.ModuleID.Length >0 && this.CarrierID >= 0)
                return true;
            else
                return false;
        }

        public bool IsSameStage(CarrierContentControl Stage)
        {
            if (Stage.IsValidStage() && this.IsValidStage())
            {
                if (this.ModuleID == Stage.ModuleID && this.CarrierID == Stage.CarrierID)
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
