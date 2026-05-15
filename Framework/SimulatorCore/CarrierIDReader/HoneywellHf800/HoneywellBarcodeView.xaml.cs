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

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    /// <summary>
    /// OmronCIDRWView.xaml 的交互逻辑
    /// </summary>
    public partial class HoneywellBarcodeView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(int), typeof(HoneywellBarcodeView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public int Port 
        {
            get
            {
                return (int)this.GetValue(PortProperty);
            }
            set
            {
                this.SetValue(PortProperty, value);
            }
        }

        public HoneywellBarcodeView()
        {
            InitializeComponent();
 
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new HoneywellBarcodeViewModel( Port );
                (DataContext as TimerViewModelBase).Start();
            }
         }
    }

    class HoneywellBarcodeViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Honeywell Hf800 Simulator"; }
        }
 
        public HoneywellBarcodeViewModel(int port) : base("HoneywellBarcodeViewModel")
        {
            Init(new HoneywellBarcodeSimulator(port));
        }
    }
}
