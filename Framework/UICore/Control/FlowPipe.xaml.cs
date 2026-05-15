using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Windows.Media.Animation;
using Aitex.Core.UI.ControlDataContext;


namespace Aitex.Core.UI.Control
{
    public enum GasTypeEnum
    {
        MO,
        CarrierGas,
        N2,

        Hydride,
        Exhaust,
        Fast,
        Slow
    }

    /// <summary>
    /// Interaction logic for FlowPipe.xaml
    /// </summary>
    public partial class FlowPipe : UserControl
    {
        public FlowPipe()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsFlowingProperty = DependencyProperty.Register(
            "IsFlowing", typeof(GasValveDataItem), typeof(FlowPipe),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsSlowFlowingProperty = DependencyProperty.Register(
          "IsSlowFlowing", typeof(GasValveDataItem), typeof(FlowPipe),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsReverseProperty = DependencyProperty.Register(
            "IsReverse", typeof(bool), typeof(FlowPipe),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasTypeProperty = DependencyProperty.Register(
            "GasType", typeof(GasTypeEnum), typeof(FlowPipe),
            new FrameworkPropertyMetadata(GasTypeEnum.CarrierGas, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When 'IsFlowing' is true, the gas will flow; otherwise, there is no gas flow.
        /// default fast valve
        /// </summary>
        public GasValveDataItem IsFlowing
        {
            get
            {
                return (GasValveDataItem)this.GetValue(IsFlowingProperty);
            }
            set
            {
                this.SetValue(IsFlowingProperty, value);
            }
        }
        public GasValveDataItem IsSlowFlowing
        {
            get
            {
                return (GasValveDataItem)this.GetValue(IsSlowFlowingProperty);
            }
            set
            {
                this.SetValue(IsSlowFlowingProperty, value);
            }
        }
        public GasTypeEnum GasType
        {
            get
            {
                return (GasTypeEnum)this.GetValue(GasTypeProperty);
            }
            set
            {
                this.SetValue(GasTypeProperty, value);
            }
        }

        /// <summary>
        /// When 'IsReverse' is true, the gas flows from right to left, or bottom to top.
        /// When 'IsReverse' is false, the gas flows from left to right, or top to bottom.
        /// </summary>
        public bool IsReverse
        {
            get
            {
                return (bool)this.GetValue(IsReverseProperty);
            }
            set
            {
                this.SetValue(IsReverseProperty, value);
            }
        }



        public bool IsVertical { get; set; }
        public bool IsShowFlowing { get { return (IsFlowing != null && IsFlowing.Feedback) || (IsSlowFlowing != null && IsSlowFlowing.Feedback); } }

        static readonly SolidColorBrush CarrierGasBrush = new SolidColorBrush(Colors.Green);
        static readonly SolidColorBrush MOBrush = new SolidColorBrush(Colors.DarkRed);
        static readonly SolidColorBrush HydrideBrush = new SolidColorBrush(Colors.Navy);
        static readonly SolidColorBrush ExhaustBrush = new SolidColorBrush(Colors.Gray);

        Brush pathStrokeBrush = CarrierGasBrush;
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (IsVertical)
                rotateTransform.Angle = 90;
            path1.Visibility = (IsFlowing != null && IsFlowing.Feedback) || (IsSlowFlowing != null && IsSlowFlowing.Feedback) ? Visibility.Visible : Visibility.Hidden;

            switch (GasType)
            {
                //case GasTypeEnum.Fast:
                //    pathStrokeBrush = CarrierGasBrush;
                //    path1.StrokeThickness = 5;
                //    break;
                //case GasTypeEnum.Slow:
                //    pathStrokeBrush = CarrierGasBrush;
                //    path1.StrokeThickness = 3;
                //    break;
                case GasTypeEnum.CarrierGas:
                    pathStrokeBrush = CarrierGasBrush;
                    break;
                case GasTypeEnum.MO:
                    pathStrokeBrush = MOBrush;
                    break;
                case GasTypeEnum.Hydride:
                    pathStrokeBrush = HydrideBrush;
                    break;
                case GasTypeEnum.Exhaust:
                    pathStrokeBrush = ExhaustBrush;
                    break;
            }

            if (pathStrokeBrush != path1.Stroke)
                path1.Stroke = pathStrokeBrush;

            if ((IsSlowFlowing != null && IsSlowFlowing.Feedback) && !(IsFlowing != null && IsFlowing.Feedback))
                path1.StrokeThickness = 3;
            else if ((IsFlowing!=null &&IsFlowing.Feedback))
                path1.StrokeThickness = 5;

        }
    }
}
