using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// AITMksRfPlasmaGenerator.xaml 的交互逻辑
    /// </summary>
    public partial class AITMksRfPlasmaGenerator : UserControl
    {
        public AITMksRfPlasmaGenerator()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITMksRfPlasmaGenerator),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITRfPowerData), typeof(AITMksRfPlasmaGenerator),
                        new FrameworkPropertyMetadata(new AITRfPowerData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsMicrowaveModeProperty = DependencyProperty.Register(
            "IsMicrowaveMode", typeof(bool), typeof(AITMksRfPlasmaGenerator),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public bool IsMicrowaveMode
        {
            get
            {
                return (bool)this.GetValue(IsMicrowaveModeProperty);
            }
            set
            {
                this.SetValue(IsMicrowaveModeProperty, value);
            }
        }


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
        public AITRfPowerData DeviceData
        {
            get
            {
                return (AITRfPowerData)this.GetValue(DeviceDataProperty);
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
                if (DeviceData.IsToleranceError)
                {
                    rectFeedback.Stroke = Brushes.OrangeRed;
                }
                else if (DeviceData.IsToleranceWarning)
                {
                    rectFeedback.Stroke = Brushes.Yellow;
                }
                else
                {
                    rectFeedback.Stroke = Brushes.Gray;
                }

                if (DeviceData.IsRfAlarm)
                {
                    rectSetPoint.Stroke = Brushes.OrangeRed;
                }
                else if (!DeviceData.IsInterlockOk)
                {
                    rectSetPoint.Stroke = Brushes.Yellow;
                }
                else
                {
                    rectSetPoint.Stroke = Brushes.Gray;
                }

                if (DeviceData.IsRfOn)
                {
                    rectFeedback.Fill = Brushes.HotPink;
                }
                else
                {
                    rectFeedback.Fill = Brushes.MediumPurple;
                }

                labelValue.Content = $"{DeviceData.ForwardPower:F1} : {DeviceData.ReflectPower:F1}";
                labelSetPoint.Content = $"{DeviceData.PowerSetPoint:F1} {DeviceData.UnitPower}";

                if (_dialogRf != null)
                {
                    _dialogRf.DeviceData = DeviceData;
                    _dialogRf.DeviceData.InvokePropertyChanged();

                    _dialogRf.NotifyOfPropertyChange(nameof(_dialogRf.IsEnablePowerOff));
                    _dialogRf.NotifyOfPropertyChange(nameof(_dialogRf.IsEnablePowerOn));
                }
            }
        }

        private AITMksRfPlasmaGeneratorSettingDialogViewModel _dialogRf;
 
        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            

            _dialogRf = new AITMksRfPlasmaGeneratorSettingDialogViewModel($"{DeviceData.DisplayName} Setting");
            _dialogRf.DeviceData = DeviceData;
            _dialogRf.InputSetPoint = DeviceData.PowerSetPoint.ToString("F1");

            WindowManager wm = new WindowManager();

            Window owner = Application.Current.MainWindow;
            if (owner != null)
            {
                Mouse.Capture(owner);
                Point pointToWindow = Mouse.GetPosition(owner);
                Point pointToScreen = owner.PointToScreen(pointToWindow);
                pointToScreen.X = pointToScreen.X + 50;
                pointToScreen.Y = pointToScreen.Y - 150;
                Mouse.Capture(null);

                wm.ShowDialog(_dialogRf, pointToScreen);
            }
            else
            {
                wm.ShowDialog(_dialogRf);
            }
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

                           string.Format("{0}：{1}\r\n\r\nID：{2}\r\nMax (A)：{3} {7}\r\nForward：{4} W\r\nReverse：{5} W\r\nSetPoint：{6} {7}",
                                "DC",
                                DeviceData.DisplayName,
                                DeviceData.DeviceSchematicId,
                                DeviceData.ScalePower.ToString("F1"),
                                DeviceData.ForwardPower.ToString("F1"),
                                DeviceData.ReflectPower.ToString("F1"),
                                DeviceData.PowerSetPoint.ToString("F1"),
                                DeviceData.UnitPower);

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

