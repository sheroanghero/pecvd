using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MECF.Framework.Simulator.Core.RFs.AE
{
    /// <summary>
    /// SimAePowerView.xaml 的交互逻辑
    /// </summary>
    public partial class SimDxkdpPowerView : UserControl
    {
        SimDxkdpPowerViewModel _viewModel = null;

        public void Initialize(string name, string port)
        {
            _viewModel = new SimDxkdpPowerViewModel(port, name);
            this.DataContext = _viewModel;
            
            (DataContext as TimerViewModelBase).Start();
            this.Loaded += OnViewLoaded;
        }

        

        public SimDxkdpPowerView()
        {
            InitializeComponent();
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
        }
    }


    class SimDxkdpPowerViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Dxkdp Dc Power Simulator"; }
        }

        private string _port;
        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }

        private SimDxkdpPower _sim;

        public bool IsFailed
        {
            get
            {
                return _sim.Failed;
            }
            set
            {
                _sim.Failed = value;
            }
        }
        public bool IsHalo
        {
            get
            {
                return _sim.IsHalo;
            }
            set
            {
                _sim.IsHalo = value;
            }
        }

        public bool IsOn
        {
            get
            {
                return _sim.IsOn;
            }
            set
            {
                _sim.IsOn = value;
            }
        }

        public bool IsContinueAck
        {
            get
            {
                return _sim.IsContinueAck;
            }
            set
            {
                _sim.IsContinueAck = value;
            }
        }



        //private string _value;

        [IgnorePropertyChange]
        public string ResultValue
        {
            get
            {
                return _sim.ResultValue;
            }
            set
            {
                _sim.ResultValue = value;
            }
        }

        public SimDxkdpPowerViewModel(string port,string name) : base(name)
        {
            _sim = new SimDxkdpPower(port);

            Init(_sim);
        }
    }
}
