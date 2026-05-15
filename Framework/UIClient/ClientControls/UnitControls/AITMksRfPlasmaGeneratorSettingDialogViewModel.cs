using System.ComponentModel;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    public class AITMksRfPlasmaGeneratorSettingDialogViewModel : DialogViewModel<string>, INotifyPropertyChanged
    {
        public AITMksRfPlasmaGeneratorSettingDialogViewModel(string dialogName = "")
        {
            this.DisplayName = dialogName;
        }

        private AITRfPowerData _data;

        public AITRfPowerData DeviceData
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

        public string MaxWithUnit
        {
            get
            {
                if (DeviceData == null)
                    return "Max (W) :";
                else
                    return $"Max ({DeviceData.UnitPower}) :";
            }
        }

        public string MaxVoltageWithUnit
        {
            get
            {
                if (DeviceData == null)
                    return "Max (V) :";
                else
                    return $"Max ({DeviceData.UnitVoltage}) :";
            }
        }

        public string MaxCurrentWithUnit
        {
            get
            {
                if (DeviceData == null)
                    return "Max (A) :";
                else
                    return $"Max ({DeviceData.UnitCurrent}) :";
            }
        }
         

        public string PowerWithUnit
        {
            get
            {
                if (DeviceData == null)
                    return "Power (W) :";
                else
                    return $"Power ({DeviceData.UnitPower}) :";
            }
        }

        public string SetPointWithUnit
        {
            get
            {
                if (DeviceData == null)
                    return "SetPower (W) :";
                else
                    return $"SetPower ({DeviceData.UnitPower}) :";
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
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPowerOn}");
        }

        public void SetPowerOff()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetPowerOff}");
        }

        public void SetCurrent()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetCurrent}", InputSetPoint);
        }

        public void SetVoltage()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITRfOperation.SetVoltage}", InputSetPoint);
        }
    }
}
