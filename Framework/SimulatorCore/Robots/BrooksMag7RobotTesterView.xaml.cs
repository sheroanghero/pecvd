using Aitex.Core.UI.MVVM;
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
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// BrooksMag7View.xaml 的交互逻辑
    /// </summary>
    public partial class BrooksMag7RobotTesterView : UserControl
    {
        public BrooksMag7RobotTesterView()
        {
            InitializeComponent();
            this.DataContext = new BrooksMag7ViewModel();



            this.Loaded += SxFxView_Loaded;
        }

        private void SxFxView_Loaded(object sender, RoutedEventArgs e)
        {
            (DataContext as BrooksMag7ViewModel).Start();
        }
    }


    class BrooksMag7ViewModel : TimerViewModelBase
    {
        public ICommand ClearLogCommand { get; set; }
        public ICommand EnableCommand { get; set; }
        public ICommand DisableCommand { get; set; }
        public ICommand HomeCommand { get; set; }
        public ICommand PickCommand { get; set; }
        public ICommand PlaceCommand { get; set; }


        public bool IsEnableEnable
        {
            get { return !_robot.IsEnabled; }
        }
        public bool IsEnableDisable
        {
            get { return _robot.IsEnabled; }
        }

        public ObservableCollection< TransactionLogItem> TransactionLogItems { get; set; }

        private BrooksMag7RobotSimulator _robot;



        public BrooksMag7ViewModel() : base("BrooksMag7ViewModel")
        {
            ClearLogCommand = new DelegateCommand<string>(ClearLog);
            //EnableCommand = new DelegateCommand<string>(Enable);
            //DisableCommand = new DelegateCommand<string>(Disable);


            TransactionLogItems = new ObservableCollection<TransactionLogItem>();

            _robot = new BrooksMag7RobotSimulator();

            //HomeCommand = new DelegateCommand<string>(HomeAll);
            //PickCommand = new DelegateCommand<string>(Pick);
            //PlaceCommand = new DelegateCommand<string>(Place);

            _robot.MessageOut += _robot_MessageOut;
            _robot.MessageIn += _robot_MessageIn;
        }

        //private void HomeAll(string obj)
        //{
        //    _robot.HomeAll();
        //}

        //private void Pick(string obj)
        //{
        //    _robot.Pick();
        //}

        //private void Place(string obj)
        //{
        //    _robot.Place();
        //}

        //private void Disable(string obj)
        //{
        //    _robot.Disable();
        //}

        //private void Enable(string obj)
        //{
        //    _robot.Enable();
        //}

        private void ClearLog(string obj)
        {
            TransactionLogItems.Clear();
        }

        private void _robot_MessageIn(string obj)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TransactionLogItems.Add(new TransactionLogItem() { Incoming = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

            }));
        }

        private void _robot_MessageOut(string obj)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TransactionLogItems.Add(new TransactionLogItem() { Outgoing = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

            }));
        }

        protected override void Poll()
        {
 

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
 

                InvokeAllPropertyChanged();

            }));
        }


 
    }
}
