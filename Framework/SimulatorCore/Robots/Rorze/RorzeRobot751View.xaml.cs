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
using MECF.Framework.Simulator.Core.Aligners;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// YaskawaSR100RobotView.xaml 的交互逻辑
    /// </summary>
    public partial class RorzeRobot751View : UserControl
    {
        public RorzeRobot751View()
        {
            InitializeComponent();
            this.DataContext = new RorzeRobot751ViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class RorzeRobot751ViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Rorze Robot 751 Simulator"; }
        }

        public RorzeRobot751ViewModel() : base("RorzeRobot751ViewModel")
        {
            Init(new RorzeRobot751Simulator());

        }
    }
 
}

