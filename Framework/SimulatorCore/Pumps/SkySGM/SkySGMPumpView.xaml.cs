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

namespace MECF.Framework.Simulator.Core.Pumps.SkySGM
{
    /// <summary>
    /// SkySGMPumpView.xaml 的交互逻辑
    /// </summary>
    public partial class SkySGMPumpView : UserControl
    {
        public SkySGMPumpView()
        {
            InitializeComponent();

           this.DataContext = new SkySGMPumpViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }

    }

    class SkySGMPumpViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "SkySGM Pump Simulator"; }
        }

        private SkySGMPumpSimulator _skySGMPump;


        public SkySGMPumpViewModel() : base("SkySGMPumpViewModel")
        {
            _skySGMPump = new SkySGMPumpSimulator(1104,0,"\r\n",'\r',10);
            Init(_skySGMPump);
        }
    }
}
