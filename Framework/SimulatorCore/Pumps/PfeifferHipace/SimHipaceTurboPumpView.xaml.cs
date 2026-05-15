using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
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

namespace MECF.Framework.Simulator.Core.Pumps.PfeifferHipace
{
    public partial class SimHipaceTurboPumpView : UserControl
    {
        //public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
        //    "Port", typeof(string), typeof(SimHipaceTurboPumpView),
        //    new FrameworkPropertyMetadata("COM11", FrameworkPropertyMetadataOptions.AffectsRender));

        //public string Port
        //{
        //    get { return (string) this.GetValue(PortProperty); }
        //    set { this.SetValue(PortProperty, value); }
        //}

        public SimHipaceTurboPumpView()
        {
            InitializeComponent();

            DataContext = new SimHipaceTurboPumpViewModel("COM11");
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

    class SimHipaceTurboPumpViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Sim Hipace Turbo Pump Simulator"; }
        }

        private SimHipaceTurboPump _reader;

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

        public SimHipaceTurboPumpViewModel(string port) : base("SimHipaceTurboPumpViewModel")
        {
            _reader = new SimHipaceTurboPump(port);
            Init(_reader);
        }
    }
}
