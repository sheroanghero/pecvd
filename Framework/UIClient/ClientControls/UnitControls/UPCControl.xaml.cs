using MECF.Framework.Common.CommonData.DeviceData;
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.Common.DeviceData;
using Caliburn.Micro;
using MECF.Framework.UI.Client.ClientControls.UnitControls;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// UPCControl.xaml 的交互逻辑
    /// </summary>
    public partial class UPCControl : UserControl
    {
        public UPCControl()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(UPCControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITUPCData), typeof(UPCControl),
                        new FrameworkPropertyMetadata(new AITUPCData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(UPCControl),
                         new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 输入值是否百分比，默认否
        /// </summary>
        public bool IsPercent { get; set; }

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
        public AITUPCData DeviceData
        {
            get
            {
                return (AITUPCData)this.GetValue(DeviceDataProperty);
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
        private UpcSettingDialogViewModel _dialog;
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color
            rectBkground1.Fill = BackColor;

            if (DeviceData != null)
            {
                rectBkground1.Stroke = DeviceData.IsError ? Brushes.Red : (DeviceData.IsWarning ? Brushes.Yellow : Brushes.DimGray);
                labelPressureValue.Content = DeviceData.PressureFeedBack.ToString("F1");

                if (_dialog != null)
                {
                    _dialog.DeviceData = DeviceData;
                    _dialog.DeviceData.InvokePropertyChanged();
                }

                labelFlowValue.Content = DeviceData.FlowFeedBack.ToString("F1");
            }
        }

        //private InputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            _dialog = new UpcSettingDialogViewModel($"MFC {DeviceData.DisplayName} Setting");
            _dialog.DeviceData = DeviceData;
            _dialog.InputSetPoint = DeviceData.PressureSetPoint.ToString("F1");

            WindowManager wm = new WindowManager();

            Window owner = Application.Current.MainWindow;
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
            //dialogBox = new InputDialogBox
            //{
            //    CommandDelegate = Execute,
            //    DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
            //    DeviceId = DeviceData.DeviceSchematicId,
            //    DefaultValue = DeviceData.DefaultValue,
            //    RealValue = DeviceData.FeedBack.ToString("F1"),
            //    SetPoint = Math.Round(DeviceData.SetPoint, 1),
            //    MaxValue = DeviceData.Scale,
            //    Unit = DeviceData.Unit,
            //};

            //dialogBox.IsPercent = IsPercent;
            //if (IsPercent)
            //    dialogBox.SetPoint = Math.Round(DeviceData.SetPoint * 100.0, 1);
            //if (AnalogOwner != null)
            //    dialogBox.Owner = AnalogOwner;
            //dialogBox.Topmost = true;
            //dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //dialogBox.FocasAll();
            //dialogBox.ShowDialog();

            //dialogBox = null;
        }

        private void Execute(double value)
        {
            //if (Command == null)
            //{
            //    InvokeClient.Instance.Service.DoOperation($"{DeviceData.UniqueName}.{AITMfcOperation.Ramp}", value, 0);
            //    return;
            //}

            //Command.Execute(new object[] { DeviceData.DeviceName, AITMfcOperation.Ramp, value });
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format("{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nPressureSetPoint：{5} {4}\r\nPressureFeedback：{6} {4}\r\nFlowFeedback：{7} {8}",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.Scale,
                        DeviceData.PressureUnit,
                        DeviceData.PressureSetPoint.ToString(DeviceData.FormatString),
                        DeviceData.PressureFeedBack.ToString(DeviceData.FormatString),
                        DeviceData.FlowFeedBack.ToString(DeviceData.FormatString),
                        DeviceData.FlowUnit);

                ToolTip = tooltipValue;
            }
        }
    }
}
