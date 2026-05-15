using System.ComponentModel;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.CommonData.DeviceData;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using MECF.Framework.UI.Client.Ctrlib.UnitControls;
using OpenSEMI.ClientBase;


namespace MECF.Framework.UI.Client.ClientControls.UnitControls
{
   public  class UpcSettingDialogViewModel : DialogViewModel<string>, INotifyPropertyChanged
    {
        public UpcSettingDialogViewModel(string dialogName = "")
        {
            this.DisplayName = dialogName;
        }

        private AITUPCData _data;

        public AITUPCData DeviceData
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;


                NotifyOfPropertyChange(nameof(DeviceData));
            }
        }

        private string _setPoint;

        public string InputSetPoint
        {
            get { return _setPoint; }
            set
            {
                _setPoint = value;
                NotifyOfPropertyChange(nameof(InputSetPoint));
            }
        }

        private bool _enableOk;
        public bool IsEnableOk
        {
            get
            {
                return _enableOk;
            }
            set
            {
                _enableOk = value;
                NotifyOfPropertyChange(nameof(IsEnableOk));
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
            UpcSettingDialogView v = (UpcSettingDialogView)view;
        }

        public void OK()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.UniqueName}.SetRecipeControl", InputSetPoint, 0);

            //this.DialogResult = string.Empty;
            //IsCancel = false;
            //TryClose(true);
        }
    }
}
