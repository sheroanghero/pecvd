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
    public partial class AITRfPowerSettingDialogView : UserControl
    {
        public AITRfPowerSettingDialogView()
        {
            InitializeComponent();
 
 
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           
            double input;
            if (!double.TryParse(inputBox.Text, out input))
                (this.DataContext as AITRfPowerSettingDialogViewModel).IsEnableOk = false;
            else if (input < 0 || input > (this.DataContext as AITRfPowerSettingDialogViewModel).DeviceData.ScalePower)
                (this.DataContext as AITRfPowerSettingDialogViewModel).IsEnableOk = false;
            else
                (this.DataContext as AITRfPowerSettingDialogViewModel).IsEnableOk = true;
            inputBox.Foreground = (this.DataContext as AITRfPowerSettingDialogViewModel).IsEnableOk ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!(this.DataContext as AITRfPowerSettingDialogViewModel).IsEnableOk) return;
                if (e.Key == Key.Return)
                {
                    (this.DataContext as AITRfPowerSettingDialogViewModel).SetPower();
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
