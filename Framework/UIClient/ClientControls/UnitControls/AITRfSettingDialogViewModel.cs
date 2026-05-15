using System.ComponentModel;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    public class AITRfSettingDialogViewModel : DialogViewModel<string>, INotifyPropertyChanged
    {
        public AITRfSettingDialogViewModel(string dialogName = "")
        {
            this.DisplayName = dialogName;
        }

        private AITRfData _data;

        public AITRfData DeviceData
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
 

        public bool IsEnablePowerOn
        {
            get
            {
                return DeviceData != null && !DeviceData.IsRfOn;
            }
        }

        public bool IsEnablePowerOff
        {
            get
            {
                return DeviceData != null && DeviceData.IsRfOn;
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
            
        }

        public void SetPower()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPower}", InputSetPoint);

        }

        public void SetPowerOn()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPowerOnOff}", "true");

        }

        public void SetPowerOff()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPowerOnOff}", "false");

        }
    }
}
