using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// InputFileNameDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class TVSettingDialogView : UserControl
    {
        public TVSettingDialogView()
        {
            InitializeComponent();
 
 
        }
        public void FocasAll()
        {
            inputBoxPosition.Text = Math.Round(SetPointPosition, 2).ToString();
            if (IsPositionMode)
            {
                inputBoxPosition.Focus();
                inputBoxPosition.SelectAll();
            }


            inputBoxPressure.Text = Math.Round(SetPointPressure, 2).ToString();

            if (IsPressureMode)
            {
                inputBoxPressure.Focus();
                inputBoxPressure.SelectAll();
            }

        }
        /// <summary>
        /// Vilidate input range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputTextBoxPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            double maxValue = MaxValuePosition;
            var data = this.DataContext as TVSettingDialogViewModel;
            if (data != null)
            {
                maxValue = data.DeviceData.MaxValuePosition;
            }

            double input;
            if (!double.TryParse(inputBoxPosition.Text, out input)) buttonSet.IsEnabled = false;
            else if (input < 0 || input > maxValue) buttonSet.IsEnabled = false;
            else buttonSet.IsEnabled = true;
            inputBoxPosition.Foreground = buttonSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void InputTextBoxPressure_TextChanged(object sender, TextChangedEventArgs e)
        {
            double maxValue = MaxValuePosition;
            var data = this.DataContext as TVSettingDialogViewModel;
            if (data != null)
            {
                maxValue = data.DeviceData.MaxValuePressure;
            }

            double input;
            if (!double.TryParse(inputBoxPressure.Text, out input)) buttonSet.IsEnabled = false;
            else if (input < 0 || input > maxValue) buttonSet.IsEnabled = false;
            else buttonSet.IsEnabled = true;
            inputBoxPressure.Foreground = buttonSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }


        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValuePositionProperty = DependencyProperty.Register(
                                "MaxValuePosition", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValuePressureProperty = DependencyProperty.Register(
                                "MaxValuePressure", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitPositionProperty = DependencyProperty.Register(
                                "UnitPosition", typeof(string), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty UnitPressureProperty = DependencyProperty.Register(
                                "UnitPressure", typeof(string), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SetPointPositionProperty = DependencyProperty.Register(
                                "SetPointPosition", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointPressureProperty = DependencyProperty.Register(
                                "SetPointPressure", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FeedbackPositionProperty = DependencyProperty.Register(
                                "FeedbackPosition", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FeedbackPressureProperty = DependencyProperty.Register(
                                "FeedbackPressure", typeof(double), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsPositionModeProperty = DependencyProperty.Register(
                                "IsPositionMode", typeof(bool), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty IsPressureModeProperty = DependencyProperty.Register(
                                "IsPressureMode", typeof(bool), typeof(TVSettingDialogView),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsPercent { get; set; }

        public string DeviceName
        {
            get
            {
                return (string)this.GetValue(DeviceNameProperty);
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && !value.StartsWith("_"))
                    value = "_" + value;
                this.SetValue(DeviceNameProperty, value);
            }
        }

        public string DeviceId
        {
            get
            {
                return (string)this.GetValue(DeviceIdProperty);
            }
            set
            {
                this.SetValue(DeviceIdProperty, value);
            }
        }
        public double MaxValuePosition
        {
            get
            {
                return (double)this.GetValue(MaxValuePositionProperty);
            }
            set
            {
                this.SetValue(MaxValuePositionProperty, value);
            }
        }
        public double MaxValuePressure
        {
            get
            {
                return (double)this.GetValue(MaxValuePressureProperty);
            }
            set
            {
                this.SetValue(MaxValuePressureProperty, value);
            }
        }

        public string UnitPosition
        {
            get
            {
                return (string)this.GetValue(UnitPositionProperty);
            }
            set
            {
                this.SetValue(UnitPositionProperty, value);
            }
        }
        public string UnitPressure
        {
            get
            {
                return (string)this.GetValue(UnitPressureProperty);
            }
            set
            {
                this.SetValue(UnitPressureProperty, value);
            }
        }

        public double SetPointPosition
        {
            get
            {
                return (double)this.GetValue(SetPointPositionProperty);
            }
            set
            {
                this.SetValue(SetPointPositionProperty, value);
            }
        }
        public double SetPointPressure
        {
            get
            {
                return (double)this.GetValue(SetPointPressureProperty);
            }
            set
            {
                this.SetValue(SetPointPressureProperty, value);
            }
        }
        public double FeedbackPosition
        {
            get
            {
                return (double)this.GetValue(FeedbackPositionProperty);
            }
            set
            {
                this.SetValue(FeedbackPositionProperty, value);
            }
        }
        public double FeedbackPressure
        {
            get
            {
                return (double)this.GetValue(FeedbackPressureProperty);
            }
            set
            {
                this.SetValue(FeedbackPressureProperty, value);
            }
        }
        public bool IsPositionMode
        {
            get
            {
                return (bool)this.GetValue(IsPositionModeProperty);
            }
            set
            {
                this.SetValue(IsPositionModeProperty, value);

                ckPosition.IsChecked = IsPositionMode;
            }
        }

        public bool IsPressureMode
        {
            get
            {
                return (bool)this.GetValue(IsPressureModeProperty);
            }
            set
            {
                this.SetValue(IsPressureModeProperty, value);
                ckPressure.IsChecked = IsPressureMode;
            }
        }
        public Action<PressureCtrlMode> SetThrottleModeCommandDelegate;

        public Action<double> SetPositionCommandDelegate;
        public Action<double> SetPressureCommandDelegate;

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            if (IsPressureMode)
                SetPressureCommandDelegate(Convert.ToDouble(inputBoxPressure.Text));
            else if (IsPositionMode)
                SetPositionCommandDelegate(Convert.ToDouble(inputBoxPosition.Text));

            //Close();
        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!buttonSet.IsEnabled) return;
                if (e.Key == Key.Return)
                {
                    ButtonSet_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
 

        private void CkPosition_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsPositionMode)
                return;

            ckPosition.IsChecked = false;

            try
            {
                if (SetThrottleModeCommandDelegate != null)
                {
                    SetThrottleModeCommandDelegate(PressureCtrlMode.TVPositionCtrl);
                }

                var data = this.DataContext as TVSettingDialogViewModel;
                if (data != null)
                {
                    InvokeClient.Instance.Service.DoOperation($"{data.DeviceData.Module}.{data.DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", PressureCtrlMode.TVPositionCtrl.ToString());
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void SetThrottleModeExecute(PressureCtrlMode value)
        {
        }

        private void SetPressureExecute(double value)
        {
            //InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPressure}", (float)value);
        }

        private void SetPositionExecute(double value)
        {
            //InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPosition}", (float)value);
        }
        private void CkPressure_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsPressureMode)
                return;

            ckPressure.IsChecked = false;

            try
            {
                if (SetThrottleModeCommandDelegate != null)
                {
                    SetThrottleModeCommandDelegate(PressureCtrlMode.TVPressureCtrl);
                }

                var data = this.DataContext as TVSettingDialogViewModel;
                if (data != null)
                {
                    InvokeClient.Instance.Service.DoOperation($"{data.DeviceData.Module}.{data.DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", PressureCtrlMode.TVPressureCtrl.ToString());
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }


        private void CkPosition_OnClick(object sender, RoutedEventArgs e)
        {
            ckPosition.IsChecked = IsPositionMode;
        }

        private void CkPressure_OnClick(object sender, RoutedEventArgs e)
        {
            ckPressure.IsChecked = IsPressureMode;
        }
    }
}
 
