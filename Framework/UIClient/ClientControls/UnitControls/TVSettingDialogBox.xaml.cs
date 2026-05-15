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
    public partial class TVSettingDialogBox : Window
    {
        public TVSettingDialogBox()
        {
            InitializeComponent();

            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
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

        public static readonly DependencyProperty DeviceNameProperty = DependencyProperty.Register(
                                "DeviceName", typeof(string), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
                                "DeviceId", typeof(string), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TextModeProperty = DependencyProperty.Register(
                                "TextMode", typeof(string), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValuePositionProperty = DependencyProperty.Register(
                                "MaxValuePosition", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty MaxValuePressureProperty = DependencyProperty.Register(
                                "MaxValuePressure", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitPositionProperty = DependencyProperty.Register(
                                "UnitPosition", typeof(string), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty UnitPressureProperty = DependencyProperty.Register(
                                "UnitPressure", typeof(string), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SetPointPositionProperty = DependencyProperty.Register(
                                "SetPointPosition", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty SetPointPressureProperty = DependencyProperty.Register(
                                "SetPointPressure", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FeedbackPositionProperty = DependencyProperty.Register(
                                "FeedbackPosition", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FeedbackPressureProperty = DependencyProperty.Register(
                                "FeedbackPressure", typeof(double), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsPositionModeProperty = DependencyProperty.Register(
                                "IsPositionMode", typeof(bool), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty IsPressureModeProperty = DependencyProperty.Register(
                                "IsPressureMode", typeof(bool), typeof(TVSettingDialogBox),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty LearnButtonVisibilityProperty = DependencyProperty.Register(
                             "LearnButtonVisibility", typeof(Visibility), typeof(TVSettingDialogBox),
                             new FrameworkPropertyMetadata(Visibility.Hidden, FrameworkPropertyMetadataOptions.AffectsRender));

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

        public string TextMode
        {
            get
            {
                return (string)this.GetValue(TextModeProperty);
            }
            set
            {
                this.SetValue(TextModeProperty, value);
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
            }
        }

        public Visibility LearnButtonVisibility
        {
            get
            {
                return (Visibility)this.GetValue(LearnButtonVisibilityProperty);
            }
            set
            {
                this.SetValue(LearnButtonVisibilityProperty, value);
            }
        }
        public Action<PressureCtrlMode> SetThrottleModeCommandDelegate;

        public Action<double> SetPositionCommandDelegate;
        public Action<double> SetPressureCommandDelegate;
        public Func<bool> LearnCommandDelegate;
        public Func<bool> OpenCommandDelegate;
        public Func<bool> CloseCommandDelegate;

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {

            //Close();
        }

        private void ButtonLearn_Click(object sender, RoutedEventArgs e)
        {
            LearnCommandDelegate();
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


        private void CkPosition_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsPositionMode)
                return;

            try
            {
                if (SetThrottleModeCommandDelegate != null)
                {
                    SetThrottleModeCommandDelegate(PressureCtrlMode.TVPositionCtrl);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void CkPressure_OnChecked(object sender, RoutedEventArgs e)
        {
            if (IsPressureMode)
                return;

            try
            {
                if (SetThrottleModeCommandDelegate != null)
                {
                    SetThrottleModeCommandDelegate(PressureCtrlMode.TVPressureCtrl);
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        private void ButtonPressureSet_Click(object sender, RoutedEventArgs e)
        {
            SetPressureCommandDelegate(Convert.ToDouble(inputBoxPressure.Text));
        }

        private void ButtonPositionSet_Click(object sender, RoutedEventArgs e)
        {
            SetPositionCommandDelegate(Convert.ToDouble(inputBoxPosition.Text));
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenCommandDelegate();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            CloseCommandDelegate();
        }
    }
}
 
