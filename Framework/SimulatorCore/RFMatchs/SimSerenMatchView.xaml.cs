using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.RFMatchs
{
    /// <summary>
    /// SimSerenMatchView.xaml 的交互逻辑
    /// </summary>
    public partial class SimSerenMatchView : UserControl
    {

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimSerenMatchView),
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

        public SimSerenMatchView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (DataContext == null || !(DataContext is SimSerenRfMatchViewModel))
            {
                DataContext = new SimSerenRfMatchViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
 
        }
    }


    class SimSerenRfMatchViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Seren RF Match Simulator"; }
        }

        private SimSerenRfMatch _sim;

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

        public SimSerenRfMatchViewModel(string port) : base("SimSerenRfMatchViewModel")
        {
            _sim = new SimSerenRfMatch(port);

            Init(_sim);
        }
    }
}
