using System;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferTransferDualDialogViewModel : DialogViewModel<WaferTransferCondition>
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

        public bool DisplayPassAlignerCondition { get; set; }
        public bool DisplayPassCoolingCondition { get; set; }
        public bool AddMoreWafer { get; set; }
        public Visibility CoolingVisible => DisplayPassCoolingCondition ? Visibility.Visible : Visibility.Hidden;
        public Visibility AlignerVisible => DisplayPassAlignerCondition ? Visibility.Visible : Visibility.Hidden;

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

        private ICommand _AddMoreCommand;
        public ICommand AddMoreCommand
        {
            get
            {
                if (this._AddMoreCommand == null)
                    this._AddMoreCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnAddMoreWaferCommand(arg));
                return this._AddMoreCommand;
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

        public WaferTransferDualDialogViewModel(string message, bool displayPassAlignerCondition, bool displayPassCoolingCondition)
        {
            this.DisplayName = "Wafer Transfer Dialog";

            ConfirmText = message;
            Conditions = new WaferTransferCondition();

            DisplayPassAlignerCondition = displayPassAlignerCondition;
            DisplayPassCoolingCondition = displayPassCoolingCondition;
            try
            {
                if(DisplayPassAlignerCondition)
                {
                    var defaultAutoAlign = QueryDataClient.Instance.Service.GetConfig("System.AutoAlignManualTransfer");
                    if (!displayPassAlignerCondition)
                        defaultAutoAlign = false;

                    Conditions.IsPassAligner = true;
                    var alignerAngle = QueryDataClient.Instance.Service.GetConfig("Aligner.DefaultNotchDegree");
                    if (alignerAngle != null)
                        Conditions.AlignerAngle = (int)((double)alignerAngle);
                }

                if(DisplayPassCoolingCondition)
                {
                    var defaultPassCooling = QueryDataClient.Instance.Service.GetConfig("System.AutoPassCooling");
                    if (!displayPassCoolingCondition)
                        defaultPassCooling = false;

                    Conditions.IsPassCooling = false;
                    var coolingTime = QueryDataClient.Instance.Service.GetConfig("LoadLock.DefaultCoolingTime");

                    if (coolingTime != null)
                        Conditions.CoolingTime = (int)coolingTime;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }


        }

        private void OnTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            DialogResult = Conditions;
            AddMoreWafer = false;
            TryClose(true);
        }

        private void OnAddMoreWaferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            DialogResult = Conditions;
            AddMoreWafer = true;
            TryClose(true);
        }

        private void OnCancelCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            TryClose(false);
        }

    }
}
