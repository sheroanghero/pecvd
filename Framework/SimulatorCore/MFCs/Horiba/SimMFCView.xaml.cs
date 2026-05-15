using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.LoadPorts;

namespace MECF.Framework.Simulator.Core.MFCs.Horiba
{
    /// <summary>
    /// OmronCIDRWView.xaml 的交互逻辑
    /// </summary>
    public partial class SimMFCView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimMFCView),
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

        public SimMFCView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new SimMFCViewModle( Port );
                (DataContext as TimerViewModelBase).Start();
            }
         }
    }

    class SimMFCViewModle : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Horiba MFC Simulator"; }
        }

        private SimMFC _reader;

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
        
        public SimMFCViewModle(string port) : base("SimMFCViewModle")
        {
           _reader = new SimMFC(port);
            Init(_reader);
        }
    }
}
