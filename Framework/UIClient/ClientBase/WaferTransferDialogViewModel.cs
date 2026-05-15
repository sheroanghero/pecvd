using System;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferTransferDialogViewModel : DialogViewModel<WaferTransferCondition>
    {

        private WaferTransferCondition _Conditions;
        public WaferTransferCondition Conditions
        {
            get { return _Conditions; }
            set { _Conditions = value; }
        }

        private string _ConfirmText;
        public string ConfirmText
        {
            get { return _ConfirmText; }
            set { _ConfirmText = value; }
        }

        public bool IsBlade1 { get; set; }
        public bool IsBlade2 { get; set; }

        public bool DisplayPassAlignerCondition { get; set; }
        public bool DisplayPassCoolingCondition { get; set; }
        public bool DisplayBladeCondition { get; set; }

        public Visibility AlignerVisibility { get; set; }
        public Visibility CoolingVisibility { get; set; }
        public Visibility BladeVisibility { get; set; }

        private ICommand _TransferCommand;
        public ICommand TransferCommand
        {
            get
            {
                if (this._TransferCommand == null)
                    this._TransferCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnTransferCommand(arg));
                return this._TransferCommand;
            }
        }

        private ICommand _BtnCancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                if (this._BtnCancelCommand == null)
                    this._BtnCancelCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnCancelCommand(arg));
                return this._BtnCancelCommand;
            }
        }


        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        public WaferTransferDialogViewModel(string message, bool displayPassAlignerCondition, bool displayPassCoolingCondition, bool displayBladeCondition = false)
        {
            AlignerVisibility = Visibility.Visible;
            CoolingVisibility = Visibility.Visible;
            BladeVisibility = displayBladeCondition ? Visibility.Visible : Visibility.Hidden;

            this.DisplayName = "Wafer Transfer Dialog";

            ConfirmText = message;
            Conditions = new WaferTransferCondition();

            DisplayPassAlignerCondition = displayPassAlignerCondition;
            DisplayPassCoolingCondition = displayPassCoolingCondition;
            DisplayBladeCondition = displayBladeCondition;
            try
            {
                var defaultAutoAlign = QueryDataClient.Instance.Service.GetConfig("System.AutoAlignManualTransfer");
                if (!displayPassAlignerCondition)
                    defaultAutoAlign = false;

                var defaultPassCooling = QueryDataClient.Instance.Service.GetConfig("System.AutoPassCooling");
                if (!displayPassCoolingCondition)
                    defaultPassCooling = false;

                Conditions.IsPassAligner = defaultAutoAlign == null || (bool)defaultAutoAlign;
                Conditions.IsPassCooling = defaultPassCooling == null || (bool)defaultPassCooling;

                var alignerAngle = QueryDataClient.Instance.Service.GetConfig("Aligner.DefaultNotchDegree");
                var coolingTime = QueryDataClient.Instance.Service.GetConfig("Cooler.DefaultCoolingTime");

                if (alignerAngle != null)
                    Conditions.AlignerAngle = (int)((double)alignerAngle);

                if (coolingTime != null)
                    Conditions.CoolingTime = (int)coolingTime;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


        }

        private void OnTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            if (IsBlade1)
                Conditions.Blade = RobotArm.ArmA;
            else if (IsBlade2)
                Conditions.Blade = RobotArm.ArmB;

            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnCancelCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            TryClose(false);
        }

    }
}
