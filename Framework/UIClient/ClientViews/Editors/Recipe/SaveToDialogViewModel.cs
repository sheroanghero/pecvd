using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using MECF.Framework.Common.CommonData;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class ChamberItem : NotifiableItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; }
    }
    public class SaveToDialogViewModel : DialogViewModel<string>
    {
        public string Chamber { get; set; }
 
        public ObservableCollection<ChamberItem> Chambers { get; set; }

        public SaveToDialogViewModel(string dialogName, string chamber, List<string> chambers)
        {
            this.DisplayName = dialogName;

            Chambers  = new ObservableCollection<ChamberItem>();
            foreach (var chamber1 in chambers)
            {
                Chambers.Add(new ChamberItem()
                {
                    IsChecked = false,
                    IsEnabled = chamber1!=chamber,
                    Name = chamber1,
                });
            }
        }


        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            SaveToDialogView v = (SaveToDialogView)view;
            FocusManager.SetFocusedElement(v, v.ListBoxChamber);
        }

        public void OK()
        {
            this.DialogResult = Chamber;
            IsCancel = false;
            TryClose(true);
        }
    }
}
