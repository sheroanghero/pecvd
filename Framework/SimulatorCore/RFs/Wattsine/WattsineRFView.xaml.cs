using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
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

namespace MECF.Framework.Simulator.Core.RFs
{


    /// <summary>
    /// WattsineRFView.xaml 的交互逻辑
    /// </summary>
    public partial class WattsineRFView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
       "Port", typeof(string), typeof(SimSerenPowerView),
       new FrameworkPropertyMetadata("COM188", FrameworkPropertyMetadataOptions.AffectsRender));

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

        public WattsineRFView()
        {

            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            //if (DataContext == null)
            {
                DataContext = new WattsineRFViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
        }
    }

    class WattsineRFViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Seren RF Power Simulator"; }
        }

        private WattsineRfPower _sim;

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

        public WattsineRFViewModel(string port) : base("WattsineRFViewModel")
        {
            _sim = new WattsineRfPower(port);

            Init(_sim);
        }
    }
}
