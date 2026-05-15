using Aitex.Core.RT.Log;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// AITChillerInputDialogBox.xaml 的交互逻辑
    /// </summary>
    public partial class AITChillerInputDialogBox : Window
    {
        public AITChillerInputDialogBox()
        {
            InitializeComponent();

            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH1StatusProperty = DependencyProperty.Register(
                                "CH1Status", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH2StatusProperty = DependencyProperty.Register(
                                "CH2Status", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH1OnOffProperty = DependencyProperty.Register(
                                "CH1OnOff", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH2OnOffProperty = DependencyProperty.Register(
                                "CH2OnOff", typeof(string), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH1FlowProperty = DependencyProperty.Register(
                               "CH1Flow", typeof(string), typeof(AITChillerInputDialogBox),
                               new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH2FlowProperty = DependencyProperty.Register(
                               "CH2Flow", typeof(string), typeof(AITChillerInputDialogBox),
                               new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH1TemperatureProperty = DependencyProperty.Register(
                               "CH1Temperature", typeof(string), typeof(AITChillerInputDialogBox),
                               new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH2TemperatureProperty = DependencyProperty.Register(
                               "CH2Temperature", typeof(string), typeof(AITChillerInputDialogBox),
                               new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty CH1TemperatureSetpointProperty = DependencyProperty.Register(
                               "CH1TemperatureSetpoint", typeof(double), typeof(AITChillerInputDialogBox), null);
        public static readonly DependencyProperty CH2TemperatureSetpointProperty = DependencyProperty.Register(
                               "CH2TemperatureSetpoint", typeof(double), typeof(AITChillerInputDialogBox), null);
        public static readonly DependencyProperty TemperatureHighLimitProperty = DependencyProperty.Register(
                               "TemperatureHighLimit", typeof(double), typeof(AITChillerInputDialogBox), null);
        public static readonly DependencyProperty TemperatureLowLimitProperty = DependencyProperty.Register(
                               "TemperatureLowLimit", typeof(double), typeof(AITChillerInputDialogBox), null);

        public static readonly DependencyProperty IsShowCH2Property = DependencyProperty.Register(
                                "IsShowCH2", typeof(bool), typeof(AITChillerInputDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));


        public bool IsShowCH2
        {
            get
            {
                return (bool)this.GetValue(IsShowCH2Property);
            }
            set
            {
                this.SetValue(IsShowCH2Property, value);
            }
        }

        public Visibility IsShow => IsShowCH2 ? Visibility.Visible : Visibility.Hidden;

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

        public string CH1Status
        {
            get
            {
                return (string)this.GetValue(CH1StatusProperty);
            }
            set
            {
                this.SetValue(CH1StatusProperty, value);
            }
        }
        public string CH2Status
        {
            get
            {
                return (string)this.GetValue(CH2StatusProperty);
            }
            set
            {
                this.SetValue(CH2StatusProperty, value);
            }
        }

        public string CH1OnOff
        {
            get
            {
                return (string)this.GetValue(CH1OnOffProperty);
            }
            set
            {
                if (value == "On")
                {
                    _ch1OnButton.IsEnabled = false;
                    _ch1OffButton.IsEnabled = true;
                    _ch1SetTempButton.IsEnabled = true;
                }
                else
                {
                    _ch1OnButton.IsEnabled = true;
                    _ch1OffButton.IsEnabled = false;
                    _ch1SetTempButton.IsEnabled = false;
                }
                this.SetValue(CH1OnOffProperty, value);
            }
        }

        public string CH2OnOff
        {
            get
            {
                return (string)this.GetValue(CH2OnOffProperty);
            }
            set
            {
                if (value == "On")
                {
                    _ch2OnButton.IsEnabled = false;
                    _ch2OffButton.IsEnabled = true;
                    _ch2SetTempButton.IsEnabled = true;
                }
                else
                {
                    _ch2OnButton.IsEnabled = true;
                    _ch2OffButton.IsEnabled = false;
                    _ch2SetTempButton.IsEnabled = false;
                }
                this.SetValue(CH2OnOffProperty, value);
            }
        }
        public string CH1Flow
        {
            get
            {
                return (string)this.GetValue(CH1FlowProperty);
            }
            set
            {
                this.SetValue(CH1FlowProperty, value);
            }
        }

        public string CH2Flow
        {
            get
            {
                return (string)this.GetValue(CH2FlowProperty);
            }
            set
            {
                this.SetValue(CH2FlowProperty, value);
            }
        }

        public string CH1Temperature
        {
            get
            {
                return (string)this.GetValue(CH1TemperatureProperty);
            }
            set
            {
                this.SetValue(CH1TemperatureProperty, value);
            }
        }

        public string CH2Temperature
        {
            get
            {
                return (string)this.GetValue(CH2TemperatureProperty);
            }
            set
            {
                this.SetValue(CH2TemperatureProperty, value);
            }
        }

        public double CH1TemperatureSetpoint
        {
            get
            {
                return (double)this.GetValue(CH1TemperatureSetpointProperty);
            }
            set
            {
                this.SetValue(CH1TemperatureSetpointProperty, value);
            }
        }



        public double CH2TemperatureSetpoint
        {
            get
            {
                return (double)this.GetValue(CH2TemperatureSetpointProperty);
            }
            set
            {
                this.SetValue(CH2TemperatureSetpointProperty, value);
            }
        }

        public double TemperatureHighLimit
        {
            get
            {
                return (double)this.GetValue(TemperatureHighLimitProperty);
            }
            set
            {
                this.SetValue(TemperatureHighLimitProperty, value);
            }
        }
        public double TemperatureLowLimit
        {
            get
            {
                return (double)this.GetValue(TemperatureLowLimitProperty);
            }
            set
            {
                this.SetValue(TemperatureLowLimitProperty, value);
            }
        }

        public Action<string, double> SetTemperatureCommandDelegate;
        public Action<string, bool> SetOnCommandDelegate;

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {

            //Close();
        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
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

        private void CH1TemperatureButton_Click(object sender, RoutedEventArgs e)
        {
            SetTemperatureCommandDelegate("CH1", CH1TemperatureSetpoint);
        }

        private void CH2TemperatureButton_Click(object sender, RoutedEventArgs e)
        {
            SetTemperatureCommandDelegate("CH2", CH2TemperatureSetpoint);
        }

        private void CH1OnButton_Click(object sender, RoutedEventArgs e)
        {
            SetOnCommandDelegate("CH1", true);
        }

        private void CH1OffButton_Click(object sender, RoutedEventArgs e)
        {
            SetOnCommandDelegate("CH1", false);
        }

        private void CH2OnButton_Click(object sender, RoutedEventArgs e)
        {
            SetOnCommandDelegate("CH2", true);
        }

        private void CH2OffButton_Click(object sender, RoutedEventArgs e)
        {
            SetOnCommandDelegate("CH2", false);
        }
    }
}
