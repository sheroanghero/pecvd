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
    public class Wafer : Control
    {
        static Wafer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Wafer), new FrameworkPropertyMetadata(typeof(Wafer)));
        }

        #region ViewType (DependencyProperty)Front/Top
        public string ViewType
        {
            get { return (string)GetValue(ViewTypeProperty); }
            set { SetValue(ViewTypeProperty, value); }
        }
        public static readonly DependencyProperty ViewTypeProperty =
            DependencyProperty.Register("ViewType", typeof(string), typeof(Wafer),
           new UIPropertyMetadata("Front"));
        #endregion

        #region int (DependencyProperty)
        /// <summary>
        /// refer to enum WaferStatus in CommonEnums.cs
        /// </summary>
        public int WaferStatus
        {
            get { return (int)GetValue(WaferStatusProperty); }
            set { SetValue(WaferStatusProperty, value); }
        }
        public static readonly DependencyProperty WaferStatusProperty =
            DependencyProperty.Register("WaferStatus", typeof(int), typeof(Wafer),
           new UIPropertyMetadata(0));
        #endregion

        #region SlotID (DependencyProperty)
        public int SlotID
        {
            get { return (int)GetValue(SlotIDProperty); }
            set { SetValue(SlotIDProperty, value); }
        }
        public static readonly DependencyProperty SlotIDProperty =
            DependencyProperty.Register("SlotID", typeof(int), typeof(Wafer),
            new UIPropertyMetadata(0));
        #endregion

        #region ModuleID (DependencyProperty)
        public string ModuleID
        {
            get { return (string)GetValue(ModuleIDProperty); }
            set { SetValue(ModuleIDProperty, value); }
        }
        public static readonly DependencyProperty ModuleIDProperty =
            DependencyProperty.Register("ModuleID", typeof(string), typeof(Wafer), new UIPropertyMetadata(string.Empty));
        #endregion

        #region WaferTooltip (DependencyProperty)
        public string WaferTooltip
        {
            get { return (string)GetValue(WaferTooltipProperty); }
            set { SetValue(WaferTooltipProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipProperty =
            DependencyProperty.Register("WaferTooltip", typeof(string), typeof(Wafer), new PropertyMetadata(string.Empty));
        #endregion

        #region WaferTooltipExt (DependencyProperty)
        public string WaferTooltipExt
        {
            get { return (string)GetValue(WaferTooltipExtProperty); }
            set { SetValue(WaferTooltipExtProperty, value); }
        }
        public static readonly DependencyProperty WaferTooltipExtProperty =
            DependencyProperty.Register("WaferTooltipExt", typeof(string), typeof(Wafer), new PropertyMetadata(string.Empty));
        #endregion

        #region SourceName (DependencyProperty)
        public string SourceName
        {
            get { return (string)GetValue(SourceNameProperty); }
            set { SetValue(SourceNameProperty, value); }
        }
        public static readonly DependencyProperty SourceNameProperty =
            DependencyProperty.Register("SourceName", typeof(string), typeof(Wafer), new UIPropertyMetadata(string.Empty));
        #endregion

        #region VisiblilityDuplicated (DependencyProperty)
        public Visibility DuplicatedVisibility
        {
            get { return (Visibility)GetValue(DuplicatedVisibilityProperty); }
            set { SetValue(DuplicatedVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DuplicatedVisibilityProperty =
            DependencyProperty.Register("DuplicatedVisibility", typeof(Visibility), typeof(Wafer), new UIPropertyMetadata(Visibility.Collapsed));
        #endregion
    }
}
