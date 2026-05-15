using System;
using System.Windows.Controls;
using MECF.Framework.UI.Client.ClientBase;

namespace OpenSEMI.ClientBase
{
    public class MessageDialogViewModel : MessageDialog
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Dialog Box";
        }

        public void OnButtonClick(object sender)
        {
            Button m_btn = sender as Button;
            DialogResult = (DialogButton)Enum.Parse(typeof(DialogButton), m_btn.Content.ToString());
            TryClose();
        }
    }
}
