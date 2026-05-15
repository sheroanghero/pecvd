using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.RFMatchs;

namespace MECF.Framework.Simulator.Core.Pendulums
{
    /// <summary>
    /// SimVatPendulumView.xaml 的交互逻辑
    /// </summary>
    public partial class SimVatPendulumView : UserControl
    {

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimVatPendulumView),
            new FrameworkPropertyMetadata("COM1", FrameworkPropertyMetadataOptions.AffectsRender));

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

        public SimVatPendulumView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (DataContext == null || !(DataContext is SimVatPendulumViewModel))
            {
                DataContext = new SimVatPendulumViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
        }
    }


    class SimVatPendulumViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "VAT Pendulum Simulator"; }
        }

        private SimVatPendulum _sim;

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

        public SimVatPendulumViewModel(string port) : base("SimVatPendulumViewModel")
        {
            _sim = new SimVatPendulum(port);

            Init(_sim);
        }
    }
}
