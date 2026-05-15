using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.LoadPorts;

namespace MECF.Framework.Simulator.Core.ThrottleValves.KITZ
{
    /// <summary>
    /// OmronCIDRWView.xaml 的交互逻辑
    /// </summary>
    public partial class SimThrottleValveView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimThrottleValveView),
            new FrameworkPropertyMetadata("COM9", FrameworkPropertyMetadataOptions.AffectsRender));

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

        public SimThrottleValveView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new SimThrottleValveViewModel( Port );
                (DataContext as TimerViewModelBase).Start();
            }
         }
    }

    class SimThrottleValveViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "KITZ Throttle Valve Simulator"; }
        }

        private SimThrottleValve _reader;

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
        
        public SimThrottleValveViewModel(string port) : base("SimThrottleValveViewModel")
        {
           _reader = new SimThrottleValve(port);
            Init(_reader);
        }
    }
}
