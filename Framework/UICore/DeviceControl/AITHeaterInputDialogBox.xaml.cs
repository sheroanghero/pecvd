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
using System.Globalization;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.DeviceControl
{
    public class DoubleConverter : IValueConverter
    {
        public DoubleConverter()
        {
        }

        // Summary:
        //     Converts a value.
        //
        // Parameters:
        //   value:
        //     The value produced by the binding source.
        //
        //   targetType:
        //     The type of the binding target property.
        //
        //   parameter:
        //     The converter parameter to use.
        //
        //   culture:
        //     The culture to use in the converter.
        //
        // Returns:
        //     A converted value. If the method returns null, the valid null value is used.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
        //
        // Summary:
        //     Converts a value.
        //
        // Parameters:
        //   value:
        //     The value that is produced by the binding target.
        //
        //   targetType:
        //     The type to convert to.
        //
        //   parameter:
        //     The converter parameter to use.
        //
        //   culture:
        //     The culture to use in the converter.
        //
        // Returns:
        //     A converted value. If the method returns null, the valid null value is used.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = (string)value;
            double result = 0;
            if (double.TryParse(input, out result))
            {
                return result;
            }
            return DependencyProperty.UnsetValue;

        }
    }

    public class InputDialogValidationRule : ValidationRule
    {
        double minInput;
        double maxInput;

        public double MinInput
        {
            get
            {
                return this.minInput;
            }
            set
            {
                this.minInput = value;
            }
        }

        public double MaxInput
        {
            get
            {
                return this.maxInput;
            }
            set
            {
                this.maxInput = value;
            }
        }

        public InputDialogValidationRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double result;

            // Is a number?
            if (!double.TryParse((string)value, out result))
            {
                string msg = string.Format("‘{0}’不是一个数.", value);
                return new ValidationResult(false, msg);
            }

            // Is in range?
            if ((result < this.minInput) || (result > this.maxInput))
            {
                string msg = string.Format("输入值必须在‘{0}’与‘{1}’之间.", this.minInput, this.maxInput);
                return new ValidationResult(false, msg);
            }

            // Number is valid
            return new ValidationResult(true, null);
        }
    }

    /// <summary>
    /// Interaction logic for InputDialogBox.xaml
    /// </summary>
    public partial class AITHeaterInputDialogBox : Window
    {
        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
                                "MaxValue", typeof(double), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register(
                                "DefaultValue", typeof(double), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty RealValueProperty = DependencyProperty.Register(
                                "RealValue", typeof(string), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata("0.0", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
                                "Unit", typeof(string), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointProperty = DependencyProperty.Register(
                                "SetPoint", typeof(double), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty IsHeaterOnProperty = DependencyProperty.Register(
                                "IsHeaterOn", typeof(bool), typeof(AITHeaterInputDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
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
        public double DefaultValue
        {
            get
            {
                return (double)this.GetValue(DefaultValueProperty);
            }
            set
            {
                this.SetValue(DefaultValueProperty, value);
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

        public bool IsHeaterOn
        {
            get
            {
                return (bool)this.GetValue(IsHeaterOnProperty);
            }
            set
            {
                this.SetValue(IsHeaterOnProperty, value);

                btnHeaterOff.IsEnabled = value;
                btnHeaterOn.IsEnabled = !value;
            }
        }

        public bool EnableTemperatureSetting
        {
            get;
            set;
        }

        public bool EnablePowerControl
        {
            get;
            set;
        }

        public Action<bool> SetHeaterPowerOnOffCommandDelegate;

        public Action<double> SetHeaterValueCommandDelegate;

        public AITHeaterInputDialogBox()
        {
            InitializeComponent();

            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.Loaded += AITHeaterInputDialogBox_Loaded;
        }

        private void AITHeaterInputDialogBox_Loaded(object sender, RoutedEventArgs e)
        {
            btnHeaterOn.Visibility = EnablePowerControl ? Visibility.Visible : Visibility.Hidden;
            btnHeaterOff.Visibility = EnablePowerControl ? Visibility.Visible : Visibility.Hidden;

            btnSet.IsEnabled = EnableTemperatureSetting;
            inputBox.IsEnabled = EnableTemperatureSetting;

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

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetHeaterValueCommandDelegate(Convert.ToDouble(inputBox.Text));
                
                //Close();
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

        private void ButtonHeaterOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SetHeaterPowerOnOffCommandDelegate != null)
                {
                    SetHeaterPowerOnOffCommandDelegate(true);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void ButtonHeaterOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SetHeaterPowerOnOffCommandDelegate != null)
                {
                    SetHeaterPowerOnOffCommandDelegate(false);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }


    }
}