using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.Common.DeviceData;
using Caliburn.Micro;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// Interaction logic for AnalogControl.xaml
    /// </summary>
    public partial class MfcControl : UserControl
    {
        public MfcControl()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(MfcControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITMfcData), typeof(MfcControl),
                        new FrameworkPropertyMetadata(new AITMfcData(), FrameworkPropertyMetadataOptions.AffectsRender,
                        new PropertyChangedCallback(OnDeviceDataChanged)));
 
        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(MfcControl),
                         new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HideDialogProperty = DependencyProperty.Register(
                    "HideDialog", typeof(bool), typeof(MfcControl),
                    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        private static void OnDeviceDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
 

        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }

        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITMfcData DeviceData
        {
            get
            {
                return (AITMfcData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public Brush BackColor
        {
            get
            {
                return (Brush)this.GetValue(BackColorProperty);
            }
            set
            {
                this.SetValue(BackColorProperty, value);
            }
        }
 
        public bool HideDialog
        {
            get
            {
                return (bool)this.GetValue(HideDialogProperty);
            }
            set
            {
                this.SetValue(HideDialogProperty, value);
            }
        }

        //
        /// <summary>
        /// MFC 控件的显示逻辑：
        ///
        /// 下面：设定值SetPoint
        /// 
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color
            rectBkground.Fill = BackColor;

            if (DeviceData != null)
            {
                rectBkground.Stroke = DeviceData.IsError ? Brushes.Red : (DeviceData.IsWarning ? Brushes.Yellow : Brushes.DimGray);
                labelValue.Content = DeviceData.FeedBack.ToString("F1");

                if (_dialog != null)
                {
                    _dialog.DeviceData = DeviceData;
                    _dialog.DeviceData.InvokePropertyChanged();
                }

                labelSetPoint.Content = DeviceData.SetPoint.ToString("F1");
            }
        }

        private MfcSettingDialogViewModel _dialog;
 
        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            if (HideDialog)
                return;

            _dialog = new MfcSettingDialogViewModel($"MFC {DeviceData.DisplayName} Setting");
            _dialog.DeviceData = DeviceData;
            _dialog.InputSetPoint = DeviceData.SetPoint.ToString("F1");

            WindowManager wm = new WindowManager();

            Window owner  = Application.Current.MainWindow;
            if (owner != null)
            {
                Mouse.Capture(owner);
                Point pointToWindow = Mouse.GetPosition(owner);
                Point pointToScreen = owner.PointToScreen(pointToWindow);
                pointToScreen.X = pointToScreen.X + 50;
                pointToScreen.Y = pointToScreen.Y - 150;
                Mouse.Capture(null);

                wm.ShowDialog(_dialog, pointToScreen);
            }
            else
            {
                wm.ShowDialog(_dialog);
            }
        }
 

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    $"{DeviceData.Type}：{DeviceData.DisplayName}\r\n\r\nID：{DeviceData.DeviceSchematicId}\r\nScale：{DeviceData.Scale} {DeviceData.Unit}\r\nSetPoint：{DeviceData.SetPoint} {DeviceData.Unit} \r\nFeedback：{DeviceData.FeedBack} {DeviceData.Unit}";
 
                ToolTip = tooltipValue;
            }
        }
    }
}
