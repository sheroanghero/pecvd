using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;

namespace MECF.Framework.UI.Client.ClientControls.UnitControls
{
    /// <summary>
    /// UpcSettingDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class UpcSettingDialogView : UserControl
    {
        public UpcSettingDialogView()
        {
            InitializeComponent();
        }
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            double input;
            if (!double.TryParse(inputBox.Text, out input))
                (this.DataContext as UpcSettingDialogViewModel).IsEnableOk = false;
            else if (input < 0 || input > (this.DataContext as UpcSettingDialogViewModel).DeviceData.Scale)
                (this.DataContext as UpcSettingDialogViewModel).IsEnableOk = false;
            else
                (this.DataContext as UpcSettingDialogViewModel).IsEnableOk = true;
            inputBox.Foreground = (this.DataContext as UpcSettingDialogViewModel).IsEnableOk ?
                System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Red;

        }

        private void OnEnterKeyIsHit(object sender, KeyEventArgs e)
        {
            try
            {
                if (!(this.DataContext as UpcSettingDialogViewModel).IsEnableOk) return;
                if (e.Key == Key.Return)
                {
                    (this.DataContext as UpcSettingDialogViewModel).OK();
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
    }
}
