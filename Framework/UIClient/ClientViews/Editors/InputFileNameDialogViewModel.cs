using System.Windows.Input;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{
    public class InputFileNameDialogViewModel : DialogViewModel<string>
    {
        public InputFileNameDialogViewModel(string dialogName = "")
        {
            this.DisplayName = dialogName;
        }

        public string FileName { get; set; }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            InputFileNameDialogView v = (InputFileNameDialogView)view;
            FocusManager.SetFocusedElement(v, v.tbName);
        }


        public void OK()
        {
            this.DialogResult = FileName;
            IsCancel = false;
            TryClose(true);
        }
    }
}
