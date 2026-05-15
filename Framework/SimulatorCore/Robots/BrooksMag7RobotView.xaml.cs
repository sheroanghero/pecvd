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
    /// BrooksMag7RobotView.xaml 的交互逻辑
    /// </summary>
    public partial class BrooksMag7RobotView : UserControl
    {
        public BrooksMag7RobotView()
        {
            InitializeComponent();
            this.DataContext = new BrooksMag7RobotViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class BrooksMag7RobotViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Brooks Mag7 Robot Simulator"; }
        }

        private BrooksMag7RobotSimulator _robot;

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
        public string ErrorCode
        {
            get
            {
                return _robot.ErrorCode;
            }
            set
            {
                _robot.ErrorCode = value;

            }
        }
        public bool EventChecked
        {
            get
            {
                return _robot.EventChecked;
            }
            set
            {   
                _robot.EventChecked = value;
                if (value) EventCode = "_EVENT ROBOR 02610 0003 B -00028 000028 000028 -00002\r";
                else EventCode = null;
            }
        }

        [IgnorePropertyChange]
        public string EventCode
        {
            get
            {
                return _robot.EventCode;
            }
            set
            {
                _robot.EventCode = value;
            }
        }
        public BrooksMag7RobotViewModel() : base("BrooksMag7RobotViewModel")
        {
            _robot = new BrooksMag7RobotSimulator();
            Init(_robot);
 

        }
    }

}

