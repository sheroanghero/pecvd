using System.ComponentModel;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using DocumentFormat.OpenXml.Presentation;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    public class AITMicrowaveSettingDialogViewModel : DialogViewModel<string>, INotifyPropertyChanged
    {
        public AITMicrowaveSettingDialogViewModel(string dialogName = "")
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
                NotifyOfPropertyChange(nameof(IsEnableSet));
            }
        }

        public bool IsEnableSet
        {
            get
            {
                return float.Parse(InputSetPoint) <= DeviceData.ScalePower;
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

        public bool IsEnableHeatOn
        {
            get
            {
                if (DeviceData == null || !DeviceData.AttrValue.ContainsKey("HeatOnSetPoint"))
                    return false;

                return !(bool)DeviceData.AttrValue["HeatOnSetPoint"];
            }
        }

        public bool IsEnableHeatOff
        {
            get
            {
                if (DeviceData == null || !DeviceData.AttrValue.ContainsKey("HeatOnSetPoint"))
                    return false;

                return (bool)DeviceData.AttrValue["HeatOnSetPoint"];
            }
        }

        public bool IsHeatOn
        {
            get
            {
                if (DeviceData == null || !DeviceData.AttrValue.ContainsKey("HeatOnComplete"))
                    return false;

                return (bool)DeviceData.AttrValue["HeatOnComplete"];
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

        public void SetHeatOn()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetHeatOnOff}", "true");

        }

        public void SetHeatOff()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetHeatOnOff}", "false");

        }
    }
}
