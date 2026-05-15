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
    /// AITHeaterControl.xaml 的交互逻辑
    /// </summary>
    public partial class AITHeaterControl : UserControl
    {
        public AITHeaterControl()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITHeaterControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITHeaterData), typeof(AITHeaterControl),
                        new FrameworkPropertyMetadata(new AITHeaterData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(AITHeaterControl),
                         new FrameworkPropertyMetadata(Brushes.DarkMagenta, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HideDialogProperty = DependencyProperty.Register(
                        "HideDialog", typeof(bool), typeof(AITHeaterControl),
                         new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AlwaysPowerOnProperty = DependencyProperty.Register(
            "AlwaysPowerOn", typeof(bool), typeof(AITHeaterControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EnablePowerControlProperty = DependencyProperty.Register(
            "EnablePowerControl", typeof(bool), typeof(AITHeaterControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EnableTemperatureSettingProperty = DependencyProperty.Register(
            "EnableTemperatureSetting", typeof(bool), typeof(AITHeaterControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeSettingProperty = DependencyProperty.Register(
            "FontSizeSetting", typeof(int), typeof(AITHeaterControl),
            new FrameworkPropertyMetadata(13, FrameworkPropertyMetadataOptions.AffectsRender));

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
            "ForegroundSetting", typeof(string), typeof(AITHeaterControl),
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

        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITHeaterData DeviceData
        {
            get
            {
                return (AITHeaterData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
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

        public bool EnableTemperatureSetting
        {
            get
            {
                return (bool)this.GetValue(EnableTemperatureSettingProperty);
            }
            set
            {
                this.SetValue(EnableTemperatureSettingProperty, value);
            }
        }
        public bool EnablePowerControl
        {
            get
            {
                return (bool)this.GetValue(EnablePowerControlProperty);
            }
            set
            {
                this.SetValue(EnablePowerControlProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color
            

            if (DeviceData != null)
            {
                rectBkground.Fill = (DeviceData.IsPowerOn || AlwaysPowerOn) ? Brushes.DarkMagenta : Brushes.Gray;

                //draw red board if mfc meets a warning
                rectBkground.Stroke = DeviceData.IsWarning ? Brushes.Red : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0X37, 0X37, 0X37));

                labelValue.Foreground = DeviceData.IsWarning ? Brushes.Pink : Brushes.LightYellow;
                rectBkground.StrokeThickness = DeviceData.IsWarning ? 2 : 1;

                if (dialogBox != null)
                {
                    dialogBox.RealValue = DeviceData.FeedBack.ToString("F1");
                    dialogBox.IsHeaterOn = DeviceData.IsPowerOn;
                    dialogBox.SetPoint = DeviceData.SetPoint;
                    
                }
            }
        }

        private void ExecutePowerOnOff(bool value)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetPowerOnOff", value.ToString());
            }
            else
            {
                Command.Execute(new object[] { DeviceData.DeviceName, AITHeaterOperation.SetPowerOnOff.ToString(), value.ToString() });

            }
        }


        private void ExecuteSetHeaterValue(double power)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.Ramp",
                    power.ToString());
            }
            else
            {
                Command.Execute(new object[]
                    {DeviceData.DeviceName, AITHeaterOperation.Ramp.ToString(), power.ToString()});
            }
        }

        private AITHeaterInputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            if (HideDialog)
                return;

            dialogBox = new AITHeaterInputDialogBox
            {
                SetHeaterValueCommandDelegate = ExecuteSetHeaterValue,
                SetHeaterPowerOnOffCommandDelegate = ExecutePowerOnOff,
                 
                DeviceName = DeviceData.DisplayName,
                DeviceId = DeviceData.DeviceSchematicId,
                DefaultValue = DeviceData.DefaultValue,
                RealValue = DeviceData.FeedBack.ToString("F1"),
                SetPoint = Math.Round(DeviceData.SetPoint, 1),
                MaxValue = DeviceData.Scale,
                Unit = DeviceData.Unit,
                IsHeaterOn = DeviceData.IsPowerOn,
                EnablePowerControl = EnablePowerControl,
                EnableTemperatureSetting = EnableTemperatureSetting,
            };
 
            if (AnalogOwner != null)
                dialogBox.Owner = AnalogOwner;
            dialogBox.Topmost = true;
            dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialogBox.FocasAll();
            dialogBox.ShowDialog();

            dialogBox = null;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format("{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nSetPoint：{5} {4} \r\nFeedback：{6} {4}\r\nHeaterPower：{7}",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.Scale, 
                        DeviceData.Unit,
                        DeviceData.SetPoint.ToString("F1"),
                        DeviceData.FeedBack.ToString("F1"),
                        DeviceData.IsPowerOn ? "On" : "Off");

                ToolTip = tooltipValue;
            }
        }
    }
}

