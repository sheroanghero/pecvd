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
using System.Windows.Media.Animation;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for PressureSwitch.xaml
    /// </summary>
    public partial class PressureSwitch : UserControl
    {
        public PressureSwitch()
        {
            InitializeComponent();
        }

      

        public static readonly DependencyProperty IsManualModeProperty = DependencyProperty.Register(
          "IsManualModePS", typeof(bool), typeof(PressureSwitch),
          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
          "OrientationPS", typeof(Orientation), typeof(PressureSwitch),
           new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CheckOkProperty = DependencyProperty.Register(
          "CheckOk", typeof(bool), typeof(PressureSwitch),
           new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ReverseValueProperty = DependencyProperty.Register(
          "ReverseValue", typeof(bool), typeof(PressureSwitch),
           new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// checkok
        /// </summary>
        public bool CheckOk
        {
            get
            {
                return (bool)GetValue(CheckOkProperty);
            }
            set
            {
                SetValue(CheckOkProperty, value);
            }
        }

        /// <summary>
        /// checkok
        /// </summary>
        public bool ReverseValue
        {
            get
            {
                return (bool)GetValue(ReverseValueProperty);
            }
            set
            {
                SetValue(ReverseValueProperty, value);
            }
        }

        /// <summary>
        /// filter oritation
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return (Orientation)this.GetValue(OrientationProperty);
            }
            set
            {
                this.SetValue(OrientationProperty, value);
            }
        }

 

        /// <summary>
        /// valve on/off status
        /// </summary>
        public bool IsManualMode
        {
            get
            {
                return (bool)GetValue(IsManualModeProperty);
            }
            set
            {
                SetValue(IsManualModeProperty, value);
            }
        }

        /// <summary>
        /// context menu property
        /// </summary>
        private ContextMenu MouseClickMenu
        {
            get;
            set;
        }
        public string PressureName { get; set; }
        /// <summary>
        /// over rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            switch (Orientation)
            {
                case Orientation.Horizontal:
                    rotateTransform.Angle = 0;
                    break;
                case Orientation.Vertical:
                    rotateTransform.Angle = 90;
                    break;
                default:
                    break;
            }

            if (ReverseValue)
                FillValue(!CheckOk);
            else
                FillValue(CheckOk);
            
        }

        private void FillValue(bool checkValue)
        {
            if (checkValue)
            {
                this.ToolTip = PressureName + "压力正常";
                psEllipse.Fill = Brushes.Green;
            }
            else
            {
                this.ToolTip = PressureName + "压力报警";
                psEllipse.Fill = Brushes.Red;
            }
        }

        /// <summary>
        /// open 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            CheckOk = true;
        }

        /// <summary>
        /// close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnOffValve(object sender, RoutedEventArgs e)
        {
            CheckOk = false;
        }

        /// <summary>
        /// mouse click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualMode)
                return;
        }
    }
}
