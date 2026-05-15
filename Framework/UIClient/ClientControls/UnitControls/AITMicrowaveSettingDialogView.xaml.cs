using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// InputFileNameDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class AITMicrowaveSettingDialogView : UserControl
    {
        public AITMicrowaveSettingDialogView()
        {
            InitializeComponent();
 
 
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           
            double input;
            if (!double.TryParse(inputBox.Text, out input))
                (this.DataContext as AITMicrowaveSettingDialogViewModel).IsEnableOk = false;
            else if (input < 0 || input > (this.DataContext as AITMicrowaveSettingDialogViewModel).DeviceData.ScalePower)
                (this.DataContext as AITMicrowaveSettingDialogViewModel).IsEnableOk = false;
            else
                (this.DataContext as AITMicrowaveSettingDialogViewModel).IsEnableOk = true;
            inputBox.Foreground = (this.DataContext as AITMicrowaveSettingDialogViewModel).IsEnableOk ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!(this.DataContext as AITMicrowaveSettingDialogViewModel).IsEnableOk) return;
                if (e.Key == Key.Return)
                {
                    (this.DataContext as AITMicrowaveSettingDialogViewModel).SetPower();
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
