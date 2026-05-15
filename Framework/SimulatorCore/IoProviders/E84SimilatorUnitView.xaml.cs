using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    public partial class E84SimulatorUnitView : UserControl
    {
        public static readonly DependencyProperty LoadPortNameProperty = DependencyProperty.Register(
            "LoadPortName", typeof(string), typeof(E84SimulatorUnitView), 
            null);

        public string LoadPortName
        {
            get => (string) GetValue(LoadPortNameProperty);
            set => SetValue(LoadPortNameProperty, value);
        }

        public static readonly DependencyProperty IsFloorVehicleProperty = DependencyProperty.Register(
            "IsFloorVehicle", typeof(bool), typeof(E84SimulatorUnitView), 
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsFloorVehicle
        {
            get => (bool) GetValue(IsFloorVehicleProperty);
            set => SetValue(IsFloorVehicleProperty, value);
        }

        private E84SimulatorUnitViewModel _viewModel;
        
        public ICommand E84Command { get; set; }

        public E84SimulatorUnitView()
        {
            InitializeComponent();
            E84Command = new DelegateCommand<string>(E84Operation);
            this.Loaded += OnViewLoaded;
        }

        private void E84Operation(string obj)
        {
            var x = obj.ToString();
            // var command = CommandHelper.GetCommandItem(obj);
            // var lstParameter = new List<object>
            // {
            //     Station
            // };
            //lstParameter.AddRange(obj.Parameters);
            //InvokeClient.Instance.Service.DoOperation(command.CommandName, lstParameter.ToArray());
        }

        
        
        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                _viewModel = new E84SimulatorUnitViewModel(LoadPortName, IsFloorVehicle);
                DataContext = _viewModel;
                (DataContext as TimerViewModelBase)?.Start();
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if ((string) button.Content == "Pick From Lp")
                {
                    _viewModel._simulatorE84.Stage = E84Stage.TD0;
                    //_viewModel._simulatorE84.LReq;
                }
                else
                {
                    _viewModel._simulatorE84.Stage = E84Stage.TD0;
                }
            }
            
        }
    }

    class E84SimulatorUnitViewModel: TimerViewModelBase
    {
        public readonly SimulatorE84 _simulatorE84;

        public bool IsFloorMode => _simulatorE84.IsFloor;
        public bool IsLoading => _simulatorE84.IsLoading;
        public bool IsUnloading => _simulatorE84.IsUnLoading;

        public bool LReq => _simulatorE84.LReq;
        public bool UReq => _simulatorE84.UReq;
        public bool Ready => _simulatorE84.Ready;
        public bool HoAvbl => _simulatorE84.HoAvbl;
        public bool ES => _simulatorE84.ES;
        public bool VA => _simulatorE84.VA;
        public bool VS0 => _simulatorE84.VS0;
        public bool VS1 => _simulatorE84.VS1;
        
        public bool ON => _simulatorE84.ON;
        public bool VALID => _simulatorE84.VALID;
        public bool CS_0 => _simulatorE84.CS_0;
        public bool TR_REQ => _simulatorE84.TR_REQ;
        public bool BUSY => _simulatorE84.BUSY;
        public bool COMPT => _simulatorE84.COMPT;
        public bool CONT => _simulatorE84.CONT;
        public bool AM_AVBL => _simulatorE84.AM_AVBL;
        public E84SimulatorUnitViewModel(string loadPortName, bool isFloorVehicle) : base("E84SimulatorUnitViewModel")
        {
            _simulatorE84 = new SimulatorE84(loadPortName, isFloorVehicle);
        }

        protected override void Poll()
        {
            InvokePropertyChanged();
        }
    }
}

