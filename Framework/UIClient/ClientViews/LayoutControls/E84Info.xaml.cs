using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Core.View.Common;

namespace MECF.Framework.UI.Client.CenterViews.Controls
{
    public partial class E84Info : UserControl
    {
        public static readonly DependencyProperty LoadPortNameProperty = DependencyProperty.Register(
            "LoadPortName", typeof(string), typeof(E84Info), new
                PropertyMetadata(""));

        public static readonly DependencyProperty E84DataProperty = DependencyProperty.Register(
            "E84Data", typeof(E84InfoData), typeof(E84Info), new PropertyMetadata(null));

        private readonly E84InfoViewModel _e84InfoViewModel;

        public E84Info()
        {
            InitializeComponent();
            root.DataContext = this;
            _e84InfoViewModel = new E84InfoViewModel();
            ConfigBorder.DataContext = _e84InfoViewModel;
            ConfigBorder1.DataContext = _e84InfoViewModel;

            Inputs.DataContext = this;
            Outputs.DataContext = this;
            //BindingErrorTraceListener.SetTrace();
            E84Command = new DelegateCommand<string>(DoLoadPortCmd);
            IsVisibleChanged += E84Info_IsVisibleChanged;
        }

        public ICommand E84Command { get; set; }

        public string LoadPortName
        {
            get => (string) GetValue(LoadPortNameProperty);
            set => SetValue(LoadPortNameProperty, value);
        }

        public E84InfoData E84Data
        {
            get => (E84InfoData) GetValue(E84DataProperty);
            set => SetValue(E84DataProperty, value);
        }

        private void DoLoadPortCmd(string cmd)
        {
            var deviceName = LoadPortName;
            var param = new object[] {deviceName};
            InvokeClient.Instance.Service.DoOperation($"{param[0]}.{cmd}");
        }

