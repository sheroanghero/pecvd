using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.DeviceControl;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData.DeviceData;
using MECF.Framework.Common.OperationCenter;
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
    /// AITChillerControl.xaml 的交互逻辑
    /// </summary>
    public partial class AITChillerControl : UserControl
    {
        public AITChillerControl()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITChillerControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ChillerDeviceDataProperty = DependencyProperty.Register(
                      "ChillerDeviceData", typeof(AITChillerData1), typeof(AITChillerControl),
                      new FrameworkPropertyMetadata(new AITChillerData1(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(AITChillerControl),
                         new FrameworkPropertyMetadata(Brushes.DarkMagenta, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HideDialogProperty = DependencyProperty.Register(
                        "HideDialog", typeof(bool), typeof(AITChillerControl),
                         new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EnabeTooltipProperty = DependencyProperty.Register(
                        "EnabeTooltip", typeof(bool), typeof(AITChillerControl),
                         new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AlwaysPowerOnProperty = DependencyProperty.Register(
            "AlwaysPowerOn", typeof(bool), typeof(AITChillerControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FontSizeSettingProperty = DependencyProperty.Register(
            "FontSizeSetting", typeof(int), typeof(AITChillerControl),
            new FrameworkPropertyMetadata(13, FrameworkPropertyMetadataOptions.AffectsRender));


        public static readonly DependencyProperty IsShowCH2Property = DependencyProperty.Register(
    "IsShowCH2", typeof(bool), typeof(AITTurboPump),
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


        [Subscription("Chiller.DeviceData")]
        public AITChillerData1 ChillerData { get; set; }

        public int FontSizeSetting
        {
            get
            {
                return (int)this.GetValue(FontSizeSettingProperty);
            }
            set
            {
                this.SetValue(FontSizeSettingProperty, value);
            }
        }

        public static readonly DependencyProperty ForegroundSettingProperty = DependencyProperty.Register(
            "ForegroundSetting", typeof(string), typeof(AITChillerControl),
            new FrameworkPropertyMetadata("LightYellow", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ForegroundSetting
        {
            get
            {
                return (string)this.GetValue(ForegroundSettingProperty);
            }
            set
            {
                this.SetValue(ForegroundSettingProperty, value);
            }
        }

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

        public AITChillerData1 ChillerDeviceData
        {
            get
            {
                return (AITChillerData1)this.GetValue(ChillerDeviceDataProperty);
            }
            set
            {
                this.SetValue(ChillerDeviceDataProperty, value);
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

        public bool HideDialog
        {
            get
            {
                return (bool)this.GetValue(HideDialogProperty);
            }
            set
            {
                this.SetValue(HideDialogProperty, value);
            }
        }

        public bool EnabeTooltip
        {
            get
            {
                return (bool)this.GetValue(EnabeTooltipProperty);
            }
            set
            {
                this.SetValue(EnabeTooltipProperty, value);
            }
        }

        public bool AlwaysPowerOn
        {
            get
            {
                return (bool)this.GetValue(AlwaysPowerOnProperty);
            }
            set
            {
                this.SetValue(AlwaysPowerOnProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color

            var aaa = ChillerData;
            if (ChillerDeviceData != null)
            {
                //rectBkground.Fill = (DeviceData.IsPowerOn || AlwaysPowerOn) ? Brushes.DarkMagenta : Brushes.Gray;

                //draw red board if mfc meets a warning
                // rectBkground.Stroke = DeviceData.IsWarning ? Brushes.Red : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0X37, 0X37, 0X37));

                // rectBkground.StrokeThickness = DeviceData.IsWarning ? 2 : 1;

                if (dialogBox != null)
                {
                    dialogBox.CH1Status = ChillerDeviceData.IsCH1Warning || ChillerDeviceData.IsCH1Alarm ? "Alarm" : "Normal";
                    dialogBox.CH2Status = ChillerDeviceData.IsCH2Warning || ChillerDeviceData.IsCH2Alarm ? "Alarm" : "Normal";
                    dialogBox.CH1OnOff = ChillerDeviceData.IsCH1On ? "On" : "Off";
                    dialogBox.CH2OnOff = ChillerDeviceData.IsCH2On ? "On" : "Off";
                    dialogBox.CH1Flow = ChillerDeviceData.CH1WaterFlow.ToString("f1");
                    dialogBox.CH2Flow = ChillerDeviceData.CH1WaterFlow.ToString("f1");
                    dialogBox.CH1Temperature = ChillerDeviceData.CH1Temperature.ToString("f1");
                    dialogBox.CH2Temperature = ChillerDeviceData.CH2Temperature.ToString("f1");
                }
            }
        }
        private void ExecuteSetTemperatureValue(string chanel, double temp)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation(
                    $"{ChillerDeviceData.Module}.{ChillerDeviceData.DeviceName}.SetChiller{chanel}Temperature", temp.ToString());
            }
            else
            {
                Command.Execute(new object[]
                    {ChillerDeviceData.DeviceName, $"SetChiller{chanel}Temperature", temp.ToString()});
            }
        }

        private void ExecuteSetOnValue(string chanel, bool isOn)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation(
                    $"{ChillerDeviceData.Module}.{ChillerDeviceData.DeviceName}.SetChiller{chanel}On", isOn);
            }
            else
            {
                Command.Execute(new object[]
                    {ChillerDeviceData.DeviceName, $"SetChiller{chanel}Temperature", isOn});
            }
        }

        private AITChillerInputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void AITHeaterControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            var aaa = ChillerData;
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ChillerDeviceData == null)
                return;

            if (HideDialog)
                return;

            dialogBox = new AITChillerInputDialogBox
            {
                SetTemperatureCommandDelegate = ExecuteSetTemperatureValue,
                SetOnCommandDelegate = ExecuteSetOnValue,
                DeviceName = ChillerDeviceData.DeviceName,
                CH1Status = ChillerDeviceData.IsCH1Warning || ChillerDeviceData.IsCH1Alarm ? "Alarm" : "Normal",
                CH2Status = ChillerDeviceData.IsCH2Warning || ChillerDeviceData.IsCH2Alarm ? "Alarm" : "Normal",
                CH1OnOff = ChillerDeviceData.IsCH1On ? "On" : "Off",
                CH2OnOff = ChillerDeviceData.IsCH2On ? "On" : "Off",
                CH1Flow = ChillerDeviceData.CH1WaterFlow.ToString(ChillerDeviceData.FormatString),
                CH2Flow = ChillerDeviceData.CH1WaterFlow.ToString(ChillerDeviceData.FormatString),
                CH1Temperature = ChillerDeviceData.CH1Temperature.ToString(ChillerDeviceData.FormatString),
                CH2Temperature = ChillerDeviceData.CH2Temperature.ToString(ChillerDeviceData.FormatString),
                CH1TemperatureSetpoint = ChillerDeviceData.CH1TemperatureSetPoint,
                CH2TemperatureSetpoint = ChillerDeviceData.CH2TemperatureSetPoint,
                IsShowCH2 = IsShowCH2,
            };

            if (AnalogOwner != null)
                dialogBox.Owner = AnalogOwner;
            dialogBox.Topmost = true;
            dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialogBox.ShowDialog();

            dialogBox = null;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!EnabeTooltip)
                return;
            //if (DeviceData != null)
            //{
            //    string tooltipValue =
            //        string.Format("{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nSetPoint：{5} {4} \r\nFeedback：{6} {4}\r\nHeaterPower：{7}",
            //            DeviceData.Type,
            //            DeviceData.DisplayName,
            //            DeviceData.DeviceSchematicId,
            //            DeviceData.Scale,
            //            DeviceData.Unit,
            //            DeviceData.SetPoint.ToString("F1"),
            //            DeviceData.FeedBack.ToString("F1"),
            //            DeviceData.IsPowerOn ? "On" : "Off");

            //    ToolTip = tooltipValue;
            //}
        }
    }
}
