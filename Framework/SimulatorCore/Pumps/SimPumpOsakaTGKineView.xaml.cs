using System;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Pumps
{
    /// <summary>
    /// SimPumpOsakaTGkineView.xaml 的交互逻辑
    /// </summary>
    public partial class SimPumpOsakaTGkineView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimPumpOsakaTGkineView),
            new FrameworkPropertyMetadata("COM101", FrameworkPropertyMetadataOptions.AffectsRender));

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

        public SimPumpOsakaTGkineView()
        {
            InitializeComponent();

            

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (DataContext == null || !(DataContext is SimSimPumpOsakaTGkineViewModel))
            {
                DataContext = new SimSimPumpOsakaTGkineViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }

                

        }
    }


    class SimSimPumpOsakaTGkineViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Osaka Pump TGkine Simulator"; }
        }

        private SimPumpOsakaTGkine _sim;
        

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


        public SimSimPumpOsakaTGkineViewModel(string port) : base("SimPumpOsakaTGkineViewModel")
        {
            _sim = new SimPumpOsakaTGkine(port);

            Init(_sim);

             

        }

 
    }
    class SimPumpOsakaTGkine : SerialPortDeviceSimulator
    {
        Random _rd = new Random();

        private object _locker = new object();

        public bool IsNoResponse { get; internal set; }

        public bool IsRemote { get; internal set; }

        public bool IsRun { get; internal set; }

        public bool IsFault { get; internal set; }

        public string TempSetPoint { get; internal set; }

        public float TempFeedback { get; internal set; }

        public SimPumpOsakaTGkine(string port)
            : base(port, -1, "", ' ', false)
        {

        }
 

        protected override void ProcessUnsplitMessage(byte[] message)
        {
            if (message.Length == 14)
            {
                var result = new byte[] { 0x02, 0x01, 0x00, 0x05, 0x71, 0x01, 0x4A, 0x87, 0x03, 0x03 };
                OnWriteMessage(result);
                return;
            }

            if (message.Length == 15)
            {
                var result = new byte[] {0x02, 0x01, 0x00, 0x03, 0x71, 0x89, 0x03, 0x03 };
                OnWriteMessage(result);
                return;
            }

        }
 

    }
}
