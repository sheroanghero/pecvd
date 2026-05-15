using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.Simulator.Core.Chillers
{
    /// <summary>
    /// SimChillerSmcHRZView.xaml 的交互逻辑
    /// </summary>
    public partial class SimChillerSmcHRZView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimChillerSmcHRZView),
            new FrameworkPropertyMetadata("COM2", FrameworkPropertyMetadataOptions.AffectsRender));

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

        public SimChillerSmcHRZView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            if (DataContext == null || !(DataContext is SimSimChillerSmcHRZViewModel))
            {
                DataContext = new SimSimChillerSmcHRZViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
            

        }
    }


    class SimSimChillerSmcHRZViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "SMC Chiller HRZ Simulator"; }
        }

        private SimChillerSmcHRZ _sim;

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

        public ICommand SetTempCommand { get; set; }


        public SimSimChillerSmcHRZViewModel(string port) : base("SimChillerSmcHRZViewModel")
        {
            _sim = new SimChillerSmcHRZ(port);

            Init(_sim);

            SetTempCommand = new DelegateCommand<string>(SetTemp);

        }

        private void SetTemp(string obj)
        {
            _sim.SetTemp();
        }
    }
}
