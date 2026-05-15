using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Aligners;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    /// <summary>
    /// OmronCIDRWView.xaml 的交互逻辑
    /// </summary>
    public partial class OmronV640SerialView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(OmronBarcodeReaderView),
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

        public OmronV640SerialView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new OmronV640SerialViewModel( Port );
                (DataContext as TimerViewModelBase).Start();
            }
         }
    }

    class OmronV640SerialViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Omron V640 Serial Simulator"; }
        }

        private OmronV640SerialSimulator _reader;
 
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
        
        public OmronV640SerialViewModel(string port) : base("OmronV640SerialViewModel")
        {
            _reader = new OmronV640SerialSimulator(port);
            Init(_reader);
        }
    }
}
