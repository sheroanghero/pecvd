using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferUIMoveDialogViewModel : DialogViewModel<WaferUIMoveCondition>
    {
        private List<string> _modules = new List<string>() { };
        private Dictionary<string, int> moduleSlotInfo = new Dictionary<string, int>();
        private List<string> _blades = new List<string>() { "Blade1", "Blade2" };
        private List<int> GetVCESlots()
        {
            List<int> slots = new List<int>();

            int num = 25;


            int j = 0;
            while (j < num)
            {
                slots.Add(j + 1);
                j++;
            }
            return slots;
        }

   



        public List<string> PMModules
        {
            get { return _modules; }
            set { _modules = value; NotifyOfPropertyChange("PMModules"); }
        }

        public List<string> Modules
        {
            get { return _modules; }
            set { _modules = value; NotifyOfPropertyChange("Modules"); }
        }

        public List<string> TMSlots
        {
            get { return _blades; }
            set { _blades = value; NotifyOfPropertyChange("TMSlots"); }
        }

        private string _PMSelectedModule;
        public string PMSelectedModule
        {
            get { return _PMSelectedModule; }
            set { _PMSelectedModule = value; NotifyOfPropertyChange("PMSelectedModule"); }
        }

        private string _SelectedModule;
        public string SelectedModule
        {
            get { return _SelectedModule; }
            set { _SelectedModule = value;
                SelectedModuleSlots = GetSlotsByModule(_SelectedModule);
                SelectedSlot = 1;
                NotifyOfPropertyChange("SelectedModule"); }
        }

        private List<int> _SelectedModuleSlots;
        public List<int> SelectedModuleSlots
        {
            get { return _SelectedModuleSlots; }
            set { _SelectedModuleSlots = value; NotifyOfPropertyChange("SelectedModuleSlots"); }

        }

        private int _SelectedSlot;
        public int SelectedSlot
        {
            get { return _SelectedSlot; }
            set { _SelectedSlot = value; NotifyOfPropertyChange("SelectedSlot"); }
        }

        private int _VCEASelectedSlot;
        public int VCEASelectedSlot
        {
            get { return _VCEASelectedSlot; }
            set { _VCEASelectedSlot = value; NotifyOfPropertyChange("VCEASelectedSlot"); }
        }

        private int _VCEBSelectedSlot;
        public int VCEBSelectedSlot
        {
            get { return _VCEBSelectedSlot; }
            set { _VCEBSelectedSlot = value; NotifyOfPropertyChange("VCEBSelectedSlot"); }
        }

        private string _TMSelectedBlade;
        public string TMSelectedBlade
        {
            get { return _TMSelectedBlade; }
            set { _TMSelectedBlade = value; NotifyOfPropertyChange("TMSelectedBlade"); }
        }

        private WaferUIMoveCondition _Conditions;
        public WaferUIMoveCondition Conditions
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

        private ICommand _TMTransferCommand;
        public ICommand TMTransferCommand
        {
            get
            {
                if (this._TMTransferCommand == null)
                    this._TMTransferCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnTMTransferCommand(arg));
                return this._TMTransferCommand;
            }
        }

        private ICommand _PMTransferCommand;
        public ICommand PMTransferCommand
        {
            get
            {
                if (this._PMTransferCommand == null)
                    this._PMTransferCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnPMTransferCommand(arg));
                return this._PMTransferCommand;
            }
        }

        private ICommand _VCEATransferCommand;
        public ICommand VCEATransferCommand
        {
            get
            {
                if (this._VCEATransferCommand == null)
                    this._VCEATransferCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnVCEATransferCommand(arg));
                return this._VCEATransferCommand;
            }
        }

        private ICommand _VCEBTransferCommand;
        public ICommand VCEBTransferCommand
        {
            get
            {
                if (this._VCEBTransferCommand == null)
                    this._VCEBTransferCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnVCEBTransferCommand(arg));
                return this._VCEBTransferCommand;
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

        public WaferUIMoveDialogViewModel(Dictionary<string, int> moduleA, string message)
        {
            this.DisplayName = "Wafer Move Dialog";

            ConfirmText = message;
            Conditions = new WaferUIMoveCondition();
            moduleSlotInfo = moduleA;
            List<string> cur = new List<string>();
            foreach (string a in moduleA.Keys)
            {
                cur.Add(a.ToString());
            }
            Modules = cur;
            SelectedModule = cur[0];
            SelectedSlot = 1;
      

            TMSelectedBlade = "Blade1";
            SelectedModule = "TMRobot";
      
        }
        private List<int> GetSlotsByModule(string module)
        {
            int count = 0;
            if (moduleSlotInfo.ContainsKey(module))
            {
                count = moduleSlotInfo[module];
                List<int> c = new List<int>();
                for (int i = 1; i <= count; i++)
                {
                    c.Add(i);
                }
                return c;
            }
            else
            {
                return new List<int>() { 1 };
            }

        }
        private void OnTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {

            Conditions.MoveToModule = SelectedModule;

            Conditions.MoveToSlot = SelectedSlot - 1;

            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnTMTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            Conditions.MoveToModule = "TMRobot";

            if (TMSelectedBlade == "Blade1")
            {
                Conditions.MoveToSlot = 0;
            }
            else
            {
                Conditions.MoveToSlot = 1;
            }


            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnPMTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            Conditions.MoveToModule = SelectedModule;

            Conditions.MoveToSlot = 0;

            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnVCEATransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            Conditions.MoveToModule = "VCEA";

            Conditions.MoveToSlot = VCEASelectedSlot - 1;

            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnVCEBTransferCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            Conditions.MoveToModule = "VCEB";

            Conditions.MoveToSlot = VCEBSelectedSlot - 1;

            DialogResult = Conditions;

            TryClose(true);
        }

        private void OnCancelCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            TryClose(false);
        }

    }
}
