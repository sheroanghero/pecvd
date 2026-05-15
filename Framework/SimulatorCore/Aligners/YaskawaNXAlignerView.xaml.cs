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
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using MECF.Framework.Simulator.Core.Robots;

namespace MECF.Framework.Simulator.Core.Aligners
{
    /// <summary>
    /// YaskawaAlignerView.xaml 的交互逻辑
    /// </summary>
    public partial class YaskawaNXAlignerView : UserControl
    {
        public YaskawaNXAlignerView()
        {
            InitializeComponent();

            this.DataContext = new YaskawaNXAlignerViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class YaskawaNXAlignerViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Yaskawa Aligner Simulator"; }
        }

        public YaskawaNXAlignerViewModel() : base("YaskawaNXAlignerViewModel")
        {
            Init(new YaskawaNXAlignerSimulator());

        }


    }
}

