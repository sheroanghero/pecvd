using Aitex.Core.UI.MVVM;
using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MECF.Framework.Simulator.Core.SMIFs.RejeSMIF
{
    /// <summary>
    /// RejeSMIFSimulatorView.xaml 的交互逻辑
    /// </summary>
    public partial class RejeSMIFSimulatorView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
"Port", typeof(string), typeof(RejeSMIFSimulatorView),
new FrameworkPropertyMetadata("COM192", FrameworkPropertyMetadataOptions.AffectsRender));

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
        public RejeSMIFSimulatorView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }
        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            if (DataContext == null)
            {
                DataContext = new RejeSMIFSimulatorViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
        }
    }

    class RejeSMIFSimulatorViewModel : SerialPortDeviceViewModel
    {
        public bool Failed { get; set; }

        public bool IsOn { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        RejeSMIF _sim;
        public RejeSMIFSimulatorViewModel(string port) : base("RejeSMIFSimulatorViewModel")
        {

            _sim = new RejeSMIF(port);

            Init(_sim);
        }
    }
}
