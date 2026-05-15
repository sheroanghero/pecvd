using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// <summary>
    /// Interaction logic for FlowPipe.xaml
    /// </summary>
    public partial class FlowPipeV2 : UserControl
    {
        public FlowPipeV2()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsFlowingProperty = DependencyProperty.Register(
            "IsFlowing", typeof (bool), typeof (FlowPipeV2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsSlowFlowingProperty = DependencyProperty.Register(
            "IsSlowFlowing", typeof (bool), typeof (FlowPipeV2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveOpenOrientationProperty = DependencyProperty.Register(
            "FlowOrientation", typeof (LineOrientation), typeof (FlowPipeV2),
            new FrameworkPropertyMetadata(LineOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsReverseProperty = DependencyProperty.Register(
            "IsReverse", typeof(bool), typeof(FlowPipeV2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasTypeProperty = DependencyProperty.Register(
            "GasType", typeof (GasTypeEnum), typeof (FlowPipeV2),
            new FrameworkPropertyMetadata(GasTypeEnum.CarrierGas, FrameworkPropertyMetadataOptions.AffectsRender));

        public LineOrientation FlowOrientation
        {
            get { return (LineOrientation) GetValue(ValveOpenOrientationProperty); }
            set { SetValue(ValveOpenOrientationProperty, value); }
        }


        /// <summary>
        /// When 'IsFlowing' is true, the gas will flow; otherwise, there is no gas flow.
        /// default fast valve
        /// </summary>
        public bool IsFlowing
        {
            get { return (bool) this.GetValue(IsFlowingProperty); }
            set { this.SetValue(IsFlowingProperty, value); }
        }

        public bool IsSlowFlowing
        {
            get { return (bool) this.GetValue(IsSlowFlowingProperty); }
            set { this.SetValue(IsSlowFlowingProperty, value); }
        }

        public GasTypeEnum GasType
        {
            get { return (GasTypeEnum) this.GetValue(GasTypeProperty); }
            set { this.SetValue(GasTypeProperty, value); }
        }

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

        private static readonly SolidColorBrush CarrierGasBrush = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush MOBrush = new SolidColorBrush(Colors.DarkRed);
        private static readonly SolidColorBrush N2Brush = new SolidColorBrush(Colors.LightYellow);
        private static readonly SolidColorBrush HydrideBrush = new SolidColorBrush(Colors.Navy);
        private static readonly SolidColorBrush ExhaustBrush = new SolidColorBrush(Colors.Gray);

        private Brush _pathStrokeBrush = CarrierGasBrush;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (FlowOrientation == LineOrientation.Vertical)
                rotateTransform.Angle = 90;

            path1.Visibility = IsFlowing ? Visibility.Visible : Visibility.Hidden;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.Visibility = IsFlowing ? Visibility.Visible : Visibility.Hidden;
            }
 
            switch (GasType)
            {
                    //case GasTypeEnum.Fast:
                    //    _pathStrokeBrush = CarrierGasBrush;
                    //    path1.StrokeThickness = 5;
                    //    break;
                    //case GasTypeEnum.Slow:
                    //    _pathStrokeBrush = CarrierGasBrush;
                    //    path1.StrokeThickness = 3;
                    //    break;
                case GasTypeEnum.CarrierGas:
                    _pathStrokeBrush = CarrierGasBrush;
                    break;
                case GasTypeEnum.MO:
                    _pathStrokeBrush = MOBrush;
                    break;
                case GasTypeEnum.Hydride:
                    _pathStrokeBrush = HydrideBrush;
                    break;
                case GasTypeEnum.Exhaust:
                    _pathStrokeBrush = ExhaustBrush;
                    break;
                case GasTypeEnum.N2:
                    _pathStrokeBrush = N2Brush;
                    break;
            }

            if (_pathStrokeBrush != path1.Stroke)
                path1.Stroke = _pathStrokeBrush;

            if (IsSlowFlowing && !IsFlowing)
                path1.StrokeThickness = 3;
            else if (IsFlowing)
                path1.StrokeThickness = 5;
        }
    }
}
