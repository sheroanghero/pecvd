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

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// YaskawaNX100RobotView.xaml 的交互逻辑
    /// </summary>
    public partial class YaskawaNX100RobotView : UserControl
    {
        public YaskawaNX100RobotView()
        {
            InitializeComponent();
            this.DataContext = new YaskawaNX100RobotViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class YaskawaNX100RobotViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Yaskawa NX100 Robot Simulator"; }
        }

        private YaskawaNX100RobotSimulator _robot;

        public bool IsFailed
        {
            get
            {
                return _robot.Failed;
            }
            set
            {
                _robot.Failed = value;
            }
        }

        //private string _value;

        [IgnorePropertyChange]
        public string ResultValue
        {
            get
            {
                return _robot.ResultValue;
            }
            set
            {
                _robot.ResultValue = value;
            }
        }

        public YaskawaNX100RobotViewModel() : base("YaskawaNX100RobotViewModel")
        {
            _robot = new YaskawaNX100RobotSimulator();
            Init(_robot);
 

        }
    }

}

