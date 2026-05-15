using System;
using System.ComponentModel;
using System.Windows.Input;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    public class TVSettingDialogViewModel : DialogViewModel<string>, INotifyPropertyChanged
    {


        private AITThrottleValveData _data;

        public AITThrottleValveData DeviceData
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;

                NotifyOfPropertyChange(nameof(DeviceData));
                NotifyOfPropertyChange(nameof(IsPositionMode));
                NotifyOfPropertyChange(nameof(IsPressureMode));
            }
        }

        private string _setPointPosition;
        public string InputSetPointPosition
        {
            get { return _setPointPosition; }
            set
            {
                _setPointPosition = value;
                NotifyOfPropertyChange(nameof(InputSetPointPosition));
            }
        }

        private string _setPointPressure;
        public string InputSetPointPressure
        {
            get { return _setPointPressure; }
            set
            {
                _setPointPressure = value;
                NotifyOfPropertyChange(nameof(InputSetPointPressure));
            }
        }

        public bool IsPositionMode
        {
            get
            {
                return DeviceData != null && DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl;
            }
            set
            {

            }
        }

        private bool _isPressureMode;
        public bool IsPressureMode
        {
            get
            {
                //return _isPressureMode;

                return DeviceData != null && DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl;
            }
            set
            {
                if (value != _isPressureMode)
                {

                }
                _isPressureMode = value;
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
 
        public ICommand PositionCommand { get; set; }

        public TVSettingDialogViewModel(string dialogName = "")
        {
            this.DisplayName = dialogName;

             
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

        public void SetPosition()
        {
            if (IsPositionMode)
                return;

            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", PressureCtrlMode.TVPositionCtrl.ToString());

        }

        public void SetPressure()
        {
            if (IsPressureMode)
                return;

            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", PressureCtrlMode.TVPressureCtrl.ToString());

        }



        public void Set()
        {
            if (IsPressureMode)
                SetPressureExecute(Convert.ToDouble(InputSetPointPressure));
            else if (IsPositionMode)
                SetPositionExecute(Convert.ToDouble(InputSetPointPosition));
        }

        private void SetPressureExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPressure}", (float)value);
        }

        private void SetPositionExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPosition}", (float)value);
        }

    }
}
