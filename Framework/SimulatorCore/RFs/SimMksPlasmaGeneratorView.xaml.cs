using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.RFs
{
    /// <summary>
    /// SimMksPlasmaGeneratorView.xaml 的交互逻辑
    /// </summary>
    public partial class SimMksPlasmaGeneratorView : UserControl
    {
        SimMksPlasmaGeneratorViewModel _viewModel = null;
        //public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
        //    "Port", typeof(string), typeof(SimMksPlasmaGeneratorView),
        //    new FrameworkPropertyMetadata("COM1", FrameworkPropertyMetadataOptions.AffectsRender));

        //public string Port
        //{
        //    get
        //    {
        //        return (string)this.GetValue(PortProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(PortProperty, value);
        //    }
        //}

        public void Initialize(string name, string port)
        {
            _viewModel = new SimMksPlasmaGeneratorViewModel(port);
            this.DataContext = _viewModel;

            (DataContext as TimerViewModelBase).Start();
            this.Loaded += OnViewLoaded;
        }

        public SimMksPlasmaGeneratorView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
        }
    }


    class SimMksPlasmaGeneratorViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Mks RF Plasma Generator Simulator"; }
        }

        private SimMksRfPlasmaGenerator _sim;

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



        ////private string _value;

        //[IgnorePropertyChange]
        //public string ResultValue
        //{
        //    get
        //    {
        //        return _sim.ResultValue;
        //    }
        //    set
        //    {
        //        _sim.ResultValue = value;
        //    }
        //}

        public SimMksPlasmaGeneratorViewModel(string port) : base("SimMksPlasmaGeneratorViewModel")
        {
            _sim = new SimMksRfPlasmaGenerator(port);

            Init(_sim);
        }
    }
}
