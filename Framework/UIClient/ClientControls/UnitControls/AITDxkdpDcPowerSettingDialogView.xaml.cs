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
    public partial class AITDxkdpDcPowerSettingDialogView : UserControl
    {
        public AITDxkdpDcPowerSettingDialogView()
        {
            InitializeComponent();
 
 
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           
            double input;
            if (!double.TryParse(inputBox.Text, out input))
                (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).IsEnableOk = false;
            else if (input < 0 || input > (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).DeviceData.ScaleCurrent)
                (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).IsEnableOk = false;
            else
                (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).IsEnableOk = true;
            inputBox.Foreground = (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).IsEnableOk ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!(this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).IsEnableOk) return;
                if (e.Key == Key.Return)
                {
                    (this.DataContext as AITDxkdpDcPowerSettingDialogViewModel).SetCurrent();
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
