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
using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.Control;
using Aitex.Core.Util;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// AITThrottleValve.xaml 的交互逻辑
    /// </summary>
    public partial class TVControl : UserControl
    {
        public TVControl()
        {
            InitializeComponent();
        }
      
        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(TVControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITThrottleValveData), typeof(TVControl),
                        new FrameworkPropertyMetadata(new AITThrottleValveData(), FrameworkPropertyMetadataOptions.AffectsRender));

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
        RD_TRIG rdTrig = new RD_TRIG();
        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITThrottleValveData DeviceData
        {
            get
            {
                return (AITThrottleValveData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }
        public Visibility MenuVisibility
        {
            get { return (Visibility)this.GetValue(MenuVisibilityProperty); }
            set { this.SetValue(MenuVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuVisibilityProperty =
            DependencyProperty.Register("MenuVisibility", typeof(Visibility), typeof(TVControl),
                new FrameworkPropertyMetadata(Visibility.Visible));

        private TVSettingDialogViewModel _dialog;
        private AITThrottleValveInputDialogBox _dialogBox;
        public Window AnalogOwner { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                if (DeviceData.State == 0)
                {
                    if (GrdThrottleValve.ContextMenu.Items != null && GrdThrottleValve.ContextMenu.Items.Count == 2)
                    {
                        ((MenuItem)GrdThrottleValve.ContextMenu.Items[0]).IsEnabled = true;
                        ((MenuItem)GrdThrottleValve.ContextMenu.Items[1]).IsEnabled = false;
                    }
                }
                else
                {
                    if (GrdThrottleValve.ContextMenu.Items != null && GrdThrottleValve.ContextMenu.Items.Count == 2)
                    {
                        ((MenuItem)GrdThrottleValve.ContextMenu.Items[0]).IsEnabled = false;
                        ((MenuItem)GrdThrottleValve.ContextMenu.Items[1]).IsEnabled = true;
                    }
                }
                if (DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl)
                {
                    //rotateTransform.Angle = DeviceData.PressureFeedback * 90.0 / DeviceData.MaxValuePressure;

                    rectPosition.Stroke = Brushes.Gray;
                    rectPressure.Stroke = Brushes.LightCyan;
                }
                else if (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl)
                {
                    

                    rectPosition.Stroke = Brushes.LightCyan;
                    rectPressure.Stroke = Brushes.Gray;
                }
                else
                {
                    rectPosition.Stroke = Brushes.Gray;
                    rectPressure.Stroke = Brushes.Gray;
                }

                rotateTransform.Angle = DeviceData.PositionFeedback * 180.0 / DeviceData.MaxValuePosition;

                PositionValue.Content = DeviceData.PositionFeedback.ToString("F1");
                PositionValueSet.Content = DeviceData.PositionSetPoint.ToString("F1");
                //PositionUnit.Content = "%";
                PressureValue.Content = DeviceData.PressureFeedback.ToString("F1") + " mTorr";
                PressureValueSet.Content = DeviceData.PressureSetPoint.ToString("F1") + " mTorr";
                //PressureUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPressure) ? DeviceData.UnitPressure : "mTorr";

                if (_dialog != null)
                {
                    _dialog.DeviceData = DeviceData;
                }
                if (_dialogBox != null)
                {
                    //_dialogBox.IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl;
                    //_dialogBox.IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl;

                    //_dialogBox.SetPointPosition = DeviceData.PositionSetPoint;
                    //_dialogBox.SetPointPressure = DeviceData.PressureSetPoint;
                }
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            //if (DeviceData != null)
            //{
            //    string tooltipValue =
            //        string.Format(Application.Current.Resources["GlobalLableThrottleValveToolTip"].ToString(),
            //            DeviceData.Type,
            //            DeviceData.DisplayName,
            //            DeviceData.DeviceSchematicId,
            //            DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl ? "Pressure" : (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl ? "Position" : ""),

            //            DeviceData.PositionFeedback.ToString("F1"),
            //            DeviceData.PressureFeedback.ToString("F1"));

            //    ToolTip = tooltipValue;
            //}
        }



        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;


            //_dialog = new TVSettingDialogViewModel($"{DeviceData.DisplayName} Setting");
            //_dialog.DeviceData = DeviceData;
            //_dialog.InputSetPointPosition = DeviceData.PositionSetPoint.ToString("F1");
            //_dialog.InputSetPointPressure = DeviceData.PressureSetPoint.ToString("F1");

            //WindowManager wm = new WindowManager();

            //Window owner = Application.Current.MainWindow;
            //if (owner != null)
            //{
            //    Mouse.Capture(owner);
            //    Point pointToWindow = Mouse.GetPosition(owner);
            //    Point pointToScreen = owner.PointToScreen(pointToWindow);
            //    pointToScreen.X = pointToScreen.X + 50;
            //    pointToScreen.Y = pointToScreen.Y - 150;
            //    Mouse.Capture(null);

            //    wm.ShowDialog(_dialog, pointToScreen);
            //}
            //else
            //{
            //    wm.ShowDialog(_dialog);
            //}

            _dialogBox = new AITThrottleValveInputDialogBox
            {
                SetThrottleModeCommandDelegate = SetThrottleModeExecute,
                SetPressureCommandDelegate = SetPressureExecute,
                SetPositionCommandDelegate = SetPositionExecute,

                DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
                DeviceId = DeviceData.DeviceSchematicId,

                SetPointPosition = Math.Round(DeviceData.PositionSetPoint, 1),
                SetPointPressure = Math.Round(DeviceData.PressureSetPoint, 1),

                MaxValuePressure = DeviceData.MaxValuePressure,
                MaxValuePosition = DeviceData.MaxValuePosition,

                UnitPosition = DeviceData.UnitPosition,
                UnitPressure = DeviceData.UnitPressure,

                FeedbackPosition = DeviceData.PositionFeedback,
                FeedbackPressure = DeviceData.PressureFeedback,

                IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl,
                IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl,

            };

            if (AnalogOwner != null)
                _dialogBox.Owner = AnalogOwner;
            _dialogBox.Topmost = true;
            _dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _dialogBox.FocasAll();
            _dialogBox.ShowDialog();

            _dialogBox = null;
        }

        private void SetThrottleModeExecute(PressureCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetModeSetpoint", value.ToString());
        }

        private void SetPressureExecute(double value)
        {
            //InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetModeSetpoint", 1);
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPressure}", (float)value);
        }

        private void SetPositionExecute(double value)
        {
            //InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetModeSetpoint", 0);
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPosition}", (float)value);
        }
        private void OpenValve(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", "TVOpen");
        }

        private void CloseValve(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", "TVClose");
        }
    }
}
