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
    public partial class MfcSettingDialogView : UserControl
    {
        public MfcSettingDialogView()
        {
            InitializeComponent();

            this.Loaded += MfcSettingDialogView_Loaded;
        }

        private void MfcSettingDialogView_Loaded(object sender, RoutedEventArgs e)
        {
 
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           
            double input;
            if (!double.TryParse(inputBox.Text, out input))
                (this.DataContext as MfcSettingDialogViewModel).IsEnableOk = false;
            else if (input < 0 || input > (this.DataContext as MfcSettingDialogViewModel).DeviceData.Scale)
                (this.DataContext as MfcSettingDialogViewModel).IsEnableOk = false;
            else
                (this.DataContext as MfcSettingDialogViewModel).IsEnableOk = true;
            inputBox.Foreground = (this.DataContext as MfcSettingDialogViewModel).IsEnableOk ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!(this.DataContext as MfcSettingDialogViewModel).IsEnableOk) return;
                if (e.Key == Key.Return)
                {
                    (this.DataContext as MfcSettingDialogViewModel).OK();
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
