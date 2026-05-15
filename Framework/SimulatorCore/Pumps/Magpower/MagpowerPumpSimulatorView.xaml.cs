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

namespace MECF.Framework.Simulator.Core.Pumps.Magpower
{
    /// <summary>
    /// MagpowerPumpSimulatorView.xaml 的交互逻辑
    /// </summary>

    public partial class MagpowerPumpSimulatorView : UserControl
    {
        public MagpowerPumpSimulatorView()
        {
            InitializeComponent();
            this.DataContext = new MagpowerPumpSimulatorViewModel("COM22");
            this.Loaded += MagpowerPumpSimulatorView_Loaded;
        }

        private void MagpowerPumpSimulatorView_Loaded(object sender, RoutedEventArgs e)
        {
            (DataContext as MagpowerPumpSimulatorViewModel).EnableTimer(true);
        }
    }

    class MagpowerPumpSimulatorViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Magpower Pump Simulator"; }
        }

        private MagpowerSimulator _reader;

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

        public MagpowerPumpSimulatorViewModel(string port) : base("MagpowerPumpSimulatorViewModel")
        {
            _reader = new MagpowerSimulator(port);
            Init(_reader);
        }
    }
}
