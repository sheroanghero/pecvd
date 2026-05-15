using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.RFs
{
    /// <summary>
    /// SimSerenPowerView.xaml 的交互逻辑
    /// </summary>
    public partial class SimSerenPowerView : UserControl
    {

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimSerenPowerView),
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

        public SimSerenPowerView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (DataContext == null || !(DataContext is SimSerenRfPowerViewModel))
            {
                DataContext = new SimSerenRfPowerViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
 
        }
    }


    class SimSerenRfPowerViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Seren RF Power Simulator"; }
        }

        private SimSerenRfPower _sim;

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

        public SimSerenRfPowerViewModel(string port) : base("SimSerenRfPowerViewModel")
        {
            _sim = new SimSerenRfPower(port);

            Init(_sim);
        }
    }
}
