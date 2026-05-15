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

namespace MECF.Framework.Simulator.Core.Chillers
{
    /// <summary>
    /// SimChillerSmcHRSView.xaml 的交互逻辑
    /// </summary>
    public partial class SimChillerSmcHRSView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
              "Port", typeof(string), typeof(SimChillerSmcHRSView),
              new FrameworkPropertyMetadata("COM110", FrameworkPropertyMetadataOptions.AffectsRender));

        public string Port
        {
            get
            {
                return (string)this.GetValue(PortProperty);
            }
            set
            {
                this.SetValue(PortProperty, value);
            }
        }

        public SimChillerSmcHRSView()
        {
            InitializeComponent();

            DataContext = new SimChillerSmcHRZViewModel(Port);

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            (DataContext as TimerViewModelBase).Start();

        }
    }


    class SimChillerSmcHRZViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "SMC Chiller HRS Simulator"; }
        }

        private SimChillerSmcHRS _sim;

        public bool IsNoResponse
        {
            get
            {
                return _sim.IsNoResponse;
            }
            set
            {
                _sim.IsNoResponse = value;
            }
        }
        public bool IsRemote
        {
            get
            {
                return _sim.IsRemote;
            }
            set
            {
                _sim.IsRemote = value;
            }
        }

        public bool IsRun
        {
            get
            {
                return _sim.IsRun;
            }
            set
            {
                _sim.IsRun = value;
            }
        }

        public bool IsFault
        {
            get
            {
                return _sim.IsFault;
            }
            set
            {
                _sim.IsFault = value;
            }
        }

        public float TempFeedback
        {
            get
            {
                return _sim.TempFeedback;
            }
            set
            {
                _sim.TempFeedback = value;
            }
        }

        [IgnorePropertyChange]
        public string TempSetPoint
        {
            get
            {
                return _sim.TempSetPoint;
            }
            set
            {
                _sim.TempSetPoint = value;
            }
        }

        bool _isAlarm = false;
        [IgnorePropertyChange]
        public bool IsAlarm
        {
            get
            {
                return _isAlarm;
            }
            set
            {
                _isAlarm = value;
                _sim.SetAlarm(_isAlarm);
            }
        }

        public ICommand SetTempCommand { get; set; }

        public ICommand SetAlramCommand { get; set; }


        public SimChillerSmcHRZViewModel(string port) : base("SimChillerSmcHRZViewModel")
        {
            _sim = new SimChillerSmcHRS(port);

            Init(_sim);

            SetTempCommand = new DelegateCommand<string>(SetTemp);
            SetAlramCommand = new DelegateCommand<bool>(SetAlram);

        }

        private void SetTemp(string obj)
        {
            _sim.SetTemp();

        }

        private void SetAlram(bool isAlram)
        {
            _sim.SetAlarm(isAlram);

        }
    }
}
