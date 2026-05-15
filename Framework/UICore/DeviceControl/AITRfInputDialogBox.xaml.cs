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
using System.Windows.Shapes;
using System.Globalization;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for InputDialogBox.xaml
    /// </summary>
    public partial class AITRfInputDialogBox : Window
    {
        public AITRfInputDialogBox()
        {
            InitializeComponent();

            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public void FocasAll()
        {
            inputBoxDuty.Text = Math.Round(SetPointDuty, 2).ToString();
            inputBoxFrequency.Text = Math.Round(SetPointFrequency, 2).ToString();

            inputBoxPower.Text = Math.Round(SetPointPower, 2).ToString();
            inputBoxPower.Focus();
            inputBoxPower.SelectAll();
        }
        /// <summary>
        /// Vilidate input range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputTextBoxPower_TextChanged(object sender, TextChangedEventArgs e)
        {
            double input;
            if (!double.TryParse(inputBoxPower.Text, out input)) buttonSet.IsEnabled = false;
            else if (input < 0 || input > MaxValuePower) buttonSet.IsEnabled = false;
            else buttonSet.IsEnabled = true;
            inputBoxPower.Foreground = buttonSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }
        private void InputTextBoxFrequency_TextChanged(object sender, TextChangedEventArgs e)
        {
            double input;
            if (!double.TryParse(inputBoxFrequency.Text, out input)) buttonSet.IsEnabled = false;
            else if (input < 0 || input > MaxValueFrequency) buttonSet.IsEnabled = false;
            else buttonSet.IsEnabled = true;
            inputBoxFrequency.Foreground = buttonSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }
        private void InputTextBoxDuty_TextChanged(object sender, TextChangedEventArgs e)
        {
            double input;
            if (!double.TryParse(inputBoxDuty.Text, out input)) buttonSet.IsEnabled = false;
            else if (input < 0 || input > MaxValueDuty) buttonSet.IsEnabled = false;
            else buttonSet.IsEnabled = true;
            inputBoxDuty.Foreground = buttonSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }
        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty ForwardPowerProperty = DependencyProperty.Register(
                                "ForwardPower", typeof(float), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ReflectPowerProperty = DependencyProperty.Register(
                                "ReflectPower", typeof(float), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty UnitPowerProperty = DependencyProperty.Register(
                                "UnitPower", typeof(string), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointPowerProperty = DependencyProperty.Register(
                                "SetPointPower", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValuePowerProperty = DependencyProperty.Register(
                                "MaxValuePower", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty VoltageProperty = DependencyProperty.Register(
                                "Voltage", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register(
                                "Current", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitFrequencyProperty = DependencyProperty.Register(
                                "UnitFrequency", typeof(string), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointFrequencyProperty = DependencyProperty.Register(
                                "SetPointFrequency", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValueFrequencyProperty = DependencyProperty.Register(
                                "MaxValueFrequency", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitDutyProperty = DependencyProperty.Register(
                                "UnitDuty", typeof(string), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointDutyProperty = DependencyProperty.Register(
                                "SetPointDuty", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValueDutyProperty = DependencyProperty.Register(
                                "MaxValueDuty", typeof(double), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsRfOnProperty = DependencyProperty.Register(
                                "IsRfOn", typeof(bool), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsContinuousModeProperty = DependencyProperty.Register(
                                "IsContinuousMode", typeof(bool), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty IsPulsingModeProperty = DependencyProperty.Register(
                                "IsPulsingMode", typeof(bool), typeof(AITRfInputDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EnablePulsingProperty = DependencyProperty.Register(
                        "EnablePulsing", typeof(bool), typeof(AITRfInputDialogBox),
                        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty EnableReflectProperty = DependencyProperty.Register(
                        "EnableReflect", typeof(bool), typeof(AITRfInputDialogBox),
                        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty EnableVoltageCurrentProperty = DependencyProperty.Register(
                        "EnableVoltageCurrent", typeof(bool), typeof(AITRfInputDialogBox),
                        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty GridLengthReflectProperty = DependencyProperty.Register(
                        "GridLengthReflect", typeof(GridLength), typeof(AITRfInputDialogBox),
                        new FrameworkPropertyMetadata(GridLength.Auto, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty GridLengthVoltageCurrentProperty = DependencyProperty.Register(
                        "GridLengthVoltageCurrent", typeof(GridLength), typeof(AITRfInputDialogBox),
                        new FrameworkPropertyMetadata(GridLength.Auto, FrameworkPropertyMetadataOptions.AffectsRender));
        /// <summary>
        /// 是否百分比显示
        /// </summary>
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
        public string UnitPower
        {
            get
            {
                return (string)this.GetValue(UnitPowerProperty);
            }
            set
            {
                this.SetValue(UnitPowerProperty, value);
            }
        }

        public double SetPointPower
        {
            get
            {
                return (double)this.GetValue(SetPointPowerProperty);
            }
            set
            {
                this.SetValue(SetPointPowerProperty, value);
            }
        }
        public double MaxValuePower
        {
            get
            {
                return (double)this.GetValue(MaxValuePowerProperty);
            }
            set
            {
                this.SetValue(MaxValuePowerProperty, value);
            }
        }

        public string UnitFrequency
        {
            get
            {
                return (string)this.GetValue(UnitFrequencyProperty);
            }
            set
            {
                this.SetValue(UnitFrequencyProperty, value);
            }
        }

        public double SetPointFrequency
        {
            get
            {
                return (double)this.GetValue(SetPointFrequencyProperty);
            }
            set
            {
                this.SetValue(SetPointFrequencyProperty, value);
            }
        }
        public double MaxValueFrequency
        {
            get
            {
                return (double)this.GetValue(MaxValueFrequencyProperty);
            }
            set
            {
                this.SetValue(MaxValueFrequencyProperty, value);
            }
        }
        public double Voltage
        {
            get
            {
                return (double)this.GetValue(VoltageProperty);
            }
            set
            {
                this.SetValue(VoltageProperty, value);
            }
        }
        public double Current
        {
            get
            {
                return (double)this.GetValue(CurrentProperty);
            }
            set
            {
                this.SetValue(CurrentProperty, value);
            }
        }

        public string UnitDuty
        {
            get
            {
                return (string)this.GetValue(UnitDutyProperty);
            }
            set
            {
                this.SetValue(UnitDutyProperty, value);
            }
        }

        public double SetPointDuty
        {
            get
            {
                return (double)this.GetValue(SetPointDutyProperty);
            }
            set
            {
                this.SetValue(SetPointDutyProperty, value);
            }
        }
        public double MaxValueDuty
        {
            get
            {
                return (double)this.GetValue(MaxValueDutyProperty);
            }
            set
            {
                this.SetValue(MaxValueDutyProperty, value);
            }
        }



        public float ForwardPower
        {
            get
            {
                return (float)this.GetValue(ForwardPowerProperty);
            }
            set
            {
                this.SetValue(ForwardPowerProperty, value);
            }
        }
        public float ReflectPower
        {
            get
            {
                return (float)this.GetValue(ReflectPowerProperty);
            }
            set
            {
                this.SetValue(ReflectPowerProperty, value);
            }
        }


        public bool IsRfOn
        {
            get
            {
                return (bool)this.GetValue(IsRfOnProperty);
            }
            set
            {
                this.SetValue(IsRfOnProperty, value);

                buttonRFOff.IsEnabled = value;
                buttonRFOn.IsEnabled = !value;
                //if (!value)
                //    buttonSet.IsEnabled = false;
            }
        }

        public bool IsContinuousMode
        {
            get
            {
                return (bool)this.GetValue(IsContinuousModeProperty);
            }
            set
            {
                this.SetValue(IsContinuousModeProperty, value);

                ckContinuous.IsChecked = IsContinuousMode;

                lblFrequency.Foreground = IsPulsingMode ? Brushes.Black : Brushes.Gray;
                lblDuty.Foreground = IsPulsingMode ? Brushes.Black : Brushes.Gray;
                inputBoxFrequency.IsEnabled = IsPulsingMode;
                inputBoxDuty.IsEnabled = IsPulsingMode;
            }
        }

        public bool IsPulsingMode
        {
            get
            {
                return (bool)this.GetValue(IsPulsingModeProperty);
            }
            set
            {
                this.SetValue(IsPulsingModeProperty, value);
                ckPulsing.IsChecked = IsPulsingMode;

                lblFrequency.Foreground = IsPulsingMode ? Brushes.Black : Brushes.Gray;
                lblDuty.Foreground = IsPulsingMode ? Brushes.Black : Brushes.Gray;
                inputBoxFrequency.IsEnabled = IsPulsingMode;
                inputBoxDuty.IsEnabled = IsPulsingMode;
            }
        }

        public bool EnablePulsing
        {
            get
            {
                return (bool)this.GetValue(EnablePulsingProperty);
            }
            set
            {
                this.SetValue(EnablePulsingProperty, value);
                ckPulsing.Visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public bool EnableReflect
        {
            get
            {
                return (bool)this.GetValue(EnableReflectProperty);
            }
            set
            {
                this.SetValue(EnableReflectProperty, value);
            }
        }
        public bool EnableVoltageCurrent
        {
            get
            {
                return (bool)this.GetValue(EnableVoltageCurrentProperty);
            }
            set
            {
                this.SetValue(EnableVoltageCurrentProperty, value);
            }
        }

        public GridLength GridLengthReflect
        {
            get
            {
                return (GridLength)this.GetValue(GridLengthReflectProperty);
            }
            set
            {
                this.SetValue(GridLengthReflectProperty, value);
            }
        }

        public GridLength GridLengthWorkMode
        {
            get;
            set;
        }

        
        public GridLength GridLengthVoltageCurrent
        {
            get
            {
                return (GridLength)this.GetValue(GridLengthVoltageCurrentProperty);
            }
            set
            {
                this.SetValue(GridLengthVoltageCurrentProperty, value);
            }
        }


        public Action<RfMode> SetRfModeCommandDelegate;
        public Action<bool> SetRfPowerOnOffCommandDelegate;

        public Action<double> SetContinuousCommandDelegate;
        public Action<double, double, double> SetPulsingCommandDelegate;

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsContinuousMode)
                    SetContinuousCommandDelegate(Convert.ToDouble(inputBoxPower.Text));
                else if (IsPulsingMode)
                    SetPulsingCommandDelegate(Convert.ToDouble(inputBoxPower.Text), Convert.ToDouble(inputBoxFrequency.Text), Convert.ToDouble(inputBoxDuty.Text));
 
                Close();
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
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

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CkPulsing_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsPulsingMode)
                return;

            ckPulsing.IsChecked = false;

            try
            {
                if (SetRfModeCommandDelegate != null)
                {
                    SetRfModeCommandDelegate(RfMode.PulsingMode);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void CkContinuous_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsContinuousMode)
                return;

            ckContinuous.IsChecked = false;

            try
            {
                if (SetRfModeCommandDelegate != null)
                {
                    SetRfModeCommandDelegate(RfMode.ContinuousWaveMode);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void CkContinuous_OnClick(object sender, RoutedEventArgs e)
        {
            ckContinuous.IsChecked = IsContinuousMode;
        }

        private void CkPulsing_OnClick(object sender, RoutedEventArgs e)
        {
            ckPulsing.IsChecked = IsPulsingMode;
        }

        private void ButtonRFOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SetRfPowerOnOffCommandDelegate != null)
                {
                    SetRfPowerOnOffCommandDelegate(true);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void ButtonRFOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SetRfPowerOnOffCommandDelegate != null)
                {
                    SetRfPowerOnOffCommandDelegate(false);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
