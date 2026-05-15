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
using MECF.Framework.Simulator.Core.LoadPorts;
using MECF.Framework.Simulator.Core.LoadPorts.Hirata;
using MECF.Framework.Simulator.Core.Robots;

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    //public class WaferItem
    //{
    //    public int index { get; set; }

    //    public string Display { get; set; }

    //    public int State { get; set; } 
    //}
    /// <summary>
    /// TDKLoadPort.xaml 的交互逻辑
    /// </summary>
    public partial class HirataLoadPortView : UserControl
    {
        public static readonly DependencyProperty PortNameProperty = DependencyProperty.Register(
            "PortName", typeof(string), typeof(HirataLoadPortView),
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
            "PortNumber", typeof(int), typeof(HirataLoadPortView),
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

        public HirataLoadPortView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new HirataLoadPortViewModel(PortName, PortNumber);
                (DataContext as TimerViewModelBase).Start();
            }
            
        }
    }

    class HirataLoadPortViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Hirata LoadPort Simulator"; }
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
        
        public ObservableCollection<WaferItem> WaferList { get; set; }

        public ICommand PlaceCommand { get; set; }
        public ICommand RemoveCommand { get; set; }

        public ICommand ClearCommand { get; set; }
        public ICommand SetAllCommand { get; set; }
        public ICommand RandomCommand { get; set; }

        public ICommand SetInfoPadCommand { get; set; }

        private HirataLoadPortSimulator _sim;


        public HirataLoadPortViewModel(string port, int index) : base("HirataLoadPortViewModel")
        {
            PlaceCommand = new DelegateCommand<string>(Place);
            RemoveCommand = new DelegateCommand<string>(Remove);

            ClearCommand = new DelegateCommand<string>(Clear);
            SetAllCommand = new DelegateCommand<string>(SetAll);
            RandomCommand = new DelegateCommand<string>(RandomGenerateWafer);
            SetInfoPadCommand = new DelegateCommand<string>(SetInfoPadStatus);

            _sim = new HirataLoadPortSimulator(port);
            Init(_sim);


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

