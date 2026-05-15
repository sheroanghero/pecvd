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
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// AITThrottleValve.xaml 的交互逻辑
    /// </summary>
    public partial class TVControl1 : UserControl
    {
        public TVControl1()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(TVControl1),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITThrottleValveData), typeof(TVControl1),
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

        public static readonly DependencyProperty IsLearnButtonVisibleProperty = DependencyProperty.Register(
            "IsLearnButtonVisible", typeof(bool), typeof(TVControl1),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsLearnButtonVisible
        {
            get
            {
                return (bool)this.GetValue(IsLearnButtonVisibleProperty);
            }
            set
            {
                this.SetValue(IsLearnButtonVisibleProperty, value);
            }
        }
        public Window AnalogOwner { get; set; }
        private TVSettingDialogViewModel _dialog;
        private TVSettingDialogBox _dialogBox;


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
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

                PositionValue.Content = DeviceData.PositionFeedback.ToString("F1") + $" {DeviceData.UnitPosition}";
                //PositionUnit.Content = "%";
                PressureValue.Content = DeviceData.PressureSetPoint.ToString("F1") + $" {DeviceData.UnitPressure}";
                //PressureUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPressure) ? DeviceData.UnitPressure : "mTorr";

                if (_dialog != null)
                {
                    _dialog.DeviceData = DeviceData;
                }
                if (_dialogBox != null)
                {
                    _dialogBox.TextMode = DeviceData.TextMode;
                    _dialogBox.FeedbackPressure = DeviceData.PressureFeedback;
                    _dialogBox.SetPointPressure = DeviceData.PressureSetPoint;
                    _dialogBox.FeedbackPosition = DeviceData.PositionFeedback;
                    _dialogBox.SetPointPosition = DeviceData.PositionSetPoint;
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


            _dialogBox = new TVSettingDialogBox
            {
                SetThrottleModeCommandDelegate = SetThrottleModeExecute,
                SetPressureCommandDelegate = SetPressureExecute,
                SetPositionCommandDelegate = SetPositionExecute,
                LearnCommandDelegate = LearnExecute,
                OpenCommandDelegate = OpenExecute,
                CloseCommandDelegate = CloseExecute,
                LearnButtonVisibility = IsLearnButtonVisible ? Visibility.Visible : Visibility.Hidden,

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

                //SupportPressureMode = SupportPressureMode,

                IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl,
                IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl,
                //IsCloseMode = DeviceData.Mode == (int)PressureCtrlMode.TVClose,
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
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", value.ToString());
        }

        private void SetPressureExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPressure}", (float)value);
        }

        private void SetPositionExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPosition}", (float)value);
        }
        private bool LearnExecute()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.Learn");
            return true;
        }

        private bool OpenExecute()
        {
            SetThrottleModeExecute(PressureCtrlMode.TVOpen);
            return true;
        }

        private bool CloseExecute()
        {
            SetThrottleModeExecute(PressureCtrlMode.TVClose);
            return true;
        }
    }
}
