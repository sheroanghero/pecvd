using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.DeviceControl
{
    public partial class AITBoostPumpInputDialogBox : Window
    {
        public AITBoostPumpInputDialogBox()
        {
            InitializeComponent();

            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;


        }

        public void FocasAll()
        {
            inputBox.Text = Math.Round(SetPoint, 2).ToString();
            inputBox.Focus();
            inputBox.SelectAll();
        }
        /// <summary>
        /// Vilidate input range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double input;
            if (!double.TryParse(inputBox.Text, out input))
                btnSet.IsEnabled = false;
            else if (input < 0 || input > MaxValue)
                btnSet.IsEnabled = false;
            else
                btnSet.IsEnabled = true;
            inputBox.Foreground = btnSet.IsEnabled ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }
        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
                                "MaxValue", typeof(double), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FrequencyProperty = DependencyProperty.Register(
                                "Frequency", typeof(double), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty RealValueProperty = DependencyProperty.Register(
                                "RealValue", typeof(string), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata("0.0", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
                                "Unit", typeof(string), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointProperty = DependencyProperty.Register(
                                "SetPoint", typeof(double), typeof(AITBoostPumpInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
 
        /// <summary>
        /// 是否百分比显示
        /// </summary>
        public bool IsPercent
        {
            get;
            set;
        }

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
        public double MaxValue
        {
            get
            {
                return (double)this.GetValue(MaxValueProperty);
            }
            set
            {
                this.SetValue(MaxValueProperty, value);

                // validationRule.MaxInput = value;
            }
        }
        public double Frequency
        {
            get
            {
                return (double)this.GetValue(FrequencyProperty);
            }
            set
            {
                this.SetValue(FrequencyProperty, value);
            }
        }
        public string RealValue
        {
            get
            {
                return (string)this.GetValue(RealValueProperty);
            }
            set
            {
                this.SetValue(RealValueProperty, value);
            }
        }
        public string Unit
        {
            get
            {
                return (string)this.GetValue(UnitProperty);
            }
            set
            {
                this.SetValue(UnitProperty, value);
            }
        }

        public double SetPoint
        {
            get
            {
                return (double)this.GetValue(SetPointProperty);
            }
            set
            {
                this.SetValue(SetPointProperty, value);
            }
        }
 
        public Action<double> SetPressureCommandDelegate;

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetPressureCommandDelegate(Convert.ToDouble(inputBox.Text));
                
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
                if (!btnSet.IsEnabled)
                    return;
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
 


    }
}