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

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// TazmoRobotView.xaml 的交互逻辑
    /// </summary>
  public partial class TazmoRobotView   : UserControl
  {
        public TazmoRobotView()
        {
        InitializeComponent();
        this.DataContext = new TazmoRobotViewModel();

        this.Loaded += OnViewLoaded;
        }

       private void OnViewLoaded(object sender, RoutedEventArgs e)
       {
        (DataContext as TimerViewModelBase).Start();
       }
  }  

  class TazmoRobotViewModel : SocketDeviceViewModel
    {
    public string Title
    {
        get { return "TazmoRobot Simulator"; }
    }

    private TazmoRobotSimulator _robot;

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

    public TazmoRobotViewModel() : base("TazmoRobotViewModel")
    {
        _robot = new TazmoRobotSimulator();
        Init(_robot);


    }
  }
}

