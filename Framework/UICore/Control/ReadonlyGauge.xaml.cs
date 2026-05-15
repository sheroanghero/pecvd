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
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    public enum ControlTypeEnum
    {
        MFC,
        PC,
        PT,
        CAL
    }
    /// <summary>
    /// Interaction logic for ReadonlyMFC.xaml
    /// </summary>
    public partial class ReadonlyGauge : UserControl
    {
        public ReadonlyGauge()
        {
            InitializeComponent();
        }

        /// <summary>
        /// define dependency property
        /// </summary>
        public static readonly DependencyProperty FillBackgroundProperty = DependencyProperty.Register(
        "FillBackground", typeof(Brush), typeof(ReadonlyGauge),
        new FrameworkPropertyMetadata(Brushes.CadetBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ActualValueProperty = DependencyProperty.Register(
          "ActualValue", typeof(ReadonlyGaugeDataItem), typeof(ReadonlyGauge),
          new FrameworkPropertyMetadata(default(ReadonlyGaugeDataItem), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ActualAngleProperty = DependencyProperty.Register(
        "ActualAngle", typeof(int), typeof(ReadonlyGauge),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ControlTypeProperty = DependencyProperty.Register(
            "ControlType", typeof(ControlTypeEnum), typeof(ReadonlyGauge),
            new FrameworkPropertyMetadata(ControlTypeEnum.MFC, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty NumFontSizeProperty = DependencyProperty.Register(
         "NumFontSize", typeof(int), typeof(ReadonlyGauge),
       new FrameworkPropertyMetadata(9, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty E_notationInvaildProperty = DependencyProperty.Register(
        "E_notationInvaild", typeof(bool), typeof(ReadonlyGauge),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush FillBackColor
        {
            get
            {
                return (Brush)this.GetValue(FillBackgroundProperty);
            }
            set
            {
                this.SetValue(FillBackgroundProperty, value);
            }
        }

        public ReadonlyGaugeDataItem ActualValue
        {
            get
            {
                return (ReadonlyGaugeDataItem)this.GetValue(ActualValueProperty);
            }
            set
            {
                this.SetValue(ActualValueProperty, value);
            }
        }

        public int ActualAngle
        {
            get
            {
                return (int)this.GetValue(ActualAngleProperty);
            }
            set
            {
                this.SetValue(ActualAngleProperty, value);
            }
        }


        public ControlTypeEnum ControlType
        {
            get
            {
                return (ControlTypeEnum)this.GetValue(ControlTypeProperty);
            }
            set
            {
                this.SetValue(ControlTypeProperty, value);
            }
        }

        public int NumFontSize
        {
            get
            {
                return (int)this.GetValue(NumFontSizeProperty);
            }
            set
            {
                this.SetValue(NumFontSizeProperty, value);
            }
        }

        public bool E_notationInvaild
        {
            get
            {
                return (bool)this.GetValue(E_notationInvaildProperty);
            }
            set
            {
                this.SetValue(FontSizeProperty, value);
            }
        }

        /// <summary>
        /// override rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            rectBkground.Fill = FillBackColor;

            if (ActualValue != null)
            {
                this.controlAngle.Angle = ActualAngle;
                if (E_notationInvaild)
                controlValue.Content = ActualValue.Value.ToString("#.##E+00");
                else
                    controlValue.Content = ActualValue.Value.ToString("0.0");

                if (!string.IsNullOrEmpty(ActualValue + ""))
                {
                    this.ToolTip = string.Format("{0}:{1}", ControlType.ToString(), Tag);
                }

                controlValue.FontSize = NumFontSize;
            }
            else
            {
                controlValue.Content = "0.00";
            }
        }
    }
}
