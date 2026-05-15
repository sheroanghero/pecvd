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
using Aitex.Core.RT.Log;
using Aitex.Core.UI.Control;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITRfGenerator.xaml 的交互逻辑
    /// </summary>
    public partial class AITRfGenerator : UserControl
    {
        public AITRfGenerator()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITRfGenerator),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITRfData), typeof(AITRfGenerator),
                        new FrameworkPropertyMetadata(new AITRfData(), FrameworkPropertyMetadataOptions.AffectsRender));

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
        public AITRfData DeviceData
        {
            get
            {
                return (AITRfData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                //draw red board if  meets a warning
                rectBkground.Stroke = DeviceData.HasError ? Brushes.Red : Brushes.Gray;

                //labelValue.Foreground = !DeviceData.IsInterlockOK ? Brushes.Pink : Brushes.MidnightBlue;
                rectBkground.StrokeThickness = DeviceData.HasError ? 2 : 1;

                if (dialogBox != null)
                {
                    dialogBox.ForwardPower = DeviceData.ForwardPower ;
                    dialogBox.ReflectPower = DeviceData.ReflectPower ;
                    dialogBox.IsRfOn = DeviceData.IsRfOn;
                    dialogBox.SetPointPower = DeviceData.PowerSetPoint;
                    dialogBox.IsContinuousMode = DeviceData.WorkMode == (int) RfMode.ContinuousWaveMode;
                    dialogBox.IsPulsingMode = DeviceData.WorkMode == (int) RfMode.PulsingMode;
                    dialogBox.Voltage = DeviceData.Voltage;
                    dialogBox.Current = DeviceData.Current;
                }

                 
                rectBkground.Fill = DeviceData.IsRfOn ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF1, 0xA2, 0xE4)) : (!DeviceData.HasError ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD2, 0xD3, 0xD7)) : Brushes.Red);
                 
                if (DeviceData.EnableVoltageCurrent)
                {
                    labelValue.Content = string.Format("{0}/{1}/{2}", DeviceData.ForwardPower.ToString("F0"), DeviceData.Voltage.ToString("F0"), DeviceData.Current.ToString("F0"));
                }
                else
                {
                    labelValue.Content = string.Format("{0} : {1}", DeviceData.ForwardPower.ToString("F1"), DeviceData.ReflectPower.ToString("F1"));
                }
                
            }
        }

        private AITRfInputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            dialogBox = new AITRfInputDialogBox
            {
                SetRfModeCommandDelegate = ExecuteSetMode,
                SetContinuousCommandDelegate = ExecuteContinuous,
                SetPulsingCommandDelegate = ExecutePulsing,
                SetRfPowerOnOffCommandDelegate = ExecutePowerOnOff,

                DeviceName = DeviceData.DisplayName,
                DeviceId = DeviceData.DeviceSchematicId,
                ForwardPower = DeviceData.ForwardPower,
                ReflectPower = DeviceData.ReflectPower,
                IsRfOn = DeviceData.IsRfOn,
                Voltage = DeviceData.Voltage,
                Current = DeviceData.Current,

                SetPointPower = Math.Round(DeviceData.PowerSetPoint, 1),
                MaxValuePower = DeviceData.ScalePower,
                UnitPower = DeviceData.UnitPower,

                SetPointFrequency = Math.Round(DeviceData.FrequencySetPoint, 1),
                MaxValueFrequency = DeviceData.ScaleFrequency,
                UnitFrequency = DeviceData.UnitFrequency,

                SetPointDuty = Math.Round(DeviceData.DutySetPoint, 1),
                MaxValueDuty = DeviceData.ScaleDuty,
                UnitDuty = DeviceData.UnitDuty,

                 IsContinuousMode = DeviceData.WorkMode == (int)RfMode.ContinuousWaveMode,
                 IsPulsingMode = DeviceData.WorkMode==(int)RfMode.PulsingMode,
                 EnablePulsing = DeviceData.EnablePulsing,

                 GridLengthReflect = DeviceData.EnableReflectPower ? GridLength.Auto : new GridLength(0),
                GridLengthVoltageCurrent = DeviceData.EnableVoltageCurrent ? GridLength.Auto : new GridLength(0),
                GridLengthWorkMode = DeviceData.EnablePulsing ? GridLength.Auto : new GridLength(0),
            };

            if (AnalogOwner != null)
                dialogBox.Owner = AnalogOwner;
            dialogBox.Topmost = true;
            dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialogBox.FocasAll();
            dialogBox.ShowDialog();

            dialogBox = null;
        }

        private void ExecuteSetMode(RfMode value)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetMode}", value.ToString());
                return;
            }

            Command.Execute(new object[] { DeviceData.DeviceName, AITRfOperation.SetMode.ToString(), value.ToString() });
        }

        private void ExecutePowerOnOff(bool value)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPowerOnOff}", value.ToString());
                return;
            }

            Command.Execute(new object[] { DeviceData.DeviceName, AITRfOperation.SetPowerOnOff.ToString(), value.ToString() });
        }


        private void ExecuteContinuous(double power)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetContinuousPower}", power.ToString());
                return;
            }

            Command.Execute(new object[] { DeviceData.DeviceName, AITRfOperation.SetContinuousPower.ToString(), power.ToString() });
        }

        private void ExecutePulsing(double power, double frequency, double duty)
        {
            if (Command == null)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPulsingPower}", power.ToString());
                return;
            }

            Command.Execute(new object[] { DeviceData.DeviceName, AITRfOperation.SetPulsingPower.ToString(), power.ToString(), frequency.ToString(), duty.ToString() });
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                try
                {
                    string tooltipValue =
                        DeviceData.EnableVoltageCurrent
                            ? string.Format("{0}：{1}\r\n\r\nID：{2}\r\nForward Power：{3} w\r\nVoltage：{4} \r\nCurrent：{5} \r\nSetPoint：{6}  w",
                                "RF",
                                DeviceData.DisplayName,
                                DeviceData.DeviceSchematicId,
                                DeviceData.ForwardPower.ToString("F1"),
                                DeviceData.Voltage.ToString("F1"),
                                DeviceData.Current.ToString("F1"),
                                DeviceData.PowerSetPoint.ToString("F1"))
                            : string.Format("{0}：{1}\r\n\r\nID：{2}\r\nForward Power：{3} w\r\nReflect Power：{4} w \r\nSetPoint：{5}  w",
                                "RF",
                                DeviceData.DisplayName,
                                DeviceData.DeviceSchematicId,
                                DeviceData.ForwardPower.ToString("F1"),
                                DeviceData.ReflectPower.ToString("F1"),
                                DeviceData.PowerSetPoint.ToString("F1"));
                    ToolTip = tooltipValue;
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }

            }
        }
    }
}

