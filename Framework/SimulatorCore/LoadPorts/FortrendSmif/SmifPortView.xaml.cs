using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using MECF.Framework.Simulator.Core.LoadPorts.SecsSmif;
using MECF.Framework.Simulator.Core.Robots;

namespace MECF.Framework.Simulator.Core.LoadPorts
{

    /// <summary>
    /// TDKLoadPort.xaml 的交互逻辑
    /// </summary>
    public partial class SmifPortView : UserControl
    {
        public static readonly DependencyProperty PortNameProperty = DependencyProperty.Register(
            "PortName", typeof(string), typeof(SmifPortView),
            new FrameworkPropertyMetadata("COM1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string PortName
        {
            get
            {
                return (string)this.GetValue(PortNameProperty);
            }
            set
            {
                this.SetValue(PortNameProperty, value);
            }
        }

        public static readonly DependencyProperty PortNumberProperty = DependencyProperty.Register(
            "PortNumber", typeof(int), typeof(SmifPortView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public int PortNumber
        {
            get
            {
                return (int)this.GetValue(PortNumberProperty);
            }
            set
            {
                this.SetValue(PortNumberProperty, value);
            }
        }

        public SmifPortView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new SmifPortViewModel(PortName, PortNumber);
                (DataContext as TimerViewModelBase).Start();
            }

        }
    }

    class SmifPortViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Smif LoadPort Simulator"; }
        }

        public string WaferMap
        {
            get { return _sim.SlotMap; }
        }

        public string InfoPadStatus
        {
            get { return _sim.InforPadState; }

        }

        [IgnorePropertyChange]
        public string InfoPadSet { get; set; }

        public ObservableCollection<int> ErrorCode { get; set; }
        public int codeid { get; set; }

        public ObservableCollection<WaferItem> WaferList { get; set; }

        public ICommand PlaceCommand { get; set; }
        public ICommand RemoveCommand { get; set; }
        public ICommand ReportCommand { get; set; }
        public ICommand ClearCommand { get; set; }
        public ICommand SetAllCommand { get; set; }
        public ICommand RandomCommand { get; set; }

        public ICommand SetInfoPadCommand { get; set; }

        private SmifPortSimulator _sim;


        public SmifPortViewModel(string port, int index) : base("SmifPortViewModel")
        {
            PlaceCommand = new DelegateCommand<string>(Place);
            RemoveCommand = new DelegateCommand<string>(Remove);
            ReportCommand = new DelegateCommand<string>(ReportError);
            ClearCommand = new DelegateCommand<string>(Clear);
            SetAllCommand = new DelegateCommand<string>(SetAll);
            RandomCommand = new DelegateCommand<string>(RandomGenerateWafer);
            SetInfoPadCommand = new DelegateCommand<string>(SetInfoPadStatus);
            ErrorCode = new ObservableCollection<int>() { 1, 2, 38, 39, 40, 41, 43, 44, 45, 49 };
            _sim = new SmifPortSimulator(port);
            _simulator = new SerialPortDeviceSimulator($"{port}0", 0, "\r", '\r');


            WaferList = new ObservableCollection<WaferItem>()
            {
                new WaferItem {Display = "1", Index = 2, State = 3}
            };

            if (index == 1)
                _sim.SetUpWafer();
            else
            {
                _sim.SetLowWafer();
            }

        }
        public void ReportError(string obj)
        {
            _sim.ReportError(codeid);
        }
        private void SetInfoPadStatus(string obj)
        {
            _sim.InforPadState = InfoPadSet;
        }

        private void RandomGenerateWafer(string obj)
        {
            _sim.RandomWafer();
        }

        private void SetAll(string obj)
        {
            _sim.SetAllWafer();
        }

        private void Clear(string obj)
        {
            _sim.ClearWafer();
        }

        private void Remove(string obj)
        {
            _sim.RemoveCarrier();
        }

        private void Place(string obj)
        {
            _sim.PlaceCarrier();
        }
    }
}