        private void E84Info_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (DataContext as E84InfoViewModel)?.EnableTimer(IsVisible);
        }
    }

    internal class E84InfoViewModel : UiViewModelBase
    {
        public E84InfoViewModel()
        {
            SetConfigCommand = new DelegateCommand<object>(SetConfig);
            ConfigFeedback = new E84SCs();
            ConfigSetPoint = new E84SCs();
            ConfigFeedback.UpdateKeys(typeof(E84SCs).GetProperties());
            ConfigSetPoint.UpdateKeys(typeof(E84SCs).GetProperties());
            UpdateConfig();
        }

        [IgnorePropertyChange] public ICommand SetConfigCommand { get; }
        [IgnorePropertyChange] public E84SCs ConfigFeedback { get; set; }

        [IgnorePropertyChange] public E84SCs ConfigSetPoint { get; set; }

        public bool AutomaticMode { get; set; }
        public bool KeepClampedAfterRun { get; set; }
        public bool KeepDockAfterRun { get; set; }
        public bool CloseDoorAfterMap { get; set; }

        [Subscription("E84State")] public string E84Status { get; set; }


        private void SetConfig(object param)
        {
            var sc = (object[]) param;
            InvokeClient.Instance.Service.DoOperation("System.SetConfig", sc[0].ToString(), sc[1].ToString());
            UpdateConfig();
        }

        public void UpdateConfig()
        {
            ConfigFeedback.Update(QueryDataClient.Instance.Service.PollConfig(ConfigFeedback.GetKeys()));
            ConfigSetPoint.Update(QueryDataClient.Instance.Service.PollConfig(ConfigSetPoint.GetKeys()));
            Refresh();
        }

        protected override void Poll()
        {
            Refresh();
            base.Poll();
        }
    }

    public class E84SCs : PageSCValue
    {
        public int Fa_E84_TP1 { get; set; }
        public int Fa_E84_TP2 { get; set; }
        public int Fa_E84_TP3 { get; set; }
        public int Fa_E84_TP4 { get; set; }
        public int Fa_E84_TP5 { get; set; }
        public int Fa_E84_TP6 { get; set; }
        public bool LoadPort_CarrierIDReaderInUse { get; set; } 
        public int LoadPort_DataReadSize { get; set; }
        public int LoadPort_StartPage { get; set; }
        public int LoadPort_DefaultPage { get; set; }
    }

    public class E84InfoData : INotifyPropertyChanged
    {
        private bool _busy;
        private string _carrierId;
        private bool _continuousTransfer;
        private bool _cs0;
        private bool _cs1;
        private string _e84State;
        private bool _emergencyOk;
        private bool _errorOnPlacementTimeout;
        private bool _handoffAvailable;

        private string _loadPortState;
        private string _AccessMode;
        
        //Factory Outputs
        private bool _loadRequest;
        private bool _podDocked;

        private bool _podLatched;
        private bool _podOpen;

        private bool _podPlaced;
        private bool _podReserved;
        private string _portState;
        private bool _potPresent;
        private bool _readyToTransfer;
        private string _slotMap;
        private bool _transferComplete;
        private bool _transferRequest;

        private bool _unloadRequest;

        //Factory Inputs
        private bool _valid;
        private bool _virtualMode;
        private string _carrierIDStatus;
        public string CarrierIDStatus
        {
            get => _carrierIDStatus;
            set
            {
                _carrierIDStatus = value;
                OnPropertyChanged("CarrierIDStatus");
            }
        }
        private string _slotmapStatus;
        public string SlotMapStatus
        {
            get => _slotmapStatus;
            set
            {
                _slotmapStatus = value;
                OnPropertyChanged("SlotMapStatus");
            }
        }
        private string _accessStatus;
        public string AccessStatus
        {
            get => _accessStatus;
            set
            {
                _accessStatus = value;
                OnPropertyChanged("AccessStatus");
            }
        }







        public bool PodPresent
        {
            get => _potPresent;
            set
            {
                _potPresent = value;
                OnPropertyChanged("PodPresent");
            }
        }

        public bool PodPlaced
        {
            get => _podPlaced;
            set
            {
                _podPlaced = value;
                OnPropertyChanged("PodPlaced");
            }
        }

        public bool PodLatched
        {
            get => _podLatched;
            set
            {
                _podLatched = value;
                OnPropertyChanged("PodLatched");
            }
        }

        public bool PodDocked
        {
            get => _podDocked;
            set
            {
                _podDocked = value;
                OnPropertyChanged("PodDocked");
            }
        }

        public bool PodOpen
        {
            get => _podOpen;
            set
            {
                _podOpen = value;
                OnPropertyChanged("PodOpen");
            }
        }

        public string LoadPortState
        {
            get => $"_{_loadPortState}";
            set
            {
                _loadPortState = value;
                OnPropertyChanged("LoadPortState");
            }
        }

        public string AccessMode
        {
            get => $"_{_AccessMode}";
            set
            {
                _AccessMode = value;
                OnPropertyChanged("AccessMode");
            }
        }

        public bool VirtualMode
        {
            get => _virtualMode;
            set
            {
                _virtualMode = value;
                OnPropertyChanged("VirtualMode");
            }
        }

        public bool PodReserved
        {
            get => _podReserved;
            set
            {
                _podReserved = value;
                OnPropertyChanged("PodReserved");
            }
        }

        public string PortState
        {
            get =>$"_{_portState}";
            set
            {
                _portState = value;
                OnPropertyChanged("PortState");
            }
        }

        public string CarrierID
        {
            get => _carrierId;
            set
            {
                _carrierId = value;
                OnPropertyChanged("CarrierID");
            }
        }

        public string SlotMap
        {
            get => _slotMap;
            set
            {
                _slotMap = value;
                OnPropertyChanged("SlotMap");
            }
        }

        public bool Valid
        {
            get => _valid;
            set
            {
                _valid = value;
                OnPropertyChanged("Valid");
            }
        }

        public bool TransferRequest
        {
            get => _transferRequest;
            set
            {
                _transferRequest = value;
                OnPropertyChanged("TransferRequest");
            }
        }

        public bool Busy
        {
            get => _busy;
            set
            {
                _busy = value;
                OnPropertyChanged("Busy");
            }
        }

        public bool TransferComplete
        {
            get => _transferComplete;
            set
            {
                _transferComplete = value;
                OnPropertyChanged("TransferComplete");
            }
        }

        public bool CS0
        {
            get => _cs0;
            set
            {
                _cs0 = value;
                OnPropertyChanged("CS0");
            }
        }

        public bool CS1
        {
            get => _cs1;
            set
            {
                _cs1 = value;
                OnPropertyChanged("CS1");
            }
        }

        public bool ContinuousTransfer
        {
            get => _continuousTransfer;
            set
            {
                _continuousTransfer = value;
                OnPropertyChanged("ContinuousTransfer");
            }
        }

        public bool LoadRequest
        {
            get => _loadRequest;
            set
            {
                _loadRequest = value;
                OnPropertyChanged("LoadRequest");
            }
        }

        public bool UnloadRequest
        {
            get => _unloadRequest;
            set
            {
                _unloadRequest = value;
                OnPropertyChanged("UnloadRequest");
            }
        }

        public bool ReadyToTransfer
        {
            get => _readyToTransfer;
            set
            {
                _readyToTransfer = value;
                OnPropertyChanged("ReadyToTransfer");
            }
        }

        public bool HandoffAvailable
        {
            get => _handoffAvailable;
            set
            {
                _handoffAvailable = value;
                OnPropertyChanged("HandoffAvailable");
            }
        }

        public bool EmergencyOk
        {
            get => _emergencyOk;
            set
            {
                _emergencyOk = value;
                OnPropertyChanged("EmergencyOk");
            }
        }

        public bool ErrorOnPlacementTimeout
        {
            get => _errorOnPlacementTimeout;
            set
            {
                _errorOnPlacementTimeout = value;
                OnPropertyChanged("ErrorOnPlacementTimeout");
            }
        }

        public string E84State
        {
            get => _e84State;
            set
            {
                _e84State = value;
                OnPropertyChanged("E84State");
            }
        }
        
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}