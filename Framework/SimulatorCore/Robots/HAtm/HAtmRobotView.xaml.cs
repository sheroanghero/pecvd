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

namespace MECF.Framework.Simulator.Core.Robots.HAtm
{
    /// <summary>
    /// HAtmRobotView.xaml 的交互逻辑
    /// </summary>
    public partial class HAtmRobotView : UserControl
    {
        public HAtmRobotView()
        {
            InitializeComponent();
            this.DataContext = new HAtmRobotViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }

        private void ErrorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var viewmodel = DataContext as HAtmRobotViewModel;
            if (viewmodel != null)
                viewmodel.SetError();
        }

        private void BusyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var viewmodel = DataContext as HAtmRobotViewModel;
            if (viewmodel != null)
                viewmodel.SetBusy();
        }

        private void WaferPresentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var viewmodel = DataContext as HAtmRobotViewModel;
            if (viewmodel != null)
                viewmodel.SetWaferPresent();
        }
    }

    class HAtmRobotViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "HAtm Robot Simulator"; }
        }

        private HAtmRobotSimulator _robot;
        public bool IsError { get; set; }
        public bool IsBusy { get; set; }
        public bool IsWaferPresent { get; set; }

        public void SetError()
        {
            _robot.IsError = IsError;
        }

        public void SetBusy()
        {
            _robot.IsBusy = IsBusy;
        }

        public void SetWaferPresent()
        {
            _robot.IsWaferPresent = IsWaferPresent;
        }

        public HAtmRobotViewModel() : base("HAtmRobotViewModel")
        {
            _robot = new HAtmRobotSimulator();
            Init(_robot);
        }
    }

}

