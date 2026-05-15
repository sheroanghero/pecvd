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
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITThrottleValve.xaml 的交互逻辑
    /// </summary>
    public partial class AITThrottleValve2 : UserControl
    {
        public AITThrottleValve2()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITThrottleValve2),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITThrottleValveData), typeof(AITThrottleValve2),
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

        public static readonly DependencyProperty PressureStringFormatProperty = DependencyProperty.Register(
           "PressureStringFormat", typeof(string), typeof(AITThrottleValve2),
           new FrameworkPropertyMetadata("F1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string PressureStringFormat
        {
            get
            {
                return (string)this.GetValue(PressureStringFormatProperty);
            }
            set
            {
                this.SetValue(PressureStringFormatProperty, value);
            }
        }

        public static readonly DependencyProperty PositionStringFormatProperty = DependencyProperty.Register(
            "PositionStringFormat", typeof(string), typeof(AITThrottleValve2),
            new FrameworkPropertyMetadata("F1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string PositionStringFormat
        {
            get
            {
                return (string)this.GetValue(PositionStringFormatProperty);
            }
            set
            {
                this.SetValue(PositionStringFormatProperty, value);
            }
        }

        private AITThrottleValveInputDialogBox _dialogBox;

        public Window AnalogOwner { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                if (DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl)
                {
                    rotateTransform.Angle = DeviceData.PressureFeedback * 90.0 / DeviceData.MaxValuePressure;
                }
                else if (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl)
                {
                    rotateTransform.Angle = DeviceData.PositionFeedback * 90.0 / DeviceData.MaxValuePosition;
                }

                PositionValue.Content = DeviceData.PositionFeedback.ToString(PositionStringFormat);
                PositionUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPosition) ? DeviceData.UnitPosition : "%"; ;
                PressureValue.Content = DeviceData.PressureFeedback.ToString(PressureStringFormat);
                PressureUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPressure) ? DeviceData.UnitPressure : "mTorr";

                if (_dialogBox != null)
                {
                    _dialogBox.IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl;
                    _dialogBox.IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl;

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
    }
}
