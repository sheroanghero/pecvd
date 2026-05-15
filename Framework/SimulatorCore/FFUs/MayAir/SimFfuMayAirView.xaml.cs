using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.FFUs.MayAir
{
 
    public partial class SimFfuMayAirView : UserControl
    {
        //public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
        //    "Port", typeof(string), typeof(SimHipaceTurboPumpView),
        //    new FrameworkPropertyMetadata("COM11", FrameworkPropertyMetadataOptions.AffectsRender));

        //public string Port
        //{
        //    get { return (string) this.GetValue(PortProperty); }
        //    set { this.SetValue(PortProperty, value); }
        //}

        public SimFfuMayAirView()
        {
            InitializeComponent();

            DataContext = new SimFfuMayAirViewModel("COM22");

            (DataContext as TimerViewModelBase).Start();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {

            }
        }
    }

    class SimFfuMayAirViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "MayAir FFU Simulator"; }
        }

        private SimFfuMayAir _reader;
 
        public bool IsFailed
        {
            get
            {
                return _reader.Failed;
            }
            set
            {
                _reader.Failed = value;
            }
        }
        public bool IsPumpOn
        {
            get
            {
                return _reader.IsPumpOn;
            }
            set
            {
                _reader.IsPumpOn = value;
            }
        }

        public bool IsOverTemp
        {
            get
            {
                return _reader.IsOverTemp;
            }
            set
            {
                _reader.IsOverTemp = value;
            }
        }

        public bool IsAtSpeed
        {
            get
            {
                return _reader.IsAtSpeed;
            }
            set
            {
                _reader.IsAtSpeed = value;
            }
        }

        //private string _value;

        [IgnorePropertyChange]
        public string ResultValue
        {
            get
            {
                return _reader.ResultValue;
            }
            set
            {
                _reader.ResultValue = value;
            }
        }
        
        public SimFfuMayAirViewModel(string port) : base("SimFfuMayAirViewModel")
        {
           _reader = new SimFfuMayAir(port);
            Init(_reader, false);
        }
    }
}
